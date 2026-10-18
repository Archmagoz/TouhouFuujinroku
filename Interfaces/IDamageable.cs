namespace TouhouFuujinroku.Interfaces
{
    /// <summary>
    /// Contract for entities that can receive and process damage.
    /// <para>
    /// Damage sources (projectiles, hazards) depend on this interface instead of concrete entity types,
    /// so any player, enemy, or destructible can be hit without the source knowing what it is.
    /// </para>
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Applies a damage value to the implementing entity.
        /// The internal handling (health reduction, death, effects) is implementation-specific.
        /// </summary>
        /// <param name="damage">Amount of damage to apply. Expected to be non-negative.</param>
        void ApplyDamage(int damage);
    }
}