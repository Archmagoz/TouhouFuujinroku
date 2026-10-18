using TouhouFuujinroku.Global.Controllers;

namespace TouhouFuujinroku.UI
{
	/// <summary>
	/// Main menu screen. Maps each button to its scene transition or to quitting the game.
	/// <para>
	/// The handlers are connected in the editor to the <c>pressed</c> signal of their buttons.
	/// Focus animation and selection sounds are inherited from <see cref="MenuScreen"/>.
	/// </para>
	/// </summary>
	public partial class MainMenu : MenuScreen
	{
		#region Event handlers

		/// <summary>
		/// Handles the start button: stops the menu music and transitions to the gameplay level.
		/// </summary>
		private static void OnGameStartPressed()
		{
			MenuAudioController.Instance.StopMusic();
			SceneController.Instance.TransitionTo(Scene.DebugLevel);
		}

		/// <summary>
		/// Handles the options button: transitions to the options menu. The menu music keeps playing.
		/// </summary>
		private static void OnOptionsPressed() => SceneController.Instance.TransitionTo(Scene.OptionsMenu);

		/// <summary>
		/// Handles the quit button: fades out and closes the game.
		/// </summary>
		private static void OnQuitPressed() => SceneController.Instance.QuitGame();

		#endregion // Event handlers
	}
}