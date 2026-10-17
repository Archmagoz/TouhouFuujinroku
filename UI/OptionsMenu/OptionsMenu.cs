using Godot;

using TouhouFuujinroku.Global.Controllers;

namespace TouhouFuujinroku.UI
{
    public partial class OptionsMenu : Control
    {
        #region Fields

        private AudioStreamPlayer2D _selectSfx;

        #endregion // Fields

        #region Godot overrides

        public override void _Ready()
        {
            _selectSfx = GetNode<AudioStreamPlayer2D>("SelectSFX");
        }

        #endregion // Godot overrides

        #region Signal handlers

        private void OnButtonFocusEntered()
        {
            _selectSfx.Play();
        }

        private void OnButtonMouseEntered()
        {

            // This is a workaround for a Godot bug where the button's "focus_entered" 
            // signal doesn't fire when the mouse enters the button.
            GetViewport().GuiGetHoveredControl()?.GrabFocus();
        }

        private void OnButtonMouseExited()
        {
            GetViewport().GuiGetFocusOwner()?.ReleaseFocus();
        }

        #endregion // Signal handlers

        #region Helpers

        private static void OnQuitPressed() => SceneController.Instance.TransitionTo(Scene.MainMenu);

        #endregion // Helpers
    }
}