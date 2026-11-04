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
        /// <summary>
        /// Emitted whenever the number of remaining lives actually changes.
        /// Not emitted on initialization, so listeners should read <see cref="CurrentLives"/> once when connecting.
        /// </summary>
        /// <param name="current">Remaining lives after the change.</param>
        [Signal] public delegate void LivesChangedEventHandler(int current);

        /// <summary>
        /// Emitted after the respawn delay, once health has been fully restored.
        /// </summary>
        [Signal] public delegate void RespawnedEventHandler();

        /// <summary>
        /// Emitted when the last life is lost. No respawn follows and the entity stays dead
        /// until <see cref="Restart"/> is called.
        /// </summary>
        [Signal] public delegate void GameOverEventHandler();

        /// <summary>
        /// Seconds the entity stays dead before reviving, assigned via the editor.
        /// Negative values are treated as zero.
        /// </summary>
        [Export] public float RespawnDelay { get; private set; } = 1.5f;

        /// <summary>
        /// Number of lives the player starts with, assigned via the editor.
        /// Also the baseline restored by <see cref="Restart"/>. Clamped to at least 1 on ready.
        /// </summary>
        [Export] public int MaxLives { get; private set; } = 5;

        /// <summary>
        /// Runtime lives state, managed exclusively by this component.
        /// Includes the life currently in play.
        /// </summary>
        public int CurrentLives { get; private set; }

        /// <summary>
        /// Identifies the latest death or restart. A pending respawn only proceeds if its
        /// token is still current, so a stale timer can never revive the entity early.
        /// </summary>
        private int _respawnToken;

        public override void _Ready()
        {
            base._Ready();

            // Clamp the configured values to valid ranges and initialize the runtime state.
            MaxLives = Mathf.Max(1, MaxLives);
            RespawnDelay = Mathf.Max(0f, RespawnDelay);

            // Initialize directly (not through UpdateLives) so no signal is emitted during setup.
            CurrentLives = MaxLives;
        }

        /// <summary>
        /// Starts over with all lives and full health. Meant for retrying after <see cref="GameOver"/>
        /// or starting a new run. <see cref="HealthComponent.Reset"/> alone only revives and leaves the lives untouched.
        /// Also cancels any pending respawn.
        /// </summary>
        public void Restart()
        {
            // Invalidate any respawn still waiting on its timer.
            _respawnToken++;

            UpdateLives(MaxLives);
            Reset();
        }

        /// <summary>
        /// Single write point for the lives state. Clamps the value and emits
        /// <see cref="LivesChanged"/> only when it actually changed.
        /// </summary>
        /// <param name="value">Requested lives value, clamped to 0 and <see cref="MaxLives"/>.</param>
        private void UpdateLives(int value)
        {
            var oldLives = CurrentLives;

            CurrentLives = Mathf.Clamp(value, 0, MaxLives);

            if (CurrentLives != oldLives)
                EmitSignal(SignalName.LivesChanged, CurrentLives);
        }

        /// <summary>
        /// Extends the base death hook with the lives and respawn flow.
        /// <para>
        /// <c>base.OnDeath()</c> runs first so <see cref="HealthComponent.Death"/> is emitted before
        /// the lives are decremented, letting listeners (e.g. the death animation) react to the death itself.
        /// Without lives left, <see cref="GameOver"/> is emitted and the entity stays dead.
        /// Otherwise the entity stays dead for <see cref="RespawnDelay"/> seconds, then is restored
        /// and <see cref="Respawned"/> is emitted.
        /// </para>
        /// </summary>
        protected override async void OnDeath()
        {
            base.OnDeath();

            var token = ++_respawnToken;

            UpdateLives(CurrentLives - 1);

            if (CurrentLives <= 0)
            {
                EmitSignal(SignalName.GameOver);
                return;
            }

            // IsDead stays true for the whole delay, giving the death animation time to play
            // and keeping the entity locked out of damage and control until it revives.
            await ToSignal(GetTree().CreateTimer(RespawnDelay), SceneTreeTimer.SignalName.Timeout);

            // Abort if the node was freed (e.g. scene change), the respawn was superseded
            // by a newer death or a restart, or the entity was revived externally while waiting.
            if (!IsInstanceValid(this) || token != _respawnToken || !IsDead)
                return;

            Reset();
            EmitSignal(SignalName.Respawned);
        }
    }
}