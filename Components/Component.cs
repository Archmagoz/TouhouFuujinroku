using Godot;

namespace TouhouFuujinroku.Components
{
    /// <summary>
    /// Base class for all reusable gameplay components (health, hitbox, movement, and so on).
    /// <para>
    /// A component is a <see cref="Node"/> that encapsulates a single responsibility and is attached
    /// as a child of the entity that uses it. The owner talks to it through its public API and
    /// reacts to it through signals, so entities are composed instead of inheriting behavior.
    /// </para>
    /// </summary>
    public partial class Component : Node;
}