using Godot;

namespace TouhouFuujinroku.Entities.PlayableCharacters.Reimu
{
	[GlobalClass]
	public partial class ReimuAnimatedSprite : AnimatedSprite2D
	{
		// Animation name constants — must match the names defined in the AnimatedSprite2D resource.
		private const string DefaultAnimation = "default";
		private const string LeftAnimation = "left";
		private const string RightAnimation = "right";
		private const string LeftLoopAnimation = "left_loop";
		private const string RightLoopAnimation = "right_loop";

		// Public API -------------------------------------------------------------------------------------------

		public void UpdateSprite(float inputX)
		{
			if (inputX < 0) PlayLeft();
			else if (inputX > 0) PlayRight();
			else PlayDefault();
		}

		// Helpers ----------------------------------------------------------------------------------------------

		private void PlayDefault()
		{
			Play(DefaultAnimation);
		}

		private void PlayLeft()
		{
			// Already in the left cycle — let it finish naturally.
			if (Animation == LeftAnimation || Animation == LeftLoopAnimation) return;
			Play(LeftAnimation);
		}

		private void PlayRight()
		{
			// Already in the right cycle — let it finish naturally.
			if (Animation == RightAnimation || Animation == RightLoopAnimation) return;
			Play(RightAnimation);
		}

		private void OnAnimationFinished()
		{
			// Transition from the intro clip to its corresponding loop.
			if (Animation == LeftAnimation) Play(LeftLoopAnimation);
			else if (Animation == RightAnimation) Play(RightLoopAnimation);
		}
	}
}