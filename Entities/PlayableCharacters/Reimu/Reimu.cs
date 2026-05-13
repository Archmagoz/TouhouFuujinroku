using TohouFuuujinoku.Components;

using Godot;

namespace TohouFuuujinoku.Entities.PlayableCharacters
{
	public partial class Reimu : CharacterBody2D
	{
		// Node references (assigned via editor).
		[ExportGroup("Components")]
		[Export] private SpriteComponent _sprite;
		[Export] private HealthComponent _health;
		[Export] private SpeedComponent _speed;
		[Export] private ReimuWeapon _weapon;

		// Focus mode movement multiplier.
		private const float FocusSpeedMultiplier = 0.5f;

		public override void _PhysicsProcess(double delta)
		{
			HandleMovement();
		}

		private void HandleMovement()
		{
			// Build raw movement input vector.
			var input = Vector2.Zero;

			if (Input.IsActionPressed("up")) input.Y -= 1;
			if (Input.IsActionPressed("down")) input.Y += 1;
			if (Input.IsActionPressed("left")) input.X -= 1;
			if (Input.IsActionPressed("right")) input.X += 1;

			// Handle focus state separately to allow
			// additional focus-related behaviors.
			var speed = _speed.CurrentSpeed;

			if (Input.IsActionPressed("shift"))
			{
				speed = HandleFocusMode(speed);
			}

			// Apply normalized movement velocity.
			Velocity = input.Normalized() * speed;

			MoveAndSlide();

			// Update visual state.
			UpdateSprite(input.X);
		}

		private float HandleFocusMode(float speed)
		{
			// Enable focus-specific weapon behavior.
			_weapon.SetFocusMode(true);

			// Reduce movement speed while focusing.
			return speed * FocusSpeedMultiplier;
		}

		private void UpdateSprite(float inputX)
		{
			if (inputX < 0) _sprite.PlayLeft();
			else if (inputX > 0) _sprite.PlayRight();
			else _sprite.PlayDefault();
		}
	}
}