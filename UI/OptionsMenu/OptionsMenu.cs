using TouhouFuujinroku.Global.Controllers;

namespace TouhouFuujinroku.UI
{
    /// <summary>
    /// Options menu screen. Currently only provides the way back to the main menu.
    /// <para>
    /// The handler is connected in the editor to the <c>pressed</c> signal of its button.
    /// Focus animation and selection sounds are inherited from <see cref="MenuScreen"/>.
    /// </para>
    /// </summary>
    public partial class OptionsMenu : MenuScreen
    {
        #region Event handlers

        /// <summary>
        /// Handles the exit button: transitions back to the main menu.
        /// Despite its name, it does not quit the game.
        /// </summary>
        private static void OnQuitPressed() => SceneController.Instance.TransitionTo(Scene.MainMenu);

        #endregion // Event handlers
    }
}