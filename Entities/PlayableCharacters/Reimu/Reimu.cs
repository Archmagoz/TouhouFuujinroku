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
	/// and never reach into its components directly. They react to its life cycle through the
	/// <see cref="Died"/>, <see cref="Respawned"/>, <see cref="GameOver"/>, and <see cref="LivesChanged"/> signals,
	/// connected in the editor.
	/// </para>
	/// <para>
	/// After each respawn the player returns to the position where the level started and is invincible for
	/// a short time, signaled by a blinking sprite.
	/// </para>
	/// </summary>
	public partial class Reimu : CharacterBody2D, IDamageable
	{
		#region Signals

		/// <summary>
		/// Emitted when the player is hit and dies, before the respawn delay. Listeners typically clear the
		/// playfield. It may be emitted from inside a physics callback, so listeners that change collision
		/// state must connect with the Deferred flag.
		/// </summary>
		[Signal] public delegate void DiedEventHandler();

		/// <summary>
		/// Emitted when the player comes back to life, already at the respawn position and invincible.
		/// </summary>
		[Signal] public delegate void RespawnedEventHandler();

		/// <summary>
		/// Emitted when the last life is lost. The player stays dead.
		/// </summary>
		[Signal] public delegate void GameOverEventHandler();

		/// <summary>
		/// Emitted whenever the number of remaining lives changes, after the new value is already in effect.
		/// Not emitted on initialization, so listeners should read <see cref="Lives"/> once when connecting.
		/// </summary>
		/// <param name="current">Remaining lives after the change.</param>
		/// <param name="max">Maximum number of lives.</param>
		[Signal] public delegate void LivesChangedEventHandler(int current, int max);

		#endregion // Signals

		#region Fields

		#region Exported

		/// <summary>Lives and one-hit health state. Its signals drive the death, respawn, lives, and game over handlers.</summary>
		[ExportGroup("Components")]
		[Export] private PlayerHealthComponent _health;

		/// <summary>Source of the movement speed. Halved while focus is held.</summary>
		[Export] private SpeedComponent _speed;

		/// <summary>Sprite that reflects movement direction and plays the death animation.</summary>
		[Export] private ReimuAnimatedSprite _sprite;

		/// <summary>Weapon fired by the shoot input and switched between normal and focus modes.</summary>
		[Export] private ReimuWeapon _weapon;

		/// <summary>Collision shape of the player, drawn only while focus is held so the hit area is visible.</summary>
		[Export] private ReimuHitbox _hitbox;

		/// <summary>Dead SE: played when the player dies, before the respawn delay.</summary>
		[ExportGroup("Audio")]
		[Export] private AudioStreamPlayer2D _deathSound;

		/// <summary>
		/// Seconds of invincibility granted after each respawn.
		/// </summary>
		[ExportGroup("Respawn")]
		[Export] private float _invincibilityDuration = 3f;

		#endregion // Exported

		#region Constants

		/// <summary>
		/// Number of frames the shoot input stays active after the button is released (about 0.1 s at 60 fps).
		/// Tune to taste.
		/// </summary>
		private const int ShootBufferFrames = 6;

		/// <summary>
		/// Duration, in seconds, of each visible or faded phase of the invincibility blink.
		/// </summary>
		private const float BlinkInterval = 0.08f;

		/// <summary>
		/// Sprite opacity during the faded phase of the invincibility blink.
		/// </summary>
		private const float FadedAlpha = 0.35f;

		#endregion // Constants

		#region Runtime state

		/// <summary>
		/// Raw directional input, rebuilt every physics frame.
		/// </summary>
		private Vector2 _movementInput;

		/// <summary>
		/// Frames remaining on the shoot buffer. Allows firing to register slightly after the button
		/// is released, preventing missed shots from brief input gaps.
		/// </summary>
		private int _shootBuffer;

		/// <summary>
		/// Position the player had when the level started. Every respawn returns here, not to where the player died.
		/// </summary>
		private Vector2 _spawnPosition;

		/// <summary>
		/// Seconds of invincibility remaining. Zero or less means the player can be hurt.
		/// </summary>
		private float _invincibilityTimeLeft;

		#endregion // Runtime state

		#endregion // Fields

		#region Properties

		/// <summary>True while the player has no health. Exposed here so Reimu stays the only access point to its state.</summary>
		public bool IsDead => _health.IsDead;

		/// <summary>True while the player ignores damage after a respawn.</summary>
		public bool IsInvincible => _invincibilityTimeLeft > 0f;

		/// <summary>Remaining lives, including the one currently in play.</summary>
		public int Lives => _health.CurrentLives;

		/// <summary>Maximum number of lives.</summary>
		public int MaxLives => _health.MaxLives;

		#endregion // Properties

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

		#region Signal handlers

		/// <summary>
		/// Handles the health component's Death signal: stops the body immediately, discards any buffered
		/// shots, hides the sprite, weapon, and hitbox, plays the death sound, and emits <see cref="Died"/>.
		/// </summary>
		private void OnPlayerDied()
		{
			Velocity = Vector2.Zero;
			_shootBuffer = 0;
			_sprite.SetVisible(false);
			_weapon.SetVisible(false);
			_hitbox.HideImmediately();
			_deathSound.Play();

			EmitSignal(SignalName.Died);
		}

		/// <summary>
		/// Handles the health component's LivesChanged signal and republishes it as <see cref="LivesChanged"/>.
		/// The component emits it after decrementing the lives, so <see cref="Lives"/> is already
		/// up to date by the time listeners run.
		/// </summary>
		/// <param name="current">Remaining lives after the change.</param>
		/// <param name="max">Maximum number of lives.</param>
		private void OnLivesChanged(int current, int max)
		{
			EmitSignal(SignalName.LivesChanged, current, max);
		}

		/// <summary>
		/// Handles the Respawned signal. <see cref="IsDead"/> is already false at this point, so
		/// <c>_PhysicsProcess</c> resumes on its own next frame. Returns the player to the start position,
		/// snaps the weapon formation to it, clears the death pose immediately (instead of waiting one extra
		/// frame for the input-driven sprite update to override it), starts the invincibility window,
		/// and emits <see cref="Respawned"/>.
		/// </summary>
		private void OnRespawned()
		{
			GlobalPosition = _spawnPosition;
			_sprite.SetVisible(true);
			_weapon.SetVisible(true);
			_sprite.UpdateSprite(0f);
			_weapon.SnapFormation();

			_invincibilityTimeLeft = _invincibilityDuration;

			EmitSignal(SignalName.Respawned);
		}

		/// <summary>
		/// Handles the GameOver signal. The entity stays permanently dead (no further reset),
		/// so movement, shooting, and sprite updates remain locked automatically.
		/// External systems (game over screen, scene transition, and so on) react through <see cref="GameOver"/>.
		/// </summary>
		private void OnGameOver()
		{
			EmitSignal(SignalName.GameOver);
		}

		#endregion // Signal handlers

		#region Helpers

		#region Input and movement

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
		/// Applies focus mode: halves the movement speed, switches the weapon to its focused configuration,
		/// and draws the hitbox while the focus action is held.
		/// </summary>
		/// <param name="delta">Physics frame time in seconds, forwarded to the weapon's focus transition.</param>
		private void HandleFocus(double delta)
		{
			bool focused = Input.IsActionPressed("focus");
			_speed.SetSpeed(focused ? _speed.MaxSpeed / 2 : _speed.MaxSpeed);
			_weapon.ToggleFocusMode(focused, delta);
			_hitbox.SetFocused(focused);
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

		#endregion // Input and movement

		#region Presentation

		/// <summary>
		/// Updates the sprite with the horizontal input so it can lean toward the movement direction.
		/// </summary>
		private void UpdateSprite()
		{
			_sprite.UpdateSprite(_movementInput.X);
		}

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

			Color tint = new Color(1f, 1f, 1f, faded ? FadedAlpha : 1f);
			_sprite.Modulate = tint;
			_weapon.Modulate = tint;
		}

		#endregion // Presentation

		#endregion // Helpers
	}
}