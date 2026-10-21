using Godot;

using SystemDict = System.Collections.Generic.Dictionary<Godot.PackedScene, int>;

using TouhouFuujinroku.Entities.Projectile;
using TouhouFuujinroku.Entities.Enemies.Spawning;
using TouhouFuujinroku.Global.Controllers;

namespace TouhouFuujinroku.Levels.Level01
{
    /// <summary>
    /// Level 1 controller. Owns only level-specific wiring: projectile pool prewarm, which wave script
    /// plays, and what happens when the script ends.
    /// <para>
    /// Timing, paths, and spawning are entirely the <see cref="WaveDirector"/>'s responsibility.
    /// </para>
    /// </summary>
    public partial class Level01 : Node2D
    {
        #region Fields

        /// <summary>
        /// Plays the level's enemy script. The level only chooses WHICH script runs.
        /// </summary>
        [ExportGroup("Components")]
        [Export] private WaveDirector _waveDirector;

        /// <summary>Player bullet prefab, prewarmed in the projectile pool.</summary>
        [ExportGroup("Projectiles")]
        [Export] private PackedScene _reimuBulletPrefab;

        /// <summary>Common enemy bullet prefab, prewarmed in the projectile pool.</summary>
        [Export] private PackedScene _greenDotBulletPrefab;

        /// <summary>
        /// Seconds to wait after the last enemy spawns before leaving the level. The script ends when the last enemy
        /// appears, not when it dies, so this gives the player time to clear the playfield.
        /// </summary>
        [ExportGroup("Flow")]
        [Export] private float _clearDelay = 6f;

        #endregion // Fields

        #region Godot overrides

        public override void _Ready()
        {
            // Optional projectile prewarm — populates the pool with inactive instances of each prefab if needed.
            var prewarmConfig = new SystemDict
            {
                { _reimuBulletPrefab, 128 },   // Player fires fast
				{ _greenDotBulletPrefab, 128 } // Later waves put many enemies on screen at once
			};

            ProjectilePool.Instance.Initialize(prewarmConfig);

            _waveDirector.ScriptFinished += OnScriptFinished;
            _waveDirector.Play(new Level01WaveScript());
        }

        #endregion // Godot overrides

        #region Event handlers

        /// <summary>
        /// Handles the end of the wave script: waits for the playfield to clear, then leaves the level.
        /// Currently returns to the main menu; replace with a level clear screen or the next level.
        /// </summary>
        private async void OnScriptFinished()
        {
            await ToSignal(GetTree().CreateTimer(_clearDelay), SceneTreeTimer.SignalName.Timeout);

            // The level may have been left in the meantime (e.g. by a game over transition).
            if (!IsInsideTree()) return;

            SceneController.Instance.TransitionTo(Scene.MainMenu);
        }

        #endregion // Event handlers
    }
}