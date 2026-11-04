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
		/// <summary>
		/// Emitted whenever the current speed actually changes.
		/// Not emitted on initialization, so listeners should read <see cref="CurrentSpeed"/> once when connecting.
		/// </summary>
		/// <param name="current">Speed value after the change.</param>
		[Signal] public delegate void SpeedChangedEventHandler(float current);

		/// <summary>
		/// Initial speed, assigned via the editor. Also the baseline restored by <see cref="Reset"/>.
		/// </summary>
		[Export] public float DefaultSpeed { get; private set; } = 10f;

		/// <summary> Maximum speed value. </summary>
		[Export] public float MaxSpeed { get; private set; } = 100f;

		/// <summary> Runtime speed state, managed exclusively by this component. </summary>
		public float CurrentSpeed { get; private set; }

		public override void _Ready()
		{
			// Clamp the configured values to valid ranges and initialize the runtime state.
			MaxSpeed = Mathf.Max(0f, MaxSpeed);
			DefaultSpeed = Mathf.Clamp(DefaultSpeed, 0f, MaxSpeed);

			// Initialize the runtime state to the configured default speed.
			CurrentSpeed = DefaultSpeed;
		}

		/// <summary>
		/// Restores the default speed configured in the editor, undoing any runtime modification.
		/// </summary>
		public void Reset() =>
			UpdateSpeed(DefaultSpeed);

		/// <summary>
		/// Overrides the current speed directly.
		/// </summary>
		/// <param name="value">New speed value. Clamped to 0 and <see cref="MaxSpeed"/>.</param>
		public void SetSpeed(float value) =>
			UpdateSpeed(value);

		/// <summary>
		/// Increases the current speed by the given amount. Negative values are ignored.
		/// </summary>
		/// <param name="amount">Amount to add.</param>
		public void AddSpeed(float amount) =>
			UpdateSpeed(CurrentSpeed + Mathf.Max(0f, amount));

		/// <summary>
		/// Decreases the current speed by the given amount. Negative values are ignored.
		/// </summary>
		/// <param name="amount">Amount to subtract.</param>
		public void ReduceSpeed(float amount) =>
			UpdateSpeed(CurrentSpeed - Mathf.Max(0f, amount));

		/// <summary>
		/// Single write point for the speed state. Clamps the value and emits
		/// <see cref="SpeedChanged"/> only when it actually changed.
		/// </summary>
		/// <param name="value">Requested speed value, clamped to 0 and <see cref="MaxSpeed"/>.</param>
		private void UpdateSpeed(float value)
		{
			var oldSpeed = CurrentSpeed;

			CurrentSpeed = Mathf.Clamp(value, 0f, MaxSpeed);

			// Approximate comparison avoids spurious signals from float rounding noise.
			if (!Mathf.IsEqualApprox(CurrentSpeed, oldSpeed))
				EmitSignal(SignalName.SpeedChanged, CurrentSpeed);
		}
	}
}