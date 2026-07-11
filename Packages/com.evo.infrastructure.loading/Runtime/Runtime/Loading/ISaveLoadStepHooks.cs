using Evo.Infrastructure.Services.Save;

namespace Evo.Infrastructure.Runtime.Loading
{
    public interface ISaveLoadStepHooks
    {
        void OnSaveLoaded(SaveEnvelope envelope);
    }
}
