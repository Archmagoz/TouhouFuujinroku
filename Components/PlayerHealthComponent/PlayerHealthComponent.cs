using Godot;

namespace TouhouFuujinroku.Components
{
    [GlobalClass]
    public partial class PlayerHealthComponent : HealthComponent
    {
        #region Signals

        [Signal] public delegate void LivesChangedEventHandler(int current, int max);
        [Signal] public delegate void RespawnedEventHandler();
        [Signal] public delegate void GameOverEventHandler();

        #endregion // Signals

        #region Fields

        [Export] private float _respawnDelay = 1.5f; // Seconds the entity stays dead before reviving.
        [Export] private int _maxLives = 5;

        // Fixed per-life health — the player is always one-shot per life, regardless
        // of whatever value is set on the exported _maxHealth field in the editor.
        private const int PlayerMaxHealth = 1;

        // Runtime state — managed internally by the component.
        private int _currentLives;

        // Public read-only properties for external access to lives state.
        public int MaxLives => _maxLives;
        public int CurrentLives => _currentLives;

        #endregion // Fields

        #region Godot overrides

        public override void _Ready()
        {
            // Overrides the inherited _maxHealth before HealthComponent._Ready() consumes it
            // to initialize _currentHealth — enforces the one-hit-per-life rule unconditionally.
            _maxHealth = PlayerMaxHealth;

            base._Ready();

            _currentLives = _maxLives;
        }

        #endregion // Godot overrides

        #region Event handlers

        protected override async void OnDeath()
        {
            // Extends the base death hook — base.OnDeath() emits Death first, so listeners
            // (e.g. the death animation) react before the lives are decremented.
            // Keeps this component purely reactive — no game logic here.
            base.OnDeath();

            _currentLives--;
            EmitSignal(SignalName.LivesChanged, _currentLives, _maxLives);

            if (_currentLives <= 0)
            {
                EmitSignal(SignalName.GameOver);
                return;
            }

            // Keeps the entity in a dead state (IsDead == true) for the full delay,
            // giving the death animation time to play and control time to stay locked out.
            await ToSignal(GetTree().CreateTimer(_respawnDelay), SceneTreeTimer.SignalName.Timeout);

            Reset();
            EmitSignal(SignalName.Respawned);
        }

        #endregion // Event handlers
    }
}