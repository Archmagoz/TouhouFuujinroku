using Godot;
using Godot.Collections;

using TouhouFuujinroku.Interfaces;
using TouhouFuujinroku.UI.Hud;
using TouhouFuujinroku.Entities.Enemies.GenericEnemies;

namespace TouhouFuujinroku.Entities.Enemies.Spawning
{
    // Owns enemy spawn timing, path assignment, and score-reporting wiring.
    // Fully decoupled from the level: the level only places this node in the scene
    // and wires the exported references — it knows nothing about spawn logic.
    public partial class EnemySpawner : Node
    {
        [ExportGroup("Dependencies")]
        // Reports kills through the Hud's public API — the spawner has no knowledge
        // of Score or any other internal HUD widget.
        [Export] private Hud _hud;

        [ExportGroup("Spawning")]
        // Enemy prefab to instantiate on each spawn tick.
        [Export] private PackedScene _enemyPrefab;

        // Source paths used as templates — one PathFollow2D per Path2D in the scene.
        // Each enemy receives a duplicate so instances never share progress state.
        [Export] private Array<PathFollow2D> _spawnPaths = [];

        // Seconds between spawns — ramps from start to end over _rampDuration seconds.
        [Export] private float _spawnIntervalStart = 2f;
        [Export] private float _spawnIntervalEnd = 0.4f;

        // Total duration over which the interval ramps from start to end, in seconds.
        [Export] private float _rampDuration = 30f;

        // Jitter factor applied to each spawn interval — prevents enemies from spawning
        // in sync, which would cause audio clipping when salvos overlap.
        // Value of 0.2 = ±20% of the base interval.
        [Export] private float _spawnJitter = 0.2f;

        // Elapsed spawner time — drives both the ramp calculation and the spawn timer.
        private float _elapsed;
        private float _spawnCooldown;

        // Godot overrides --------------------------------------------------------------------------------------

        public override void _Ready()
        {
            _spawnCooldown = _spawnIntervalStart;
        }

        public override void _Process(double delta)
        {
            HandleSpawning(delta);
        }

        // Helpers ----------------------------------------------------------------------------------------------

        // Decrements the spawn timer and fires an enemy when it expires.
        // The interval shrinks linearly from _spawnIntervalStart to _spawnIntervalEnd
        // over _rampDuration seconds, then holds at _spawnIntervalEnd.
        private void HandleSpawning(double delta)
        {
            if (_enemyPrefab == null || _spawnPaths.Count == 0) return;

            _elapsed += (float)delta;
            _spawnCooldown -= (float)delta;

            if (_spawnCooldown > 0) return;

            SpawnEnemy();

            // Apply jitter to desynchronize spawns — prevents salvos from overlapping
            // and audio from clipping when multiple enemies fire simultaneously.
            float t = Mathf.Clamp(_elapsed / _rampDuration, 0f, 1f);
            float baseInterval = Mathf.Lerp(_spawnIntervalStart, _spawnIntervalEnd, t);
            float jitter = baseInterval * _spawnJitter;
            _spawnCooldown = baseInterval + (float)GD.RandRange(-jitter, jitter);
        }

        // Picks a random source path, duplicates its PathFollow2D, and assigns it
        // exclusively to the new enemy — each instance owns its own progress state.
        // Also wires the enemy's IScoreable.Died event directly to Hud.ReportScore —
        // no tree-wide polling, no global event bus, just a direct subscription made
        // exactly once, at the cheapest possible moment: right after instancing.
        private void SpawnEnemy()
        {
            var sourcePath = _spawnPaths[GD.RandRange(0, _spawnPaths.Count - 1)];
            var pathCopy = sourcePath.Duplicate() as PathFollow2D;

            // PathFollow2D must be a child of a Path2D to read the curve correctly.
            sourcePath.GetParent().AddChild(pathCopy);

            var enemy = _enemyPrefab.Instantiate<GenericEnemy>();
            AddChild(enemy);
            enemy.SetPath(pathCopy);

            if (_hud != null && enemy is IScoreable scoreable)
                scoreable.Died += _hud.ReportScore;
        }
    }
}