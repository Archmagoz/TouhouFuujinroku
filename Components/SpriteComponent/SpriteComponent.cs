using Godot;

namespace TohouFuuujinoku.Components
{
	[GlobalClass]
	public partial class SpriteComponent : Node2D
	{
		// Node references (assigned via editor).
		[ExportGroup("Components")]
		[Export] private AnimatedSprite2D _sprite;

		// Base values (assigned via editor).
		[ExportGroup("Settings")]
		[Export] private StringName _defaultAnimation = "default";
		[Export] private StringName _leftAnimation = "left";
		[Export] private StringName _rightAnimation = "right";

		// Runtime state — managed internally by the component.
		private bool _looping;

		// ------------------------------------- Godot overrides ------------------------------------

		public override void _Ready()
		{
			_sprite.AnimationFinished += OnAnimationFinished;
		}

		// ---------------------------------------- Public API --------------------------------------

		public void UpdateSprite(float inputX)
		{
			if (inputX < 0) PlayLeft();
			else if (inputX > 0) PlayRight();
			else PlayDefault();
		}

		// ----------------------------------------- Helpers ----------------------------------------

		private void PlayDefault()
		{
			_sprite.Play(_defaultAnimation);
		}

		private void PlayLeft()
		{
			if (_sprite.Animation == _leftAnimation && !_looping) return;
			_sprite.Play(_leftAnimation);
		}

		private void PlayRight()
		{
			if (_sprite.Animation == _rightAnimation && !_looping) return;
			_sprite.Play(_rightAnimation);
		}

		private void OnAnimationFinished()
		{
			// After the first pass, loops from frame 4 onwards indefinitely.
			if (_sprite.Animation == _defaultAnimation) return;

			_looping = true;
			_sprite.Play(_sprite.Animation);
			_sprite.Frame = 4;
		}
	}
}