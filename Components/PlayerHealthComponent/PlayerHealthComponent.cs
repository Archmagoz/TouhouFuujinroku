using Godot;

namespace TouhouFuujinroku.Components
{
    [GlobalClass]
    public partial class PlayerHealthComponent : HealthComponent
    {
        // Signal Handlers.
        [Signal] public delegate void PlayerDiedEventHandler();
        [Signal] public delegate void LivesChangedEventHandler(int current, int max);
        [Signal] public delegate void RespawnedEventHandler();
        [Signal] public delegate void GameOverEventHandler();

        // Base values (assigned via editor).
        [Export] private int _maxLives = 5;
        [Export] private float _respawnDelay = 1.5f; // Seconds the entity stays dead before reviving.

        // Fixed per-life health — the player is always one-shot per life, regardless
        // of whatever value is set on the exported _maxHealth field in the editor.
        private const int PlayerMaxHealth = 1;

        // Runtime state — managed internally by the component.
        private int _currentLives;

        // Public read-only properties for external access to lives state.
        public int MaxLives => _maxLives;
        public int CurrentLives => _currentLives;

        // Godot overrides --------------------------------------------------------------------------------------

        public override void _Ready()
        {
            // Overrides the inherited _maxHealth before HealthComponent._Ready() consumes it
            // to initialize _currentHealth — enforces the one-hit-per-life rule unconditionally.
            _maxHealth = PlayerMaxHealth;

            base._Ready();

            _currentLives = _maxLives;

            Death += OnDeath;
        }

        // Signal callbacks ---------------------------------------------------------------------------------------

        private async void OnDeath()
        {
            // Forwards the base death event as a player-specific signal.
            // Keeps this component purely reactive — no game logic here.
            EmitSignal(SignalName.PlayerDied);

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
    }
}