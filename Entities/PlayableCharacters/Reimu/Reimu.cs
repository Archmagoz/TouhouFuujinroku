using Godot;

using TouhouFuujinroku.Components;
using TouhouFuujinroku.Interfaces;

namespace TouhouFuujinroku.Entities.PlayableCharacters.Reimu
{
	public partial class Reimu : CharacterBody2D, IDamageable
	{
		[ExportGroup("Components")]
		[Export] private PlayerHealthComponent _health;
		[Export] private SpeedComponent _speed;
		[Export] private ReimuAnimatedSprite _sprite;
		[Export] private ReimuWeapon _weapon;

		// Raw directional input built each physics frame.
		private Vector2 _movementInput;

		// Frames remaining on the shoot buffer — allows firing to register slightly after
		// the button is released, preventing missed shots from brief input gaps.
		private const int ShootBufferFrames = 6; // ~0.1 s at 60 fps — tune to taste.
		private int _shootBuffer;

		// IDamageable implementation — simply forwards to the HealthComponent.
		public void ApplyDamage(int amount) => _health.ApplyDamage(amount);

		// Godot overrides ------------------------------------------------------------------------------------

		public override void _Ready()
		{
			_health.PlayerDied += OnPlayerDied;
			_health.Respawned += OnRespawned;
			_health.GameOver += OnGameOver;
		}

		public override void _PhysicsProcess(double delta)
		{
			if (_health.IsDead) return;

			HandleMovement();
			HandleShooting();
		}

		public override void _Process(double delta)
		{
			if (_health.IsDead) return;

			HandleFocus(delta);
			UpdateSprite();
		}

		// Private helpers ------------------------------------------------------------------------------------

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
				_weapon.TryFire();
				_shootBuffer--;
			}
		}

		private void UpdateSprite()
		{
			_sprite.UpdateSprite(_movementInput.X);
		}

		// Signal callbacks -----------------------------------------------------------------------------------

		private void OnPlayerDied()
		{
			// Stops the body immediately and hands sprite control over to the death animation.
			Velocity = Vector2.Zero;
			_sprite.PlayDeath();
		}

		private void OnRespawned()
		{
			// _health.IsDead is already false at this point, so _PhysicsProcess/_Process
			// resume on their own next frame — this just clears the death pose immediately
			// instead of waiting one extra frame for input-driven UpdateSprite to override it.
			_sprite.UpdateSprite(0f);
		}

		private void OnGameOver()
		{
			// Entity stays permanently in IsDead == true (no further Reset call), so
			// movement/shooting/sprite updates remain locked automatically.
			// Hook point for external systems (game over screen, scene transition, etc.).
		}
	}
}