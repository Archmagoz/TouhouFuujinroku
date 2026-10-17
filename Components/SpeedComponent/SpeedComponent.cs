using Godot;

namespace TouhouFuujinroku.Components
{
	[GlobalClass]
	public partial class SpeedComponent : Component
	{
		#region Signals

		[Signal] public delegate void SpeedChangedEventHandler(float current, float max);

		#endregion // Signals

		#region Fields

		// Base values (assigned via editor).
		[Export] private float _currentSpeed = 10f;
		[Export] private float _maxSpeed = 100f;

		// Runtime state — cached initial speed for reset operations.
		private float _defaultSpeed;

		// Public read-only fields for external access to current speed state.
		public float CurrentSpeed => _currentSpeed;
		public float MaxSpeed => _maxSpeed;

		#endregion // Fields

		#region Godot overrides

		public override void _Ready()
		{
			// Captures initial speed as baseline for future resets.
			_defaultSpeed = _currentSpeed;
		}

		#endregion // Godot overrides

		#region Public API

		public void Reset()
		{
			// Restores speed to its initial configured value.
			UpdateSpeed(_defaultSpeed);
		}

		public void SetSpeed(float value)
		{
			// Directly overrides current speed (clamped internally).
			UpdateSpeed(value);
		}

		public void AddSpeed(float amount)
		{
			// Increases speed relative to current value.
			UpdateSpeed(_currentSpeed + amount);
		}

		public void ReduceSpeed(float amount)
		{
			// Decreases speed relative to current value.
			UpdateSpeed(_currentSpeed - amount);
		}

		#endregion // Public API

		#region Helpers

		private void UpdateSpeed(float value)
		{
			// Centralized speed mutation logic with clamping and change detection.
			var oldSpeed = _currentSpeed;

			_currentSpeed = Mathf.Clamp(value, 0, _maxSpeed);

			if (!Mathf.IsEqualApprox(_currentSpeed, oldSpeed))
				EmitSignal(SignalName.SpeedChanged, _currentSpeed, _maxSpeed);
		}

		#endregion // Helpers
	}
}