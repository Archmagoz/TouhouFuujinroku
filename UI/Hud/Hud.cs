using Godot;

namespace TouhouFuujinroku.UI.Hud
{
    /// <summary>
    /// Entry point for all HUD-related reporting.
    /// <para>
    /// External systems (spawners, enemies, game controllers) talk to the Hud and never directly to its
    /// internal widgets. This keeps <c>Score</c>, and any future HUD elements, as implementation details.
    /// </para>
    /// </summary>
    public partial class Hud : Control
    {
        #region Fields

        /// <summary>
        /// Widget that displays and accumulates the player's score.
        /// </summary>
        [ExportGroup("Components")]
        [Export] private Score.Score _score;

        #endregion // Fields

        #region Public API

        /// <summary>
        /// Forwards points to the score widget. Call this from anywhere an enemy dies.
        /// </summary>
        /// <param name="points">Points to add to the current score.</param>
        public void ReportScore(long points) => _score.AddScore(points);

        /// <summary>
        /// Resets all HUD state. Call this on game start or continue.
        /// </summary>
        public void Reset() => _score.Reset();

        #endregion // Public API
    }
}