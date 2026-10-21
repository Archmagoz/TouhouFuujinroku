using Godot;
using Godot.Collections;

using System.Collections.Generic;

using TouhouFuujinroku.Entities.Projectile;

namespace TouhouFuujinroku.Entities.PlayableCharacters.Reimu
{
	/// <summary>
	/// Reimu's weapon: four orbiting option sprites that trail the player, rotate continuously,
	/// fire straight-up projectiles, and shift formation when focus is held.
	/// <para>
	/// Each sprite is a child of this node and owns one or more <see cref="Marker2D"/> children that act as
	/// fire origins. Because markers are children of the rotating sprites, their global positions already
	/// reflect the current formation every frame.
	/// </para>
	/// <para>
	/// Sprite order is significant and is defined by child order in the scene:
	/// 0 bottom-left, 1 top-left, 2 top-right, 3 bottom-right.
	/// </para>
	/// </summary>
	[GlobalClass]
	public partial class ReimuWeapon : Node2D
	{
		#region Fields

		/// <summary>
		/// How fast each sprite chases its target position. Higher is snappier.
		/// </summary>
		[ExportGroup("Configuration")]
		[ExportSubgroup("Formation")]
		[Export] private float _followSpeed = 64f;

		/// <summary>
		/// How fast the offsets transition between the normal and focus formations.
		/// </summary>
		[Export] private float _focusTransitionSpeed = 10f;

		/// <summary>
		/// Horizontal distance, in pixels, the side sprites shift inward on focus.
		/// </summary>
		[Export] private float _focusInwardX = 10f;

		/// <summary>
		/// Vertical distance, in pixels, the bottom sprites shift forward on focus. Negative is up.
		/// </summary>
		[Export] private float _focusForwardY = -15f;

		/// <summary>
		/// Seconds between shots.
		/// </summary>
		[ExportSubgroup("Firing")]
		[Export] private float _fireRate = 0.1f;

		/// <summary>
		/// Bullet prefab rented from the projectile pool on each shot.
		/// </summary>
		[Export] private PackedScene _bulletPrefab;

		/// <summary>
		/// Audio played on each shot. Optional, may be null.
		/// </summary>
		[Export] private AudioStreamPlayer2D _shotSound;

		/// <summary>
		/// Sprite rotation speed in radians per second: one full clockwise rotation per second.
		/// </summary>
		private static readonly float RotationSpeed = Mathf.Tau;

		/// <summary>
		/// Option sprites, collected in <c>_Ready</c>. Order: 0 bottom-left, 1 top-left, 2 top-right, 3 bottom-right.
		/// </summary>
		private Array<Sprite2D> _sprites = [];

		/// <summary>
		/// Fire-origin markers, cached once in <c>_Ready</c>. They rotate with their parent sprite.
		/// </summary>
		private Array<Marker2D> _fireOrigins = [];

		/// <summary>
		/// Smoothed world position of each sprite, chasing <c>GlobalPosition + offset</c> to create the trailing effect.
		/// </summary>
		private Vector2[] _positions = [];

		/// <summary>
		/// Resting offset of each sprite relative to the weapon, baked from its editor position.
		/// </summary>
		private Vector2[] _normalOffsets = [];

		/// <summary>
		/// Offset change applied to each sprite while focused, added on top of its normal offset.
		/// </summary>
		private Vector2[] _focusDeltas = [];

		/// <summary>
		/// Current offset of each sprite, interpolated between the normal and focus formations.
		/// </summary>
		private Vector2[] _offsets = [];

		/// <summary>
		/// Seconds remaining before the next shot is allowed.
		/// </summary>
		private float _cooldown;

		#endregion // Fields

		#region Godot overrides

		public override void _Ready()
		{
			// Cache sprites and their editor offsets as the normal formation.
			foreach (var child in GetChildren())
				if (child is Sprite2D sprite)
					_sprites.Add(sprite);

			int count = _sprites.Count;
			_positions = new Vector2[count];
			_normalOffsets = new Vector2[count];
			_offsets = new Vector2[count];

			for (int i = 0; i < count; i++)
			{
				// Bake each sprite's editor position as its resting offset.
				_normalOffsets[i] = _sprites[i].Position;
				_offsets[i] = _normalOffsets[i];
				_positions[i] = _sprites[i].GlobalPosition;

				// Cache every Marker2D child found under this sprite as a fire origin.
				foreach (var child in _sprites[i].GetChildren())
					if (child is Marker2D marker)
						_fireOrigins.Add(marker);
			}

			// Per-sprite focus deltas — sprite order: 0 bottom-left, 1 top-left, 2 top-right, 3 bottom-right.
			// Side sprites shift inward; bottom sprites also shift forward.
			_focusDeltas =
			[
				new Vector2( _focusInwardX, _focusForwardY), // 0: bottom-left  → right + forward
				new Vector2( _focusInwardX, 0f),             // 1: top-left     → right only
				new Vector2(-_focusInwardX, 0f),             // 2: top-right    → left only
				new Vector2(-_focusInwardX, _focusForwardY), // 3: bottom-right → left + forward
			];
		}

		public override void _PhysicsProcess(double delta)
		{
			float f = (float)delta;

			if (_cooldown > 0f) _cooldown -= f;

			float lagWeight = ExponentialLerp(_followSpeed, f);

			for (int i = 0; i < _sprites.Count; i++)
			{
				_positions[i] = _positions[i].Lerp(GlobalPosition + _offsets[i], lagWeight);
				_sprites[i].GlobalPosition = _positions[i];
				_sprites[i].Rotate(RotationSpeed * f);
			}
		}

		#endregion // Godot overrides

		#region Public API

		/// <summary>
		/// Smoothly shifts the sprite offsets toward the focused or normal formation.
		/// Call every frame while the focus state may be changing.
		/// </summary>
		/// <param name="focused">True to move toward the focus formation, false to return to the normal one.</param>
		/// <param name="delta">Physics frame time in seconds.</param>
		public void ToggleFocusMode(bool focused, double delta)
		{
			float weight = ExponentialLerp(_focusTransitionSpeed, (float)delta);

			for (int i = 0; i < _sprites.Count; i++)
			{
				Vector2 targetOffset = focused ? _normalOffsets[i] + _focusDeltas[i] : _normalOffsets[i];
				_offsets[i] = _offsets[i].Lerp(targetOffset, weight);
			}
		}

		/// <summary>
		/// Attempts to fire one projectile from every sprite origin.
		/// Silently does nothing while on cooldown or if no bullet prefab is assigned.
		/// </summary>
		public void TryFire()
		{
			if (_cooldown > 0f || _bulletPrefab == null) return;

			_cooldown = _fireRate;

			// Angle fixed at -π/2 — straight up, matching Touhou's vertical scrolling convention.
			foreach (var origin in GetFireOrigins())
				ProjectilePool.Instance.Rent(_bulletPrefab, origin, angle: -Mathf.Pi / 2f);

			_shotSound?.Play();
		}

		/// <summary>
		/// Instantly moves every option sprite to its target position, skipping the trailing effect.
		/// Call it after teleporting the owner, so the formation does not fly across the screen to catch up.
		/// </summary>
		public void SnapFormation()
		{
			for (int i = 0; i < _sprites.Count; i++)
			{
				_positions[i] = GlobalPosition + _offsets[i];
				_sprites[i].GlobalPosition = _positions[i];
			}
		}

		#endregion // Public API

		#region Helpers

		/// <summary>
		/// Enumerates the current global position of every cached fire-origin marker.
		/// </summary>
		/// <returns>One position per marker, evaluated at call time so it reflects the current formation.</returns>
		private IEnumerable<Vector2> GetFireOrigins()
		{
			foreach (var marker in _fireOrigins)
				yield return marker.GlobalPosition;
		}

		/// <summary>
		/// Computes a frame-rate-independent interpolation weight via exponential decay: 1 - e^(-speed * delta).
		/// </summary>
		/// <param name="speed">Decay rate. Higher values converge faster.</param>
		/// <param name="delta">Frame time in seconds.</param>
		/// <returns>A weight between 0 and 1 to pass to <c>Lerp</c>.</returns>
		private static float ExponentialLerp(float speed, float delta) =>
			1f - Mathf.Exp(-speed * delta);

		#endregion // Helpers
	}
}