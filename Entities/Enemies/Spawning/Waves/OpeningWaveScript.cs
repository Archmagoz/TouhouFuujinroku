using Godot;

namespace TouhouFuujinroku.Entities.Enemies.Spawning.Waves
{
    /// <summary>
    /// Gentle opening: straight lines and a sine column.
    /// Introduces horizontal sweeps before enemies start moving laterally.
    /// </summary>
    public sealed class OpeningWaveScript : WaveScript
    {
        protected override void Compose()
        {
            // Pattern defined once and reused: left-to-right sweep at 20% of the playfield height.
            var leftToRight = PathPattern.Line(new Vector2(-0.1f, 0.2f), new Vector2(1.1f, 0.2f));

            // t = 1s: 6 enemies enter from the left, one every 0.5s.
            Spawn(time: 1f, count: 6, interval: 0.5f, path: leftToRight);

            // t = 5s: the same sweep reflected, entering from the right.
            Spawn(time: 5f, count: 6, interval: 0.5f, path: leftToRight.Mirrored());

            // t = 10s: 8 enemies descend through the center on a snaking trajectory.
            var snake = PathPattern.Sine(new Vector2(0.5f, -0.1f), new Vector2(0.5f, 1.1f), amplitude: 0.15f, cycles: 2.5f);
            Spawn(time: 10f, count: 8, interval: 0.4f, path: snake);
        }
    }
}