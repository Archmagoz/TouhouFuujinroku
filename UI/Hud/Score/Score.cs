using Godot;

namespace TouhouFuujinroku.UI.Hud.Score
{
    /// <summary>
    /// Self-contained score system: owns the state, renders the digits, and exposes the public API.
    /// <para>
    /// Digit sprites are collected from the <see cref="HBoxContainer"/> child at runtime, so no exports are needed.
    /// Each sprite must be an animation-frame strip with one frame per digit (frame 0 to 9).
    /// </para>
    /// </summary>
    public partial class Score : Control
    {
        #region Fields

        /// <summary>
        /// Current score value.
        /// </summary>
        private long _score;

        /// <summary>
        /// Digit sprites indexed by decimal place: index 0 shows the units digit, index 1 the tens digit, and so on.
        /// Entries are null for container children that are not <see cref="Sprite2D"/>.
        /// </summary>
        private Sprite2D[] _digits;

        #endregion // Fields

        #region Public API

        /// <summary>
        /// Adds points to the score and refreshes the display. Call this on enemy death.
        /// </summary>
        /// <param name="points">Points to add.</param>
        public void AddScore(long points)
        {
            _score += points;
            UpdateDisplay();
        }

        /// <summary>
        /// Resets the score to zero and refreshes the display. Call this on game start or continue.
        /// </summary>
        public void Reset()
        {
            _score = 0;
            UpdateDisplay();
        }

        #endregion // Public API

        #region Godot overrides

        public override void _Ready()
        {
            LoadDigits();
            UpdateDisplay();
        }

        #endregion // Godot overrides

        #region Helpers

        /// <summary>
        /// Locates the <see cref="HBoxContainer"/> child and collects its <see cref="Sprite2D"/> children in child order.
        /// The array index equals the decimal place shown by that sprite, so the first child must be the units digit.
        /// Leaves the digits unset (and the display untouched) if no container is found.
        /// </summary>
        private void LoadDigits()
        {
            HBoxContainer container = null;

            foreach (var child in GetChildren())
                if (child is HBoxContainer hbox)
                {
                    container = hbox;
                    break;
                }

            if (container == null) return;

            var children = container.GetChildren();
            _digits = new Sprite2D[children.Count];

            for (int i = 0; i < children.Count; i++)
                if (children[i] is Sprite2D sprite)
                    _digits[i] = sprite;
        }

        /// <summary>
        /// Decomposes the score into individual digits and sets each sprite frame accordingly.
        /// All digits are always visible: leading zeros are shown to fill the display uniformly.
        /// A score with more digits than sprites displays only its least significant digits.
        /// </summary>
        private void UpdateDisplay()
        {
            if (_digits == null) return;

            int count = _digits.Length;

            for (int i = 0; i < count; i++)
            {
                if (_digits[i] == null) continue;

                // Sprite i shows the digit at decimal place i: index 0 is the units digit.
                long divisor = (long)Mathf.Pow(10, i);
                int digit = (int)(_score / divisor % 10);

                _digits[i].Frame = digit;
                _digits[i].Visible = true;
            }
        }

        #endregion // Helpers
    }
}