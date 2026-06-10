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
		// Eagerly loaded in _Ready so GD.Load runs after the engine is fully initialised;
		// avoids hitting the ResourceLoader before the scene tree is ready.

		private readonly Dictionary<Scene, PackedScene> _preloadedScenes = new();

		// ----------------------------------- Lazy scenes -------------------------------------
		// Resolved from disk on first request, then promoted to _preloadedScenes so
		// subsequent transitions skip the disk entirely.

		private readonly Dictionary<Scene, string> _lazyPaths = new()
		{
			{ Scene.DebugLevel, "res://Levels/Debug/Debug.tscn" },
		};

		// ---------------------------------- Godot overrides ----------------------------------

		public override void _Ready()
		{
			// Enforce a single instance by evicting any duplicate that gets added to the tree.
			if (Instance != null)
			{
				QueueFree();
				return;
			}

			Instance = this;

			// Load eagerly-required scenes here rather than in field initialisers so
			// GD.Load runs inside a valid engine frame with a live ResourceLoader.
			_preloadedScenes[Scene.MainMenu] = GD.Load<PackedScene>("res://UI/MainMenu/MainMenu.tscn");
			_preloadedScenes[Scene.OptionsMenu] = GD.Load<PackedScene>("res://UI/OptionsMenu/OptionsMenu.tscn");
		}

		// ------------------------------------ Public API -------------------------------------

		// Frees the current scene before adding the incoming one to guarantee
		// only one scene is alive at a time, even within a single frame.
		public void TransitionTo(Scene id)
		{
			// If the scene fails to resolve, do nothing.
			if (!TryResolveScene(id, out var packed)) return;

			GetTree().CurrentScene?.QueueFree();

			var incoming = packed.Instantiate();
			GetTree().Root.AddChild(incoming);
			GetTree().CurrentScene = incoming;
		}

		// ------------------------------------- Helpers --------------------------------------

		// Checks _preloadedScenes first; on a miss, loads from _lazyPaths and promotes
		// the result into _preloadedScenes so the disk is only read once per lazy scene.
		private bool TryResolveScene(Scene id, out PackedScene packed)
		{
			if (_preloadedScenes.TryGetValue(id, out packed))
				return true;

			if (_lazyPaths.TryGetValue(id, out var path))
			{
				packed = GD.Load<PackedScene>(path);

				// If the scene fails to load, don't add a null entry to _preloadedScenes.
				if (packed == null) return false;

				_preloadedScenes[id] = packed;
				return true;
			}

			packed = null;
			return false;
		}
	}
}