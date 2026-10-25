using Godot;

using System.Collections.Generic;

using TouhouFuujinroku.Global.Controllers;

namespace TouhouFuujinroku.UI.Menu
{
    /// <summary>
    /// Base class for menu screens. Provides the shared button focus behavior: a squash and stretch
    /// animation, a tint based focused look, a selection sound, and synchronization between mouse hover
    /// and keyboard or gamepad focus.
    /// <para>
    /// Button textures are authored in their selected variant. The unfocused look is produced by darkening the
    /// button with <see cref="UnfocusedTint"/> through <c>SelfModulate</c>, and the focused look is the untouched
    /// texture (<see cref="FocusedTint"/>). The scenes are expected to already store the unfocused tint on every
    /// button, so the resting state is visible in the editor and no initial tint is applied by code.
    /// </para>
    /// <para>
    /// Every <see cref="TextureButton"/> found under the screen is linked automatically when the screen
    /// enters the scene tree and unlinked when it exits, so no editor signal connections are needed
    /// for the focus behavior. Buttons added at runtime can be linked with <see cref="RegisterButton"/>.
    /// </para>
    /// </summary>
    public abstract partial class MenuScreen : Control
    {
        #region Constants

        // Focus animation tuning. The stretch phase exaggerates the horizontal axis while squashing
        // the vertical one (squash & stretch), then settles into a slightly enlarged resting scale.

        /// <summary>Scale at the peak of the stretch phase: wider and flatter than the resting scale.</summary>
        private static readonly Vector2 StretchPeakScale = new(1.18f, 0.9f);

        /// <summary>Slightly enlarged scale a focused button settles into.</summary>
        private static readonly Vector2 FocusedRestScale = new(1.06f, 1.06f);

        /// <summary>
        /// Multiplicative tint of an unfocused button (#333333). Each channel scales the texture color:
        /// 1 keeps it unchanged, 0 turns it fully black. Must match the value stored in the scenes.
        /// </summary>
        private static readonly Color UnfocusedTint = new(0.3f, 0.3f, 0.3f);

        /// <summary>Tint of a focused button: white leaves the texture colors unchanged.</summary>
        private static readonly Color FocusedTint = Colors.White;

        /// <summary>Seconds to reach the stretch peak.</summary>
        private const float StretchDuration = 0.06f;

        /// <summary>Seconds to settle into the focused resting scale.</summary>
        private const float SettleDuration = 0.14f;

        /// <summary>Seconds to return to the default scale and tint.</summary>
        private const float UnfocusDuration = 0.1f;

        /// <summary>
        /// Metadata key used to store the running tween on each button. Storing it on the node
        /// avoids a separate dictionary and needs no cleanup when the button is freed.
        /// </summary>
        private const string TweenMetaKey = "focus_tween";

        #endregion // Constants

        #region Fields

        /// <summary>
        /// Button currently displaying the focused visual state. The signal handlers take no arguments,
        /// so this field is how they know which button to animate.
        /// </summary>
        private BaseButton _focusedButton;

        /// <summary>
        /// Buttons currently linked to the focus behavior. Kept so they can be unlinked symmetrically
        /// on exit, without depending on a new tree search.
        /// </summary>
        private readonly List<TextureButton> _registeredButtons = [];

        #endregion // Fields

        #region Godot overrides

        /// <summary>
        /// Links every <see cref="TextureButton"/> under this screen to the focus behavior.
        /// Runs before <c>_Ready</c>, so derived screens can override <c>_Ready</c> without calling the base.
        /// Descendants already exist in the tree at this point, even though they are not ready yet.
        /// </summary>
        public override void _EnterTree()
        {
            // owned: false also finds buttons inside instanced sub-scenes, which are not owned by this scene.
            foreach (Node node in FindChildren("*", nameof(TextureButton), recursive: true, owned: false))
            {
                if (node is TextureButton button)
                    RegisterButton(button);
            }
        }

        /// <summary>
        /// Unlinks every registered button and drops any focus state still being tracked.
        /// </summary>
        public override void _ExitTree()
        {
            // Iterates backwards because UnregisterButton removes entries from the list.
            for (int i = _registeredButtons.Count - 1; i >= 0; i--)
                UnregisterButton(_registeredButtons[i]);

            _focusedButton = null;
        }

        #endregion // Godot overrides

        #region Registration

        /// <summary>
        /// Links a button to the focus behavior. Called automatically for the buttons present when the
        /// screen enters the tree; call it manually for buttons created at runtime, which should also start
        /// with <see cref="UnfocusedTint"/> as their <c>SelfModulate</c>.
        /// Registering the same button twice has no effect.
        /// </summary>
        /// <param name="button">Button to link.</param>
        protected void RegisterButton(TextureButton button)
        {
            if (_registeredButtons.Contains(button))
                return;

            button.FocusEntered += OnButtonFocusEntered;
            button.FocusExited += OnButtonFocusExited;
            button.MouseEntered += OnButtonMouseEntered;

            _registeredButtons.Add(button);
        }

        /// <summary>
        /// Unlinks a button from the focus behavior and restores its default scale and unfocused tint.
        /// Tolerates the button having been freed in the meantime.
        /// </summary>
        /// <param name="button">Button to unlink.</param>
        protected void UnregisterButton(TextureButton button)
        {
            if (!_registeredButtons.Remove(button))
                return;

            if (!IsInstanceValid(button))
                return;

            button.FocusEntered -= OnButtonFocusEntered;
            button.FocusExited -= OnButtonFocusExited;
            button.MouseEntered -= OnButtonMouseEntered;

            // Stops any running tween so a button that outlives the screen is not left scaled or lit up.
            KillTween(button);
            button.Scale = Vector2.One;
            button.SelfModulate = UnfocusedTint;
        }

        #endregion // Registration

        #region Event handlers

        /// <summary>
        /// Handles a button's <c>focus_entered</c> signal: resets the previously focused button,
        /// plays the selection sound, and animates the new button to its focused state.
        /// </summary>
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

        /// <summary>
        /// Handles a button's <c>focus_exited</c> signal. The unfocus is evaluated at the end of the frame,
        /// once focus has settled.
        /// </summary>
        private void OnButtonFocusExited()
        {
            // The focus owner is unreliable while focus_exited is being emitted (it may still be
            // the old control, the new one, or null). Deferring the check to the end of the frame
            // lets focus settle first.
            Callable.From(ClearFocusedButtonIfLostFocus).CallDeferred();
        }

        /// <summary>
        /// Handles a button's <c>mouse_entered</c> signal by grabbing focus, keeping mouse and
        /// keyboard or gamepad navigation in sync.
        /// </summary>
        private void OnButtonMouseEntered()
        {
            // Workaround for Godot bug: focus_entered doesn't fire on mouse hover.
            // Grabbing focus manually keeps mouse and keyboard/gamepad navigation in sync.
            if (GetHoveredButton() is BaseButton button)
                button.GrabFocus();
        }

        #endregion // Event handlers

        #region Focus tracking

        /// <summary>
        /// Resets the tracked button to its unfocused state if it no longer owns focus.
        /// Runs deferred, so it also tolerates the button having been freed in the meantime.
        /// </summary>
        private void ClearFocusedButtonIfLostFocus()
        {
            if (_focusedButton == null || !IsInstanceValid(_focusedButton) || _focusedButton.HasFocus())
                return;

            AnimateFocus(_focusedButton, focused: false);
            _focusedButton = null;
        }

        /// <summary>
        /// Gets the control currently under the mouse, if it is a button.
        /// </summary>
        /// <returns>The hovered <see cref="BaseButton"/>, or null if the hovered control is not a button.</returns>
        private BaseButton GetHoveredButton() => GetViewport().GuiGetHoveredControl() as BaseButton;

        #endregion // Focus tracking

        #region Animation

        /// <summary>
        /// Animates a button to its focused or unfocused look, cancelling any animation already running on it.
        /// The tint is applied immediately, before the animation starts, so the button is already lit (or dimmed)
        /// on the first frame of the scale animation. Focusing then plays a stretch phase followed by a settle with a
        /// slight overshoot. Unfocusing returns to the default scale.
        /// </summary>
        /// <param name="button">Button to animate.</param>
        /// <param name="focused">True to play the focus animation, false to return to the resting look.</param>
        private static void AnimateFocus(BaseButton button, bool focused)
        {
            KillTween(button);

            // The tint is not tweened: it changes instantly, before the scale animation begins.
            button.SelfModulate = focused ? FocusedTint : UnfocusedTint;

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

        /// <summary>
        /// Cancels the tween stored on the button, if any, so rapid focus changes never leave
        /// two tweens fighting over the same <c>scale</c> or <c>self_modulate</c> property.
        /// </summary>
        /// <param name="button">Button whose running tween should be stopped.</param>
        private static void KillTween(BaseButton button)
        {
            if (!button.HasMeta(TweenMetaKey))
                return;

            Tween tween = button.GetMeta(TweenMetaKey).As<Tween>();
            if (tween != null && tween.IsValid())
                tween.Kill();
        }

        #endregion // Animation
    }
}