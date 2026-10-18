using System.Collections.Generic;
using System.Linq;

namespace TouhouFuujinroku.Entities.Enemies.Spawning
{
    /// <summary>
    /// Base class for a level's enemy "screenplay".
    /// <para>
    /// A script is plain C# data authoring: derive from it, override <see cref="Compose"/>, and describe
    /// the timeline with <see cref="Spawn"/> calls. Calls may be written in any order, since
    /// <see cref="Build"/> sorts the resulting events by time.
    /// </para>
    /// <para>
    /// Scripts hold no node references, so they are cheap to create, trivially testable,
    /// and can be swapped per level or per stage phase.
    /// </para>
    /// </summary>
    public abstract class WaveScript
    {
        #region Fields

        /// <summary>Events collected while <see cref="Compose"/> runs.</summary>
        private readonly List<WaveEvent> _events = [];

        #endregion // Fields

        #region Public API

        /// <summary>
        /// Composes the script and returns its events ordered by spawn time.
        /// Called once by the director when playback starts.
        /// </summary>
        /// <returns>Events sorted by <see cref="WaveEvent.Time"/>.</returns>
        public IReadOnlyList<WaveEvent> Build()
        {
            _events.Clear();
            Compose();

            // OrderBy is stable, so events sharing the same time keep their authored order.
            return _events.OrderBy(e => e.Time).ToList();
        }

        #endregion // Public API

        #region Authoring API

        /// <summary>
        /// Describes the script timeline. Override and call <see cref="Spawn"/> for each spawn group.
        /// </summary>
        protected abstract void Compose();

        /// <summary>
        /// Schedules a group of enemies sharing the same trajectory.
        /// </summary>
        /// <param name="time">Time in seconds, since the script started, when the first enemy spawns.</param>
        /// <param name="count">Number of enemies in the group.</param>
        /// <param name="interval">Seconds between consecutive enemies (0 = all at once).</param>
        /// <param name="path">Trajectory shared by every enemy in the group.</param>
        /// <param name="enemyId">Enemy identifier, see <see cref="EnemyIds"/>.</param>
        protected void Spawn(float time, int count, float interval, PathPattern path, string enemyId = EnemyIds.Generic)
        {
            _events.Add(new WaveEvent(time, count, interval, path, enemyId));
        }

        #endregion // Authoring API
    }
}