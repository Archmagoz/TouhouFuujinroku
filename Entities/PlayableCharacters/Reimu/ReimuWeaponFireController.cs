using Godot;
using TohouFuuujinoku.Global.Controllers;

namespace TohouFuuujinoku.Entities.PlayableCharacters
{
	public partial class ReimuWeaponFireController : Node
	{
		//Node references - assigned in the editor.
		[ExportGroup("Components")]
		[Export] private AudioStreamPlayer2D _shotSound;

		[ExportGroup("Configuration")]
		[Export] private ReimuWeapon _weapon;
		[Export] private PackedScene _bulletPrefab;
		[Export] private float _fireRate = 0.1f;

		// Seconds remaining before the next shot is allowed.
		private float _cooldown;

		// ---------------------------------- Godot overrides -----------------------------------

		public override void _Process(double delta)
		{
			if (_cooldown > 0) _cooldown -= (float)delta;
		}

		// --------------------------------------- Public API -----------------------------------

		// Attempts to fire from all weapon origins; silently no-ops while on cooldown.
		// Call every frame the shoot input is active — cooldown is enforced internally.
		public void TryFire()
		{
			if (_cooldown > 0) return;

			_cooldown = _fireRate;
			FireFromAllOrigins();
		}

		// ---------------------------------- Private helpers -----------------------------------

		// Rents one projectile per fire origin — straight up (-π/2) by default.
		private void FireFromAllOrigins()
		{
			foreach (var origin in _weapon.GetFireOrigins())
				ProjectilePool.Instance.Rent(_bulletPrefab, origin, angle: -Mathf.Pi / 2f);

			_shotSound.Play();
		}
	}
}