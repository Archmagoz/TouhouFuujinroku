using TouhouFuujinroku.Global.Controllers;

namespace TouhouFuujinroku.UI
{
	public partial class MainMenu : MenuScreen
	{
		private static void OnGameStartPressed()
		{
			MenuAudioController.Instance.StopMusic();
			SceneController.Instance.TransitionTo(Scene.DebugLevel);
		}
		private static void OnOptionsPressed() => SceneController.Instance.TransitionTo(Scene.OptionsMenu);
		private static void OnQuitPressed() => SceneController.Instance.QuitGame();
	}
}