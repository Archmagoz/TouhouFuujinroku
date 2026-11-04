using Godot;

namespace TouhouFuujinroku.Entities.Enemies.GenericEnemies.Weapons
{
    /// <summary>
    /// Base class for self-contained enemy weapons.
    /// <para>
    /// A weapon owns all of its combat configuration (cadence, shot quota, bullet prefab, audio) and is
    /// composed directly into the enemy scene, so no runtime instantiation is needed. The owning enemy only
    /// calls <see cref="TryFire"/>; how a shot is actually fired is defined by each derived weapon type
    /// through <see cref="Fire"/>.
    /// </para>
    /// </summary>
    public abstract partial class EnemyWeapon : Node2D
    {
        /// <summary>
        /// Bullet prefab instantiated on each shot. Defined per weapon, not per enemy.
        /// </summary>
        [ExportGroup("Configuration")]
        [Export] private PackedScene _bulletPrefab;

        /// <summary>
        /// Total shots fired before this weapon goes silent. A value of 0 disables the weapon.
        /// </summary>
        [Export] private int _shotCount;

        /// <summary>
        /// Seconds between shots.
        /// </summary>
        [Export] private float _fireRate;

        /// <summary>
        /// Sound played on every shot. Required: <see cref="TryFire"/> assumes it is assigned.
        /// </summary>
        [ExportGroup("Components")]
        [Export] private AudioStreamPlayer2D _shotSound;

        /// <summary>
        /// Shots left in the quota. Initialized from <c>_shotCount</c> on spawn.
        /// </summary>
        private int _remainingShots;

        /// <summary>
        /// Seconds until the next shot is allowed. Starts at 0, so the first shot is available immediately.
        /// </summary>
        private float _cooldown;

        public override void _Ready()
        {
            _remainingShots = _shotCount;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_cooldown > 0f) _cooldown -= (float)delta;
        }

        /// <summary>
        /// Attempts to fire a shot. Silently does nothing while on cooldown, once the shot quota
        /// is exhausted, or if no bullet prefab is assigned.
        /// Intended to be called every frame from the owning enemy's <c>_PhysicsProcess</c>.
        /// </summary>
        public void TryFire()
        {
            if (_remainingShots <= 0 || _cooldown > 0f || _bulletPrefab == null) return;

            _cooldown = _fireRate;
            _remainingShots--;
            _shotSound.Play();
            Fire(_bulletPrefab);
        }

        /// <summary>
        /// Fires a single shot. The firing pattern (direction, spread, spawn position) is defined by each weapon type.
        /// </summary>
        /// <param name="prefab">Bullet prefab to instantiate, as configured on this weapon.</param>
        protected abstract void Fire(PackedScene prefab);
    }
}