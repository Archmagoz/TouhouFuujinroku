using Godot;

namespace TouhouFuujinroku.Global.Controllers
{
    public partial class MusicController : Node
    {
        #region Fields
        public static MusicController Instance { get; private set; }

        private AudioStreamPlayer _player;

        #endregion // Fields

        #region Godot overrides

        public override void _Ready()
        {
            Instance = this;
            _player = GetNode<AudioStreamPlayer>("MenuBGM");
        }

        #endregion // Godot overrides

        #region Public API

        public void Play(AudioStream stream)
        {
            // Avoid restarting the same stream if it's already playing.
            if (_player.Stream == stream && _player.Playing)
                return;

            _player.Stream = stream;
            _player.Play();
        }

        public void Stop() => _player.Stop();

        #endregion
    }
}