using Godot;

using SystemDict = System.Collections.Generic.Dictionary<Godot.PackedScene, int>;

using TouhouFuujinroku.Entities.Projectile;
using TouhouFuujinroku.Entities.Enemies.Spawning;
using TouhouFuujinroku.Entities.Enemies.Spawning.Waves;

namespace TouhouFuujinroku.Levels.DebugLevel
{
	/// <summary>
	/// Sandbox level used to test gameplay systems in isolation.
	/// Owns only level-specific wiring: pool prewarm, debug overlay, and which wave script to play.
	/// </summary>
	public partial class DebugLevel : Node2D
	{
		#region Fields

		/// <summary>Debug overlay showing FPS and other runtime stats.</summary>
		[ExportGroup("Components")]
		[Export] private RichTextLabel _label;

		/// <summary>
		/// Plays the level's enemy script. The level only chooses WHICH script runs;
		/// timing, paths, and spawning are entirely the director's responsibility.
		/// </summary>
		[Export] private WaveDirector _waveDirector;

		/// <summary>Player bullet prefab, prewarmed in the projectile pool.</summary>
		[ExportGroup("Projectiles")]
		[Export] private PackedScene _reimuBulletPrefab;

		/// <summary>Common enemy bullet prefab, prewarmed in the projectile pool.</summary>
		[Export] private PackedScene _greenDotBulletPrefab;

		#endregion // Fields

		#region Godot overrides

		public override void _Ready()
		{
			// Optional projectile prewarm — populates the pool with inactive instances of each prefab if needed.
			var prewarmConfig = new SystemDict
			{
				{_reimuBulletPrefab, 128},  // Player fires fast
				{_greenDotBulletPrefab, 64} // Common enemy projectile
			};

			ProjectilePool.Instance.Initialize(prewarmConfig);

			_waveDirector.Play(new CircleWaveScript());
		}

		public override void _PhysicsProcess(double delta)
		{
			_label.Text = $"DEBUG\nFPS: {Engine.GetFramesPerSecond()}";
		}

		#endregion // Godot overrides
	}
}