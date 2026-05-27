using Godot;

namespace TouhouFuujinroku.Components
{
	[GlobalClass]
	public partial class SpriteComponent : Node2D
	{
		// Node references (assigned via editor).
		[ExportGroup("Components")]
		[Export] private AnimatedSprite2D _sprite;

		// Animation name constants — must match the names defined in the AnimatedSprite2D resource.
		private const string DefaultAnimation = "default";
		private const string LeftAnimation = "left";
		private const string RightAnimation = "right";
		private const string LeftLoopAnimation = "left_loop";
		private const string RightLoopAnimation = "right_loop";

		// ------------------------------------- Godot overrides ------------------------------------

		public override void _Ready()
		{
			_sprite.AnimationFinished += OnAnimationFinished;
		}

		public override void _ExitTree()
		{
			_sprite.AnimationFinished -= OnAnimationFinished;
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
			_sprite.Play(DefaultAnimation);
		}

		private void PlayLeft()
		{
			// Already in the left cycle — let it finish naturally.
			if (_sprite.Animation == LeftAnimation || _sprite.Animation == LeftLoopAnimation) return;
			_sprite.Play(LeftAnimation);
		}

		private void PlayRight()
		{
			// Already in the right cycle — let it finish naturally.
			if (_sprite.Animation == RightAnimation || _sprite.Animation == RightLoopAnimation) return;
			_sprite.Play(RightAnimation);
		}

		private void OnAnimationFinished()
		{
			// Transition from the intro clip to its corresponding loop.
			if (_sprite.Animation == LeftAnimation) _sprite.Play(LeftLoopAnimation);
			else if (_sprite.Animation == RightAnimation) _sprite.Play(RightLoopAnimation);
		}
	}
}