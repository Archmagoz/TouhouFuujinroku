using Godot;
using Godot.Collections;
using TohouFuuujinoku.Global.Controllers;
using TohouFuuujinoku.Entities.Enemies.GenericEnemies;

namespace TohouFuuujinoku.Levels.Debug
{
	public partial class Debug : Node2D
	{
		[ExportGroup("Components")]
		[Export] private RichTextLabel _label;

		[ExportGroup("Spawning")]
		// Fairy prefab to instantiate on each spawn tick.
		[Export] private PackedScene _fairyPrefab;

		// Source paths used as templates for spawning.
		// Each fairy receives its own duplicated PathFollow2D instance,
		// preventing shared progress/state between enemies.
		[Export] private Array<PathFollow2D> _spawnPaths = [];

		// Seconds between spawns — ramps from 2.0 down to 0.4 as the level progresses.
		[Export] private float _spawnIntervalStart = 2f;
		[Export] private float _spawnIntervalEnd = 0.4f;

		// Total duration over which the interval ramps from start to end, in seconds.
		[Export] private float _rampDuration = 30f;

		// Elapsed level time — drives both the ramp calculation and the spawn timer.
		private float _elapsed;
		private float _spawnCooldown;

		// ---------------------------------- Godot overrides -----------------------------------

		public override void _Ready()
		{
			// TreeExiting fires reliably even when the scene is freed via QueueFree(),
			// unlike _ExitTree() which may not trigger during deferred scene transitions.
			TreeExiting += () => ProjectilePool.Instance.Clear();
			ProjectilePool.Instance.Initialize();

			_spawnCooldown = _spawnIntervalStart;
		}

		public override void _Process(double delta)
		{
			_label.Text = $"DEBUG\nFPS: {Engine.GetFramesPerSecond()}";

			HandleSpawning(delta);
		}

		// ---------------------------------- Private helpers -----------------------------------

		// Decrements the spawn timer and fires a fairy when it expires.
		// The interval shrinks linearly from _spawnIntervalStart to _spawnIntervalEnd
		// over _rampDuration seconds, then holds at _spawnIntervalEnd.
		private void HandleSpawning(double delta)
		{
			if (_fairyPrefab == null || _spawnPaths.Count == 0) return;

			_elapsed += (float)delta;
			_spawnCooldown -= (float)delta;

			if (_spawnCooldown > 0) return;

			SpawnFairy();

			// Recalculate the next interval based on current elapsed time.
			float t = Mathf.Clamp(_elapsed / _rampDuration, 0f, 1f);
			_spawnCooldown = Mathf.Lerp(_spawnIntervalStart, _spawnIntervalEnd, t);
		}

		// Instantiates a fairy and gives it a unique duplicated path pool.
		// Each duplicated PathFollow2D maintains independent progress/state,
		// avoiding synchronization issues between enemies sharing the same node.
		private void SpawnFairy()
		{
			var fairy = _fairyPrefab.Instantiate<SmallFairy>();
			AddChild(fairy);

			Array<PathFollow2D> uniquePathPool = [];

			foreach (var sourcePath in _spawnPaths)
			{
				if (sourcePath == null) continue;

				// Duplicate the full PathFollow2D hierarchy/state for this fairy only.
				var pathCopy = sourcePath.Duplicate() as PathFollow2D;

				if (pathCopy == null) continue;

				// PathFollow2D requires a valid parent path structure.
				// Reattach the duplicate under the same Path2D owner.
				sourcePath.GetParent().AddChild(pathCopy);

				// Ensure every fairy starts from its own clean progress state.
				pathCopy.Progress = 0f;
				pathCopy.ProgressRatio = 0f;

				uniquePathPool.Add(pathCopy);
			}

			fairy.SetPathPool(uniquePathPool);
		}
	}
}