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

		// Paths available for spawning — each fairy picks one at random from its own pool.
		// Assign PathFollow2D nodes from the scene tree in the editor.
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

		// Instantiates a fairy, assigns the full path pool, and adds it to the scene.
		private void SpawnFairy()
		{
			var fairy = _fairyPrefab.Instantiate<SmallFairy>();
			AddChild(fairy);
			fairy.SetPathPool(_spawnPaths);
		}
	}
}