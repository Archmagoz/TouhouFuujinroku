using Godot;

using SystemDict = System.Collections.Generic.Dictionary<Godot.PackedScene, int>;

using TouhouFuujinroku.Entities.Enemies.Spawning;
using TouhouFuujinroku.Entities.PlayableCharacters.Reimu;
using TouhouFuujinroku.Entities.Projectile;
using TouhouFuujinroku.Global.Controllers;
using TouhouFuujinroku.UI.Hud;

namespace TouhouFuujinroku.Levels
{
    /// <summary>
    /// Base class for levels. Provides the wiring every level shares: projectile pool prewarm, the wave script
    /// lifecycle, the bridge between the player's lives and the HUD, and the exit flow.
    /// <para>
    /// A concrete level only decides WHICH wave script plays (<see cref="CreateWaveScript"/>) and, optionally,
    /// what happens when the level ends (<see cref="LeaveLevel"/>). Timing, paths, and spawning stay entirely
    /// the <see cref="WaveDirector"/>'s responsibility.
    /// </para>
    /// <para>
    /// The level is the only place that knows both the <see cref="Reimu"/> and the <see cref="Hud"/>, so neither of
    /// them has to know about the other. Both are optional: a level without a HUD or without a player simply skips
    /// the lives bridge.
    /// </para>
    /// <para>
    /// Signals connected through the editor must target the handlers of this class:
    /// <c>Reimu.LivesChanged</c> to <see cref="OnPlayerLivesChanged"/> and <c>Reimu.GameOver</c> to
    /// <see cref="OnGameOver"/>. Levels that override <c>_Ready</c> must call <c>base._Ready()</c>.
    /// </para>
    /// </summary>
    public abstract partial class Level : Node2D
    {
        #region Fields

        /// <summary>
        /// Plays the level's enemy script. The level only chooses WHICH script runs.
        /// </summary>
        [ExportGroup("Components")]
        [Export] private WaveDirector _waveDirector;

        /// <summary>
        /// Playable character. Optional. Queried once for the initial lives, then observed through its
        /// <c>LivesChanged</c> signal (connected in the editor).
        /// </summary>
        [Export] private Reimu _player;

        /// <summary>
        /// Single entry point for everything displayed on the HUD. Optional. The level never reaches into its widgets.
        /// </summary>
        [Export] private Hud _hud;

        /// <summary>Player bullet prefab, prewarmed in the projectile pool.</summary>
        [ExportGroup("Projectiles")]
        [Export] private PackedScene _reimuBulletPrefab;

        /// <summary>Common enemy bullet prefab, prewarmed in the projectile pool.</summary>
        [Export] private PackedScene _greenDotBulletPrefab;

        /// <summary>Inactive player bullets created up front. The player fires fast, so this is usually high.</summary>
        [Export] private int _reimuBulletPoolSize = 128;

        /// <summary>Inactive common enemy bullets created up front. Raise it for levels that put many enemies on screen at once.</summary>
        [Export] private int _greenDotBulletPoolSize = 128;

        /// <summary>
        /// Seconds to wait after the last enemy spawns before the level ends. The script ends when the last enemy
        /// appears, not when it dies, so this gives the player time to clear the playfield.
        /// </summary>
        [ExportGroup("Flow")]
        [Export] private float _clearDelay = 6f;

        #endregion // Fields

        #region Godot overrides

        public override void _Ready()
        {
            PrewarmProjectilePool();
            ReportInitialLives();

            _waveDirector.ScriptFinished += OnScriptFinished;
            _waveDirector.Play(CreateWaveScript());
        }

        #endregion // Godot overrides

        #region Extension points

        /// <summary>
        /// Creates the enemy script this level plays. Called once, at the end of <c>_Ready</c>.
        /// </summary>
        /// <returns>The wave script to hand to the <see cref="WaveDirector"/>.</returns>
        protected abstract WaveScript CreateWaveScript();

        /// <summary>
        /// Ends the level, both when the wave script finishes and after the clear delay, and when the player loses
        /// the last life. Returns to the main menu by default. Override it to show a level clear screen, load the
        /// next level, or, in sandbox levels, to stay where you are.
        /// </summary>
        protected virtual void LeaveLevel()
        {
            SceneController.Instance.TransitionTo(Scene.MainMenu);
        }

        #endregion // Extension points

        #region Signal handlers

        /// <summary>
        /// Mirrors the player's remaining lives on the HUD.
        /// Connected in the editor to <c>Reimu.LivesChanged</c>, which is emitted after the new value is in effect.
        /// </summary>
        /// <param name="current">Remaining lives after the change.</param>
        /// <param name="max">Maximum number of lives.</param>
        private void OnPlayerLivesChanged(int current, int max)
        {
            if (_hud == null) return;

            _hud.ReportLives(current);
        }

        /// <summary>
        /// Handles the player losing the last life by leaving the level.
        /// Connected in the editor to <c>Reimu.GameOver</c>. It is an instance method on purpose:
        /// editor-connected handlers must not be static.
        /// </summary>
        private void OnGameOver()
        {
            LeaveLevel();
        }

        /// <summary>
        /// Handles the end of the wave script: waits for the playfield to clear, then leaves the level.
        /// </summary>
        private async void OnScriptFinished()
        {
            await ToSignal(GetTree().CreateTimer(_clearDelay), SceneTreeTimer.SignalName.Timeout);

            // The level may have been left in the meantime (e.g. by a game over transition).
            if (!IsInsideTree()) return;

            LeaveLevel();
        }

        #endregion // Signal handlers

        #region Helpers

        /// <summary>
        /// Populates the projectile pool with inactive instances of each prefab, so the first shots
        /// do not allocate during gameplay.
        /// </summary>
        private void PrewarmProjectilePool()
        {
            var prewarmConfig = new SystemDict
            {
                { _reimuBulletPrefab, _reimuBulletPoolSize },
                { _greenDotBulletPrefab, _greenDotBulletPoolSize }
            };

            ProjectilePool.Instance.Initialize(prewarmConfig);
        }

        /// <summary>
        /// Pushes the starting lives to the HUD. The player does not emit <c>LivesChanged</c> on initialization,
        /// so the starting state has to be sent manually. Does nothing if the level has no player or no HUD.
        /// </summary>
        private void ReportInitialLives()
        {
            if (_player == null || _hud == null) return;

            _hud.ReportLives(_player.Lives);
        }

        #endregion // Helpers
    }
}