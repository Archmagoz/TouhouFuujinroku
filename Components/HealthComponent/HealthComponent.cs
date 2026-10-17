using Godot;

namespace TouhouFuujinroku.Components
{
	[GlobalClass]
	public partial class HealthComponent : Component
	{
		#region Signals

		[Signal] public delegate void HealthChangedEventHandler(int current, int max);
		[Signal] public delegate void DeathEventHandler();

		#endregion // Signals

		#region Fields

		// Base value (assigned via editor).
		[Export] protected int _maxHealth = 100;

		// Runtime state — managed internally by the component.
		private int _currentHealth;

		// Public read-only properties for external access to health state.
		public int MaxHealth => _maxHealth;
		public int CurrentHealth => _currentHealth;
		public bool IsDead => _currentHealth <= 0;

		#endregion // Fields

		#region Godot overrides

		public override void _Ready()
		{
			// Initializes health state to its maximum value on spawn.
			_currentHealth = _maxHealth;
		}

		#endregion // Godot overrides

		#region Public API

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

		#endregion // Public API

		#region Helpers

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

		// Virtual death hook — invoked exactly once on the alive-to-dead transition.
		// Derived components must call base.OnDeath() first so Death is always emitted
		// before any derived logic runs.
		protected virtual void OnDeath()
		{
			EmitSignal(SignalName.Death);
		}

		#endregion // Helpers
	}
}