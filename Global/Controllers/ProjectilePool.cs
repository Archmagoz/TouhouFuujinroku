using Godot;
using System.Collections.Generic;
using TouhouFuujinroku.Entities;

namespace TouhouFuujinroku.Global.Controllers
{
	// Lifecycle-aware object pool for all projectiles in the game.
	// Lives as an Autoload — always present, but only active inside a level.
	// Call Initialize() on level _Ready() and Clear() on level _ExitTree().
	//
	// All projectile nodes remain children of this pool at all times — including
	// while inactive. This prevents Godot from counting them as orphan nodes,
	// which occurs whenever an instantiated node has no parent in the scene tree.
	public partial class ProjectilePool : Node
	{
		public static ProjectilePool Instance { get; private set; }

		// Separate queue per prefab — one pool per projectile type.
		private readonly Dictionary<PackedScene, Queue<Projectile>> _pools = new();

		// Guards Rent() against being called outside a level context.
		private bool _initialized = false;

		// ------------------------------------- Godot overrides ------------------------------------

		public override void _Ready() => Instance = this;

		// --------------------------------------- Public API ---------------------------------------

		// Called by the level's _Ready(). Marks the pool as ready to serve projectiles.
		// optionalPrewarm: prefab/count pairs to instantiate immediately — skips first-use stutter.
		public void Initialize(Dictionary<PackedScene, int> optionalPrewarm = null)
		{
			Clear();
			_initialized = true;

			if (optionalPrewarm == null) return;

			foreach (var (prefab, count) in optionalPrewarm)
				for (int i = 0; i < count; i++)
					Return(CreateProjectile(prefab));
		}

		// Called by the level's TreeExiting signal. Frees all projectile nodes — both active
		// and pooled. Because all nodes are permanent children of this pool, QueueFree() is
		// guaranteed to work correctly with no orphans left behind.
		public void Clear()
		{
			foreach (var child in GetChildren())
				child.QueueFree();

			_pools.Clear();
			_initialized = false;
		}

		// Retrieves or creates a Projectile, re-enables it, and initializes its state.
		// Returns null and logs a warning if called outside an active level.
		public Projectile Rent(PackedScene prefab, Vector2 position, float angle)
		{
			if (!_initialized)
			{
				GD.PushWarning("ProjectilePool.Rent: called outside an active level — returning null.");
				return null;
			}

			var projectile = GetOrCreate(prefab);
			if (projectile == null) return null;

			// Re-enable all systems disabled by Return() — node stays in the tree the whole time.
			SetProjectileActive(projectile, true);

			projectile.Initialize(position, angle);
			return projectile;
		}

		// Disables and hides a projectile, returning it to the pool for reuse.
		// The node is NOT removed from the scene tree — keeping it as a child of this
		// autoload prevents orphan node warnings during scene transitions.
		public void Return(Projectile projectile)
		{
			SetProjectileActive(projectile, false);

			if (!_pools.TryGetValue(projectile.Prefab, out var queue))
			{
				queue = new Queue<Projectile>();
				_pools[projectile.Prefab] = queue;
			}

			queue.Enqueue(projectile);
		}

		// ---------------------------------------- Helpers ----------------------------------------

		// Pops from the existing queue or instantiates a fresh node if the pool is empty.
		private Projectile GetOrCreate(PackedScene prefab)
		{
			if (_pools.TryGetValue(prefab, out var queue) && queue.Count > 0)
				return queue.Dequeue();

			return CreateProjectile(prefab);
		}

		// Instantiates a Projectile, stamps its prefab reference, and permanently adopts it
		// as a child of this pool. The node is disabled immediately — it must be explicitly
		// enqueued via Return() by the caller (prewarm) or activated via Rent() on first use.
		private Projectile CreateProjectile(PackedScene prefab)
		{
			var node = prefab.Instantiate();
			var projectile = node as Projectile;

			if (projectile == null)
			{
				node.QueueFree();
				GD.PushError($"ProjectilePool: prefab root is not a Projectile — got {node.GetType().Name}. " +
							 "Verify the scene's root node type and attached script.");
				return null;
			}

			projectile.Prefab = prefab;

			// Add as permanent child and disable immediately — enqueuing is the caller's responsibility.
			// Separating adoption from enqueuing prevents double-enqueue when prewarm calls Return().
			AddChild(projectile);
			SetProjectileActive(projectile, false);
			return projectile;
		}

		// Toggles all systems of a projectile on or off — used by Rent() and Return()
		// to activate or deactivate without removing the node from the scene tree.
		private void SetProjectileActive(Projectile projectile, bool active)
		{
			projectile.Visible = active;
			projectile.SetProcess(active);
			projectile.Monitoring = active;
			projectile.Monitorable = active;
		}
	}
}