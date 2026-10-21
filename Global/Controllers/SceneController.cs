using Godot;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TouhouFuujinroku.Global.Controllers
{
	/// <summary>
	/// Identifies every scene the controller knows how to load.
	/// Add a new entry here before registering its path in <c>SceneController._scenePaths</c>.
	/// </summary>
	public enum Scene
	{
		/// <summary>Main menu, the entry point of the game.</summary>
		MainMenu,

		/// <summary>Options menu.</summary>
		OptionsMenu,

		/// <summary>Sandbox level used to test gameplay systems.</summary>
		DebugLevel,

		/// <summary>Stage 1 level.</summary>
		Level01,
	}

	/// <summary>
	/// Autoload responsible for scene changes.
	/// <para>
	/// Transition pipeline:
	/// <list type="number">
	/// <item><description>Fade out (overlay alpha 0 to 1) while the next scene loads on worker threads.</description></item>
	/// <item><description>Wait for both the fade and the load to finish.</description></item>
	/// <item><description>Swap the scenes while the screen is fully covered.</description></item>
	/// <item><description>Fade in (overlay alpha 1 to 0).</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// Loading and fading run concurrently, so a fast load costs nothing beyond the fade itself.
	/// </para>
	/// </summary>
	public partial class SceneController : Node
	{
		/// <summary>
		/// Singleton instance, assigned in <c>_Ready</c> and cleared in <c>_ExitTree</c>.
		/// </summary>
		public static SceneController Instance { get; private set; }

		#region Configuration

		// Kept as plain properties because this script is typically registered as a script-only
		// autoload, where [Export] values cannot be edited in the inspector.

		/// <summary>Duration, in seconds, of the fade to the overlay color. 0 or less skips the animation.</summary>
		public float FadeOutDuration { get; set; } = 0.15f;

		/// <summary>Duration, in seconds, of the fade back to the scene. 0 or less skips the animation.</summary>
		public float FadeInDuration { get; set; } = 0.15f;

		/// <summary>Color of the transition overlay.</summary>
		public Color FadeColor { get; set; } = Colors.Black;

		/// <summary>
		/// Canvas layer of the fade overlay. High enough to draw above any gameplay or UI CanvasLayer.
		/// </summary>
		private const int FadeLayerIndex = 128;

		#endregion // Configuration

		#region Events

		/// <summary>
		/// Raised on the main thread when a transition begins, with the target scene.
		/// Useful for pausing gameplay, disabling input, and so on.
		/// </summary>
		public event Action<Scene> TransitionStarted;

		/// <summary>
		/// Raised on the main thread when a transition ends, successfully or not, with the target scene.
		/// </summary>
		public event Action<Scene> TransitionFinished;

		#endregion // Events

		#region Fields

		/// <summary>
		/// Single source of truth for scene paths. Every scene is loaded the same way (threaded request),
		/// so there is no distinction between "eager" and "lazy" scenes beyond whether they are listed
		/// in <see cref="_warmupScenes"/>.
		/// </summary>
		private readonly Dictionary<Scene, string> _scenePaths = new()
		{
			{ Scene.MainMenu,    "res://UI/MainMenu/MainMenu.tscn" },
			{ Scene.OptionsMenu, "res://UI/OptionsMenu/OptionsMenu.tscn" },
			{ Scene.DebugLevel,  "res://Levels/DebugLevel/DebugLevel.tscn" },
			{ Scene.Level01, "res://Levels/Level01/Level01.tscn" },
		};

		/// <summary>
		/// Scenes requested in the background at startup so the first transition to them is instant.
		/// </summary>
		private static readonly Scene[] _warmupScenes = [Scene.MainMenu, Scene.OptionsMenu, Scene.Level01];

		/// <summary>
		/// Resolved scenes. Once a <see cref="PackedScene"/> is here, later transitions skip the disk entirely.
		/// </summary>
		private readonly Dictionary<Scene, PackedScene> _cache = [];

		/// <summary>
		/// Loads currently running. Prevents issuing duplicate threaded requests for the same scene
		/// (e.g. a warm-up still running when the player already clicked the button).
		/// </summary>
		private readonly Dictionary<Scene, Task<PackedScene>> _inFlight = [];

		/// <summary>Canvas layer hosting the fade overlay.</summary>
		private CanvasLayer _fadeLayer;

		/// <summary>Full-screen rectangle whose alpha is animated to cover and reveal the screen.</summary>
		private ColorRect _fadeRect;

		/// <summary>True while a transition (or a quit) is running. Further requests are rejected.</summary>
		private bool _isTransitioning;

		#endregion // Fields

		#region Properties

		/// <summary>True while a scene transition is in progress.</summary>
		public bool IsTransitioning => _isTransitioning;

		#endregion // Properties

		#region Godot overrides

		public override void _Ready()
		{
			// Enforce a single instance by evicting any duplicate that gets added to the tree.
			if (Instance != null)
			{
				QueueFree();
				return;
			}

			Instance = this;

			// The controller must keep working while the tree is paused (e.g. leaving a
			// pause menu to the main menu). Tweens are configured the same way in FadeAsync.
			ProcessMode = ProcessModeEnum.Always;

			BuildFadeOverlay();

			// Fire-and-forget: the result is stored in _cache by the loading routine.
			foreach (var id in _warmupScenes)
				_ = GetSceneAsync(id);
		}

		public override void _ExitTree()
		{
			// Avoid leaving a dangling reference to a freed node.
			if (Instance == this)
				Instance = null;
		}

		#endregion // Godot overrides

		#region Public API

		/// <summary>
		/// Fire-and-forget entry point, convenient for signal handlers and button callbacks.
		/// Prefer <see cref="TransitionToAsync"/> when the caller needs to await completion.
		/// </summary>
		/// <param name="id">Scene to transition to.</param>
		public void TransitionTo(Scene id) => _ = TransitionToAsync(id);

		/// <summary>
		/// Changes to the given scene with a fade transition.
		/// Requests made while another transition is running are rejected rather than queued,
		/// which avoids double-clicks stacking transitions.
		/// If the scene fails to load, the current scene is kept and the screen simply fades back in.
		/// </summary>
		/// <param name="id">Scene to transition to.</param>
		/// <returns>A task that completes when the fade-in has finished.</returns>
		public async Task TransitionToAsync(Scene id)
		{
			if (_isTransitioning)
			{
				GD.PushWarning($"[SceneController] Ignoring transition to '{id}': another transition is in progress.");
				return;
			}

			_isTransitioning = true;
			TransitionStarted?.Invoke(id);

			try
			{
				ShowOverlay();

				// Start loading BEFORE fading so both run concurrently.
				// Task<T> is only awaited after the fade, so no frame is blocked.
				Task<PackedScene> loadTask = GetSceneAsync(id);

				await FadeAsync(1f, FadeOutDuration);

				PackedScene packed = await loadTask;

				// On failure, keep the current scene alive and just fade back in.
				if (packed != null)
				{
					SwapScene(packed);

					// Let the new scene render at least one frame under the overlay so
					// its first (potentially heavy) frame is never visible to the player.
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				}

				await FadeAsync(0f, FadeInDuration);
			}
			catch (Exception e)
			{
				// This method is usually fire-and-forget; an unobserved exception would vanish silently.
				GD.PushError($"[SceneController] Transition to '{id}' failed: {e}");
			}
			finally
			{
				// Always restore a clean state, even if something above threw.
				HideOverlay();
				_isTransitioning = false;
				TransitionFinished?.Invoke(id);
			}
		}

		/// <summary>
		/// Quits the game from any scene, including the main menu, after fading the screen out.
		/// Ignored while a transition is running.
		/// </summary>
		public async void QuitGame()
		{
			if (_isTransitioning)
				return;

			_isTransitioning = true;
			ShowOverlay();

			// Hook for future work: save game, flush settings, play a farewell sound, etc.
			await FadeAsync(1f, FadeOutDuration);

			GetTree().Quit();
		}

		/// <summary>
		/// Requests a scene in the background without transitioning to it.
		/// Call this ahead of time (e.g. when hovering a "Play" button).
		/// </summary>
		/// <param name="id">Scene to load in advance.</param>
		public void Preload(Scene id) => _ = GetSceneAsync(id);

		#endregion // Public API

		#region Scene resolution

		/// <summary>
		/// Returns a cached scene, joins a load already in progress, or starts a new one.
		/// </summary>
		/// <param name="id">Scene to resolve.</param>
		/// <returns>A task producing the loaded scene, or null if loading failed.</returns>
		private Task<PackedScene> GetSceneAsync(Scene id)
		{
			if (_cache.TryGetValue(id, out var cached))
				return Task.FromResult(cached);

			if (_inFlight.TryGetValue(id, out var running))
				return running;

			Task<PackedScene> task = LoadSceneAsync(id);

			// Only track tasks that are genuinely asynchronous. A task that already completed
			// (early failure) would otherwise sit in the dictionary forever.
			if (!task.IsCompleted)
				_inFlight[id] = task;

			return task;
		}

		/// <summary>
		/// Loads a <see cref="PackedScene"/> using Godot's threaded <see cref="ResourceLoader"/>.
		/// Disk IO and resource parsing happen on worker threads; this method only polls the status
		/// once per frame on the main thread, so it never blocks rendering.
		/// </summary>
		/// <param name="id">Scene to load. Must be registered in <c>_scenePaths</c>.</param>
		/// <returns>The loaded scene, also stored in the cache, or null on failure.</returns>
		private async Task<PackedScene> LoadSceneAsync(Scene id)
		{
			try
			{
				if (!_scenePaths.TryGetValue(id, out var path))
				{
					GD.PushWarning($"[SceneController] No scene registered for '{id}'. Add it to _scenePaths.");
					return null;
				}

				// useSubThreads lets the loader parallelise dependencies (textures, scripts, etc.).
				Error requestError = ResourceLoader.LoadThreadedRequest(path, "PackedScene", useSubThreads: true);

				if (requestError != Error.Ok)
				{
					GD.PushError($"[SceneController] Failed to request '{path}': {requestError}.");
					return null;
				}

				// Reused across polls to avoid allocating a new Array every frame.
				var progress = new Godot.Collections.Array();

				while (true)
				{
					var status = ResourceLoader.LoadThreadedGetStatus(path, progress);

					switch (status)
					{
						case ResourceLoader.ThreadLoadStatus.InProgress:
							// Yield to the engine; resume next frame on the main thread.
							await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
							break;

						case ResourceLoader.ThreadLoadStatus.Loaded:
							if (ResourceLoader.LoadThreadedGet(path) is not PackedScene packed)
							{
								GD.PushError($"[SceneController] '{path}' loaded, but it is not a PackedScene.");
								return null;
							}

							_cache[id] = packed;
							return packed;

						default: // Failed or InvalidResource
							GD.PushError($"[SceneController] Threaded load of '{path}' ended with status '{status}'. " +
										 "Verify the file exists and is exported.");
							return null;
					}
				}
			}
			finally
			{
				_inFlight.Remove(id);
			}
		}

		#endregion // Scene resolution

		#region Scene swapping

		/// <summary>
		/// Replaces the current scene with a new instance of the given one.
		/// The current scene is removed from the tree BEFORE the incoming one is added.
		/// <c>QueueFree</c> alone only defers destruction to the end of the frame, so both scenes would coexist
		/// for a frame. Removing first makes "one scene alive" actually true (the old node is still freed
		/// safely at end of frame).
		/// </summary>
		/// <param name="packed">Scene to instantiate and make current.</param>
		private void SwapScene(PackedScene packed)
		{
			SceneTree tree = GetTree();
			Node current = tree.CurrentScene;

			if (IsInstanceValid(current))
			{
				tree.Root.RemoveChild(current);
				current.QueueFree();
			}

			// Instantiate() runs on the main thread: scene nodes are not thread-safe to build and
			// attach. If a specific scene becomes heavy here, split its setup into stages.
			Node incoming = packed.Instantiate();
			tree.Root.AddChild(incoming);
			tree.CurrentScene = incoming;
		}

		#endregion // Scene swapping

		#region Fade overlay

		/// <summary>
		/// Creates the overlay nodes in code so the controller has no dependency on an extra .tscn file.
		/// The overlay starts hidden and fully transparent.
		/// </summary>
		private void BuildFadeOverlay()
		{
			_fadeLayer = new CanvasLayer
			{
				Name = "FadeLayer",
				Layer = FadeLayerIndex,
			};

			_fadeRect = new ColorRect
			{
				Name = "FadeRect",
				Color = new Color(FadeColor, 0f),
				MouseFilter = Control.MouseFilterEnum.Ignore,
				Visible = false, // Hidden while idle: no draw cost, no input interception.
			};

			_fadeRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);

			_fadeLayer.AddChild(_fadeRect);
			AddChild(_fadeLayer);
		}

		/// <summary>
		/// Makes the overlay visible and applies the current <see cref="FadeColor"/>, keeping its alpha.
		/// Input is blocked so the player cannot interact with the old scene mid-fade.
		/// </summary>
		private void ShowOverlay()
		{
			_fadeRect.Color = new Color(FadeColor, _fadeRect.Color.A);

			// Stop swallows clicks so the player cannot interact with the old scene mid-fade.
			_fadeRect.MouseFilter = Control.MouseFilterEnum.Stop;
			_fadeRect.Visible = true;
		}

		/// <summary>
		/// Resets the overlay to its idle state: transparent, hidden, and not intercepting input.
		/// </summary>
		private void HideOverlay()
		{
			_fadeRect.Color = new Color(FadeColor, 0f);
			_fadeRect.MouseFilter = Control.MouseFilterEnum.Ignore;
			_fadeRect.Visible = false;
		}

		/// <summary>
		/// Animates the overlay alpha to the target value. Keeps running while the tree is paused.
		/// </summary>
		/// <param name="targetAlpha">Final alpha, from 0 (transparent) to 1 (opaque).</param>
		/// <param name="duration">Animation time in seconds. 0 or less applies the value instantly.</param>
		/// <returns>A task that completes when the tween ends.</returns>
		private async Task FadeAsync(float targetAlpha, float duration)
		{
			if (duration <= 0f)
			{
				_fadeRect.Color = new Color(FadeColor, targetAlpha);
				return;
			}

			Tween tween = CreateTween();

			// Keep animating even if the tree is paused.
			tween.SetPauseMode(Tween.TweenPauseMode.Process);

			tween.TweenProperty(_fadeRect, "color:a", targetAlpha, duration)
				 .SetTrans(Tween.TransitionType.Sine)
				 .SetEase(Tween.EaseType.InOut);

			await ToSignal(tween, Tween.SignalName.Finished);
		}

		#endregion // Fade overlay
	}
}