using Godot;

namespace TouhouFuujinroku.Components
{
    /// <summary>
    /// Health component specialized for the player: one hit per life, a limited number of lives,
    /// and automatic respawn after a delay.
    /// <para>
    /// The component only tracks state and raises signals. Presentation (death animation, input lockout,
    /// game over screen) belongs to the listeners, which keeps this class free of game-flow logic.
    /// </para>
    /// </summary>
    [GlobalClass]
    public partial class PlayerHealthComponent : HealthComponent
    {
        #region Signals

        /// <summary>
        /// Emitted whenever the number of remaining lives changes.
        /// Not emitted on initialization, so listeners should read <see cref="CurrentLives"/> once when connecting.
        /// </summary>
        /// <param name="current">Remaining lives after the change.</param>
        /// <param name="max">Maximum number of lives.</param>
        [Signal] public delegate void LivesChangedEventHandler(int current, int max);

        /// <summary>
        /// Emitted after the respawn delay, once health has been fully restored.
        /// </summary>
        [Signal] public delegate void RespawnedEventHandler();

        /// <summary>
        /// Emitted when the last life is lost. No respawn follows and the entity stays dead.
        /// </summary>
        [Signal] public delegate void GameOverEventHandler();

        #endregion // Signals

        #region Fields

        /// <summary>
        /// Seconds the entity stays dead before reviving.
        /// </summary>
        [Export] private float _respawnDelay = 1.5f;

        /// <summary>
        /// Number of lives the player starts with.
        /// </summary>
        [Export] private int _maxLives = 5;

        /// <summary>
        /// Fixed per-life health. The player is always one-shot per life,
        /// regardless of the value set on the inherited <c>_maxHealth</c> field in the editor.
        /// </summary>
        private const int PlayerMaxHealth = 1;

        /// <summary>
        /// Runtime lives state, managed exclusively by this component.
        /// </summary>
        private int _currentLives;

        /// <summary>Maximum number of lives.</summary>
        public int MaxLives => _maxLives;

        /// <summary>Remaining lives, including the one currently in play.</summary>
        public int CurrentLives => _currentLives;

        #endregion // Fields

        #region Godot overrides

        public override void _Ready()
        {
            // Must run before base._Ready(), which consumes _maxHealth to initialize the
            // current health — this enforces the one-hit-per-life rule unconditionally.
            _maxHealth = PlayerMaxHealth;

            base._Ready();

            _currentLives = _maxLives;
        }

        #endregion // Godot overrides

        #region Event handlers

        /// <summary>
        /// Extends the base death hook with the lives and respawn flow.
        /// <para>
        /// <c>base.OnDeath()</c> runs first so <see cref="HealthComponent.Death"/> is emitted before
        /// the lives are decremented, letting listeners (e.g. the death animation) react to the death itself.
        /// Without lives left, <see cref="GameOver"/> is emitted and the entity stays dead.
        /// Otherwise the entity stays dead for <c>_respawnDelay</c> seconds, then is restored
        /// and <see cref="Respawned"/> is emitted.
        /// </para>
        /// </summary>
        protected override async void OnDeath()
        {
            base.OnDeath();

            _currentLives--;
            EmitSignal(SignalName.LivesChanged, _currentLives, _maxLives);

            if (_currentLives <= 0)
            {
                EmitSignal(SignalName.GameOver);
                return;
            }

            // IsDead stays true for the whole delay, giving the death animation time to play
            // and keeping the entity locked out of damage and control until it revives.
            await ToSignal(GetTree().CreateTimer(_respawnDelay), SceneTreeTimer.SignalName.Timeout);

            Reset();
            EmitSignal(SignalName.Respawned);
        }

        #endregion // Event handlers
    }
}