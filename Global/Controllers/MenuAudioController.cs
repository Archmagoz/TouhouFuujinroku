using Godot;

namespace TouhouFuujinroku.Global.Controllers
{
    /// <summary>
    /// Global access point for menu audio: background music and UI sound effects.
    /// <para>
    /// Exposed as a singleton through <see cref="Instance"/> so menus can trigger sounds without owning
    /// audio nodes themselves. It expects two <see cref="AudioStreamPlayer"/> children named
    /// <c>MenuBGM</c> and <c>SelectSFX</c>.
    /// </para>
    /// </summary>
    public partial class MenuAudioController : Node
    {
        #region Fields

        /// <summary>
        /// Singleton instance, assigned in <c>_Ready</c>. Not guarded against duplicates, so the controller
        /// must exist exactly once (e.g. as an Autoload).
        /// </summary>
        public static MenuAudioController Instance { get; private set; }

        /// <summary>Background music player, resolved from the <c>MenuBGM</c> child.</summary>
        private AudioStreamPlayer _menuBgmPlayer;

        /// <summary>Selection sound effect player, resolved from the <c>SelectSFX</c> child.</summary>
        private AudioStreamPlayer _selectSfxPlayer;

        #endregion // Fields

        #region Godot overrides

        public override void _Ready()
        {
            Instance = this;

            _menuBgmPlayer = GetNode<AudioStreamPlayer>("MenuBGM");
            _selectSfxPlayer = GetNode<AudioStreamPlayer>("SelectSFX");
        }

        #endregion // Godot overrides

        #region Public API

        /// <summary>
        /// Plays the menu selection sound effect, restarting it if it is already playing.
        /// </summary>
        public void PlayMenuSelectSFX() => _selectSfxPlayer.Play();

        /// <summary>
        /// Starts the menu music if it is not already playing. Safe to call on every menu screen entry:
        /// it does nothing on first boot (already playing) or when returning from the options menu,
        /// and restarts the music after the gameplay level stopped it.
        /// </summary>
        public void PlayMusic()
        {
            if (_menuBgmPlayer.Playing) return;

            _menuBgmPlayer.Play();
        }

        /// <summary>
        /// Stops the menu background music, typically when leaving the menu for a level.
        /// </summary>
        public void StopMusic() => _menuBgmPlayer.Stop();

        #endregion // Public API
    }
}