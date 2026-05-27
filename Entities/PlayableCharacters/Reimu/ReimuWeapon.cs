using Godot;
using Godot.Collections;
using System.Collections.Generic;

namespace TohouFuuujinoku.Entities.PlayableCharacters
{
	[GlobalClass]
	public partial class ReimuWeapon : Node2D
	{
		[ExportGroup("Configuration")]
		// How fast each sprite chases its target — higher = snappier.
		[Export] private float _followSpeed = 64f;

		// How fast offsets transition between normal and focus formation.
		[Export] private float _focusTransitionSpeed = 10f;

		// How much the side sprites shift inward on focus.
		[Export] private float _focusInwardX = 10f;

		// How much the bottom sprites shift forward on focus.
		[Export] private float _focusForwardY = -15f;

		// One full clockwise rotation per second.
		private static readonly float RotationSpeed = Mathf.Tau;

		// Sprites (loaded in _Ready()) — order: 0 bottom-left, 1 top-left, 2 top-right, 3 bottom-right.
		private Array<Sprite2D> _sprites = [];

		// Per-sprite positions and offsets, sized in _Ready() after sprites are loaded.
		private Vector2[] _positions = [];
		private Vector2[] _normalOffsets = [];
		private Vector2[] _focusDeltas = [];
		private Vector2[] _offsets = [];

		// Cached reference to the fire controller — located as a child in _Ready().
		// Kept internal to ReimuWeapon; callers interact via TryFire() and ToggleFocusMode().
		private ReimuWeaponFireController _fireController;

		// ---------------------------------- Godot overrides -----------------------------------

		public override void _Ready()
		{
			// Locate the fire controller among direct children — decouples from node name.
			foreach (var child in GetChildren())
				if (child is ReimuWeaponFireController controller)
					_fireController = controller;

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
			// Cast once; reused for both lag weight and rotation step.
			float f = (float)delta;
			float lagWeight = ExponentialLerp(_followSpeed, f);

			for (int i = 0; i < _sprites.Count; i++)
			{
				_positions[i] = _positions[i].Lerp(GlobalPosition + _offsets[i], lagWeight);
				_sprites[i].GlobalPosition = _positions[i];
				_sprites[i].Rotate(RotationSpeed * f);
			}
		}

		// ------------------------------------- Public API -------------------------------------

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

		// Delegates the fire request to the child controller — Reimu calls this, not TryFire directly.
		// Centralizes all weapon interaction through ReimuWeapon as the single public interface.
		public void TryFire() => _fireController?.TryFire();

		// Yields the GlobalPosition of every Marker2D child found across all weapon sprites.
		// Markers rotate with their parent sprite, so positions already reflect current formation.
		public IEnumerable<Vector2> GetFireOrigins()
		{
			foreach (var sprite in _sprites)
				foreach (var child in sprite.GetChildren())
					if (child is Marker2D marker)
						yield return marker.GlobalPosition;
		}

		// ---------------------------------- Private helpers -----------------------------------

		// Frame-rate-independent lerp weight via exponential decay — 1 - e^(-speed * delta).
		private static float ExponentialLerp(float speed, float delta) =>
			1f - Mathf.Exp(-speed * delta);
	}
}