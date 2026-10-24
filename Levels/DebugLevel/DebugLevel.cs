using Godot;

using TouhouFuujinroku.Entities.Enemies.Spawning;
using TouhouFuujinroku.Entities.Enemies.Spawning.Waves;

namespace TouhouFuujinroku.Levels.DebugLevel
{
	/// <summary>
	/// Sandbox level used to test gameplay systems in isolation.
	/// Everything shared with the other levels lives in <see cref="Level"/>. This class only adds the
	/// debug overlay, chooses the wave script, and never leaves the level on its own.
	/// </summary>
	public partial class DebugLevel : Level
	{
		#region Fields

		/// <summary>Debug overlay showing FPS and other runtime stats.</summary>
		[ExportGroup("Debug")]
		[Export] private RichTextLabel _label;

		#endregion // Fields

		#region Godot overrides

		public override void _PhysicsProcess(double delta)
		{
			_label.Text = $"DEBUG\nFPS: {Engine.GetFramesPerSecond()}";
		}

		#endregion // Godot overrides

		#region Extension points

		/// <inheritdoc/>
		protected override WaveScript CreateWaveScript() => new CircleWaveScript();

		/// <summary>
		/// Intentionally empty: the sandbox stays open after the script ends or the player runs out of lives,
		/// so systems can keep being tested.
		/// </summary>
		protected override void LeaveLevel() { }

		#endregion // Extension points
	}
}