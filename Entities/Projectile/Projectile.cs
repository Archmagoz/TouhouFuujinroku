using Godot;

using TouhouFuujinroku.Interfaces;

namespace TouhouFuujinroku.Entities.Projectile
{
	[GlobalClass, Icon("res://addons/at-icons/node2d/bullet.svg")]
	public partial class Projectile : Area2D
	{
		// Stamped by ProjectilePool.CreateProjectile() — routes Return() to the correct queue.
		// Never set this manually; treat it as pool-internal state.
		public PackedScene Prefab { get; set; }

		// Configuration ---------------------------------------------------------------------------------------

		[ExportGroup("Configuration")]
		[Export] private float _speed = 300f;
		[Export] private int _damage = 1;

		// Resolved from the angle passed to Initialize() — used to move the projectile each frame.
		private Vector2 _direction;

		// World-space boundary beyond which a projectile is considered off-screen and returned
		// to the pool. Avoids viewport transform issues that arise when the projectile lives
		// in the autoload subtree rather than the level scene.
		private const float CullBoundary = 1000f;

		// Godot overrides -------------------------------------------------------------------------------------

		public override void _Process(double delta)
		{
			Position += _direction * _speed * (float)delta;

			// Cull in world space — immune to viewport resizes, fullscreen toggles, and
			// camera transforms. 1000 units comfortably exceeds any expected play area.
			if (Mathf.Abs(Position.X) > CullBoundary || Mathf.Abs(Position.Y) > CullBoundary)
				ProjectilePool.Instance.Return(this);
		}

		// Public API ------------------------------------------------------------------------------------------

		// Called by ProjectilePool.Rent() immediately after the node is re-enabled.
		// Resets all runtime state so recycled instances behave like fresh ones.
		public void Initialize(Vector2 position, float angle)
		{
			Position = position;
			Rotation = angle;
			_direction = Vector2.FromAngle(angle);
		}

		// Helpers ---------------------------------------------------------------------------------------------

		// Applies damage to any IDamageable in the overlapping area.
		// Collision layer filtering is handled in the editor — no type checks needed here.
		private void OnAreaEntered(Area2D area)
		{
			if (area is IDamageable damageable)
				damageable.ApplyDamage(_damage);
		}

		private void OnBodyEntered(Node2D body)
		{
			if (body is IDamageable damageable)
				damageable.ApplyDamage(_damage);
		}
	}
}