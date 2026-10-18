using Godot;

namespace TouhouFuujinroku.Entities.PlayableCharacters.Reimu
{
	/// <summary>
	/// Animated sprite for Reimu that selects animations from horizontal movement input.
	/// <para>
	/// Lateral movement is split into two clips: a non-looping intro (<c>left</c>, <c>right</c>) that chains
	/// into a looping clip (<c>left_loop</c>, <c>right_loop</c>) once it finishes. The intro clips must therefore
	/// be configured as non-looping in the SpriteFrames, otherwise their finished signal never fires.
	/// </para>
	/// </summary>
	[GlobalClass]
	public partial class ReimuAnimatedSprite : AnimatedSprite2D
	{
		#region Fields

		/// <summary>Idle animation, played while there is no horizontal input.</summary>
		private const string DefaultAnimation = "default";

		/// <summary>Intro clip of the leftward lean. Chains into <see cref="LeftLoopAnimation"/>.</summary>
		private const string LeftAnimation = "left";

		/// <summary>Intro clip of the rightward lean. Chains into <see cref="RightLoopAnimation"/>.</summary>
		private const string RightAnimation = "right";

		/// <summary>Looping clip held while moving left after the intro finishes.</summary>
		private const string LeftLoopAnimation = "left_loop";

		/// <summary>Looping clip held while moving right after the intro finishes.</summary>
		private const string RightLoopAnimation = "right_loop";

		/// <summary>Death animation, played once when the player dies.</summary>
		private const string DeathAnimation = "death";

		#endregion // Fields

		#region Public API

		/// <summary>
		/// Selects the animation that matches the current horizontal input.
		/// Safe to call every frame: a lean already in progress is never restarted.
		/// </summary>
		/// <param name="inputX">Horizontal input axis. Negative leans left, positive leans right, zero is idle.</param>
		public void UpdateSprite(float inputX)
		{
			if (inputX < 0) PlayLeft();
			else if (inputX > 0) PlayRight();
			else PlayDefault();
		}

		/// <summary>
		/// Plays the death animation. The owner is expected to stop calling
		/// <see cref="UpdateSprite"/> while dead so nothing overrides it.
		/// </summary>
		public void PlayDeath()
		{
			Play(DeathAnimation);
		}

		#endregion // Public API

		#region Event handlers

		/// <summary>
		/// Handles the <c>animation_finished</c> signal, connected in the editor.
		/// Transitions an intro clip into its corresponding loop.
		/// </summary>
		private void OnAnimationFinished()
		{
			if (Animation == LeftAnimation) Play(LeftLoopAnimation);
			else if (Animation == RightAnimation) Play(RightLoopAnimation);
		}

		#endregion // Event handlers

		#region Helpers

		/// <summary>
		/// Plays the idle animation.
		/// </summary>
		private void PlayDefault()
		{
			Play(DefaultAnimation);
		}

		/// <summary>
		/// Plays the leftward lean, unless it is already in progress.
		/// </summary>
		private void PlayLeft()
		{
			// Already in the left cycle — let it finish naturally.
			if (Animation == LeftAnimation || Animation == LeftLoopAnimation) return;
			Play(LeftAnimation);
		}

		/// <summary>
		/// Plays the rightward lean, unless it is already in progress.
		/// </summary>
		private void PlayRight()
		{
			// Already in the right cycle — let it finish naturally.
			if (Animation == RightAnimation || Animation == RightLoopAnimation) return;
			Play(RightAnimation);
		}

		#endregion // Helpers
	}
}