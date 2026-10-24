using Godot;

namespace TouhouFuujinroku.UI.Hud
{
    /// <summary>
    /// Entry point for all HUD-related reporting.
    /// <para>
    /// External systems (spawners, enemies, game controllers) talk to the Hud and never directly to its
    /// internal widgets. This keeps <c>Score</c>, <c>Lives</c>, and any future HUD elements as implementation details.
    /// </para>
    /// <para>
    /// The Hud is purely passive: it knows nothing about the player or the level. Whoever owns both
    /// (typically the level script) pushes the state into it through the <c>Report</c> methods.
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

        /// <summary>
        /// Widget that displays the player's remaining lives as stars.
        /// </summary>
        [Export] private Lives.Lives _lives;

        #endregion // Fields

        #region Public API

        /// <summary>
        /// Forwards points to the score widget. Call this from anywhere an enemy dies.
        /// </summary>
        /// <param name="points">Points to add to the current score.</param>
        public void ReportScore(long points) => _score.AddScore(points);

        /// <summary>
        /// Forwards the remaining life count to the lives widget. Call this once at level start
        /// (the player does not raise a lives event on initialization) and whenever the player's lives change.
        /// </summary>
        /// <param name="lives">Number of stars to display.</param>
        public void ReportLives(int lives) => _lives.SetLives(lives);

        /// <summary>
        /// Resets the score. Call this on game start or continue.
        /// Lives are not reset here because their value belongs to the player: after resetting the player,
        /// push the new value with <see cref="ReportLives"/>.
        /// </summary>
        public void Reset() => _score.Reset();

        #endregion // Public API
    }
}