using Godot;
using TohouFuuujinoku.Global.Controllers;

namespace TohouFuuujinoku.Entities
{
	[GlobalClass]
	public partial class Projectile : Area2D
	{
		// Stamped by ProjectilePool.CreateProjectile() — routes Return() to the correct queue.
		// Never set this manually; treat it as pool-internal state.
		public PackedScene Prefab { get; set; }

		// ---------------------------------------- Configuration ----------------------------------

		[ExportGroup("Configuration")]
		[Export] public float Speed = 300f;
		[Export] public int Damage = 1;

		// Direction of travel in radians — 0 = right, -π/2 = straight up.
		private float _angle;

		// World-space boundary beyond which a projectile is considered off-screen and returned
		// to the pool. Avoids viewport transform issues that arise when the projectile lives
		// in the autoload subtree rather than the level scene.
		private const float CullBoundary = 1000f;

		// ------------------------------------ Godot overrides ------------------------------------

		public override void _Process(double delta)
		{
			Position += Vector2.FromAngle(_angle) * Speed * (float)delta;

			// Cull in world space — immune to viewport resizes, fullscreen toggles, and
			// camera transforms. 1000 units comfortably exceeds any expected play area.
			if (Mathf.Abs(GlobalPosition.X) > CullBoundary || Mathf.Abs(GlobalPosition.Y) > CullBoundary)
				ProjectilePool.Instance.Return(this);
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
	}
}