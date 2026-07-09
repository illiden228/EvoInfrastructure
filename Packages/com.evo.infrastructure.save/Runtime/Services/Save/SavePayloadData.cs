using System;

namespace Evo.Infrastructure.Services.Save
{
    [Serializable]
    public sealed class SavePayloadData
    {
        public string key = string.Empty;
        public int version = 1;
        public string json = string.Empty;
    }
}
