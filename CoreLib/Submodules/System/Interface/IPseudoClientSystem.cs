using Unity.Entities;

namespace CoreLib.Submodules.ModSystem
{
    /// <summary>
    /// Define Pseudo Client System by implementing this interface
    /// Pseudo Systems can perform actions on certain entities every tick 
    /// </summary>
    public interface IPseudoClientSystem
    {
        /// <summary>
        /// Execute after client world is started
        /// </summary>
        void OnClientStarted(World world);
        
        /// <summary>
        /// Client world is about to be destroyed. Clear the reference to it
        /// </summary>
        void OnClientStopped();
    }
}