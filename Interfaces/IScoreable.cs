namespace TouhouFuujinroku.Interfaces
{
    /// <summary>
    /// Implemented by any entity that awards points on death.
    /// <para>
    /// Consumers connect to <see cref="Died"/> to receive the point value without knowing the concrete type.
    /// </para>
    /// </summary>
    public interface IScoreable
    {
        /// <summary>
        /// Points awarded to the player when this entity dies.
        /// </summary>
        long PointValue { get; }

        /// <summary>
        /// Raised on death, carrying the point value for any listener to consume.
        /// Implementations must raise it only for kills, not for despawns such as leaving the playfield.
        /// </summary>
        event System.Action<long> Died;
    }
}