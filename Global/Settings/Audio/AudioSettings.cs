using Godot;

using System;
using System.Collections.Generic;

namespace TouhouFuujinroku.Global.Settings
{
    /// <summary>
    /// Audio channels that have an independent volume setting. Each channel is routed through its own audio bus.
    /// </summary>
    public enum VolumeChannel
    {
        /// <summary>Background music, routed through the "BGM" bus.</summary>
        Bgm,

        /// <summary>Sound effects, routed through the "SE" bus.</summary>
        Se,
    }

    /// <summary>
    /// Persistent audio settings: stores the player's volume choice for each <see cref="VolumeChannel"/>,
    /// applies it to the matching audio bus, and saves it to disk.
    /// <para>
    /// Volumes are integer percentages from 0 to <see cref="MaxVolume"/>. Call <see cref="Load"/> once at
    /// startup (e.g. from an Autoload's <c>_Ready</c>) so the saved volumes are applied before any audio plays.
    /// </para>
    /// </summary>
    public static class AudioSettings
    {
        #region Constants

        /// <summary>Highest volume percentage.</summary>
        public const int MaxVolume = 100;

        /// <summary>Location of the settings file, inside Godot's per-user data folder.</summary>
        private const string SettingsPath = "user://settings.cfg";

        /// <summary>Section of the settings file that holds the audio values.</summary>
        private const string Section = "audio";

        /// <summary>
        /// Smallest linear gain passed to the dB conversion. Zero would convert to negative infinity,
        /// so silence is handled by muting the bus instead.
        /// </summary>
        private const float MinLinearVolume = 0.0001f;

        #endregion // Constants

        #region Fields

        /// <summary>
        /// Name of the audio bus of each channel. Every player of a channel must be routed to its bus,
        /// and the buses are created in the editor's Audio panel.
        /// </summary>
        private static readonly Dictionary<VolumeChannel, string> _busNames = new()
        {
            { VolumeChannel.Bgm, "BGM" },
            { VolumeChannel.Se,  "SE" },
        };

        /// <summary>
        /// Key under which the volume of each channel is stored in the settings file.
        /// </summary>
        private static readonly Dictionary<VolumeChannel, string> _settingsKeys = new()
        {
            { VolumeChannel.Bgm, "bgm_volume" },
            { VolumeChannel.Se,  "se_volume" },
        };

        /// <summary>
        /// Current volume percentage of each channel.
        /// </summary>
        private static readonly Dictionary<VolumeChannel, int> _volumes = new()
        {
            { VolumeChannel.Bgm, MaxVolume },
            { VolumeChannel.Se,  MaxVolume },
        };

        #endregion // Fields

        #region Public API

        /// <summary>
        /// Reads the saved settings (if any) and applies them to the audio buses.
        /// A missing file is not an error: the defaults are applied instead.
        /// </summary>
        public static void Load()
        {
            var config = new ConfigFile();
            bool hasFile = config.Load(SettingsPath) == Error.Ok;

            foreach (var channel in Enum.GetValues<VolumeChannel>())
            {
                if (hasFile)
                    _volumes[channel] = Mathf.Clamp(config.GetValue(Section, _settingsKeys[channel], MaxVolume).AsInt32(), 0, MaxVolume);

                ApplyVolume(channel);
            }
        }

        /// <summary>
        /// Gets the current volume of a channel.
        /// </summary>
        /// <param name="channel">Channel to query.</param>
        /// <returns>Volume percentage, from 0 to <see cref="MaxVolume"/>.</returns>
        public static int GetVolume(VolumeChannel channel) => _volumes[channel];

        /// <summary>
        /// Sets the volume of a channel, applies it to its audio bus immediately, and saves it.
        /// </summary>
        /// <param name="channel">Channel to change.</param>
        /// <param name="percent">New volume. Clamped to 0 and <see cref="MaxVolume"/>.</param>
        public static void SetVolume(VolumeChannel channel, int percent)
        {
            _volumes[channel] = Mathf.Clamp(percent, 0, MaxVolume);

            ApplyVolume(channel);
            Save();
        }

        #endregion // Public API

        #region Helpers

        /// <summary>
        /// Pushes the volume of a channel to its bus. Zero mutes the bus; any other value is converted
        /// from a linear percentage to decibels, which matches how loudness is perceived.
        /// </summary>
        /// <param name="channel">Channel whose bus should be updated.</param>
        private static void ApplyVolume(VolumeChannel channel)
        {
            string busName = _busNames[channel];
            int bus = AudioServer.GetBusIndex(busName);

            if (bus < 0)
            {
                GD.PushWarning($"[AudioSettings] Audio bus '{busName}' not found. Create it in the Audio panel.");
                return;
            }

            int volume = _volumes[channel];
            float linear = volume / (float)MaxVolume;

            AudioServer.SetBusMute(bus, volume == 0);
            AudioServer.SetBusVolumeDb(bus, Mathf.LinearToDb(Mathf.Max(linear, MinLinearVolume)));
        }

        /// <summary>
        /// Writes the volume of every channel to disk, keeping any other values already stored in the file.
        /// </summary>
        private static void Save()
        {
            var config = new ConfigFile();

            // Loaded first so keys written by other settings are preserved. A missing file is fine.
            config.Load(SettingsPath);

            foreach (var (channel, volume) in _volumes)
                config.SetValue(Section, _settingsKeys[channel], volume);

            Error error = config.Save(SettingsPath);
            if (error != Error.Ok)
                GD.PushError($"[AudioSettings] Failed to save '{SettingsPath}': {error}.");
        }

        #endregion // Helpers
    }
}