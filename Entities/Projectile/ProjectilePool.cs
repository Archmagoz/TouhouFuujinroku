using Godot;

using System.Collections.Generic;

namespace TouhouFuujinroku.Entities.Projectile
{
	/// <summary>
	/// Lifecycle-aware object pool for all projectiles in the game.
	/// <para>
	/// Instanced as a child node inside each level's scene, not as an Autoload. The level calls
	/// <see cref="Initialize"/> on <c>_Ready</c> and <see cref="Clear"/> on <c>_ExitTree</c>.
	/// </para>
	/// <para>
	/// All projectile nodes remain permanent children of the pool at all times, including while inactive.
	/// This prevents Godot from reporting orphan nodes when scenes transition and instantiated nodes
	/// lose their parent.
	/// </para>
	/// </summary>
	[GlobalClass]
	public partial class ProjectilePool : Node
	{
		#region Fields

		/// <summary>
		/// Pool of the currently loaded level. Set when the pool enters the tree and cleared when it leaves,
		/// so it never points at a freed node.
		/// </summary>
		public static ProjectilePool Instance { get; private set; }

		/// <summary>
		/// One queue per prefab. Each projectile type maintains its own recycling bin.
		/// </summary>
		private readonly Dictionary<PackedScene, Queue<Projectile>> _pools = [];

		/// <summary>
		/// Every projectile currently rented out. Guards <see cref="Return"/> against being called
		/// more than once for the same instance (double-return).
		/// </summary>
		private readonly HashSet<Projectile> _rented = [];

		/// <summary>
		/// Guards <see cref="Rent"/> against being called outside a level context.
		/// </summary>
		private bool _initialized;

		#endregion // Fields

		#region Godot overrides

		public override void _Ready()
		{
			// Evict duplicates rather than silently overwriting the existing instance.
			if (Instance != null)
			{
				QueueFree();
				return;
			}

			Instance = this;
		}

		public override void _ExitTree()
		{
			// Clears the static reference when the level unloads — without this, Instance
			// would keep pointing at a freed node after the level (and this pool) is gone.
			if (Instance == this)
				Instance = null;
		}

		#endregion // Godot overrides

		#region Public API

		/// <summary>
		/// Marks the pool as ready to serve projectiles. Calls <see cref="Clear"/> first,
		/// so re-entering a level starts from a clean state.
		/// </summary>
		/// <param name="optionalPrewarm">
		/// Prefab and count pairs instantiated immediately to avoid first-use stutter. Optional.
		/// </param>
		public void Initialize(Dictionary<PackedScene, int> optionalPrewarm = null)
		{
			Clear();
			_initialized = true;

			if (optionalPrewarm == null) return;

			foreach (var (prefab, count) in optionalPrewarm)
				for (int i = 0; i < count; i++)
					Return(CreateProjectile(prefab));
		}

		/// <summary>
		/// Frees every projectile node, both active and pooled, then resets the pool state.
		/// Safe to call mid-level: <c>GetChildren()</c> also covers nodes currently rented out
		/// (which are outside any queue).
		/// </summary>
		public void Clear()
		{
			foreach (var child in GetChildren())
				child.QueueFree();

			_pools.Clear();
			_rented.Clear();
			_initialized = false;
		}

		/// <summary>
		/// Pops or creates a projectile, re-enables all its systems, and initializes its position and angle.
		/// </summary>
		/// <param name="prefab">Projectile scene to rent. Its root node must be a <see cref="Projectile"/>.</param>
		/// <param name="position">Spawn position of the projectile.</param>
		/// <param name="angle">Travel direction in radians.</param>
		/// <returns>
		/// The activated projectile, or null (with a warning) if called outside an active level
		/// or if the prefab could not be instantiated as a <see cref="Projectile"/>.
		/// </returns>
		public Projectile Rent(PackedScene prefab, Vector2 position, float angle)
		{
			if (!_initialized)
			{
				GD.PushWarning("ProjectilePool.Rent: called outside an active level — returning null.");
				return null;
			}

			var projectile = GetOrCreate(prefab);
			if (projectile == null) return null;

			_rented.Add(projectile);

			SetProjectileActive(projectile, true);
			projectile.Initialize(position, angle);
			return projectile;
		}

		/// <summary>
		/// Disables all projectile systems and re-enqueues the projectile for reuse.
		/// The node stays in the tree as a child of this pool: it is never reparented or freed.
		/// Duplicate returns and projectiles created outside the pool are rejected with a diagnostic.
		/// </summary>
		/// <param name="projectile">Projectile to give back to the pool.</param>
		public void Return(Projectile projectile)
		{
			if (projectile.Prefab == null)
			{
				GD.PushError("ProjectilePool.Return: projectile has no Prefab reference — " +
							 "was it created outside the pool?");
				return;
			}

			// Reject double-returns — without this guard the same instance could end up
			// enqueued twice, letting a single Rent() call hand it out to two owners at once.
			if (!_rented.Remove(projectile))
			{
				GD.PushWarning("ProjectilePool.Return: projectile was already returned — ignoring duplicate call.");
				return;
			}

			SetProjectileActive(projectile, false);

			if (!_pools.TryGetValue(projectile.Prefab, out var queue))
			{
				queue = new Queue<Projectile>();
				_pools[projectile.Prefab] = queue;
			}

			queue.Enqueue(projectile);
		}

		#endregion // Public API

		#region Helpers

		/// <summary>
		/// Pops from the existing queue, or falls through to <see cref="CreateProjectile"/> if the queue is empty.
		/// </summary>
		/// <param name="prefab">Projectile scene to fetch.</param>
		/// <returns>A disabled projectile ready to be activated, or null if creation failed.</returns>
		private Projectile GetOrCreate(PackedScene prefab)
		{
			if (_pools.TryGetValue(prefab, out var queue) && queue.Count > 0)
				return queue.Dequeue();

			return CreateProjectile(prefab);
		}

		/// <summary>
		/// Instantiates a node, verifies its root is a <see cref="Projectile"/>, stamps its prefab reference,
		/// and adopts it as a permanent child in a disabled state.
		/// The caller is responsible for enqueuing it via <see cref="Return"/> or activating it via <see cref="Rent"/>.
		/// </summary>
		/// <param name="prefab">Projectile scene to instantiate.</param>
		/// <returns>The new disabled projectile, or null (with an error) if the root node is not a <see cref="Projectile"/>.</returns>
		private Projectile CreateProjectile(PackedScene prefab)
		{
			var node = prefab.Instantiate();

			if (node is not Projectile projectile)
			{
				node.QueueFree();
				GD.PushError($"ProjectilePool: prefab root is not a Projectile — got {node.GetType().Name}. " +
							 "Verify the scene's root node type and attached script.");
				return null;
			}

			projectile.Prefab = prefab;

			// Freshly created projectiles aren't in any queue yet — mark them as "out"
			// so the first Return() call (prewarm or otherwise) is accepted, not flagged
			// as a duplicate.
			_rented.Add(projectile);

			// Adopt before disabling — AddChild must run first or SetProcess calls have no effect.
			AddChild(projectile);
			SetProjectileActive(projectile, false);
			return projectile;
		}

		/// <summary>
		/// Toggles every system that would let a disabled projectile affect the simulation:
		/// visibility, per-frame callbacks, and both collision roles.
		/// </summary>
		/// <param name="projectile">Projectile to enable or disable.</param>
		/// <param name="active">True to enable all systems, false to disable them.</param>
		private void SetProjectileActive(Projectile projectile, bool active)
		{
			projectile.Visible = active;
			projectile.SetProcess(active);
			projectile.SetPhysicsProcess(active);
			projectile.Monitoring = active;
			projectile.Monitorable = active;
		}

		#endregion // Helpers
	}
}