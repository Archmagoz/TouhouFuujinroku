using Godot;
using System.Collections.Generic;
using TohouFuuujinoku.Global.Controllers;

namespace TohouFuuujinoku.Entities.Enemies.GenericEnemies.Weapons
{
	// Spread-shot weapon — fires one projectile per Marker2D child in a single salvo.
	// Markers define shot directions; all projectiles spawn at the weapon's origin.
	public partial class Shotgun : EnemyWeapon
	{
		// --------------------------------------- Public API -----------------------------------

		// Fires one projectile toward each Marker2D child, all spawning at GlobalPosition.
		// The angle from origin to marker defines each projectile's travel direction.
		public override void Fire(PackedScene prefab)
		{
			foreach (var (position, angle) in GetFireData())
				ProjectilePool.Instance.Rent(prefab, position, angle);
		}

		// ---------------------------------------- Helpers ------------------------------------

		// Yields the spawn position and direction angle for each Marker2D child.
		// All projectiles originate from GlobalPosition — markers define angles only.
		private IEnumerable<(Vector2 position, float angle)> GetFireData()
		{
			foreach (var child in GetChildren())
				if (child is Marker2D marker)
					// Use the marker's local position as direction vector — 
					// independent of where the weapon is in world space.
					yield return (GlobalPosition, marker.Position.Angle());
		}
	}
}