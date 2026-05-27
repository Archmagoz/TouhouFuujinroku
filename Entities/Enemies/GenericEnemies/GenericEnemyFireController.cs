using Godot;
using Godot.Collections;
using TouhouFuujinroku.Entities.Enemies.GenericEnemies.Weapons;

namespace TouhouFuujinroku.Entities.Enemies.GenericEnemies
{
	// Reusable fire controller for generic enemies — manages cadence, shot quota, and weapon.
	// Weapons are children of this node; swap active weapon via SetWeapon() at runtime.
	public partial class GenericEnemyFireController : Node
	{
		[ExportGroup("Configuration")]
		// Projectile prefab passed to the active weapon on each salvo.
		[Export] private PackedScene _bulletPrefab;

		// Total number of salvos fired before this controller stops shooting.
		[Export] private int _shotCount = 3;

		// Seconds between each salvo.
		[Export] private float _fireRate = 0.5f;

		// Index of the weapon to activate on _Ready() — maps to the _weapons array.
		[Export] private int _startingWeaponIndex = 0;

		[ExportGroup("Weapons")]
		// All available weapons for this enemy — populated in the editor.
		// Order determines the index used by SetWeapon().
		[Export] private Array<EnemyWeapon> _weapons = [];

		//Node references - assigned in the editor.
		[ExportGroup("Components")]
		[Export] private AudioStreamPlayer2D _shotSound;

		// Currently active weapon — swapped via SetWeapon().
		private EnemyWeapon _activeWeapon;

		// Tracks remaining salvos and time until next shot.
		private int _remainingShots;
		private float _cooldown;

		// ---------------------------------- Godot overrides -----------------------------------

		public override void _Ready()
		{
			_remainingShots = _shotCount;
			SetWeapon(_startingWeaponIndex);
		}

		public override void _Process(double delta)
		{
			if (_cooldown > 0) _cooldown -= (float)delta;
		}

		// --------------------------------------- Public API -----------------------------------

		// Attempts to fire a salvo; silently no-ops while on cooldown or quota is exhausted.
		// Call every frame from the owning enemy's _Process().
		public void TryFire()
		{
			if (_remainingShots <= 0 || _cooldown > 0 || _activeWeapon == null || _bulletPrefab == null) return;

			_cooldown = _fireRate;
			_remainingShots--;
			_activeWeapon.Fire(_bulletPrefab);
			_shotSound.Play();
		}

		// Swaps the active weapon by index — call from the owning enemy to change fire patterns.
		// Resets the shot quota and cooldown so the new weapon starts from a clean state.
		public void SetWeapon(int index)
		{
			if (index < 0 || index >= _weapons.Count) return;

			_activeWeapon = _weapons[index];
			_remainingShots = _shotCount;
			_cooldown = 0f;
		}
	}
}