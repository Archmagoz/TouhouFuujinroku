using Godot;

namespace TouhouFuujinroku.Entities.Enemies.GenericEnemies.Weapons
{
    // Abstract base for all enemy weapons — enforces a common Fire() contract.
    // Attach concrete weapon scenes as children of GenericEnemyFireController.
    public abstract partial class EnemyWeapon : Node2D
    {
        // Fires a salvo using the provided prefab — implementation defined by each weapon type.
        public abstract void Fire(PackedScene prefab);
    }
}