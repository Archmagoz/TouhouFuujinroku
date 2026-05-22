using Godot;
using TohouFuuujinoku.Components;

namespace TohouFuuujinoku.Entities.PlayableCharacters
{
	public partial class Reimu : CharacterBody2D
	{
		[ExportGroup("Components")]
		[Export] private SpriteComponent _sprite;
		[Export] private HealthComponent _health;
		[Export] private SpeedComponent _speed;
		[Export] private ReimuWeapon _weapon;
		[Export] private ReimuWeaponFireController _fireController;

		// Raw directional input built each physics frame.
		private Vector2 _movementInput;

		// Frames remaining on the shoot buffer — allows firing to register slightly after
		// the button is released, preventing missed shots from brief input gaps.
		private int _shootBuffer;
		private const int ShootBufferFrames = 6; // ~0.1 s at 60 fps — tune to taste.

		// ---------------------------------- Godot overrides -----------------------------------

		public override void _PhysicsProcess(double delta)
		{
			HandleMovement();
			UpdateSprite();
		}

		public override void _Process(double delta)
		{
			HandleFocus(delta);
			HandleShooting();
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

		// Feeds the shoot buffer on input and drains it each frame, forwarding to the
		// fire controller. The buffer absorbs brief input gaps without skipping shots.
		private void HandleShooting()
		{
			if (Input.IsActionPressed("shoot"))
				_shootBuffer = ShootBufferFrames;

			if (_shootBuffer > 0)
			{
				_fireController.TryFire();
				_shootBuffer--;
			}
		}

		private void UpdateSprite()
		{
			_sprite.UpdateSprite(_movementInput.X);
		}
	}
}