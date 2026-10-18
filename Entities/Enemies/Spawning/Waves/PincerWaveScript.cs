using Godot;

namespace TouhouFuujinroku.Entities.Enemies.Spawning.Waves
{
    /// <summary>
    /// Crossing and converging patterns.
    /// Simultaneous groups with mirrored trajectories pressure both sides of the playfield at once.
    /// </summary>
    public sealed class PincerWaveScript : WaveScript
    {
        protected override void Compose()
        {
            // t = 1s: two diagonal lines cross in an X over the middle of the screen.
            var diagonal = PathPattern.Line(new Vector2(-0.1f, -0.1f), new Vector2(1.1f, 1.1f));
            Spawn(time: 1f, count: 5, interval: 0.4f, path: diagonal);
            Spawn(time: 1f, count: 5, interval: 0.4f, path: diagonal.Mirrored());

            // t = 6s: a single column zig-zagging down the playfield.
            var zigzag = PathPattern.Polyline(
                new Vector2(0.1f, -0.1f),
                new Vector2(0.9f, 0.2f),
                new Vector2(0.1f, 0.4f),
                new Vector2(0.9f, 0.6f),
                new Vector2(0.1f, 0.8f),
                new Vector2(0.5f, 1.1f));
            Spawn(time: 6f, count: 8, interval: 0.4f, path: zigzag);

            // t = 12s: pincer — two waves sweep in from opposite sides on weaving trajectories.
            var weave = PathPattern.Sine(new Vector2(-0.1f, 0.25f), new Vector2(1.1f, 0.25f), amplitude: 0.08f, cycles: 3f);
            Spawn(time: 12f, count: 8, interval: 0.3f, path: weave);
            Spawn(time: 12f, count: 8, interval: 0.3f, path: weave.Mirrored());
        }
    }
}