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

		public override void _PhysicsProcess(double delta)
		{
			HandleMovement();
		}

		private void HandleMovement()
		{
			var input = Vector2.Zero;

			if (Input.IsActionPressed("up")) input.Y -= 1;
			if (Input.IsActionPressed("down")) input.Y += 1;
			if (Input.IsActionPressed("left")) input.X -= 1;
			if (Input.IsActionPressed("right")) input.X += 1;

			Velocity = input.Normalized() * _speed.CurrentSpeed;

			MoveAndSlide();
			UpdateSprite(input.X);
		}

		private void UpdateSprite(float inputX)
		{
			if (inputX < 0) _sprite.PlayLeft();
			else if (inputX > 0) _sprite.PlayRight();
			else _sprite.PlayDefault();
		}
	}
}