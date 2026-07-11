using System;

namespace Evo.Infrastructure.GooglePlayGames.Save
{
    [Serializable]
    public sealed class GooglePlayGamesSaveOptions
    {
        public string slotName = "evo-save";
        public int priority = 100;
        public int operationTimeoutMs = 15000;
    }
}
