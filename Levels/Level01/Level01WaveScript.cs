using Godot;

using TouhouFuujinroku.Entities.Enemies.Spawning;

namespace TouhouFuujinroku.Levels.Level01
{
    /// <summary>
    /// Enemy screenplay of level 1: ten waves with a steadily growing number of enemies.
    /// <para>
    /// Each wave introduces or recombines a movement pattern (sweeps, snaking columns, crossing diagonals,
    /// orbits, rings), and the total enemy count per wave rises from 4 up to 24 in the finale.
    /// Waves are spaced so the previous one has mostly left the playfield when the next begins.
    /// </para>
    /// <para>
    /// To rebalance the level, change the start times, counts, and intervals in the wave methods.
    /// Positions are normalized to the playfield (see <see cref="PathPattern"/>).
    /// </para>
    /// </summary>
    public sealed class Level01WaveScript : WaveScript
    {
        #region Constants

        // Normalized coordinates just outside each playfield edge, where enemies enter and leave.
        private const float OffLeft = -0.1f;
        private const float OffRight = 1.1f;
        private const float OffTop = -0.1f;
        private const float OffBottom = 1.1f;

        #endregion // Constants

        #region Composition

        protected override void Compose()
        {
            Wave01();
            Wave02();
            Wave03();
            Wave04();
            Wave05();
            Wave06();
            Wave07();
            Wave08();
            Wave09();
            Wave10();
        }

        /// <summary>
        /// Wave 1 (t = 2s, 4 enemies): a short sweep from the left to ease the player in.
        /// </summary>
        private void Wave01()
        {
            var sweep = PathPattern.Line(new Vector2(OffLeft, 0.18f), new Vector2(OffRight, 0.18f));

            Spawn(time: 2f, count: 4, interval: 0.6f, path: sweep);
        }

        /// <summary>
        /// Wave 2 (t = 8s, 6 enemies): the sweep reflected, entering from the right and a little lower.
        /// </summary>
        private void Wave02()
        {
            var sweep = PathPattern.Line(new Vector2(OffLeft, 0.3f), new Vector2(OffRight, 0.3f));

            Spawn(time: 8f, count: 6, interval: 0.5f, path: sweep.Mirrored());
        }

        /// <summary>
        /// Wave 3 (t = 15s, 8 enemies): a column snaking down through the center.
        /// </summary>
        private void Wave03()
        {
            var snake = PathPattern.Sine(new Vector2(0.5f, OffTop), new Vector2(0.5f, OffBottom), amplitude: 0.15f, cycles: 2f);

            Spawn(time: 15f, count: 8, interval: 0.45f, path: snake);
        }

        /// <summary>
        /// Wave 4 (t = 23s, 10 enemies): two diagonal lines crossing in an X over the middle of the screen.
        /// </summary>
        private void Wave04()
        {
            var diagonal = PathPattern.Line(new Vector2(OffLeft, OffTop), new Vector2(OffRight, OffBottom));

            Spawn(time: 23f, count: 5, interval: 0.45f, path: diagonal);
            Spawn(time: 23f, count: 5, interval: 0.45f, path: diagonal.Mirrored());
        }

        /// <summary>
        /// Wave 5 (t = 31s, 12 enemies): a single column zig-zagging down the playfield.
        /// </summary>
        private void Wave05()
        {
            var zigzag = PathPattern.Polyline(
                new Vector2(0.1f, OffTop),
                new Vector2(0.9f, 0.2f),
                new Vector2(0.1f, 0.4f),
                new Vector2(0.9f, 0.6f),
                new Vector2(0.1f, 0.8f),
                new Vector2(0.5f, OffBottom));

            Spawn(time: 31f, count: 12, interval: 0.4f, path: zigzag);
        }

        /// <summary>
        /// Wave 6 (t = 40s, 14 enemies): the first orbit. Enemies enter from the left, circle the center
        /// clockwise, and exit to the right.
        /// </summary>
        private void Wave06()
        {
            Spawn(time: 40f, count: 14, interval: 0.3f, path: CenterOrbit(turns: 1.5f));
        }

        /// <summary>
        /// Wave 7 (t = 49s, 16 enemies): a mirrored orbit, followed by a sweep along the top that
        /// pins the player down while the ring is still turning.
        /// </summary>
        private void Wave07()
        {
            var topSweep = PathPattern.Line(new Vector2(OffLeft, 0.12f), new Vector2(OffRight, 0.12f));

            Spawn(time: 49f, count: 8, interval: 0.35f, path: CenterOrbit(turns: 1.5f).Mirrored());
            Spawn(time: 52f, count: 8, interval: 0.4f, path: topSweep);
        }

        /// <summary>
        /// Wave 8 (t = 59s, 18 enemies): a pincer. Weaving lines sweep in from both sides at once.
        /// </summary>
        private void Wave08()
        {
            var weave = PathPattern.Sine(new Vector2(OffLeft, 0.25f), new Vector2(OffRight, 0.25f), amplitude: 0.08f, cycles: 3f);

            Spawn(time: 59f, count: 9, interval: 0.3f, path: weave);
            Spawn(time: 59f, count: 9, interval: 0.3f, path: weave.Mirrored());
        }

        /// <summary>
        /// Wave 9 (t = 69s, 20 enemies): two rings at once. Enemies dive from the top, loop once, and retreat upward.
        /// </summary>
        private void Wave09()
        {
            var ring = PathPattern.Circle(
                from: new Vector2(0.3f, OffTop),
                center: new Vector2(0.3f, 0.3f),
                radius: 0.14f,
                turns: 1f,
                to: new Vector2(0.3f, OffTop));

            Spawn(time: 69f, count: 10, interval: 0.3f, path: ring);
            Spawn(time: 69f, count: 10, interval: 0.3f, path: ring.Mirrored());
        }

        /// <summary>
        /// Wave 10 (t = 80s, 24 enemies): the finale. Two long orbits (clockwise and counter-clockwise) fill the
        /// center of the screen at the same time.
        /// </summary>
        private void Wave10()
        {
            var orbit = CenterOrbit(turns: 2.5f);

            Spawn(time: 80f, count: 12, interval: 0.3f, path: orbit);
            Spawn(time: 80f, count: 12, interval: 0.3f, path: orbit.Mirrored());
        }

        #endregion // Composition

        #region Helpers

        /// <summary>
        /// Orbit around the screen center, entering from the left and exiting to the right.
        /// A half-integer number of turns ends the orbit on the right edge of the circle, so the exit is a clean line.
        /// </summary>
        /// <param name="turns">Number of revolutions. Positive is clockwise.</param>
        /// <returns>The orbit pattern.</returns>
        private static PathPattern CenterOrbit(float turns) => PathPattern.Circle(
            from: new Vector2(OffLeft, 0.4f),
            center: new Vector2(0.5f, 0.4f),
            radius: 0.2f,
            turns: turns,
            to: new Vector2(OffRight, 0.4f));

        #endregion // Helpers
    }
}