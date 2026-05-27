using Godot;

namespace TohouFuuujinoku.Entities.Enemies.GenericEnemies
{
	public partial class SmallFairy : Area2D
	{
		[ExportGroup("Configuration")]
		// Movement speed along the assigned path in pixels per second.
		[Export] private float _speed = 100f;

		// Health points — enemy is defeated when this reaches zero.
		[Export] private int _health = 100;

		[ExportGroup("Components")]
		[Export] private GenericEnemyFireController _fireController;

		// Exclusive PathFollow2D instance — duplicated from the source path by the spawner.
		// Owned by this fairy; freed alongside it when despawning.
		private PathFollow2D _pathFollow;

		// ---------------------------------- Godot overrides -----------------------------------

		public override void _Process(double delta)
		{
			HandleMovement(delta);
			_fireController.TryFire();
		}

		// --------------------------------------- Public API -----------------------------------

		// Receives an already-duplicated PathFollow2D owned exclusively by this fairy.
		// Called by the spawner immediately after instantiation.
		public void SetPath(PathFollow2D pathFollow)
		{
			_pathFollow = pathFollow;
			_pathFollow.Progress = 0f;
			GlobalPosition = _pathFollow.GlobalPosition;
		}

		// ---------------------------------- Private helpers -----------------------------------

		private void HandleMovement(double delta)
		{
			if (_pathFollow == null) return;

			_pathFollow.Progress += _speed * (float)delta;
			GlobalPosition = _pathFollow.GlobalPosition;

			// Despawn once the path is fully traversed — free the owned PathFollow2D too.
			if (_pathFollow.ProgressRatio >= 1f)
			{
				_pathFollow.QueueFree();
				QueueFree();
			}
		}
	}
}