namespace TouhouFuujinroku.Entities.Enemies.Spawning
{
    /// <summary>
    /// Immutable description of a single scripted spawn group.
    /// Pure data: it knows nothing about nodes, prefabs, or the scene tree.
    /// </summary>
    /// <param name="Time">Time in seconds, since the script started, when the group starts spawning.</param>
    /// <param name="Count">Number of enemies in the group.</param>
    /// <param name="Interval">Seconds between consecutive enemies (0 = all at once).</param>
    /// <param name="Path">Trajectory shared by every enemy in the group.</param>
    /// <param name="EnemyId">Enemy identifier resolved by the director into a prefab.</param>
    public sealed record WaveEvent(float Time, int Count, float Interval, PathPattern Path, string EnemyId);
}