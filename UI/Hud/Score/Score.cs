using Godot;
using TouhouFuujinroku.Interfaces;

namespace TouhouFuujinroku.UI.Hud.Score
{
    // Self-contained score system — owns state, renders digits, and exposes the public API.
    // Digit sprites are loaded from the HBoxContainer child at runtime; no exports needed.
    public partial class Score : Control
    {
        // ---------------------------------------- State -------------------------------------

        private long _score;
        private Sprite2D[] _digits;

        // --------------------------------------- Public API ----------------------------------

        // Adds points and refreshes the display — call this on enemy death.
        public void AddScore(long points)
        {
            _score += points;
            UpdateDisplay();
        }

        // Resets the score to zero — call this on game start or continue.
        public void Reset()
        {
            _score = 0;
            UpdateDisplay();
        }

        // ---------------------------------- Godot overrides ----------------------------------

        public override void _Ready()
        {
            LoadDigits();
            GetTree().NodeAdded += OnNodeAdded;
            UpdateDisplay();
        }

        public override void _ExitTree()
        {
            GetTree().NodeAdded -= OnNodeAdded;
        }

        // ---------------------------------- Private helpers ----------------------------------

        // Locates the HBoxContainer child and collects its Sprite2D children left to right.
        // Index 0 = most significant digit; index N-1 = units digit.
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

        // Connects to any IScoreable node entering the "enemies" group — type-agnostic.
        private void OnNodeAdded(Node node)
        {
            if (node.IsInGroup("enemies") && node is IScoreable scoreable)
                scoreable.Died += AddScore;
        }

        // Decomposes the score into individual digits and sets each sprite frame accordingly.
        // All digits are always visible — leading zeros shown to fill the display uniformly.
        private void UpdateDisplay()
        {
            if (_digits == null) return;

            int count = _digits.Length;

            for (int i = 0; i < count; i++)
            {
                if (_digits[i] == null) continue;

                // Index 0 is the rightmost (least significant) digit — place increases left to right.
                long divisor = (long)Mathf.Pow(10, i);
                int digit = (int)(_score / divisor % 10);

                _digits[i].Frame = digit;
                _digits[i].Visible = true;
            }
        }
    }
}