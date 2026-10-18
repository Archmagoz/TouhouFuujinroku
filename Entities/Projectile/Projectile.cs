using Godot;

using TouhouFuujinroku.Interfaces;

namespace TouhouFuujinroku.Entities.Projectile
{
	/// <summary>
	/// Straight-moving projectile that damages any <see cref="IDamageable"/> it touches.
	/// <para>
	/// Instances are pooled: they are rented from and returned to the <see cref="ProjectilePool"/> instead of
	/// being instantiated and freed. <see cref="Initialize"/> must reset every piece of runtime state so a
	/// recycled instance behaves exactly like a fresh one.
	/// </para>
	/// </summary>
	[GlobalClass]
	public partial class Projectile : Area2D
	{
		#region Fields

		/// <summary>
		/// Prefab this instance was created from. Stamped by <c>ProjectilePool.CreateProjectile()</c> and used to
		/// route <c>Return()</c> to the correct queue. Pool-internal state: never set it manually.
		/// </summary>
		public PackedScene Prefab { get; set; }

		/// <summary>
		/// Travel speed in pixels per second.
		/// </summary>
		[ExportGroup("Configuration")]
		[Export] private float _speed = 300f;

		/// <summary>
		/// Damage applied to each <see cref="IDamageable"/> on contact.
		/// </summary>
		[Export] private int _damage = 1;

		/// <summary>
		/// Unit travel direction, resolved from the angle passed to <see cref="Initialize"/>.
		/// </summary>
		private Vector2 _direction;

		/// <summary>
		/// Distance from the origin, on either axis, beyond which a projectile is considered off-screen and
		/// returned to the pool. Evaluated against <c>Position</c> instead of viewport coordinates, which avoids
		/// viewport transform issues (resizes, fullscreen toggles, camera movement). Assumes the level root
		/// sits at the world origin, so local and world coordinates match.
		/// </summary>
		private const float CullBoundary = 1000f;

		#endregion // Fields

		#region Godot overrides

		public override void _Process(double delta)
		{
			Position += _direction * _speed * (float)delta;

			// Cull in world space — immune to viewport resizes, fullscreen toggles, and
			// camera transforms. 1000 units comfortably exceeds any expected play area.
			if (Mathf.Abs(Position.X) > CullBoundary || Mathf.Abs(Position.Y) > CullBoundary)
				ProjectilePool.Instance.Return(this);
		}

		#endregion // Godot overrides

		#region Public API

		/// <summary>
		/// Prepares the projectile for a new flight. Called by <c>ProjectilePool.Rent()</c> immediately after
		/// the node is re-enabled, and resets all runtime state so recycled instances behave like fresh ones.
		/// </summary>
		/// <param name="position">Spawn position, in the same space as the pool (level-local, equal to world space at the origin).</param>
		/// <param name="angle">Travel direction in radians. Also applied as the node's rotation.</param>
		public void Initialize(Vector2 position, float angle)
		{
			Position = position;
			Rotation = angle;
			_direction = Vector2.FromAngle(angle);
		}

		#endregion // Public API

		#region Event handlers

		/// <summary>
		/// Applies damage to any <see cref="IDamageable"/> area that overlaps the projectile.
		/// Collision layer filtering is handled in the editor, so no type checks beyond the interface are needed.
		/// Connected to the <c>area_entered</c> signal in the editor.
		/// </summary>
		/// <param name="area">Overlapping area.</param>
		private void OnAreaEntered(Area2D area)
		{
			if (area is IDamageable damageable)
				damageable.ApplyDamage(_damage);
		}

		/// <summary>
		/// Applies damage to any <see cref="IDamageable"/> physics body that overlaps the projectile.
		/// Connected to the <c>body_entered</c> signal in the editor.
		/// </summary>
		/// <param name="body">Overlapping body.</param>
		private void OnBodyEntered(Node2D body)
		{
			if (body is IDamageable damageable)
				damageable.ApplyDamage(_damage);
		}

		#endregion // Event handlers
	}
}