using Godot;
using Godot.Collections;
using System.Collections.Generic;
using TouhouFuujinroku.Global.Controllers;

namespace TouhouFuujinroku.Entities.PlayableCharacters
{
	[GlobalClass]
	public partial class ReimuWeapon : Node2D
	{
		// ----------------------------------- Configuration ------------------------------------

		[ExportGroup("Configuration")]
		// How fast each sprite chases its target — higher = snappier.
		[Export] private float _followSpeed = 64f;

		// How fast offsets transition between normal and focus formation.
		[Export] private float _focusTransitionSpeed = 10f;

		// How much the side sprites shift inward on focus.
		[Export] private float _focusInwardX = 10f;

		// How much the bottom sprites shift forward on focus.
		[Export] private float _focusForwardY = -15f;

		// Bullet prefab instantiated on each salvo.
		[Export] private PackedScene _bulletPrefab;

		// Seconds between salvos.
		[Export] private float _fireRate = 0.1f;

		// ------------------------------------- Components ------------------------------------

		[ExportGroup("Components")]
		[Export] private AudioStreamPlayer2D _shotSound;

		// -------------------------------------- State ----------------------------------------

		// One full clockwise rotation per second.
		private static readonly float RotationSpeed = Mathf.Tau;

		// Sprites (loaded in _Ready()) — order: 0 bottom-left, 1 top-left, 2 top-right, 3 bottom-right.
		private Array<Sprite2D> _sprites = [];

		// Per-sprite positions and offsets, sized in _Ready() after sprites are loaded.
		private Vector2[] _positions = [];
		private Vector2[] _normalOffsets = [];
		private Vector2[] _focusDeltas = [];
		private Vector2[] _offsets = [];

		// Seconds remaining before the next shot is allowed.
		private float _cooldown;

		// ---------------------------------- Godot overrides ----------------------------------

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

		public override void _Process(double delta)
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

		// --------------------------------------- Public API ----------------------------------

		// Smoothly shifts sprite offsets toward the focused or normal formation.
		// Call every frame while the focus state may be changing.
		public void ToggleFocusMode(bool focused, double delta)
		{
			float weight = ExponentialLerp(_focusTransitionSpeed, (float)delta);

			for (int i = 0; i < _sprites.Count; i++)
			{
				Vector2 targetOffset = focused ? _normalOffsets[i] + _focusDeltas[i] : _normalOffsets[i];
				_offsets[i] = _offsets[i].Lerp(targetOffset, weight);
			}
		}

		// Attempts to fire from all sprite origins; silently no-ops while on cooldown.
		public void TryFire()
		{
			if (_cooldown > 0f || _bulletPrefab == null) return;

			_cooldown = _fireRate;

			// Angle fixed at -π/2 — straight up, matching Touhou's vertical scrolling convention.
			foreach (var origin in GetFireOrigins())
				ProjectilePool.Instance.Rent(_bulletPrefab, origin, angle: -Mathf.Pi / 2f);

			_shotSound?.Play();
		}

		// ---------------------------------- Private helpers ----------------------------------

		// Yields the GlobalPosition of every Marker2D child found across all weapon sprites.
		// Markers rotate with their parent sprite, so positions already reflect current formation.
		private IEnumerable<Vector2> GetFireOrigins()
		{
			foreach (var sprite in _sprites)
				foreach (var child in sprite.GetChildren())
					if (child is Marker2D marker)
						yield return marker.GlobalPosition;
		}

		// Frame-rate-independent lerp weight via exponential decay — 1 - e^(-speed * delta).
		private static float ExponentialLerp(float speed, float delta) =>
			1f - Mathf.Exp(-speed * delta);
	}
}