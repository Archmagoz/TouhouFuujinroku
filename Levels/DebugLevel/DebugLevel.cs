using TouhouFuujinroku.Global.Controllers;
using TouhouFuujinroku.Entities.Enemies.Spawning;
using TouhouFuujinroku.Entities.Enemies.Spawning.Waves;

namespace TouhouFuujinroku.Levels.DebugLevel
{
	/// <summary>
	/// Sandbox level used to test gameplay systems in isolation.
	/// Handles debug stats, wave selection, and level exit behavior.
	/// </summary>
	public partial class DebugLevel : Level
	{

		#region Godot overrides

		public override void _Ready()
		{
			MenuAudioController.Instance.StopMusic();
		}

		#endregion // Godot overrides

		#region Extension points

		/// <inheritdoc/>
		protected override WaveScript CreateWaveScript() => new EmptyWaveScript();

		/// <summary>
		/// Intentionally empty: the sandbox stays open after the script ends or the player runs out of lives,
		/// so systems can keep being tested.
		/// </summary>
		protected override void LeaveLevel() { }

		#endregion // Extension points
	}
}