using Godot;
using TouhouFuujinroku.Components;
using TouhouFuujinroku.Interfaces;

namespace TouhouFuujinroku.Entities.Enemies.GenericEnemies
{
	public partial class SmallFairy : Area2D, IDamageable
	{
		[ExportGroup("Components")]
		[Export] private GenericEnemyFireController _fireController;
		[Export] private AnimatedSprite2D _sprite;
		[Export] private HealthComponent _health;
		[Export] private SpeedComponent _speed;

		// Exclusive PathFollow2D instance — duplicated from the source path by the spawner.
		// Owned by this fairy; freed alongside it when despawning.
		private PathFollow2D _pathFollow;

		// Locks movement and firing during the death animation — set on Death signal.
		private bool _dying = false;

		// IDamageable implementation — forwards damage to the HealthComponent.
		public void ApplyDamage(int amount) => _health.ApplyDamage(amount);

		// ---------------------------------- Godot overrides -----------------------------------

		public override void _Ready()
		{
			// Subscribe to the health component's death signal — drives the death sequence.
			_health.Death += OnDeath;
		}

		public override void _ExitTree()
		{
			_health.Death -= OnDeath;
		}

		public override void _PhysicsProcess(double delta)
		{
			if (_dying) return;

			HandleMovement(delta);
		}

		public override void _Process(double delta)
		{
			if (_dying) return;

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

		// Triggered by HealthComponent.Death — locks movement, plays death animation,
		// and queues the node for removal once the animation finishes.
		private void OnDeath()
		{
			_dying = true;
			_pathFollow?.QueueFree();

			_sprite.Play("death");

			// Despawn once the death animation completes.
			_sprite.AnimationFinished += QueueFree;
		}
	}
}