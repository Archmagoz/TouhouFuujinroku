using Godot;
using Godot.Collections;

namespace TohouFuuujinoku.Entities.Enemies.GenericEnemies
{
	public partial class SmallFairy : Area2D
	{
		[ExportGroup("Configuration")]
		// Movement speed along the assigned path in pixels per second.
		[Export] private float _speed = 100f;

		// Health points — enemy is defeated when this reaches zero.
		[Export] private int _health = 3;

		[ExportGroup("Components")]
		[Export] private GenericEnemyFireController _fireController;

		// The randomly selected path this instance will follow.
		// Assigned externally via SetPathPool() by the spawner after instantiation.
		private PathFollow2D _pathFollow;

		// ---------------------------------- Godot overrides -----------------------------------

		public override void _Ready()
		{
			AreaEntered += OnAreaEntered;
		}

		public override void _Process(double delta)
		{
			HandleMovement(delta);
			_fireController.TryFire();
		}

		// --------------------------------------- Public API -----------------------------------

		// Called by the spawner immediately after instantiation — selects a random path
		// from the pool and positions the fairy at its start point.
		public void SetPathPool(Array<PathFollow2D> paths)
		{
			if (paths.Count == 0) return;

			_pathFollow = paths[GD.RandRange(0, paths.Count - 1)];
			GlobalPosition = _pathFollow.GlobalPosition;
		}

		// ---------------------------------- Private helpers -----------------------------------

		private void HandleMovement(double delta)
		{
			if (_pathFollow == null) return;

			_pathFollow.Progress += _speed * (float)delta;
			GlobalPosition = _pathFollow.GlobalPosition;

			// Remove self once the path is fully traversed.
			if (_pathFollow.ProgressRatio >= 1f)
				QueueFree();
		}

		// Handles incoming hits from player projectiles.
		private void OnAreaEntered(Area2D area)
		{
			if (area is not Projectile projectile) return;

			Global.Controllers.ProjectilePool.Instance.Return(projectile);
			_health -= projectile.Damage;

			if (_health <= 0)
				QueueFree();
		}
	}
}