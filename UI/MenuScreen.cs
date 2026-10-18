using Godot;

using TouhouFuujinroku.Global.Controllers;

namespace TouhouFuujinroku.UI
{
    public abstract partial class MenuScreen : Control
    {
        #region Constants

        // Focus animation tuning. The stretch phase exaggerates the horizontal axis while squashing
        // the vertical one (squash & stretch), then settles into a slightly enlarged resting scale.
        private static readonly Vector2 StretchPeakScale = new(1.18f, 0.9f);
        private static readonly Vector2 FocusedRestScale = new(1.06f, 1.06f);

        private const float StretchDuration = 0.06f; // Seconds to reach the stretch peak.
        private const float SettleDuration = 0.14f;  // Seconds to settle into the focused resting scale.
        private const float UnfocusDuration = 0.1f;  // Seconds to return to the default scale.

        // Metadata key used to store the running tween on each button. Storing it on the node
        // avoids a separate dictionary and needs no cleanup when the button is freed.
        private const string TweenMetaKey = "focus_tween";

        #endregion // Constants

        #region Fields

        // Button currently displaying the focused visual state. Signals connected through the
        // editor carry no arguments, so this field is how the handlers know which button to animate.
        private BaseButton _focusedButton;

        #endregion // Fields

        #region Event handlers

        // Connect these methods in the editor to each button's signals:
        // focus_entered, focus_exited, mouse_entered and mouse_exited.

        private void OnButtonFocusEntered()
        {
            // At focus_entered time the viewport already reports the new focus owner.
            if (GetViewport().GuiGetFocusOwner() is not BaseButton button)
                return;

            // Focus moved directly from another button: reset the previous one right away,
            // so two buttons are never displayed as focused at the same time.
            if (_focusedButton != null && _focusedButton != button)
                AnimateFocus(_focusedButton, focused: false);

            _focusedButton = button;

            MenuAudioController.Instance.PlayMenuSelectSFX();
            AnimateFocus(button, focused: true);
        }

        private void OnButtonFocusExited()
        {
            // The focus owner is unreliable while focus_exited is being emitted (it may still be
            // the old control, the new one, or null). Deferring the check to the end of the frame
            // lets focus settle first.
            Callable.From(ClearFocusedButtonIfLostFocus).CallDeferred();
        }

        private void OnButtonMouseEntered()
        {
            // Workaround for Godot bug: focus_entered doesn't fire on mouse hover.
            // Grabbing focus manually keeps mouse and keyboard/gamepad navigation in sync.
            if (GetHoveredButton() is BaseButton button)
                button.GrabFocus();
        }

        private void OnButtonMouseExited()
        {
            if (_focusedButton == null)
                return;

            // Releases focus only if the mouse is no longer over the focused button. If keyboard or
            // gamepad navigation already moved focus elsewhere, this prevents releasing the new owner.
            if (!_focusedButton.GetGlobalRect().HasPoint(GetGlobalMousePosition()))
                _focusedButton.ReleaseFocus();
        }

        #endregion // Event handlers

        #region Focus tracking

        private void ClearFocusedButtonIfLostFocus()
        {
            if (_focusedButton == null || !IsInstanceValid(_focusedButton) || _focusedButton.HasFocus())
                return;

            AnimateFocus(_focusedButton, focused: false);
            _focusedButton = null;
        }

        private BaseButton GetHoveredButton() => GetViewport().GuiGetHoveredControl() as BaseButton;

        #endregion // Focus tracking

        #region Animation

        private static void AnimateFocus(BaseButton button, bool focused)
        {
            KillTween(button);

            // Scales from the button center instead of the top-left corner. Updated on every
            // animation because the button size may have changed since the last one.
            button.PivotOffset = button.Size / 2f;

            // Binds the tween to the button so it stops automatically if the button is freed.
            Tween tween = button.CreateTween();
            button.SetMeta(TweenMetaKey, tween);

            if (focused)
            {
                tween.TweenProperty(button, "scale", StretchPeakScale, StretchDuration)
                     .SetTrans(Tween.TransitionType.Quad)
                     .SetEase(Tween.EaseType.Out);

                // Back easing overshoots slightly before settling, giving the small "bounce".
                tween.TweenProperty(button, "scale", FocusedRestScale, SettleDuration)
                     .SetTrans(Tween.TransitionType.Back)
                     .SetEase(Tween.EaseType.Out);
            }
            else
            {
                tween.TweenProperty(button, "scale", Vector2.One, UnfocusDuration)
                     .SetTrans(Tween.TransitionType.Quad)
                     .SetEase(Tween.EaseType.Out);
            }
        }

        private static void KillTween(BaseButton button)
        {
            // Cancels the previous tween so rapid focus changes never leave two tweens
            // fighting over the same "scale" property.
            if (!button.HasMeta(TweenMetaKey))
                return;

            Tween tween = button.GetMeta(TweenMetaKey).As<Tween>();
            if (tween != null && tween.IsValid())
                tween.Kill();
        }

        #endregion // Animation
    }
}