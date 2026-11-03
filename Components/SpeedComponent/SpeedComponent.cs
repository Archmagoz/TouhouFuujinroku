using Godot;

namespace TouhouFuujinroku.Components
{
	/// <summary>
	/// Reusable speed component for any entity that moves.
	/// <para>
	/// The component owns the speed state and enforces its invariants (value always within
	/// 0 and <see cref="MaxSpeed"/>). Owners read <see cref="CurrentSpeed"/> to move and react
	/// to <see cref="SpeedChanged"/>, and never write the state directly.
	/// </para>
	/// </summary>
	[GlobalClass]
	public partial class SpeedComponent : Component
	{
		#region Signals

		/// <summary>
		/// Emitted whenever the current speed actually changes.
		/// Not emitted on initialization, so listeners should read <see cref="CurrentSpeed"/> once when connecting.
		/// </summary>
		/// <param name="current">Speed value after the change.</param>
		[Signal] public delegate void SpeedChangedEventHandler(float current);

		#endregion // Signals

		#region Fields

		/// <summary>
		/// Initial speed, assigned via the editor. Also the baseline restored by <see cref="Reset"/>.
		/// </summary>
		[Export] private float _defaultSpeed = 10f;

		/// <summary>
		/// Upper bound for the speed value, assigned via the editor.
		/// </summary>
		[Export] private float _maxSpeed = 100f;

		/// <summary>
		/// Runtime speed state, managed exclusively by this component.
		/// </summary>
		private float _currentSpeed;

		/// <summary>Public property for accessing the maximum speed.</summary>
		public float MaxSpeed => _maxSpeed;

		/// <summary>Public property for accessing the current speed.</summary>
		public float CurrentSpeed => _currentSpeed;

		#endregion // Fields

		#region Godot overrides

		public override void _Ready()
		{
			// Clamp the configured values to valid ranges and initialize the runtime state.
			_maxSpeed = Mathf.Max(0f, _maxSpeed);
			_defaultSpeed = Mathf.Clamp(_defaultSpeed, 0f, _maxSpeed);

			// Initialize the runtime state to the configured default speed.
			_currentSpeed = _defaultSpeed;
		}

		#endregion // Godot overrides

		#region Public API

		/// <summary>
		/// Restores the default speed configured in the editor, undoing any runtime modification.
		/// </summary>
		public void Reset()
		{
			UpdateSpeed(_defaultSpeed);
		}

		/// <summary>
		/// Overrides the current speed directly.
		/// </summary>
		/// <param name="value">New speed value. Clamped to 0 and <see cref="MaxSpeed"/>.</param>
		public void SetSpeed(float value)
		{
			UpdateSpeed(value);
		}

		/// <summary>
		/// Increases the current speed by the given amount. Negative values are ignored.
		/// </summary>
		/// <param name="amount">Amount to add.</param>
		public void AddSpeed(float amount)
		{
			UpdateSpeed(_currentSpeed + Mathf.Max(0f, amount));
		}

		/// <summary>
		/// Decreases the current speed by the given amount. Negative values are ignored.
		/// </summary>
		/// <param name="amount">Amount to subtract.</param>
		public void ReduceSpeed(float amount)
		{
			UpdateSpeed(_currentSpeed - Mathf.Max(0f, amount));
		}

		#endregion // Public API

		#region Helpers

		/// <summary>
		/// Single write point for the speed state. Clamps the value and emits
		/// <see cref="SpeedChanged"/> only when it actually changed.
		/// </summary>
		/// <param name="value">Requested speed value, clamped to 0 and <see cref="MaxSpeed"/>.</param>
		private void UpdateSpeed(float value)
		{
			var oldSpeed = _currentSpeed;

			_currentSpeed = Mathf.Clamp(value, 0, _maxSpeed);

			// Approximate comparison avoids spurious signals from float rounding noise.
			if (!Mathf.IsEqualApprox(_currentSpeed, oldSpeed))
				EmitSignal(SignalName.SpeedChanged, _currentSpeed);
		}

		#endregion // Helpers
	}
}