using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Services.Pooling;
using NUnit.Framework;

namespace Evo.Infrastructure.Pooling.Tests
{
    public sealed class KeyedAsyncPoolTests
    {
        [Test]
        public async Task Dispose_DestroysActiveAndInactiveValuesOnce_AndIsIdempotent()
        {
            var nextValue = 0;
            var destroyedValues = new List<int>();
            var pool = new KeyedAsyncPool<string, int>(
                async (key, cancellationToken) =>
                {
                    await UniTask.Yield(cancellationToken);
                    return ++nextValue;
                },
                onDestroy: (key, value) => destroyedValues.Add(value),
                resolveMaxInactive: key => 1);

            var inactiveValue = await pool.GetAsync("projectile", default);
            Assert.That(pool.Release("projectile", inactiveValue), Is.True);
            var reusedValue = await pool.GetAsync("projectile", default);
            var activeValue = await pool.GetAsync("projectile", default);
            Assert.That(pool.Release("projectile", reusedValue), Is.True);

            Assert.DoesNotThrow(pool.Dispose);
            Assert.DoesNotThrow(pool.Dispose);

            Assert.That(activeValue, Is.Not.EqualTo(inactiveValue));
            Assert.That(destroyedValues, Is.EquivalentTo(new[] { inactiveValue, activeValue }));
            Assert.That(destroyedValues, Has.Count.EqualTo(2));
        }
    }
}
