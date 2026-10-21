using System;
using System.Collections.Generic;
using Godot;

using GodotPrefabDict = Godot.Collections.Dictionary<string, Godot.PackedScene>;

using TouhouFuujinroku.Interfaces;
using TouhouFuujinroku.UI.Hud;
using TouhouFuujinroku.Entities.Enemies.GenericEnemies;

namespace TouhouFuujinroku.Entities.Enemies.Spawning
{
    /// <summary>
    /// Plays a <see cref="WaveScript"/> against the physics clock, spawning enemy groups at their scheduled times.
    /// <para>
    /// The clock advances exclusively in <c>_PhysicsProcess</c>, so the timeline is frame-rate independent,
    /// deterministic, and pauses automatically with the scene tree.
    /// </para>
    /// <para>
    /// Fully decoupled from the level: the level only places this node, wires the exported references,
    /// and calls <see cref="Play"/> — it knows nothing about timing, paths, or score wiring.
    /// </para>
    /// </summary>
    public partial class WaveDirector : Node
    {
        #region Types

        /// <summary>
        /// Runtime state of a group that has started but has not yet spawned all of its enemies.
        /// </summary>
        private sealed class ActiveGroup
        {
            /// <summary>Scripted event this group is playing.</summary>
            public WaveEvent Event;

            /// <summary>Number of enemies already spawned.</summary>
            public int Spawned;

            /// <summary>
            /// Absolute script time at which the next enemy is due.
            /// Computed from the event start instead of accumulated, so float error never drifts.
            /// </summary>
            public float NextDueTime => Event.Time + Spawned * Event.Interval;
        }

        #endregion // Types

        #region Fields

        /// <summary>
        /// Reports kills through the Hud's public API — the director has no knowledge
        /// of Score or any other internal HUD widget.
        /// </summary>
        [ExportGroup("Dependencies")]
        [Export] private Hud _hud;

        /// <summary>
        /// Maps <see cref="EnemyIds"/> keys to prefabs. Every id used by a script must be registered here.
        /// </summary>
        [ExportGroup("Spawning")]
        [Export] private GodotPrefabDict _enemyPrefabs = [];

        /// <summary>
        /// Rectangle, in level-local coordinates, that normalized path points are mapped onto.
        /// Adjust it to match the actual gameplay area.
        /// </summary>
        [Export] private Rect2 _playfield = new(-254f, -212f, 312f, 424f);

        /// <summary>
        /// Raised once the last enemy of the script has been spawned (not when the last one dies).
        /// </summary>
        public event Action ScriptFinished;

        /// <summary>Current script timeline, sorted by time.</summary>
        private IReadOnlyList<WaveEvent> _events = [];

        /// <summary>Cursor of the first event that has not started yet.</summary>
        private int _nextEventIndex;

        /// <summary>Groups that have started but still owe enemies.</summary>
        private readonly List<ActiveGroup> _activeGroups = [];

        /// <summary>Seconds elapsed since <see cref="Play"/>, driven by physics delta.</summary>
        private float _elapsed;

        #endregion // Fields

        #region Godot overrides

        public override void _Ready()
        {
            // Idle until a script is started.
            SetPhysicsProcess(false);
        }

        public override void _PhysicsProcess(double delta)
        {
            _elapsed += (float)delta;

            StartDueEvents();
            SpawnDueEnemies();
            CheckCompletion();
        }

        #endregion // Godot overrides

        #region Public API

        /// <summary>
        /// Starts (or restarts) the timeline with the given script.
        /// Already spawned enemies are left untouched; only pending spawns are discarded.
        /// </summary>
        /// <param name="script">Script to play.</param>
        public void Play(WaveScript script)
        {
            _events = script.Build();
            _nextEventIndex = 0;
            _elapsed = 0f;
            _activeGroups.Clear();

            SetPhysicsProcess(true);
        }

        /// <summary>
        /// Aborts the current script. Pending spawns are discarded; living enemies are untouched.
        /// </summary>
        public void Stop()
        {
            _activeGroups.Clear();
            _nextEventIndex = _events.Count;
            SetPhysicsProcess(false);
        }

        /// <summary>
        /// Cancels the active spawn groups currently spawning enemies, discarding their remaining unspawned units.
        /// Future upcoming waves remain scheduled and will spawn at their original script times without delay or loss.
        /// </summary>
        public void CancelCurrentWave()
        {
            _activeGroups.Clear();
        }

        /// <summary>
        /// Frees every enemy currently alive, without awarding score. Pending spawns are untouched, so the
        /// script timeline keeps running. Each enemy's Path2D is freed together with it.
        /// </summary>
        public void ClearEnemies()
        {
            foreach (var child in GetChildren())
                if (child is GenericEnemy enemy)
                    enemy.QueueFree();
        }

        #endregion // Public API

        #region Helpers

        /// <summary>
        /// Moves every event whose start time has been reached into the active list.
        /// A while loop (instead of an if) guarantees events are never skipped when a single
        /// physics tick covers more than one event, e.g. after a lag spike.
        /// </summary>
        private void StartDueEvents()
        {
            while (_nextEventIndex < _events.Count && _events[_nextEventIndex].Time <= _elapsed)
            {
                _activeGroups.Add(new ActiveGroup { Event = _events[_nextEventIndex] });
                _nextEventIndex++;
            }
        }

        /// <summary>
        /// Spawns every enemy whose due time has been reached and drops finished groups.
        /// Iterates backwards so groups can be removed in place without index shifting.
        /// </summary>
        private void SpawnDueEnemies()
        {
            for (int i = _activeGroups.Count - 1; i >= 0; i--)
            {
                var group = _activeGroups[i];

                // Catch-up loop: a long tick may owe more than one enemy to the same group.
                while (group.Spawned < group.Event.Count && _elapsed >= group.NextDueTime)
                {
                    SpawnEnemy(group.Event);
                    group.Spawned++;
                }

                if (group.Spawned >= group.Event.Count)
                    _activeGroups.RemoveAt(i);
            }
        }

        /// <summary>
        /// Stops the clock and notifies listeners once nothing is left to spawn.
        /// </summary>
        private void CheckCompletion()
        {
            if (_nextEventIndex < _events.Count || _activeGroups.Count > 0) return;

            SetPhysicsProcess(false);
            ScriptFinished?.Invoke();
        }

        /// <summary>
        /// Builds a dedicated trajectory for the enemy, instantiates it, and wires its score event.
        /// Each enemy owns its own Path2D and PathFollow2D pair, so no progress state is ever shared.
        /// The pair is freed together with the enemy.
        /// </summary>
        /// <param name="spawnEvent">Event describing the enemy to spawn.</param>
        private void SpawnEnemy(WaveEvent spawnEvent)
        {
            if (!_enemyPrefabs.TryGetValue(spawnEvent.EnemyId, out var prefab) || prefab == null)
            {
                GD.PushWarning($"WaveDirector: no prefab registered for enemy id '{spawnEvent.EnemyId}'.");
                return;
            }

            var path = new Path2D { Curve = BuildCurve(spawnEvent.Path) };

            // Loop is disabled so the enemy stops at the end of the trajectory instead of wrapping around.
            var follow = new PathFollow2D { Loop = false };
            path.AddChild(follow);
            AddChild(path);

            var enemy = prefab.Instantiate<GenericEnemy>();
            AddChild(enemy);
            enemy.SetPath(follow);

            // The validity check guards against scene teardown, where the Path2D may already be gone.
            enemy.TreeExited += () =>
            {
                if (GodotObject.IsInstanceValid(path))
                    path.QueueFree();
            };

            if (_hud != null && enemy is IScoreable scoreable)
                scoreable.Died += _hud.ReportScore;
        }

        /// <summary>
        /// Converts a <see cref="PathPattern"/> into a <see cref="Curve2D"/> using the configured playfield.
        /// </summary>
        /// <param name="pattern">Pattern to resolve.</param>
        /// <returns>A curve passing through every resolved point, in order.</returns>
        private Curve2D BuildCurve(PathPattern pattern)
        {
            var curve = new Curve2D();

            foreach (var point in pattern.GetPoints(_playfield))
                curve.AddPoint(point);

            return curve;
        }

        #endregion // Helpers
    }
}