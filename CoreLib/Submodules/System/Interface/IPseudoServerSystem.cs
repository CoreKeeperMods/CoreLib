using System;
using Unity.Entities;

namespace CoreLib.Submodules.ModSystem
{
    /// <summary>
    /// Define Pseudo Server System by implementing this interface
    /// Pseudo Systems can perform actions on certain entities every tick 
    /// </summary>
    [Obsolete("Use BaseModSystem instead")]
    public interface IPseudoServerSystem
    {
        /// <summary>
        /// Execute after server world is started
        /// </summary>
        void OnServerStarted(World world);
        
        /// <summary>
        /// Server world is about to be destroyed. Clear the reference to it
        /// </summary>
        void OnServerStopped();
    }
}