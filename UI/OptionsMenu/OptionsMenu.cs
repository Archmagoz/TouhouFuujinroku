using Godot;
using TohouFuuujinoku.Global.Controllers;

namespace TohouFuuujinoku.UI
{
    public partial class OptionsMenu : Control
    {
        [ExportGroup("UI Elements")]
        [Export] private TextureButton _quitButton;

        // ---------------------------------- Godot overrides -----------------------------------

        public override void _Ready()
        {
            _quitButton.Pressed += OnQuitPressed;
        }

        public override void _ExitTree()
        {
            _quitButton.Pressed -= OnQuitPressed;
        }

        // ---------------------------------- Private helpers -----------------------------------

        private void OnQuitPressed()
        {
            SceneController.Instance.ChangeScene(SceneType.MainMenu);
        }
    }
}