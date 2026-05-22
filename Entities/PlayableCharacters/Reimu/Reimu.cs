using Godot;
using TohouFuuujinoku.Components;

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

		// Raw directional input built each physics frame.
		private Vector2 _movementInput;

		// ---------------------------------- Godot overrides -----------------------------------

		public override void _PhysicsProcess(double delta)
		{
			HandleMovement();
			UpdateSprite();
		}

		public override void _Process(double delta)
		{
			HandleFocus(delta);
		}

		// ---------------------------------- Private helpers -----------------------------------

		private void HandleMovement()
		{
			_movementInput = Vector2.Zero;

			if (Input.IsActionPressed("up")) _movementInput.Y -= 1;
			if (Input.IsActionPressed("down")) _movementInput.Y += 1;
			if (Input.IsActionPressed("left")) _movementInput.X -= 1;
			if (Input.IsActionPressed("right")) _movementInput.X += 1;

			Velocity = _movementInput.Normalized() * _speed.CurrentSpeed;
			MoveAndSlide();
		}

		private void HandleFocus(double delta)
		{
			bool focused = Input.IsActionPressed("focus");
			_speed.SetSpeed(focused ? _speed.MaxSpeed / 2 : _speed.MaxSpeed);
			_weapon.ToggleFocusMode(focused, delta);
		}

		private void UpdateSprite()
		{
			_sprite.UpdateSprite(_movementInput.X);
		}
	}
}