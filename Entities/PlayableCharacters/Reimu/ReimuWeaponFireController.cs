using Godot;
using TouhouFuujinroku.Global.Controllers;

namespace TouhouFuujinroku.Entities.PlayableCharacters
{
	// Handles fire cadence, projectile spawning, and shot audio for ReimuWeapon.
	// Lives as a child of ReimuWeapon — all external calls go through the parent.
	public partial class ReimuWeaponFireController : Node
	{
		[ExportGroup("Configuration")]
		[Export] private PackedScene _bulletPrefab;
		[Export] private float _fireRate = 0.1f;

		[ExportGroup("Components")]
		[Export] private AudioStreamPlayer2D _shotSound;

		// Seconds remaining before the next shot is allowed.
		private float _cooldown;

		// ---------------------------------- Godot overrides -----------------------------------

		public override void _Process(double delta)
		{
			if (_cooldown > 0) _cooldown -= (float)delta;
		}

		// --------------------------------------- Public API -----------------------------------

		// Attempts to fire from all weapon origins; silently no-ops while on cooldown.
		// Called exclusively by the parent ReimuWeapon — not by external systems.
		public void TryFire()
		{
			if (_cooldown > 0) return;

			_cooldown = _fireRate;
			FireFromAllOrigins();
		}

		// ---------------------------------- Private helpers -----------------------------------

		// Retrieves fire origins from the parent ReimuWeapon and rents one projectile per origin.
		// Angle fixed at -π/2 — straight up, matching Touhou's vertical scrolling convention.
		private void FireFromAllOrigins()
		{
			var weapon = GetParent<ReimuWeapon>();

			foreach (var origin in weapon.GetFireOrigins())
				ProjectilePool.Instance.Rent(_bulletPrefab, origin, angle: -Mathf.Pi / 2f);

			_shotSound?.Play();
		}
	}
}