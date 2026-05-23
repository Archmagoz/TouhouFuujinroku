using Godot;
using TohouFuuujinoku.Global.Controllers;

namespace TohouFuuujinoku.UI
{
	public partial class MainMenu : Control
	{
		[ExportGroup("UI Elements")]
		[Export] private TextureButton _gameStartButton;
		[Export] private TextureButton _optionsButton;
		[Export] private TextureButton _quitButton;

		// ---------------------------------- Godot overrides -----------------------------------

		public override void _Ready()
		{
			_gameStartButton.Pressed += OnGameStartPressed;
			_optionsButton.Pressed += OnOptionsPressed;
			_quitButton.Pressed += OnQuitPressed;
		}

		public override void _ExitTree()
		{
			_gameStartButton.Pressed -= OnGameStartPressed;
			_optionsButton.Pressed -= OnOptionsPressed;
			_quitButton.Pressed -= OnQuitPressed;
		}

		// ---------------------------------- Private helpers -----------------------------------

		private void OnGameStartPressed()
		{
			SceneController.Instance.ChangeScene(SceneType.DebugLevel);
		}

		private void OnOptionsPressed()
		{
			SceneController.Instance.ChangeScene(SceneType.OptionsMenu);
		}

		private void OnQuitPressed()
		{
			GetTree().Quit();
		}
	}
}