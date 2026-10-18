using Godot;

using TouhouFuujinroku.Components;
using TouhouFuujinroku.Interfaces;

namespace TouhouFuujinroku.Entities.PlayableCharacters.Reimu
{
	/// <summary>
	/// Playable character controller: reads input, moves the body, drives the sprite and weapon,
	/// and reacts to the life cycle signals of its <see cref="PlayerHealthComponent"/>.
	/// <para>
	/// Reimu is the only access point to its own state. External systems query it through its public API
	/// and never reach into its components directly.
	/// </para>
	/// </summary>
	public partial class Reimu : CharacterBody2D, IDamageable
	{
		#region Fields

		/// <summary>Lives and one-hit health state. Its signals drive the death, respawn, and game over handlers.</summary>
		[ExportGroup("Components")]
		[Export] private PlayerHealthComponent _health;

		/// <summary>Source of the movement speed. Halved while focus is held.</summary>
		[Export] private SpeedComponent _speed;

		/// <summary>Sprite that reflects movement direction and plays the death animation.</summary>
		[Export] private ReimuAnimatedSprite _sprite;

		/// <summary>Weapon fired by the shoot input and switched between normal and focus modes.</summary>
		[Export] private ReimuWeapon _weapon;

		/// <summary>
		/// Raw directional input, rebuilt every physics frame.
		/// </summary>
		private Vector2 _movementInput;

		/// <summary>
		/// Number of frames the shoot input stays active after the button is released (about 0.1 s at 60 fps).
		/// Tune to taste.
		/// </summary>
		private const int ShootBufferFrames = 6;

		/// <summary>
		/// Frames remaining on the shoot buffer. Allows firing to register slightly after the button
		/// is released, preventing missed shots from brief input gaps.
		/// </summary>
		private int _shootBuffer;

		/// <summary>True while the player has no health. Exposed here so Reimu stays the only access point to its state.</summary>
		public bool IsDead => _health.IsDead;

		#endregion // Fields

		#region Godot overrides

		public override void _PhysicsProcess(double delta)
		{
			if (IsDead) return;

			HandleFocus(delta);
			HandleMovement();
			UpdateSprite();
			HandleShooting();
		}

		#endregion // Godot overrides

		#region Public API

		/// <summary>
		/// Forwards damage to the <see cref="PlayerHealthComponent"/>.
		/// </summary>
		/// <param name="amount">Amount of damage to apply.</param>
		public void ApplyDamage(int amount) => _health.ApplyDamage(amount);

		#endregion // Public API

		#region Event handlers

		/// <summary>
		/// Handles the health component's Death signal: stops the body immediately
		/// and hands sprite control over to the death animation.
		/// </summary>
		private void OnPlayerDied()
		{
			Velocity = Vector2.Zero;
			_sprite.PlayDeath();
		}

		/// <summary>
		/// Handles the Respawned signal. <see cref="IsDead"/> is already false at this point, so
		/// <c>_PhysicsProcess</c> resumes on its own next frame. This only clears the death pose immediately,
		/// instead of waiting one extra frame for the input-driven sprite update to override it.
		/// </summary>
		private void OnRespawned()
		{
			_sprite.UpdateSprite(0f);
		}

		/// <summary>
		/// Handles the GameOver signal. The entity stays permanently dead (no further reset),
		/// so movement, shooting, and sprite updates remain locked automatically.
		/// Hook point for external systems (game over screen, scene transition, and so on).
		/// </summary>
		private void OnGameOver()
		{
		}

		#endregion // Event handlers

		#region Helpers

		/// <summary>
		/// Builds the movement input from the directional actions and moves the body.
		/// Diagonal input is normalized so it is never faster than straight movement.
		/// </summary>
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

		/// <summary>
		/// Applies focus mode: halves the movement speed and switches the weapon to its focused configuration
		/// while the focus action is held.
		/// </summary>
		/// <param name="delta">Physics frame time in seconds, forwarded to the weapon's focus transition.</param>
		private void HandleFocus(double delta)
		{
			bool focused = Input.IsActionPressed("focus");
			_speed.SetSpeed(focused ? _speed.MaxSpeed / 2 : _speed.MaxSpeed);
			_weapon.ToggleFocusMode(focused, delta);
		}

		/// <summary>
		/// Feeds the shoot buffer on input and drains it each frame, forwarding to the weapon.
		/// The buffer absorbs brief input gaps without skipping shots.
		/// </summary>
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

		/// <summary>
		/// Updates the sprite with the horizontal input so it can lean toward the movement direction.
		/// </summary>
		private void UpdateSprite()
		{
			_sprite.UpdateSprite(_movementInput.X);
		}

		#endregion // Helpers
	}
}