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

		// Cached viewport rect — kept valid across fullscreen toggles and window resizes
		// by subscribing to SizeChanged in _Ready(). Avoids per-frame API calls in _Process().
		private Rect2 _viewportRect;

		// ------------------------------------ Godot overrides ------------------------------------

		public override void _Ready()
		{
			_viewportRect = GetViewportRect();

			// Invalidate the cache whenever the viewport dimensions change — covers both
			// manual window resizing and fullscreen toggles.
			GetViewport().SizeChanged += () => _viewportRect = GetViewportRect();
		}

		public override void _Process(double delta)
		{
			Position += Vector2.FromAngle(_angle) * Speed * (float)delta;

			// Convert world position to screen space before checking against the viewport rect.
			// GlobalPosition is in world space; _viewportRect is in screen space.
			if (!_viewportRect.HasPoint(GetViewportTransform() * GlobalPosition))
				ProjectilePool.Instance.Return(this);
		}

		// --------------------------------------- Public API --------------------------------------

		// Called by ProjectilePool.Rent() immediately after the node is re-enabled.
		// Resets all runtime state so recycled instances behave like fresh ones.
		public void Initialize(Vector2 position, float angle)
		{
			Position = position;
			_angle = angle;
			Rotation = angle;
		}
	}
}