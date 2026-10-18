using Godot;

using System;

using TouhouFuujinroku.Components;
using TouhouFuujinroku.Entities.Enemies.GenericEnemies.Weapons;
using TouhouFuujinroku.Interfaces;

namespace TouhouFuujinroku.Entities.Enemies.GenericEnemies
{
	/// <summary>
	/// Basic enemy that follows a scripted path, fires its weapon continuously, and reports its score on death.
	/// <para>
	/// Each variant is a scene that configures its own components (health, speed, weapon, sprite) and
	/// point value through exports. Spawning, path creation, and score wiring are owned by the
	/// <c>WaveDirector</c>, so the enemy only handles its own behavior.
	/// </para>
	/// </summary>
	[GlobalClass]
	public partial class GenericEnemy : Area2D, IDamageable, IScoreable
	{
		#region Fields

		/// <summary>
		/// Points awarded to the player on death. Each variant declares its own value directly in the scene.
		/// </summary>
		[Export] private long _pointValue;

		/// <summary>Points awarded to the player on death.</summary>
		public long PointValue => _pointValue;

		/// <summary>
		/// Weapon node composed directly into this scene — configured entirely on the weapon side.
		/// </summary>
		[ExportGroup("Components")]
		[Export] private EnemyWeapon _weapon;

		/// <summary>Sprite that plays the movement visuals and the "death" animation.</summary>
		[Export] private AnimatedSprite2D _sprite;

		/// <summary>Health state. Its Death signal drives <see cref="OnDeath"/>.</summary>
		[Export] private HealthComponent _health;

		/// <summary>Source of the movement speed, in path pixels per second.</summary>
		[Export] private SpeedComponent _speed;

		/// <summary>
		/// PathFollow2D owned exclusively by this enemy, created by the spawner together with its Path2D.
		/// Freed alongside the enemy on despawn or death.
		/// </summary>
		private PathFollow2D _pathFollow;

		/// <summary>
		/// Locks movement and firing during the death animation. Set when the Death signal is handled.
		/// </summary>
		private bool _dying = false;

		#endregion // Fields

		#region Godot overrides

		public override void _PhysicsProcess(double delta)
		{
			if (_dying) return;

			HandleMovement(delta);
			_weapon.TryFire();
		}

		#endregion // Godot overrides

		#region Public API

		/// <summary>
		/// Raised once on death, carrying the point value. Subscribed to by score listeners (e.g. the HUD).
		/// Not raised when the enemy despawns by reaching the end of its path.
		/// </summary>
		public event Action<long> Died;

		/// <summary>
		/// Forwards damage to the <see cref="HealthComponent"/>.
		/// </summary>
		/// <param name="amount">Amount of damage to apply.</param>
		public void ApplyDamage(int amount) => _health.ApplyDamage(amount);

		/// <summary>
		/// Assigns the trajectory this enemy follows and snaps it to the starting point.
		/// The PathFollow2D must already be inside the scene tree and owned exclusively by this enemy.
		/// Called by the spawner immediately after instantiation.
		/// </summary>
		/// <param name="pathFollow">Exclusive PathFollow2D whose Path2D parent holds the trajectory curve.</param>
		public void SetPath(PathFollow2D pathFollow)
		{
			_pathFollow = pathFollow;
			_pathFollow.Progress = 0f;
			GlobalPosition = _pathFollow.GlobalPosition;
		}

		#endregion // Public API

		#region Event handlers

		/// <summary>
		/// Handles <see cref="HealthComponent"/>'s Death signal: locks movement, reports the score,
		/// plays the death animation, and queues the node for removal once the animation finishes.
		/// </summary>
		private void OnDeath()
		{
			_dying = true;
			_pathFollow?.QueueFree();

			// Notify score listeners before the animation starts — node is still valid here.
			Died?.Invoke(_pointValue);

			_sprite.Play("death");
			_sprite.AnimationFinished += QueueFree;
		}

		#endregion // Event handlers

		#region Helpers

		/// <summary>
		/// Advances along the path at the current speed and despawns once the path is fully traversed.
		/// </summary>
		/// <param name="delta">Physics frame time in seconds.</param>
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

		#endregion // Helpers
	}
}