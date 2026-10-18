using Godot;

using System.Collections.Generic;

using TouhouFuujinroku.Entities.Projectile;

namespace TouhouFuujinroku.Entities.Enemies.GenericEnemies.Weapons
{
	/// <summary>
	/// Spread-shot weapon that fires one projectile per <see cref="Marker2D"/> child in a single salvo.
	/// <para>
	/// Markers define shot directions only; every projectile spawns at the weapon's origin.
	/// Spread patterns are authored in the scene by placing markers, with no code changes needed.
	/// </para>
	/// </summary>
	public partial class Shotgun : EnemyWeapon
	{
		#region Protected API

		/// <summary>
		/// Fires one projectile toward each <see cref="Marker2D"/> child, all spawning at the weapon's
		/// global position. The angle from the origin to a marker defines that projectile's travel direction.
		/// </summary>
		/// <param name="prefab">Bullet prefab rented from the <see cref="ProjectilePool"/> for each projectile.</param>
		protected override void Fire(PackedScene prefab)
		{
			foreach (var (position, angle) in GetFireData())
				ProjectilePool.Instance.Rent(prefab, position, angle);
		}

		#endregion // Protected API

		#region Helpers

		/// <summary>
		/// Enumerates the spawn position and direction angle for each <see cref="Marker2D"/> child.
		/// Non-marker children are ignored.
		/// </summary>
		/// <returns>
		/// One tuple per marker: the weapon's global position (shared by all markers) and the
		/// angle of the marker's local position, in radians.
		/// </returns>
		private IEnumerable<(Vector2 position, float angle)> GetFireData()
		{
			foreach (var child in GetChildren())
				if (child is Marker2D marker)
					yield return (GlobalPosition, marker.Position.Angle());
		}

		#endregion // Helpers
	}
}