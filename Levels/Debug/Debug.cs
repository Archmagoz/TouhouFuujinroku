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

		// Source paths used as templates — one PathFollow2D per Path2D in the scene.
		// Each fairy receives a duplicate so instances never share progress state.
		[Export] private Array<PathFollow2D> _spawnPaths = [];

		// Seconds between spawns — ramps from start to end over _rampDuration seconds.
		[Export] private float _spawnIntervalStart = 2f;
		[Export] private float _spawnIntervalEnd = 0.4f;

		// Total duration over which the interval ramps from start to end, in seconds.
		[Export] private float _rampDuration = 30f;

		// Jitter factor applied to each spawn interval — prevents fairies from spawning
		// in sync, which would cause audio clipping when salvos overlap.
		// Value of 0.2 = ±20% of the base interval.
		[Export] private float _spawnJitter = 0.2f;

		// Elapsed level time — drives both the ramp calculation and the spawn timer.
		private float _elapsed;
		private float _spawnCooldown;

		// ---------------------------------- Godot overrides -----------------------------------

		public override void _Ready()
		{
			// TreeExiting fires reliably even when the scene is freed via QueueFree(),
			// unlike _ExitTree() which may not trigger during deferred scene transitions.
			TreeExiting += () => ProjectilePool.Instance.Clear();
			_spawnCooldown = _spawnIntervalStart;
			ProjectilePool.Instance.Initialize();
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

			// Apply jitter to desynchronize spawns — prevents salvos from overlapping
			// and audio from clipping when multiple fairies fire simultaneously.
			float t = Mathf.Clamp(_elapsed / _rampDuration, 0f, 1f);
			float baseInterval = Mathf.Lerp(_spawnIntervalStart, _spawnIntervalEnd, t);
			float jitter = baseInterval * _spawnJitter;
			_spawnCooldown = baseInterval + (float)GD.RandRange(-jitter, jitter);
		}

		// Picks a random source path, duplicates its PathFollow2D, and assigns it
		// exclusively to the new fairy — each instance owns its own progress state.
		private void SpawnFairy()
		{
			var sourcePath = _spawnPaths[GD.RandRange(0, _spawnPaths.Count - 1)];
			var pathCopy = sourcePath.Duplicate() as PathFollow2D;

			// PathFollow2D must be a child of a Path2D to read the curve correctly.
			sourcePath.GetParent().AddChild(pathCopy);

			var fairy = _fairyPrefab.Instantiate<SmallFairy>();
			AddChild(fairy);
			fairy.SetPath(pathCopy);
		}
	}
}