using Godot;

namespace TouhouFuujinroku.Components
{
	/// <summary>
	/// Reusable health container for any entity that can take damage, be healed, or die.
	/// <para>
	/// The component owns the health state and enforces its invariants (value always within
	/// 0 and <see cref="MaxHealth"/>, death reported exactly once per alive-to-dead transition).
	/// Owners read <see cref="CurrentHealth"/> and react to <see cref="HealthChanged"/> and
	/// <see cref="Death"/>, and never write the state directly.
	/// </para>
	/// </summary>
	[GlobalClass]
	public partial class HealthComponent : Component
	{
		#region Signals

		/// <summary>
		/// Emitted whenever the current health value actually changes.
		/// Not emitted on initialization, so listeners should read <see cref="CurrentHealth"/> once when connecting.
		/// </summary>
		/// <param name="current">Health value after the change.</param>
		/// <param name="max">Maximum health value.</param>
		[Signal] public delegate void HealthChangedEventHandler(int current, int max);
		/// <summary>
		/// Emitted exactly once each time health transitions from alive to dead.
		/// Emitted after <see cref="HealthChanged"/> for the same change.
		/// </summary>
		[Signal] public delegate void DeathEventHandler();

		#endregion // Signals

		#region Fields

		/// <summary>
		/// Maximum health value, assigned via the editor. Also the initial value on spawn
		/// and the baseline restored by <see cref="Reset"/>. Validated to be at least 1 on ready.
		/// </summary>
		[Export] protected int _maxHealth = 100;

		/// <summary>
		/// Runtime health state, managed exclusively by this component.
		/// </summary>
		private int _currentHealth;

		/// <summary>Public property for accessing the maximum health.</summary>
		public int MaxHealth => _maxHealth;

		/// <summary>Public property for accessing the current health, always between 0 and <see cref="MaxHealth"/>.</summary>
		public int CurrentHealth => _currentHealth;

		/// <summary>True once health has reached zero.</summary>
		public bool IsDead => _currentHealth <= 0;

		#endregion // Fields

		#region Godot overrides

		public override void _Ready()
		{
			// Clamp the configured value to a valid range so the component never spawns dead by misconfiguration.
			_maxHealth = Mathf.Max(1, _maxHealth);

			// Initialize directly (not through UpdateHealth) so no signal is emitted during setup.
			_currentHealth = _maxHealth;
		}

		#endregion // Godot overrides

		#region Public API

		/// <summary>
		/// Restores the entity to full health, undoing any runtime modification. Also revives a dead entity,
		/// so <see cref="Death"/> may be emitted again on a later lethal hit.
		/// </summary>
		public void Reset()
		{
			UpdateHealth(_maxHealth);
		}

		/// <summary>
		/// Overrides the current health value directly. Works on dead entities too, acting as a revive when above zero.
		/// </summary>
		/// <param name="value">New health value. Clamped to 0 and <see cref="MaxHealth"/>.</param>
		public void SetHealth(int value)
		{
			UpdateHealth(value);
		}

		/// <summary>
		/// Reduces health by the given amount. Negative values are ignored.
		/// Ignored if the entity is already dead.
		/// </summary>
		/// <param name="damage">Amount of damage to apply.</param>
		public void ApplyDamage(int damage)
		{
			if (IsDead) return;
			UpdateHealth(_currentHealth - Mathf.Max(0, damage));
		}

		/// <summary>
		/// Increases health by the given amount, capped at <see cref="MaxHealth"/>. Negative values are ignored.
		/// Ignored if the entity is already dead; use <see cref="Reset"/> or <see cref="SetHealth"/> to revive.
		/// </summary>
		/// <param name="amount">Amount of health to restore.</param>
		public void Heal(int amount)
		{
			if (IsDead) return;
			UpdateHealth(_currentHealth + Mathf.Max(0, amount));
		}

		/// <summary>
		/// Forces the dead state regardless of remaining health. Ignored if already dead,
		/// which guarantees <see cref="Death"/> is emitted only once.
		/// </summary>
		public void Kill()
		{
			if (IsDead) return;
			UpdateHealth(0);
		}

		#endregion // Public API

		#region Helpers

		/// <summary>
		/// Single write point for the health state. Clamps the value, emits <see cref="HealthChanged"/>
		/// only when it actually changed, and triggers <see cref="OnDeath"/> on the alive-to-dead transition.
		/// </summary>
		/// <param name="value">Requested health value, clamped to 0 and <see cref="MaxHealth"/>.</param>
		private void UpdateHealth(int value)
		{
			var oldHealth = _currentHealth;

			_currentHealth = Mathf.Clamp(value, 0, _maxHealth);

			// No-op writes (e.g. damaging at zero, healing at full) must stay silent.
			if (_currentHealth == oldHealth)
				return;

			EmitSignal(SignalName.HealthChanged, _currentHealth, _maxHealth);

			if (_currentHealth == 0 && oldHealth > 0)
				OnDeath();
		}

		/// <summary>
		/// Death hook, invoked exactly once on the alive-to-dead transition.
		/// Derived components must call <c>base.OnDeath()</c> first so <see cref="Death"/>
		/// is always emitted before any derived logic runs.
		/// </summary>
		protected virtual void OnDeath()
		{
			EmitSignal(SignalName.Death);
		}

		#endregion // Helpers
	}
}