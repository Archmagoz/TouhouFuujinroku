using Godot;
using SystemDict = System.Collections.Generic.Dictionary<Godot.PackedScene, int>;

using TouhouFuujinroku.Global.Controllers;

namespace TouhouFuujinroku.Levels.Debug
{
	public partial class Debug : Node2D
	{
		[ExportGroup("Components")]
		[Export] private RichTextLabel _label;

		[ExportGroup("Projectile Warmup")]
		[Export] private PackedScene _reimuBulletPrefab;
		[Export] private PackedScene _greenDotBulletPrefab;

		// Godot overrides --------------------------------------------------------------------------------------

		public override void _Ready()
		{
			// TreeExiting fires reliably even when the scene is freed via QueueFree(),
			// unlike _ExitTree() which may not trigger during deferred scene transitions.
			TreeExiting += () => ProjectilePool.Instance.Clear();

			// Optional projectile prewarm — populates the pool with inactive instances of each prefab,
			// preventing first-use stutter when the pool has to instantiate on demand.
			var prewarmConfig = new SystemDict
			{
				{ _reimuBulletPrefab, 64 },    // player fires fast
				{ _greenDotBulletPrefab, 64 }, // green dot is a common enemy projectile
			};

			ProjectilePool.Instance.Initialize(prewarmConfig);
		}

		public override void _Process(double delta)
		{
			_label.Text = $"DEBUG\nFPS: {Engine.GetFramesPerSecond()}";
		}
	}
}