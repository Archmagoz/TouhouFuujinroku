using Godot;

using TouhouFuujinroku.Entities.Projectile;

namespace TouhouFuujinroku.Interfaces
{
    public interface IProjectilePool
    {
        Projectile Rent(PackedScene prefab, Vector2 position, float angle);
        void Return(Projectile projectile);
        void ReturnAll();
    }
}
