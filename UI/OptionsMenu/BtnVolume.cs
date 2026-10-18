using Godot;

using TouhouFuujinroku.Global.Controllers;
using TouhouFuujinroku.Global.Settings.Audio;

namespace TouhouFuujinroku.UI.OptionsMenu
{
	/// <summary>
	/// Button that shows and edits the volume of one audio channel (background music or sound effects).
	/// <para>
	/// While focused, left and right (keyboard or gamepad) lower and raise the volume by <c>_step</c> percent
	/// (5 by default). Clicking or confirming raises it and wraps back to 0 after the maximum.
	/// </para>
	/// <para>
	/// The digits are a <see cref="DigitDisplay"/> child of this button, so they follow the button's position and
	/// the focus animation applied by <see cref="MenuScreen"/>. They are bright while the button is focused and
	/// dimmed otherwise. The volume itself lives in <see cref="AudioSettings"/>; this class only edits it and
	/// reflects it on screen. Which channel it edits is chosen per instance with <c>_channel</c>.
	/// </para>
	/// </summary>
	[GlobalClass]
	public partial class BtnVolume : TextureButton
	{
		#region Fields

		/// <summary>
		/// Audio channel whose volume this button edits.
		/// </summary>
		[Export] private VolumeChannel _channel = VolumeChannel.Bgm;

		/// <summary>
		/// Digit display that shows the current volume. Usually a child of this button.
		/// </summary>
		[Export] private DigitDisplay _display;

		/// <summary>
		/// Percentage added or removed per step.
		/// </summary>
		[Export(PropertyHint.Range, "1,100,1")] private int _step = 5;

		#endregion // Fields

		#region Godot overrides

		public override void _Ready()
		{
			// Connected in code, not in the editor, because these signals belong to this very node.
			// The focus signals only refresh the look, so they coexist with the MenuScreen handlers.
			Pressed += OnPressed;
			FocusEntered += Refresh;
			FocusExited += Refresh;

			Refresh();
		}

		public override void _GuiInput(InputEvent @event)
		{
			if (@event.IsActionPressed("ui_left"))
				ChangeVolume(AudioSettings.GetVolume(_channel) - _step);
			else if (@event.IsActionPressed("ui_right"))
				ChangeVolume(AudioSettings.GetVolume(_channel) + _step);
			else
				return;

			// Without this, the same press would also move focus to a neighboring control.
			AcceptEvent();
		}

		#endregion // Godot overrides

		#region Event handlers

		/// <summary>
		/// Handles a click or confirm press: raises the volume one step and wraps to 0 past the maximum.
		/// </summary>
		private void OnPressed()
		{
			int next = AudioSettings.GetVolume(_channel) + _step;

			ChangeVolume(next > AudioSettings.MaxVolume ? 0 : next);
		}

		#endregion // Event handlers

		#region Helpers

		/// <summary>
		/// Applies a new volume, refreshes the digits, and plays the selection sound as feedback.
		/// When this button edits the sound effects channel, that sound is also a preview of the new volume.
		/// </summary>
		/// <param name="percent">Requested volume. Clamped by <see cref="AudioSettings"/>.</param>
		private void ChangeVolume(int percent)
		{
			AudioSettings.SetVolume(_channel, percent);

			Refresh();
			MenuAudioController.Instance?.PlayMenuSelectSFX();
		}

		/// <summary>
		/// Updates the digits to the current volume and to the bright or dimmed look that matches the focus state.
		/// </summary>
		private void Refresh()
		{
			if (_display == null) return;

			_display.SetValue(AudioSettings.GetVolume(_channel));
			_display.SetHighlighted(HasFocus());
		}

		#endregion // Helpers
	}
}