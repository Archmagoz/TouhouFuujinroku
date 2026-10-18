namespace TouhouFuujinroku.Entities.Enemies.Spawning
{
    /// <summary>
    /// String keys used by wave scripts to request an enemy type.
    /// Each key must be registered in the <c>WaveDirector</c> prefab dictionary.
    /// Constants avoid typos and keep scripts free of any <c>PackedScene</c> reference.
    /// </summary>
    public static class EnemyIds
    {
        /// <summary>Default enemy type, backed by <c>GenericEnemy</c>.</summary>
        public const string Generic = "generic";
        /// <summary>Small fairy enemy, the common fodder unit.</summary>
        public const string SmallFairy = "SmallFairy";
    }
}