namespace TouhouFuujinroku.Interfaces
{
    // Implemented by any entity that awards points on death.
    // Consumers connect to Died to receive the point value without knowing the concrete type.
    public interface IScoreable
    {
        long PointValue { get; }

        // Raised on death — carries the point value for any listener to consume.
        event System.Action<long> Died;
    }
}