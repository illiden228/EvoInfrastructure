using System;
namespace Evo.Infrastructure.Services.Purchases
{
    [Obsolete("Purchase platform routing uses PlatformCatalog platform IDs starting with purchases 0.5.16.")]
    [Flags]
    public enum PurchasePlatformMask
    {
        None = 0,
        Editor = 1 << 0,
        Android = 1 << 1,
        IOS = 1 << 2,
        WebGL = 1 << 3,
        Windows = 1 << 4,
        MacOS = 1 << 5,
        Linux = 1 << 6,
        All = ~0
    }
}

