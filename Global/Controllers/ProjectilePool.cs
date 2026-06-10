using Godot;
using System.Collections.Generic;
using TouhouFuujinroku.Entities;

namespace TouhouFuujinroku.Global.Controllers
{
	// Lifecycle-aware object pool for all projectiles in the game.
	// Lives as an Autoload — always present, but only active inside a level.
	// Call Initialize() on level _Ready() and Clear() on level _ExitTree().
	//
	// All projectile nodes remain permanent children of this pool at all times,
	// including while inactive — prevents Godot from reporting orphan nodes
	// when scenes transition and instantiated nodes lose their parent.
	public partial class ProjectilePool : Node
	{
		public static ProjectilePool Instance { get; private set; }

		// One queue per prefab — each projectile type maintains its own recycling bin.
		private readonly Dictionary<PackedScene, Queue<Projectile>> _pools = [];

		// Guards Rent() against being called outside a level context.
		private bool _initialized;

		// ------------------------------------- Godot overrides ------------------------------------

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

		// --------------------------------------- Public API ---------------------------------------

		// Marks the pool as ready to serve projectiles.
		// Calls Clear() first so re-entering a level starts from a clean state.
		// optionalPrewarm: prefab/count pairs instantiated immediately to avoid first-use stutter.
		public void Initialize(Dictionary<PackedScene, int> optionalPrewarm = null)
		{
			Clear();
			_initialized = true;

			if (optionalPrewarm == null) return;

			foreach (var (prefab, count) in optionalPrewarm)
				for (int i = 0; i < count; i++)
					Return(CreateProjectile(prefab));
		}

		// Frees every projectile node — both active and pooled — then resets pool state.
		// Safe to call mid-level; GetChildren() covers nodes currently rented (outside any queue).
		public void Clear()
		{
			foreach (var child in GetChildren())
				child.QueueFree();

			_pools.Clear();
			_initialized = false;
		}

		// Pops or creates a Projectile, re-enables all its systems, and calls Initialize()
		// to set position and angle. Returns null with a warning if called outside a level.
		public Projectile Rent(PackedScene prefab, Vector2 position, float angle)
		{
			if (!_initialized)
			{
				GD.PushWarning("ProjectilePool.Rent: called outside an active level — returning null.");
				return null;
			}

			var projectile = GetOrCreate(prefab);
			if (projectile == null) return null;

			SetProjectileActive(projectile, true);
			projectile.Initialize(position, angle);
			return projectile;
		}

		// Disables all projectile systems and re-enqueues it for reuse.
		// The node stays in the tree as a child of this pool — never reparented or freed.
		public void Return(Projectile projectile)
		{
			if (projectile.Prefab == null)
			{
				GD.PushError("ProjectilePool.Return: projectile has no Prefab reference — " +
							 "was it created outside the pool?");
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

		// ---------------------------------------- Helpers ----------------------------------------

		// Pops from the existing queue or falls through to CreateProjectile if the pool is empty.
		private Projectile GetOrCreate(PackedScene prefab)
		{
			if (_pools.TryGetValue(prefab, out var queue) && queue.Count > 0)
				return queue.Dequeue();

			return CreateProjectile(prefab);
		}

		// Instantiates a node, verifies its root is a Projectile, stamps its prefab reference,
		// and adopts it as a permanent child in a disabled state.
		// Caller is responsible for enqueuing via Return() or activating via Rent().
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

			// Adopt before disabling — AddChild must run first or SetProcess calls have no effect.
			AddChild(projectile);
			SetProjectileActive(projectile, false);
			return projectile;
		}

		// Toggles every system that would let a disabled projectile affect the simulation —
		// visibility, per-frame callbacks, and both collision roles.
		private void SetProjectileActive(Projectile projectile, bool active)
		{
			projectile.Visible = active;
			projectile.SetProcess(active);
			projectile.SetPhysicsProcess(active);
			projectile.Monitoring = active;
			projectile.Monitorable = active;
		}
	}
}