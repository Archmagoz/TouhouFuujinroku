using Godot;

using TouhouFuujinroku.Entities.Enemies.Spawning;
using TouhouFuujinroku.Entities.PlayableCharacters.Reimu;
using TouhouFuujinroku.Entities.Projectile;

namespace TouhouFuujinroku.Levels.Tools
{
    /// <summary>
    /// Clears the playfield and plays a full-screen flash whenever the player dies.
    /// Every enemy is removed and every projectile is returned to the pool, so the player
    /// respawns into an empty screen.
    /// <para>
    /// Reusable in any level: add the node to the scene and assign the required references.
    /// The overlay is created automatically.
    /// The flash can also be triggered from any script through <see cref="Flash"/>.
    /// </para>
    /// </summary>
    [GlobalClass]
    public partial class PlayfieldClearer : CanvasLayer
    {
        #region Fields

        /// <summary>
        /// Player whose death triggers the clear and flash.
        /// </summary>
        [ExportGroup("Dependencies")]
        [Export] private Reimu _reimu;

        /// <summary>
        /// Director that owns the level's enemies.
        /// </summary>
        [Export] private WaveDirector _waveDirector;

        /// <summary>
        /// Color of the flash. Its alpha channel is ignored.
        /// </summary>
        [ExportGroup("Flash")]
        [Export] private Color _flashColor = Colors.White;

        /// <summary>
        /// Maximum opacity of the flash, from 0 (invisible) to 1 (fully opaque).
        /// </summary>
        [Export(PropertyHint.Range, "0,1,0.05")]
        private float _peakAlpha = 0.85f;

        /// <summary>
        /// Seconds to reach the peak opacity.
        /// </summary>
        [Export] private float _fadeInDuration = 0.05f;

        /// <summary>
        /// Seconds to fade back to transparent.
        /// </summary>
        [Export] private float _fadeOutDuration = 0.5f;

        /// <summary>
        /// Overlay layer: above gameplay and HUD, below the scene transition fade (128).
        /// </summary>
        private const int OverlayLayerIndex = 100;

        /// <summary>
        /// Full-screen rectangle whose opacity is animated.
        /// </summary>
        private ColorRect _overlay;

        /// <summary>
        /// Current flash animation, retained so a new flash can restart it cleanly.
        /// </summary>
        private Tween _tween;

        #endregion // Fields

        #region Godot overrides

        public override void _Ready()
        {
            Layer = OverlayLayerIndex;

            BuildOverlay();

            if (_reimu != null)
                _reimu.Died += OnPlayerDied;
        }

        public override void _ExitTree()
        {
            if (IsInstanceValid(_reimu))
                _reimu.Died -= OnPlayerDied;

            _tween?.Kill();
        }

        #endregion // Godot overrides

        #region Event handlers

        /// <summary>
        /// Handles the player's death. Defers the clear because death can occur inside
        /// a collision callback, where changing collision state is restricted.
        /// </summary>
        private void OnPlayerDied()
        {
            Flash();
            Callable.From(ClearPlayfield).CallDeferred();
        }

        #endregion // Event handlers

        #region Public API

        /// <summary>
        /// Plays the flash. An existing flash is restarted from transparent.
        /// Can also be called manually, for example when activating a bomb.
        /// </summary>
        public void Flash()
        {
            _tween?.Kill();

            _overlay.Color = new Color(_flashColor, 0f);
            _overlay.Visible = true;

            _tween = CreateTween();

            _tween.TweenProperty(
                _overlay,
                "color:a",
                _peakAlpha,
                _fadeInDuration
            );

            _tween.TweenProperty(
                _overlay,
                "color:a",
                0f,
                _fadeOutDuration
            )
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

            // Hide the overlay when finished to avoid unnecessary drawing.
            _tween.TweenCallback(
                Callable.From(() => _overlay.Visible = false)
            );
        }

        #endregion // Public API

        #region Helpers

        /// <summary>
        /// Cancels the current wave, removes every enemy, and returns every projectile,
        /// including the player's, to the pool.
        /// </summary>
        private void ClearPlayfield()
        {
            _waveDirector.CancelCurrentWave();
            _waveDirector.ClearEnemies();

            ProjectilePool.Instance.ReturnAll();
        }

        /// <summary>
        /// Creates a transparent, hidden, mouse-transparent rectangle covering the screen.
        /// </summary>
        private void BuildOverlay()
        {
            _overlay = new ColorRect
            {
                Name = "FlashRect",
                Color = new Color(_flashColor, 0f),
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Visible = false,
            };

            _overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);

            AddChild(_overlay);
        }

        #endregion // Helpers
    }
}