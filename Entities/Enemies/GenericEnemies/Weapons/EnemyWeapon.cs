using Godot;

namespace TouhouFuujinroku.Entities.Enemies.GenericEnemies.Weapons
{
    // Self-contained enemy weapon — owns all combat configuration (cadence, quota, prefab, audio).
    // Composed directly into the enemy scene; no runtime instantiation needed.
    [Icon("res://addons/at-icons/node2d/gun.svg")]
    public abstract partial class EnemyWeapon : Node2D
    {
        // Configuration --------------------------------------------------------------------------------------

        [ExportGroup("Configuration")]
        // Bullet prefab instantiated on each shot — defined per weapon, not per enemy.
        [Export] private PackedScene _bulletPrefab;

        // Total shots fired before this weapon goes silent.
        [Export] private int _shotCount;

        // Seconds between shots.
        [Export] private float _fireRate;

        [ExportGroup("Components")]
        [Export] private AudioStreamPlayer2D _shotSound;

        private int _remainingShots;
        private float _cooldown;

        // Godot overrides ------------------------------------------------------------------------------------

        public override void _Ready()
        {
            _remainingShots = _shotCount;
        }

        public override void _Process(double delta)
        {
            if (_cooldown > 0f) _cooldown -= (float)delta;
        }

        // Public API -----------------------------------------------------------------------------------------

        // Attempts to fire a shot; silently no-ops while on cooldown or quota is exhausted.
        // Call every frame from the owning enemy's _Process().
        public void TryFire()
        {
            if (_remainingShots <= 0 || _cooldown > 0f || _bulletPrefab == null) return;

            _cooldown = _fireRate;
            _remainingShots--;
            Fire(_bulletPrefab);
            _shotSound.Play();
        }

        // Fires a shot using the stored prefab — implementation defined by each weapon type.
        protected abstract void Fire(PackedScene prefab);
    }
}