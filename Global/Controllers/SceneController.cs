using Godot;
using System.Collections.Generic;

namespace TouhouFuujinroku.Global.Controllers
{
	// Identifies every scene the controller knows how to load.
	// Add a new entry here before registering the scene in SceneController.
	public enum Scene
	{
		MainMenu,
		OptionsMenu,
		DebugLevel,
	}

	public partial class SceneController : Node
	{
		public static SceneController Instance { get; private set; }

		// ---------------------------------- Preloaded scenes ----------------------------------
		// Instantiated at startup so scene transitions never stall on a disk read.
		// Only put scenes that must be available immediately (e.g. menus) here.

		private readonly Dictionary<Scene, PackedScene> _ready = new()
		{
			{ Scene.MainMenu,    GD.Load<PackedScene>("res://UI/MainMenu/MainMenu.tscn") },
			{ Scene.OptionsMenu, GD.Load<PackedScene>("res://UI/OptionsMenu/OptionsMenu.tscn") },
		};

		// ----------------------------------- Lazy scenes -------------------------------------
		// Resolved from disk on first request, then promoted to _ready so subsequent
		// transitions are as fast as preloaded ones. Good for large/infrequent scenes.

		private readonly Dictionary<Scene, string> _lazyPaths = new()
		{
			{ Scene.DebugLevel, "res://Levels/Debug/Debug.tscn" },
		};

		// ---------------------------------- Godot overrides ----------------------------------

		public override void _Ready() => Instance = this;

		// ------------------------------------ Public API -------------------------------------

		// Replaces the active scene with the one mapped to the given id.
		// Silently no-ops when the id has no registered path or packed scene.
		public void TransitionTo(Scene id)
		{
			if (!TryResolveScene(id, out var packed)) return;

			// Free the outgoing scene before adding the incoming one to avoid
			// running two scenes simultaneously even for a single frame.
			GetTree().CurrentScene?.QueueFree();

			var incoming = packed.Instantiate();
			GetTree().Root.AddChild(incoming);
			GetTree().CurrentScene = incoming;
		}

		// ------------------------------------- Helpers --------------------------------------

		// Returns the PackedScene for a given id, loading it from disk if needed.
		// Lazy-loaded scenes are cached in _ready so the disk is only hit once.
		private bool TryResolveScene(Scene id, out PackedScene packed)
		{
			if (_ready.TryGetValue(id, out packed))
				return true;

			if (_lazyPaths.TryGetValue(id, out var path))
			{
				packed = GD.Load<PackedScene>(path);
				_ready[id] = packed; // promote — future calls skip this branch entirely
				return true;
			}

			packed = null;
			return false;
		}
	}
}