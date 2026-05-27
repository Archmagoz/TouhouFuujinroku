namespace TouhouFuujinroku.Interfaces
{
    // Defines a contract for entities that can receive and process damage.
    public interface IDamageable
    {
        // Applies a damage value to the implementing entity.
        // The internal handling (health reduction, death, effects) is implementation-specific.
        void ApplyDamage(int damage);
    }
}