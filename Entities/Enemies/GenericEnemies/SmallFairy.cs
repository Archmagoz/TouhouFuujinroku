using Godot;
using System;
using TouhouFuujinroku.Components;
using TouhouFuujinroku.Entities.Enemies.GenericEnemies.Weapons;
using TouhouFuujinroku.Interfaces;

namespace TouhouFuujinroku.Entities.Enemies.GenericEnemies
{
	public partial class SmallFairy : Area2D, IDamageable, IScoreable
	{
		// Each SmallFairy variant declares its own point value directly in the scene via export.
		[Export] private long _pointValue;
		public long PointValue => _pointValue;

		// IScoreable implementation — raised on death with the point value.
		public event Action<long> Died;

		// ------------------------------------- Components ------------------------------------

		[ExportGroup("Components")]
		// Weapon node composed directly into this scene — configured entirely on the weapon side.
		[Export] private EnemyWeapon _weapon;
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

		// ---------------------------------- Godot overrides ----------------------------------

		public override void _Ready()
		{
			_health.Death += OnDeath;
		}

		public override void _EnterTree()
		{
			// Added here instead of _Ready() — NodeAdded fires during _EnterTree(),
			// so the group membership must exist at this point for listeners to catch it.
			AddToGroup("enemies");
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

			_weapon?.TryFire();
		}

		// --------------------------------------- Public API ----------------------------------

		// Receives an already-duplicated PathFollow2D owned exclusively by this fairy.
		// Called by the spawner immediately after instantiation.
		public void SetPath(PathFollow2D pathFollow)
		{
			_pathFollow = pathFollow;
			_pathFollow.Progress = 0f;
			GlobalPosition = _pathFollow.GlobalPosition;
		}

		// ---------------------------------- Private helpers ----------------------------------

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

			// Notify score listeners before the animation starts — node is still valid here.
			Died?.Invoke(_pointValue);

			_sprite.Play("death");
			_sprite.AnimationFinished += QueueFree;
		}
	}
}