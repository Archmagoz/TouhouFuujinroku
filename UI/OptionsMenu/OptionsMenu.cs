using TouhouFuujinroku.Global.Controllers;

namespace TouhouFuujinroku.UI
{
    public partial class OptionsMenu : MenuScreen
    {
        private static void OnQuitPressed() => SceneController.Instance.TransitionTo(Scene.MainMenu);
    }
}