namespace Evo.Infrastructure.Services.Pooling
{
    public readonly struct PoolStatistics
    {
        public PoolStatistics(int activeCount, int inactiveCount, int createdCount, int destroyedCount)
        {
            ActiveCount = activeCount;
            InactiveCount = inactiveCount;
            CreatedCount = createdCount;
            DestroyedCount = destroyedCount;
        }

        public int ActiveCount { get; }
        public int InactiveCount { get; }
        public int CreatedCount { get; }
        public int DestroyedCount { get; }
    }
}
