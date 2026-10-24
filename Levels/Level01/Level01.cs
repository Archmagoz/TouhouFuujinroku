using TouhouFuujinroku.Entities.Enemies.Spawning;

namespace TouhouFuujinroku.Levels.Level01
{
    /// <summary>
    /// Level 1. Everything shared with the other levels (pool prewarm, HUD lives bridge, exit flow)
    /// lives in <see cref="Level"/>. This class only chooses which wave script plays.
    /// </summary>
    public partial class Level01 : Level
    {
        #region Extension points

        /// <inheritdoc/>
        protected override WaveScript CreateWaveScript() => new Level01WaveScript();

        #endregion // Extension points
    }
}