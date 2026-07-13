using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Evo.Infrastructure.Core.Async;
using NUnit.Framework;

namespace Evo.Infrastructure.Core.Tests
{
    public sealed class AsyncGateTests
    {
        [Test]
        public async Task ConcurrentCallers_RunInFifoOrder()
        {
            var gate = new AsyncGate();
            var first = await gate.EnterAsync();
            var order = new List<int>();
            var second = EnterAndRecordAsync(gate, 2, order);
            var third = EnterAndRecordAsync(gate, 3, order);

            await UniTask.Yield();
            Assert.That(order, Is.Empty);
            first.Dispose();
            await UniTask.WhenAll(second, third);

            Assert.That(order, Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public async Task CancelledWaiter_DoesNotBlockFollowingWaiter()
        {
            var gate = new AsyncGate();
            var owner = await gate.EnterAsync();
            using var cancellation = new CancellationTokenSource();
            var cancelled = gate.EnterAsync(cancellation.Token);
            var following = gate.EnterAsync();

            cancellation.Cancel();
            try
            {
                await cancelled;
                Assert.Fail("The cancelled waiter completed successfully.");
            }
            catch (OperationCanceledException)
            {
                // Expected.
            }

            owner.Dispose();
            var followingLease = await following;
            followingLease.Dispose();
        }

        [Test]
        public async Task ExceptionInsideProtectedOperation_ReleasesGate()
        {
            var gate = new AsyncGate();
            try
            {
                await ThrowInsideGateAsync(gate);
                Assert.Fail("The protected operation completed successfully.");
            }
            catch (InvalidOperationException)
            {
                // Expected.
            }

            var lease = await gate.EnterAsync();
            lease.Dispose();
        }

        [Test]
        public async Task DoubleDispose_IsIgnored()
        {
            var gate = new AsyncGate();
            var lease = await gate.EnterAsync();
            lease.Dispose();
            lease.Dispose();
            var next = await gate.EnterAsync();
            next.Dispose();
        }

        private static async UniTask EnterAndRecordAsync(AsyncGate gate, int value, ICollection<int> order)
        {
            using var lease = await gate.EnterAsync();
            order.Add(value);
            await UniTask.Yield();
        }

        private static async UniTask ThrowInsideGateAsync(AsyncGate gate)
        {
            using var lease = await gate.EnterAsync();
            await UniTask.Yield();
            throw new InvalidOperationException("Expected test exception.");
        }
    }
}
