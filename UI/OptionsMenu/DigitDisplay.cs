using Godot;

using System.Collections.Generic;

namespace TouhouFuujinroku.UI.OptionsMenu
{
	/// <summary>
	/// Displays a non-negative integer with digit sprites placed by hand in the scene, in a bright or a dimmed look.
	/// <para>
	/// It follows the same convention as the HUD score: every <see cref="Sprite2D"/> child is one digit, collected
	/// in child order, where the array index equals the decimal place (the first child is the units digit, the next
	/// one the tens digit, and so on). Where each sprite sits on screen is decided in the scene.
	/// </para>
	/// <para>
	/// Each sprite must be a frame strip in which frames 0 to 9 are the bright digits. The dimmed look uses the
	/// counterpart frame <c>d + _dimFrameOffset</c> when the strip has one (e.g. a dim row right below the bright
	/// one, with Vframes = 2). Without it, the bright digit is darkened with a tint instead, so the dimmed look
	/// works even before the dim art is set up.
	/// </para>
	/// </summary>
	[GlobalClass]
	public partial class DigitDisplay : Node2D
	{
		#region Fields

		/// <summary>
		/// Number of digits, counted from the units, that are always visible. Digits beyond this count are hidden
		/// while they would be a leading zero. With 2, 5 is shown as "05", 50 as "50" and 100 as "100".
		/// </summary>
		[Export(PropertyHint.Range, "1,9,1")] private int _minimumVisibleDigits = 1;

		/// <summary>
		/// Frame offset from a bright digit to its dimmed counterpart. 10 when the dim row sits right below the
		/// bright row in a strip with Vframes = 2. 0 means the strip has no dim row, so the dimmed look is
		/// produced with <c>_dimTint</c>. A frame outside the strip is treated the same way.
		/// </summary>
		[Export] private int _dimFrameOffset = 10;

		/// <summary>
		/// Multiplicative tint applied to the value digits so they match the darker look of the button.
		/// Each channel scales the texture color: 1 keeps it unchanged, 0 turns it fully black.
		/// </summary>
		[Export] private Color _dimTint = new(0.3f, 0.3f, 0.3f);

		/// <summary>
		/// Digit sprites indexed by decimal place: index 0 shows the units digit.
		/// </summary>
		private Sprite2D[] _digits = [];

		/// <summary>Value currently displayed.</summary>
		private int _value;

		/// <summary>True for the bright look, false for the dimmed one.</summary>
		private bool _highlighted;

		#endregion // Fields

		#region Godot overrides

		public override void _Ready()
		{
			var digits = new List<Sprite2D>();

			foreach (var child in GetChildren())
				if (child is Sprite2D sprite)
					digits.Add(sprite);

			_digits = digits.ToArray();

			if (_digits.Length == 0)
				GD.PushWarning("DigitDisplay: no Sprite2D children found, nothing will be displayed.");

			Render();
		}

		#endregion // Godot overrides

		#region Public API

		/// <summary>
		/// Sets the displayed value.
		/// </summary>
		/// <param name="value">Value to display. Clamped to 0 and the largest value the sprites can represent.</param>
		public void SetValue(int value)
		{
			_value = value;
			Render();
		}

		/// <summary>
		/// Switches between the bright and the dimmed look, e.g. to follow whether the owner is selected.
		/// </summary>
		/// <param name="highlighted">True for the bright digits, false for the dimmed look.</param>
		public void SetHighlighted(bool highlighted)
		{
			_highlighted = highlighted;
			Render();
		}

		#endregion // Public API

		#region Helpers

		/// <summary>
		/// Decomposes the value into digits and applies the visibility and look of every sprite.
		/// </summary>
		private void Render()
		{
			int count = _digits.Length;
			if (count == 0) return;

			int value = Mathf.Clamp(_value, 0, Pow10(count) - 1);

			for (int place = 0; place < count; place++)
			{
				int digit = value / Pow10(place) % 10;

				// A digit is a leading zero when the value is too small to reach its place.
				bool isLeadingZero = place >= _minimumVisibleDigits && value < Pow10(place);

				_digits[place].Visible = !isLeadingZero;
				ApplyLook(_digits[place], digit);
			}
		}

		/// <summary>
		/// Shows a digit on a sprite in the current bright or dimmed look. The dimmed look prefers the dim frame
		/// of the strip and falls back to tinting the bright digit when the strip has none.
		/// </summary>
		/// <param name="sprite">Sprite that displays the digit.</param>
		/// <param name="digit">Digit to show, from 0 to 9.</param>
		private void ApplyLook(Sprite2D sprite, int digit)
		{
			int dimFrame = digit + _dimFrameOffset;
			bool hasDimFrame = _dimFrameOffset != 0 && dimFrame >= 0 && dimFrame < sprite.Hframes * sprite.Vframes;

			if (_highlighted)
			{
				sprite.Frame = digit;
				sprite.SelfModulate = Colors.White;
			}
			else if (hasDimFrame)
			{
				sprite.Frame = dimFrame;
				sprite.SelfModulate = Colors.White;
			}
			else
			{
				sprite.Frame = digit;
				sprite.SelfModulate = _dimTint;
			}
		}

		/// <summary>
		/// Integer power of ten, avoiding the floating point round trip of <c>Mathf.Pow</c>.
		/// </summary>
		/// <param name="exponent">Non-negative exponent.</param>
		/// <returns>10 raised to <paramref name="exponent"/>.</returns>
		private static int Pow10(int exponent)
		{
			int result = 1;

			for (int i = 0; i < exponent; i++)
				result *= 10;

			return result;
		}

		#endregion // Helpers
	}
}