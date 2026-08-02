using Godot;

using TouhouFuujinroku.Global.Controllers;

namespace TouhouFuujinroku.UI
{
    public partial class OptionsMenu : Control
    {
        private void OnQuitPressed() => SceneController.Instance.TransitionTo(Scene.MainMenu);
    }
}