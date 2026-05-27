using Godot;
using TouhouFuujinroku.Components;
using TouhouFuujinroku.Interfaces;

namespace TouhouFuujinroku.Entities.Enemies.GenericEnemies
{
	public partial class SmallFairy : Area2D, IDamageable
	{
		[ExportGroup("Configuration")]
		// Movement speed along the assigned path in pixels per second.
		[Export] private SpeedComponent _speed;

		// Health points — enemy is defeated when this reaches zero.
		[Export] private HealthComponent _health;

		[ExportGroup("Components")]
		[Export] private GenericEnemyFireController _fireController;

		// Exclusive PathFollow2D instance — duplicated from the source path by the spawner.
		// Owned by this fairy; freed alongside it when despawning.
		private PathFollow2D _pathFollow;

		// IDamageable implementation — simply forwards to the HealthComponent.
		public void ApplyDamage(int amount) => _health.ApplyDamage(amount);

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

			_pathFollow.Progress += _speed.CurrentSpeed * (float)delta;
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