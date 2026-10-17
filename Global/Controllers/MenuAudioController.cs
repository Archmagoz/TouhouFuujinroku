using Godot;

namespace TouhouFuujinroku.Global.Controllers
{
    public partial class MenuAudioController : Node
    {
        #region Fields

        public static MenuAudioController Instance { get; private set; }

        private AudioStreamPlayer _menuBgmPlayer;
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

        public void PlayMenuSelectSFX() => _selectSfxPlayer.Play();

        public void StopMusic() => _menuBgmPlayer.Stop();

        #endregion // Public API
    }
}