using Godot;
using TohouFuuujinoku.Global.Controllers;

namespace TohouFuuujinoku.Levels.Debug
{
	public partial class Debug : Node2D
	{
		[ExportGroup("Components")]
		[Export] private RichTextLabel _label;

		// ---------------------------------- Godot overrides -----------------------------------

		public override void _Ready()
		{
			// TreeExiting fires reliably even when the scene is freed via QueueFree(),
			// unlike _ExitTree() which may not trigger during deferred scene transitions.
			TreeExiting += () => ProjectilePool.Instance.Clear();
			ProjectilePool.Instance.Initialize();
		}

		public override void _Process(double delta)
		{
			_label.Text = $"DEBUG\nFPS: {Engine.GetFramesPerSecond()}";
		}
	}
}