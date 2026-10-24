using Godot;

namespace TouhouFuujinroku.UI.Hud.Lives
{
    /// <summary>
    /// Self-contained lives display: renders one star per remaining life and exposes the public API.
    /// <para>
    /// Star sprites are collected from the <see cref="HBoxContainer"/> child at runtime, so no exports are needed.
    /// Stars are hidden from the last child to the first, so the leftmost star is the last one to disappear.
    /// The widget holds no game state: it only shows the value it is given.
    /// </para>
    /// </summary>
    public partial class Lives : Control
    {
        #region Fields

        /// <summary>
        /// Star sprites in child order. Index 0 is the first star to appear and the last one to be hidden.
        /// Entries are null for container children that are not <see cref="Sprite2D"/>.
        /// </summary>
        private Sprite2D[] _stars;

        #endregion // Fields

        #region Godot overrides

        public override void _Ready()
        {
            LoadStars();
        }

        #endregion // Godot overrides

        #region Public API

        /// <summary>
        /// Shows exactly <paramref name="lives"/> stars and hides the rest.
        /// Values outside the available range need no special handling: a count larger than the number of
        /// sprites shows all of them, and zero or a negative count hides all of them.
        /// </summary>
        /// <param name="lives">Number of stars to display.</param>
        public void SetLives(int lives)
        {
            if (_stars == null) return;

            for (int i = 0; i < _stars.Length; i++)
            {
                if (_stars[i] == null) continue;

                _stars[i].Visible = i < lives;
            }
        }

        #endregion // Public API

        #region Helpers

        /// <summary>
        /// Locates the <see cref="HBoxContainer"/> child and collects its <see cref="Sprite2D"/> children in child order.
        /// Leaves the stars unset (and the display untouched) if no container is found.
        /// </summary>
        private void LoadStars()
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
            _stars = new Sprite2D[children.Count];

            for (int i = 0; i < children.Count; i++)
                if (children[i] is Sprite2D sprite)
                    _stars[i] = sprite;
        }

        #endregion // Helpers
    }
}