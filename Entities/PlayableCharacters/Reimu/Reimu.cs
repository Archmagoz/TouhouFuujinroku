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

		private Vector2 _movementInput;

		// ------------------------------------- Godot overrides ------------------------------------

		public override void _PhysicsProcess(double delta)
		{
			HandleMovement();
			UpdateSprite();
		}

		// ----------------------------------------- Helpers ----------------------------------------

		private void HandleMovement()
		{
			// Build raw movement input vector.
			_movementInput = Vector2.Zero;

			if (Input.IsActionPressed("up")) _movementInput.Y -= 1;
			if (Input.IsActionPressed("down")) _movementInput.Y += 1;
			if (Input.IsActionPressed("left")) _movementInput.X -= 1;
			if (Input.IsActionPressed("right")) _movementInput.X += 1;

			Velocity = _movementInput.Normalized() * _speed.CurrentSpeed;

			MoveAndSlide();
		}

		private void UpdateSprite()
		{
			_sprite.UpdateSprite(_movementInput.X);
		}
	}
}