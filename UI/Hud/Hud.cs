using Godot;

namespace TouhouFuujinroku.UI.Hud
{
    // Entry point for all HUD-related reporting. External systems (spawners, enemies,
    // game controllers) talk to the Hud, never directly to its internal widgets —
    // this keeps Score, and any future HUD elements, as implementation details.
    public partial class Hud : Control
    {
        [ExportGroup("Components")]
        [Export] private Score.Score _score;

        // Public API -----------------------------------------------------------------------------------------

        // Forwards points to the Score widget — call this from anywhere an enemy dies.
        public void ReportScore(long points) => _score.AddScore(points);

        // Resets all HUD state — call this on game start or continue.
        public void Reset() => _score.Reset();
    }
}