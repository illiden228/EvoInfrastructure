# com.evo.infrastructure.pooling

Reusable pooling primitives.

The package is intentionally low-level. Game-specific systems should keep their own domain logic
(projectile visual reset, unit shell/visual binding, VFX configuration, popup tween cleanup) and use
these primitives only for storage, lifecycle and diagnostics.

## Types

- `Pool<T>`: generic inactive/active pool with prewarm, max inactive count, double-release guard,
  statistics and `Dispose`.
- `GameObjectPool`: prefab pool for `GameObject` instances.
- `ComponentPool<T>`: prefab pool that returns a specific `Component`.
- `KeyedPool<TKey, TValue>`: generic pool map for ids such as `prefabId` or `visualPrefabKey`.
- `KeyedGameObjectPool<TKey>`: keyed prefab pool with reverse instance-to-key release.
- `ListPool<T>`: small list pool for temporary allocations.
- `IPoolable`: optional lifecycle hook for pooled MonoBehaviours.

## Examples

```csharp
var pool = new GameObjectPool(projectilePrefab, prewarmCount: 16, maxSize: 64, root: poolRoot);
var projectile = pool.Get(position, rotation);
pool.Release(projectile);
```

```csharp
var keyed = new KeyedGameObjectPool<string>(
    resolvePrefab: prefabId => ResolvePrefab(prefabId),
    resolvePrewarmCount: prefabId => 4,
    resolveMaxInactive: prefabId => 32,
    root: poolRoot);

var instance = keyed.Get("laser", position, rotation);
keyed.Release(instance);
```

```csharp
public sealed class ProjectileView : MonoBehaviour, IPoolable
{
    public void OnPoolGet()
    {
        ResetAndPlay();
    }

    public void OnPoolRelease()
    {
        StopAndClear();
    }

    public void OnPoolDestroy()
    {
    }
}
```

## SpaceRangers-style usage

Use package pools as internal primitives, not as replacements for domain systems:

- `BattleProjectilePoolBucket` can keep its cached `ProjectileView`, `ParticleSystem`,
  `TrailRenderer` and `LineRenderer` reset logic, while delegating inactive/active storage to
  `GameObjectPool` or `Pool<CachedProjectileInstance>`.
- `BattleUnitPool` should keep shell/visual binding and async resource loading; `KeyedPool` can
  hold inactive visuals per `visualPrefabKey`.
- `BattleVfxService` and popup services can use `KeyedGameObjectPool<string>` for prefab-id buckets
  and keep their custom reset/tween cleanup in service code or `IPoolable` components.

