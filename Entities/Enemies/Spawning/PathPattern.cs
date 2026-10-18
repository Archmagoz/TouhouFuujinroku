using System;
using Godot;

namespace TouhouFuujinroku.Entities.Enemies.Spawning
{
    /// <summary>
    /// Resolution-independent description of an enemy trajectory.
    /// <para>
    /// Coordinates are normalized to the playfield: (0, 0) is the top-left corner and (1, 1)
    /// is the bottom-right corner. Values outside the 0 to 1 range are off-screen, which is how
    /// enemies enter and leave the playfield (e.g. x = -0.1 is just past the left edge).
    /// </para>
    /// <para>
    /// A pattern is lazy: it only becomes world-space points when <see cref="GetPoints"/> is called
    /// with the actual playfield rectangle. Lengths such as radii and amplitudes are fractions of the
    /// playfield's smaller side, so circles stay circular regardless of the aspect ratio.
    /// </para>
    /// </summary>
    public sealed class PathPattern
    {
        #region Fields

        /// <summary>Sampling density of circular arcs, in points per full revolution.</summary>
        private const int PointsPerTurn = 48;

        /// <summary>Sampling density of sine trajectories, in points per full period.</summary>
        private const int PointsPerSineCycle = 32;

        /// <summary>Deferred point generator that resolves the pattern for a given playfield.</summary>
        private readonly Func<Rect2, Vector2[]> _generator;

        #endregion // Fields

        #region Construction

        /// <summary>
        /// Creates a pattern from a deferred point generator. Use the static factories instead.
        /// </summary>
        /// <param name="generator">Function mapping a playfield rectangle to world-space points.</param>
        private PathPattern(Func<Rect2, Vector2[]> generator)
        {
            _generator = generator;
        }

        #endregion // Construction

        #region Public API

        /// <summary>
        /// Resolves the pattern into world-space points for the given playfield.
        /// </summary>
        /// <param name="playfield">Rectangle, in level-local coordinates, that normalized points are mapped onto.</param>
        /// <returns>Ordered world-space points describing the trajectory.</returns>
        public Vector2[] GetPoints(Rect2 playfield) => _generator(playfield);

        /// <summary>
        /// Creates a copy of this pattern reflected horizontally around the playfield's center.
        /// Mirroring also inverts the rotation direction of circular patterns.
        /// </summary>
        /// <returns>A new, mirrored pattern. The original is left untouched.</returns>
        public PathPattern Mirrored() => new(playfield =>
        {
            var points = _generator(playfield);
            float axis = playfield.Position.X * 2f + playfield.Size.X;

            for (int i = 0; i < points.Length; i++)
                points[i].X = axis - points[i].X;

            return points;
        });

        #endregion // Public API

        #region Factories

        /// <summary>
        /// Straight segment from one point to another.
        /// </summary>
        /// <param name="from">Start point, in playfield-normalized coordinates.</param>
        /// <param name="to">End point, in playfield-normalized coordinates.</param>
        public static PathPattern Line(Vector2 from, Vector2 to) => Polyline(from, to);

        /// <summary>
        /// Sequence of straight segments through the given waypoints.
        /// </summary>
        /// <param name="waypoints">Ordered waypoints, in playfield-normalized coordinates.</param>
        public static PathPattern Polyline(params Vector2[] waypoints)
        {
            // Defensive copy — the caller's array must not be able to mutate the pattern later.
            var copy = (Vector2[])waypoints.Clone();

            return new PathPattern(playfield =>
            {
                var points = new Vector2[copy.Length];
                for (int i = 0; i < copy.Length; i++)
                    points[i] = ToWorld(copy[i], playfield);
                return points;
            });
        }

        /// <summary>
        /// Enters at <paramref name="from"/>, travels radially to the circle around <paramref name="center"/>,
        /// orbits it, then leaves toward <paramref name="to"/>.
        /// The orbit starts at the angle where <paramref name="from"/> sits relative to <paramref name="center"/>.
        /// </summary>
        /// <param name="from">Entry point, in playfield-normalized coordinates.</param>
        /// <param name="center">Center of the orbit, in playfield-normalized coordinates.</param>
        /// <param name="radius">Orbit radius as a fraction of the playfield's smaller side.</param>
        /// <param name="turns">
        /// Number of revolutions. Positive is clockwise on screen, negative is counter-clockwise.
        /// Fractional values are allowed (1.5 turns starting on the left ends on the right).
        /// </param>
        /// <param name="to">Exit point, in playfield-normalized coordinates.</param>
        public static PathPattern Circle(Vector2 from, Vector2 center, float radius, float turns, Vector2 to)
        {
            return new PathPattern(playfield =>
            {
                Vector2 worldFrom = ToWorld(from, playfield);
                Vector2 worldCenter = ToWorld(center, playfield);
                Vector2 worldTo = ToWorld(to, playfield);
                float worldRadius = radius * Mathf.Min(playfield.Size.X, playfield.Size.Y);

                float startAngle = (worldFrom - worldCenter).Angle();
                float sweep = turns * Mathf.Tau;
                int steps = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(turns) * PointsPerTurn));

                // Layout: [from] + (steps + 1) arc points + [to].
                var points = new Vector2[steps + 3];
                points[0] = worldFrom;

                for (int i = 0; i <= steps; i++)
                {
                    float angle = startAngle + sweep * i / steps;
                    points[i + 1] = worldCenter + Vector2.FromAngle(angle) * worldRadius;
                }

                points[^1] = worldTo;
                return points;
            });
        }

        /// <summary>
        /// Straight trajectory with a sinusoidal lateral offset.
        /// </summary>
        /// <param name="from">Start point, in playfield-normalized coordinates.</param>
        /// <param name="to">End point, in playfield-normalized coordinates.</param>
        /// <param name="amplitude">Peak lateral offset as a fraction of the playfield's smaller side.</param>
        /// <param name="cycles">Number of full sine periods between <paramref name="from"/> and <paramref name="to"/>.</param>
        public static PathPattern Sine(Vector2 from, Vector2 to, float amplitude, float cycles)
        {
            return new PathPattern(playfield =>
            {
                Vector2 worldFrom = ToWorld(from, playfield);
                Vector2 worldTo = ToWorld(to, playfield);
                Vector2 normal = (worldTo - worldFrom).Normalized().Orthogonal();
                float worldAmplitude = amplitude * Mathf.Min(playfield.Size.X, playfield.Size.Y);
                int steps = Mathf.Max(2, Mathf.CeilToInt(cycles * PointsPerSineCycle));

                var points = new Vector2[steps + 1];
                for (int i = 0; i <= steps; i++)
                {
                    float u = (float)i / steps;
                    float offset = Mathf.Sin(u * cycles * Mathf.Tau) * worldAmplitude;
                    points[i] = worldFrom.Lerp(worldTo, u) + normal * offset;
                }

                return points;
            });
        }

        #endregion // Factories

        #region Helpers

        /// <summary>
        /// Maps a playfield-normalized coordinate into world space.
        /// </summary>
        /// <param name="normalized">Point in playfield-normalized coordinates.</param>
        /// <param name="playfield">Rectangle the point is mapped onto.</param>
        /// <returns>The corresponding world-space point.</returns>
        private static Vector2 ToWorld(Vector2 normalized, Rect2 playfield)
        {
            return playfield.Position + normalized * playfield.Size;
        }

        #endregion // Helpers
    }
}