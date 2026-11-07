using Godot;

using TouhouFuujinroku.Components;
using TouhouFuujinroku.Interfaces;

namespace TouhouFuujinroku.Entities.PlayableCharacters
{
    /// <summary>
    /// Base for every playable character. Owns only what is common to all of them: input, movement,
    /// focus speed, shoot buffering, invincibility timing, and the life cycle driven by its
    /// <see cref="PlayerHealthComponent"/>.
    /// <para>
    /// Anything that depends on a specific sprite, weapon, or hitbox is delegated to the abstract hooks,
    /// which each character implements with its own concrete types.
    /// </para>
    /// <para>
    /// The character is the only access point to its own state. External systems query it through its public API
    /// and react to its life cycle through <see cref="Died"/>, <see cref="Respawned"/>, <see cref="GameOver"/>,
    /// and <see cref="LivesChanged"/>, connected in the editor.
    /// </para>
    /// </summary>
    public abstract partial class PlayableCharacter : CharacterBody2D, IDamageable
    {
        /// <summary>
        /// Emitted whenever the number of remaining lives changes, after the new value is already in effect.
        /// Not emitted on initialization, so listeners should read <see cref="Lives"/> once when connecting.
        /// </summary>
        /// <param name="current">Remaining lives after the change.</param>
        [Signal] public delegate void LivesChangedEventHandler(int current);

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

        /// <summary>Lives and one-hit health state. Its signals drive the death, respawn, lives, and game over handlers.</summary>
        [ExportGroup("Components")]
        [Export] private PlayerHealthComponent _health;

        /// <summary>Source of the movement speed. Halved while focus is held.</summary>
        [Export] private SpeedComponent _speed;

        /// <summary>Dead SE: played when the player dies, before the respawn delay.</summary>
        [ExportGroup("Audio")]
        [Export] private AudioStreamPlayer2D _deathSound;

        /// <summary>
        /// Seconds of invincibility granted after each respawn.
        /// </summary>
        [ExportGroup("Respawn")]
        [Export] private float _invincibilityDuration = 3f;

        /// <summary>
        /// Number of frames the shoot input stays active after the button is released (about 0.1 s at 60 fps).
        /// </summary>
        private const int ShootBufferFrames = 6;

        /// <summary>
        /// Duration, in seconds, of each visible or faded phase of the invincibility blink.
        /// </summary>
        private const float BlinkInterval = 0.08f;

        /// <summary>
        /// Opacity during the faded phase of the invincibility blink.
        /// </summary>
        private const float FadedAlpha = 0.35f;

        /// <summary>True while the player has no health.</summary>
        public bool IsDead => _health.IsDead;

        /// <summary>True while the player ignores damage after a respawn.</summary>
        public bool IsInvincible => _invincibilityTimeLeft > 0f;

        /// <summary>Remaining lives, including the one currently in play.</summary>
        public int Lives => _health.CurrentLives;

        /// <summary>Maximum number of lives.</summary>
        public int MaxLives => _health.MaxLives;

        /// <summary>
        /// Raw directional input, rebuilt every physics frame.
        /// </summary>
        private Vector2 _movementInput;

        /// <summary>
        /// Position the player had when the level started. Every respawn returns here.
        /// </summary>
        private Vector2 _spawnPosition;

        /// <summary>
        /// Seconds of invincibility remaining. Zero or less means the player can be hurt.
        /// </summary>
        private float _invincibilityTimeLeft;

        /// <summary>
        /// Frames remaining on the shoot buffer. Absorbs brief input gaps so shots are not skipped.
        /// </summary>
        private int _shootBuffer;

        public override void _Ready()
        {
            // Remembered once, so place the character at the desired start position in the level scene.
            _spawnPosition = GlobalPosition;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (IsDead) return;

            UpdateInvincibility(delta);
            HandleFocus(delta);
            HandleMovement();
            UpdatePresentation(_movementInput.X);
            HandleShooting();
        }

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

        /// <summary>
        /// Hides everything that represents the character (sprite, weapon, hitbox) when it dies.
        /// </summary>
        protected abstract void OnDeathPresentation();

        /// <summary>
        /// Shows the character again and resets its visuals to the neutral pose when it respawns.
        /// The position is already at the spawn point when this runs.
        /// </summary>
        protected abstract void OnRespawnPresentation();

        /// <summary>
        /// Updates the visuals every physics frame.
        /// </summary>
        /// <param name="horizontalInput">Horizontal input in the range -1 to 1, so the sprite can lean toward the movement.</param>
        protected abstract void UpdatePresentation(float horizontalInput);

        /// <summary>
        /// Applies the focus state to the weapon and hitbox.
        /// </summary>
        /// <param name="focused">True while the focus action is held.</param>
        /// <param name="delta">Physics frame time in seconds, for focus transitions.</param>
        protected abstract void ApplyFocus(bool focused, double delta);

        /// <summary>
        /// Asks the weapon to fire. Called every frame the shoot buffer is active.
        /// </summary>
        protected abstract void Fire();

        /// <summary>
        /// Applies the invincibility tint to every visual that should blink.
        /// </summary>
        /// <param name="tint">Color to apply. Alpha carries the blink.</param>
        protected abstract void ApplyTint(Color tint);

        /// <summary>
        /// Handles the health component's Death signal: stops the body, discards buffered shots,
        /// lets the character hide its visuals, plays the death sound, and emits <see cref="Died"/>.
        /// </summary>
        private void OnPlayerDied()
        {
            Velocity = Vector2.Zero;
            _shootBuffer = 0;
            OnDeathPresentation();
            _deathSound.Play();

            EmitSignal(SignalName.Died);
        }

        /// <summary>
        /// Handles the health component's LivesChanged signal and republishes it as <see cref="LivesChanged"/>.
        /// </summary>
        /// <param name="current">Remaining lives after the change.</param>
        private void OnLivesChanged(int current)
        {
            EmitSignal(SignalName.LivesChanged, current);
        }

        /// <summary>
        /// Handles the Respawned signal. <see cref="IsDead"/> is already false, so <c>_PhysicsProcess</c>
        /// resumes on its own next frame. Returns the player to the start position, lets the character
        /// reset its visuals, starts the invincibility window, and emits <see cref="Respawned"/>.
        /// </summary>
        private void OnRespawned()
        {
            GlobalPosition = _spawnPosition;
            OnRespawnPresentation();

            _invincibilityTimeLeft = _invincibilityDuration;

            EmitSignal(SignalName.Respawned);
        }

        /// <summary>
        /// Handles the GameOver signal. The entity stays permanently dead, so everything remains locked.
        /// </summary>
        private void OnGameOver()
        {
            EmitSignal(SignalName.GameOver);
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
        /// Halves the movement speed while the focus action is held and forwards the state to the character.
        /// </summary>
        /// <param name="delta">Physics frame time in seconds.</param>
        private void HandleFocus(double delta)
        {
            bool focused = Input.IsActionPressed("focus");
            _speed.SetSpeed(focused ? _speed.MaxSpeed / 2 : _speed.MaxSpeed);
            ApplyFocus(focused, delta);
        }

        /// <summary>
        /// Feeds the shoot buffer on input and drains it each frame, firing while it is active.
        /// </summary>
        private void HandleShooting()
        {
            if (Input.IsActionPressed("shoot"))
                _shootBuffer = ShootBufferFrames;

            if (_shootBuffer > 0)
            {
                Fire();
                _shootBuffer--;
            }
        }

        /// <summary>
        /// Counts down the invincibility window and blinks the visuals while it lasts.
        /// Full opacity is restored on the frame the window ends.
        /// </summary>
        /// <param name="delta">Physics frame time in seconds.</param>
        private void UpdateInvincibility(double delta)
        {
            if (_invincibilityTimeLeft <= 0f) return;

            _invincibilityTimeLeft -= (float)delta;

            bool faded = _invincibilityTimeLeft > 0f
                && Mathf.PosMod(_invincibilityTimeLeft, 2f * BlinkInterval) < BlinkInterval;

            ApplyTint(new Color(1f, 1f, 1f, faded ? FadedAlpha : 1f));
        }
    }
}