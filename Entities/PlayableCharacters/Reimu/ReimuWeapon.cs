using Godot;
using Godot.Collections;

namespace TohouFuuujinoku.Entities.PlayableCharacters
{
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
		private const float RotationSpeed = Mathf.Tau;

		// Sprites (loaded in _ready) — order: 0 bottom-left, 1 top-left, 2 top-right, 3 bottom-right.
		private Array<Sprite2D> _sprites = new Array<Sprite2D>();

		// Runtime state.
		private Vector2[] _positions;
		private Vector2[] _normalOffsets;
		private Vector2[] _focusDeltas;
		private Vector2[] _offsets;

		// ------------------------------------- Godot overrides ------------------------------------

		public override void _Ready()
		{
			// Cache sprites and their editor offsets as the normal formation.
			foreach (Node child in GetChildren())
			{
				if (child is Sprite2D sprite)
					_sprites.Add(sprite);
			}

			_positions = new Vector2[_sprites.Count];
			_normalOffsets = new Vector2[_sprites.Count];
			_offsets = new Vector2[_sprites.Count];

			for (int i = 0; i < _sprites.Count; i++)
			{
				// Bake each sprite's editor position as its resting offset.
				_normalOffsets[i] = _sprites[i].Position;
				_offsets[i] = _normalOffsets[i];
				_positions[i] = _sprites[i].GlobalPosition;
			}

			// Per-sprite focus deltas — sprite order: 0 bottom-left, 1 top-left, 2 top-right, 3 bottom-right.
			// Side sprites shift inward; bottom sprites also shift forward.
			_focusDeltas = new Vector2[]
			{
				new Vector2( _focusInwardX, _focusForwardY), // 0: bottom-left  → right + forward
				new Vector2( _focusInwardX, 0f),             // 1: top-left     → right only
				new Vector2(-_focusInwardX, 0f),             // 2: top-right    → left only
				new Vector2(-_focusInwardX, _focusForwardY), // 3: bottom-right → left + forward
			};
		}

		public override void _Process(double delta)
		{
			float lagWeight = 1f - Mathf.Exp(-_followSpeed * (float)delta);

			for (int i = 0; i < _sprites.Count; i++)
			{
				Vector2 target = GlobalPosition + _offsets[i];

				_positions[i] = _positions[i].Lerp(target, lagWeight);
				_sprites[i].GlobalPosition = _positions[i];
				_sprites[i].Rotate(RotationSpeed * (float)delta);
			}
		}

		// ---------------------------------------- Public API --------------------------------------

		public void ToggleFocusMode(bool focused, double delta)
		{
			float weight = 1f - Mathf.Exp(-_focusTransitionSpeed * (float)delta);

			for (int i = 0; i < _sprites.Count; i++)
			{
				Vector2 targetOffset = focused ? _normalOffsets[i] + _focusDeltas[i] : _normalOffsets[i];
				_offsets[i] = _offsets[i].Lerp(targetOffset, weight);
			}
		}
	}
}