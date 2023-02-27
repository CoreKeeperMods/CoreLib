using Unity.Entities;

namespace CoreLib.Submodules.ModSystem
{
    public interface IPseudoClientSystem
    {
        void OnClientStarted(World world);
        void OnClientStopped();
    }
}