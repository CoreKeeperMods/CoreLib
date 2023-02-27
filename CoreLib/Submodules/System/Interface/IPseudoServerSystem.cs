using Unity.Entities;

namespace CoreLib.Submodules.ModSystem
{
    public interface IPseudoServerSystem
    {
        void OnServerStarted(World world);
        void OnServerStopped();
    }
}