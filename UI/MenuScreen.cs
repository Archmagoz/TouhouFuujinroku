using Godot;

using TouhouFuujinroku.Global.Controllers;

namespace TouhouFuujinroku.UI
{
    public abstract partial class MenuScreen : Control
    {
        private static void OnButtonFocusEntered() => MenuAudioController.Instance.PlayMenuSelectSFX();

        private void OnButtonMouseEntered()
        {
            // Workaround for Godot bug: focus_entered doesn't fire on mouse hover.
            GetViewport().GuiGetHoveredControl()?.GrabFocus();
        }

        private void OnButtonMouseExited()
        {
            GetViewport().GuiGetFocusOwner()?.ReleaseFocus();
        }
    }
}