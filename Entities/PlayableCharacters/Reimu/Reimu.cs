using Godot;

using System;

using TouhouFuujinroku.Components;
using TouhouFuujinroku.Interfaces;

namespace TouhouFuujinroku.Entities.PlayableCharacters.Reimu
{
	/// <summary>
	/// Playable character controller: reads input, moves the body, drives the sprite and weapon,
	/// and reacts to the life cycle signals of its <see cref="PlayerHealthComponent"/>.
	/// <para>
	/// Reimu is the only access point to its own state. External systems query it through its public API
	/// and never reach into its components directly. They react to its life cycle through the
	/// <see cref="Died"/>, <see cref="Respawned"/>, and <see cref="GameOver"/> events.
	/// </para>
	/// <para>
	/// After each respawn the player returns to the position where the level started and is invincible for
	/// a short time, signaled by a blinking sprite.
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

		/// <summary>Dead SE: played when the player dies, before the respawn delay.</summary>
		[ExportGroup("Audio")]
		[Export] private AudioStreamPlayer2D _deathSound;

		/// <summary>
		/// Seconds of invincibility granted after each respawn.
		/// </summary>
		[ExportGroup("Respawn")]
		[Export] private float _invincibilityDuration = 3f;

		/// <summary>
		/// Raised when the player is hit and dies, before the respawn delay. Listeners typically clear the
		/// playfield. It may be raised from inside a physics callback, so listeners must defer any work that
		/// changes collision state.
		/// </summary>
		public event Action Died;

		/// <summary>
		/// Raised when the player comes back to life, already at the respawn position and invincible.
		/// </summary>
		public event Action Respawned;

		/// <summary>
		/// Raised when the last life is lost. The player stays dead.
		/// </summary>
		public event Action GameOver;

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

		/// <summary>
		/// Duration, in seconds, of each visible or faded phase of the invincibility blink.
		/// </summary>
		private const float BlinkInterval = 0.08f;

		/// <summary>
		/// Sprite opacity during the faded phase of the invincibility blink.
		/// </summary>
		private const float FadedAlpha = 0.35f;

		/// <summary>
		/// Position the player had when the level started. Every respawn returns here, not to where the player died.
		/// </summary>
		private Vector2 _spawnPosition;

		/// <summary>
		/// Seconds of invincibility remaining. Zero or less means the player can be hurt.
		/// </summary>
		private float _invincibilityTimeLeft;

		/// <summary>True while the player has no health. Exposed here so Reimu stays the only access point to its state.</summary>
		public bool IsDead => _health.IsDead;

		/// <summary>True while the player ignores damage after a respawn.</summary>
		public bool IsInvincible => _invincibilityTimeLeft > 0f;

		#endregion // Fields

		#region Godot overrides

		public override void _Ready()
		{
			// Remembered once, so place Reimu at the desired start position in the level scene.
			_spawnPosition = GlobalPosition;
		}

		public override void _PhysicsProcess(double delta)
		{
			if (IsDead) return;

			UpdateInvincibility(delta);
			HandleFocus(delta);
			HandleMovement();
			UpdateSprite();
			HandleShooting();
		}

		#endregion // Godot overrides

		#region Public API

		/// <summary>
		/// Forwards damage to the <see cref="PlayerHealthComponent"/>, unless the player is invincible.
		/// Every damage source goes through here, so the invincibility covers all of them.
		/// </summary>
		/// <param name="amount">Amount of damage to apply.</param>
		public void ApplyDamage(int amount)
		{
			if (IsInvincible) return;

			_health.ApplyDamage(amount);
		}

		#endregion // Public API

		#region Event handlers

		/// <summary>
		/// Handles the health component's Death signal: stops the body immediately, discards any buffered
		/// shots, hands sprite control over to the death animation, and notifies listeners.
		/// </summary>
		private void OnPlayerDied()
		{
			Velocity = Vector2.Zero;
			_shootBuffer = 0;
			_sprite.SetVisible(false);
			_weapon.SetVisible(false);
			_deathSound.Play();

			Died?.Invoke();
		}

		/// <summary>
		/// Handles the Respawned signal. <see cref="IsDead"/> is already false at this point, so
		/// <c>_PhysicsProcess</c> resumes on its own next frame. Returns the player to the start position,
		/// snaps the weapon formation to it, clears the death pose immediately (instead of waiting one extra
		/// frame for the input-driven sprite update to override it), and starts the invincibility window.
		/// </summary>
		private void OnRespawned()
		{
			GlobalPosition = _spawnPosition;
			_sprite.SetVisible(true);
			_weapon.SetVisible(true);
			_sprite.UpdateSprite(0f);
			_weapon.SnapFormation();

			_invincibilityTimeLeft = _invincibilityDuration;

			Respawned?.Invoke();
		}

		/// <summary>
		/// Handles the GameOver signal. The entity stays permanently dead (no further reset),
		/// so movement, shooting, and sprite updates remain locked automatically.
		/// External systems (game over screen, scene transition, and so on) react through <see cref="GameOver"/>.
		/// </summary>
		private void OnGameOver()
		{
			GameOver?.Invoke();
		}

		#endregion // Event handlers

		#region Helpers

		/// <summary>
		/// Counts down the invincibility window and blinks the sprite while it lasts.
		/// Full opacity is restored on the frame the window ends.
		/// </summary>
		/// <param name="delta">Physics frame time in seconds.</param>
		private void UpdateInvincibility(double delta)
		{
			if (_invincibilityTimeLeft <= 0f) return;

			_invincibilityTimeLeft -= (float)delta;

			bool faded = _invincibilityTimeLeft > 0f
				&& Mathf.PosMod(_invincibilityTimeLeft, 2f * BlinkInterval) < BlinkInterval;

			_sprite.Modulate = new Color(1f, 1f, 1f, faded ? FadedAlpha : 1f);
			_weapon.Modulate = new Color(1f, 1f, 1f, faded ? FadedAlpha : 1f);
		}

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