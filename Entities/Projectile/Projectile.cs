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

		// Cached viewport rect — refreshed on Initialize() to avoid per-frame allocation.
		private Rect2 _viewportRect;

		// ------------------------------------ Godot overrides ------------------------------------

		public override void _Process(double delta)
		{
			Position += Vector2.FromAngle(_angle) * Speed * (float)delta;

			// Return to pool once the projectile exits the viewport.
			// GlobalPosition is used so the check is independent of any parent transform.
			if (!_viewportRect.HasPoint(GetViewportTransform() * GlobalPosition))
				ProjectilePool.Instance.Return(this);
		}

		// --------------------------------------- Public API --------------------------------------

		// Called by ProjectilePool.Rent() immediately after the node is added to the scene.
		// Resets all runtime state so recycled instances behave like fresh ones.
		public void Initialize(Vector2 position, float angle)
		{
			Position = position;
			_angle = angle;
			Rotation = angle;
			_viewportRect = GetViewportRect();
		}
	}
}