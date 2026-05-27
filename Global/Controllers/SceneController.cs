using Godot;
using System.Collections.Generic;

namespace TouhouFuujinroku.Global.Controllers
{
	public enum SceneType
	{
		MainMenu,
		OptionsMenu,
		DebugLevel,
	}

	public partial class SceneController : Node
	{
		public static SceneController Instance { get; private set; }

		// Preloaded scenes — instantiated immediately, no runtime disk reads.
		private readonly Dictionary<SceneType, PackedScene> _preloaded = new()
		{
			{ SceneType.MainMenu, GD.Load<PackedScene>("res://UI/MainMenu/MainMenu.tscn") },
			{ SceneType.OptionsMenu, GD.Load<PackedScene>("res://UI/OptionsMenu/OptionsMenu.tscn") },
		};

		// Lazy-loaded scenes — resolved on first request, then promoted to _preloaded.
		private readonly Dictionary<SceneType, string> _paths = new()
		{
			{ SceneType.DebugLevel, "res://Levels/Debug/Debug.tscn" },
		};

		// ------------------------------------- Godot overrides ------------------------------------

		public override void _Ready() => Instance = this;

		// --------------------------------------- Public API ---------------------------------------

		// Swaps the current scene for the requested type; no-ops silently if type is unregistered.
		public void ChangeScene(SceneType type)
		{
			if (!TryGetScene(type, out var scene)) return;

			GetTree().CurrentScene?.QueueFree();

			var newScene = scene.Instantiate();
			GetTree().Root.AddChild(newScene);
			GetTree().CurrentScene = newScene;
		}

		// ---------------------------------------- Helpers ----------------------------------------

		// Resolves a SceneType to a PackedScene, lazy-loading and caching if necessary.
		private bool TryGetScene(SceneType type, out PackedScene scene)
		{
			if (_preloaded.TryGetValue(type, out scene))
				return true;

			if (_paths.TryGetValue(type, out var path))
			{
				scene = GD.Load<PackedScene>(path);
				_preloaded[type] = scene; // promote to preloaded — skips disk on subsequent calls.
				return true;
			}

			scene = null;
			return false;
		}
	}
}