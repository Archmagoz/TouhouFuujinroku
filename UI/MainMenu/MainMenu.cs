using Godot;

using TouhouFuujinroku.Global.Controllers;

namespace TouhouFuujinroku.UI
{
	public partial class MainMenu : Control
	{
		private void OnGameStartPressed() => SceneController.Instance.TransitionTo(Scene.DebugLevel);
		private void OnOptionsPressed() => SceneController.Instance.TransitionTo(Scene.OptionsMenu);
		private void OnQuitPressed() => SceneController.Instance.QuitGame();
	}
}