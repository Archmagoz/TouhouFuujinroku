using Godot;

namespace TohouFuuujinoku.Components
{
	[GlobalClass]
	public partial class HealthComponent : Node
	{
		// Signal Handlers.
		[Signal] public delegate void HealthChangedEventHandler(int current, int max);
		[Signal] public delegate void DeathEventHandler();

		// Base value (assigned via editor).
		[Export] private int _maxHealth = 100;

		// Runtime state — managed internally by the component.
		private int _currentHealth;

		// Public read-only properties for external access to health state.
		public int MaxHealth => _maxHealth;
		public int CurrentHealth => _currentHealth;
		public bool IsDead => _currentHealth <= 0;

		// ------------------------------------- Godot overrides ------------------------------------

		public override void _Ready()
		{
			// Initializes health state to its maximum value on spawn.
			_currentHealth = _maxHealth;
		}

		// ---------------------------------------- Public API --------------------------------------

		public void Reset()
		{
			// Restores entity to a fully alive state.
			UpdateHealth(_maxHealth);
		}

		public void SetHealth(int value)
		{
			// Directly overrides current health value (clamped internally).
			UpdateHealth(value);
		}

		public void ApplyDamage(int damage)
		{
			// Applies damage only if the entity is still alive.
			if (IsDead) return;
			UpdateHealth(_currentHealth - damage);
		}

		public void Heal(int amount)
		{
			// Applies healing only if the entity is still alive.
			if (IsDead) return;
			UpdateHealth(_currentHealth + amount);
		}

		public void Kill()
		{
			// Forces death state and emits termination signal once.
			if (IsDead) return;
			UpdateHealth(0);
		}

		// ----------------------------------------- Helpers ----------------------------------------

		private void UpdateHealth(int value)
		{
			var oldHealth = _currentHealth;

			_currentHealth = Mathf.Clamp(value, 0, _maxHealth);

			if (_currentHealth == oldHealth)
				return;

			EmitSignal(SignalName.HealthChanged, _currentHealth, _maxHealth);

			if (_currentHealth == 0 && oldHealth > 0)
				OnDeath();
		}

		private void OnDeath()
		{
			EmitSignal(SignalName.Death);
		}
	}
}