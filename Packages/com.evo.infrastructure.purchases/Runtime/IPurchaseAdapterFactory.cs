using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Evo.Infrastructure.Services.Purchases
{
    public interface IPurchaseAdapterFactory
    {
        string AdapterId { get; }
        IPurchaseAdapter Create();
    }
}

