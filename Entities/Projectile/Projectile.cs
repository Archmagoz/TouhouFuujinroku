using Godot;
using TouhouFuujinroku.Interfaces;
using TouhouFuujinroku.Global.Controllers;

namespace TouhouFuujinroku.Entities
{
	[GlobalClass]
	public partial class Projectile : Area2D
	{
		// Stamped by ProjectilePool.CreateProjectile() — routes Return() to the correct queue.
		// Never set this manually; treat it as pool-internal state.
		public PackedScene Prefab { get; set; }

		// ---------------------------------------- Configuration ----------------------------------

		[ExportGroup("Configuration")]
		[Export] private float _speed = 300f;
		[Export] private int _damage = 1;

		// Direction of travel in radians — 0 = right, -π/2 = straight up.
		private float _angle;

		// World-space boundary beyond which a projectile is considered off-screen and returned
		// to the pool. Avoids viewport transform issues that arise when the projectile lives
		// in the autoload subtree rather than the level scene.
		private const float CullBoundary = 1000f;

		// ------------------------------------ Godot overrides ------------------------------------

		public override void _Ready()
		{
			AreaEntered += OnAreaEntered;
			BodyEntered += OnBodyEntered;
		}

		public override void _Process(double delta)
		{
			Position += Vector2.FromAngle(_angle) * _speed * (float)delta;

			// Cull in world space — immune to viewport resizes, fullscreen toggles, and
			// camera transforms. 1000 units comfortably exceeds any expected play area.
			if (Mathf.Abs(Position.X) > CullBoundary || Mathf.Abs(Position.Y) > CullBoundary)
				ProjectilePool.Instance.Return(this);
		}

		public override void _ExitTree()
		{
			AreaEntered -= OnAreaEntered;
		}

		// --------------------------------------- Public API --------------------------------------

		// Called by ProjectilePool.Rent() immediately after the node is re-enabled.
		// Resets all runtime state so recycled instances behave like fresh ones.
		public void Initialize(Vector2 position, float angle)
		{
			Position = position;
			Rotation = angle;
			_angle = angle;
		}

		// ---------------------------------------- Helpers ----------------------------------------

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