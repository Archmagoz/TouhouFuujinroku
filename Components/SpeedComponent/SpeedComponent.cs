using Godot;

namespace TouhouFuujinroku.Components
{
	/// <summary>
	/// Reusable speed container for any entity that moves.
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
		/// <param name="max">Maximum speed value.</param>
		[Signal] public delegate void SpeedChangedEventHandler(float current, float max);

		#endregion // Signals

		#region Fields

		/// <summary>
		/// Initial speed, assigned via the editor. Also the baseline restored by <see cref="Reset"/>.
		/// </summary>
		[Export] private float _currentSpeed = 10f;

		/// <summary>
		/// Upper bound for the speed value, assigned via the editor.
		/// </summary>
		[Export] private float _maxSpeed = 100f;

		/// <summary>
		/// Speed captured on spawn, used as the baseline for <see cref="Reset"/>.
		/// </summary>
		private float _defaultSpeed;

		/// <summary>Current speed value, always between 0 and <see cref="MaxSpeed"/>.</summary>
		public float CurrentSpeed => _currentSpeed;

		/// <summary>Maximum speed value.</summary>
		public float MaxSpeed => _maxSpeed;

		#endregion // Fields

		#region Godot overrides

		public override void _Ready()
		{
			// Captured here (not at field initialization) so the editor-assigned value is the one stored.
			_defaultSpeed = _currentSpeed;
		}

		#endregion // Godot overrides

		#region Public API

		/// <summary>
		/// Restores the speed configured in the editor, undoing any runtime modification.
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
		/// Increases the speed relative to its current value, capped at <see cref="MaxSpeed"/>.
		/// </summary>
		/// <param name="amount">Amount to add. Expected to be non-negative; use <see cref="ReduceSpeed"/> to decrease.</param>
		public void AddSpeed(float amount)
		{
			UpdateSpeed(_currentSpeed + amount);
		}

		/// <summary>
		/// Decreases the speed relative to its current value, floored at 0.
		/// </summary>
		/// <param name="amount">Amount to subtract. Expected to be non-negative; use <see cref="AddSpeed"/> to increase.</param>
		public void ReduceSpeed(float amount)
		{
			UpdateSpeed(_currentSpeed - amount);
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
				EmitSignal(SignalName.SpeedChanged, _currentSpeed, _maxSpeed);
		}

		#endregion // Helpers
	}
}