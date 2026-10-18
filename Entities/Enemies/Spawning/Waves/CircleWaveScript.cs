using Godot;

namespace TouhouFuujinroku.Entities.Enemies.Spawning.Waves
{
    /// <summary>
    /// Orbiting formations.
    /// Enemies enter from a side, orbit a fixed point, and leave, forming a rotating ring.
    /// </summary>
    public sealed class CircleWaveScript : WaveScript
    {
        protected override void Compose()
        {
            // t = 1s: 10 enemies enter from the left, orbit the screen center for 1.5 turns (clockwise)
            // and exit to the right. Starting on the left edge of the circle, 1.5 turns ends exactly
            // on its right edge, so the exit segment is a clean horizontal line.
            var centerOrbit = PathPattern.Circle(
                from: new Vector2(-0.1f, 0.4f),
                center: new Vector2(0.5f, 0.4f),
                radius: 0.2f,
                turns: 1.5f,
                to: new Vector2(1.1f, 0.4f));

            Spawn(time: 1f, count: 10, interval: 0.3f, path: centerOrbit);

            // t = 9s: mirrored — enters from the right and orbits counter-clockwise.
            Spawn(time: 9f, count: 10, interval: 0.3f, path: centerOrbit.Mirrored());

            // t = 17s: two small rings at once. Enemies dive from the top, loop once, and retreat upward.
            var leftRing = PathPattern.Circle(
                from: new Vector2(0.3f, -0.1f),
                center: new Vector2(0.3f, 0.3f),
                radius: 0.14f,
                turns: 1f,
                to: new Vector2(0.3f, -0.1f));

            Spawn(time: 17f, count: 8, interval: 0.35f, path: leftRing);
            Spawn(time: 17f, count: 8, interval: 0.35f, path: leftRing.Mirrored());
        }
    }
}