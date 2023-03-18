using Unity.Entities;

namespace CoreLib.Submodules.ModSystem
{
    /// <summary>
    /// Define State Requester by implementing this interface
    /// State Requesters can request entities to switch to a modded state, which allows custom behaviour
    /// </summary>
    public interface IStateRequester
    {
        
        /// <summary>
        /// State execution priority. Lower value is higher priority;
        /// </summary>
        int priority { get; }

        /// <summary>
        /// Called when StateRequestSystem is created. Initialize your object state here 
        /// </summary>
        /// <param name="world">Current SERVER world</param>
        void OnCreate(World world);

        /// <summary>
        /// Determine whether this system should run on this archetype CHUNK
        /// You will be passed one entity from the chunk.
        /// </summary>
        /// <param name="entity">First entity from chunk</param>
        /// <param name="data">Current State Request common data</param>
        /// <param name="containers">Component from Entity object container</param>
        /// <returns>Should the system run for whole chunk?</returns>
        bool ShouldUpdate(Entity entity, ref StateRequestData data, ref StateRequestContainers containers);
        
        /// <summary>
        /// Determine whether this entity should switch state.
        /// </summary>
        /// <remarks>
        /// Do not set the StateInfoCD to ecb within this method. Instead return true when there are changes
        /// </remarks>
        /// <param name="entity">Current entity</param>
        /// <param name="ecb">EntityCommandBuffer to write to</param>
        /// <param name="data">Current State Request common data</param>
        /// <param name="containers">Component from Entity object container</param>
        /// <param name="stateInfo">Current entity state</param>
        /// <returns>Is StateInfoCD modified</returns>
        bool OnUpdate(
            Entity entity,
            EntityCommandBuffer ecb,
            ref StateRequestData data,
            ref StateRequestContainers containers,
            ref StateInfoCD stateInfo);
    }
}