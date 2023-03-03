using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;

namespace CoreLib.Submodules.ModComponent
{
    public static class CommandBufferExtensions
    {
        /// <summary> Records a command to add component of type T to an entity.
        /// This method will work on any type, including mod created ones</summary>
        /// <remarks>Behavior at Playback: If the entity already has this type of component, the value will just be set.
        /// Will throw an error if this entity is destroyed before playback, if this entity is still deferred,
        /// if T is type Entity, or adding this componentType makes the archetype too large.</remarks>
        /// <param name="e"> The entity to have the component added. </param>
        /// <param name="component">The value to add on the new component in playback for the entity.</param>
        /// <typeparam name="T"> The type of component to add. </typeparam>
        public static unsafe void AddModComponent<T>(this EntityCommandBuffer ecb, Entity e, T component) where T : unmanaged
        {
            AddEntityComponentCommand(ecb.m_Data, &ecb.m_Data->m_MainThreadChain, ecb.MainThreadSortKey, ECBCommand.AddComponent, e, component);
        }

        /// <summary> Records a command to add component of type T to an entity.
        /// This method will work on any type, including mod created ones</summary>
        /// <remarks>Behavior at Playback: This command will do nothing if the entity already has the component.
        /// Will throw an error if this entity is destroyed before playback, if this entity is still deferred,
        /// if T is type Entity, or adding this componentType makes the archetype too large.</remarks>
        /// <param name="e"> The entity to have the component added. </param>
        /// <typeparam name="T"> The type of component to add. </typeparam>
        public static unsafe void AddModComponent<T>(this EntityCommandBuffer ecb, Entity e) where T : unmanaged
        {
            ecb.m_Data->AddEntityComponentTypeCommand(&ecb.m_Data->m_MainThreadChain, ecb.MainThreadSortKey, ECBCommand.AddComponent, e, ComponentModule.ReadWrite<T>());
        }

        /// <summary> Records a command to set a component value on an entity.
        /// This method will work on any type, including mod created ones</summary>
        /// <remarks> Behavior at Playback: Will throw an error if this entity is destroyed before playback,
        /// if this entity is still deferred, if the entity doesn't have the component type, or if T is zero sized.</remarks>
        /// <param name="e"> The entity to set the component value of. </param>
        /// <param name="component"> The component value to set. </param>
        /// <typeparam name="T"> The type of component to set. </typeparam>
        public static unsafe void SetModComponent<T>(this EntityCommandBuffer ecb, Entity e, T component) where T : unmanaged
        {
            AddEntityComponentCommand(ecb.m_Data, &ecb.m_Data->m_MainThreadChain, ecb.MainThreadSortKey, ECBCommand.SetComponent, e, component);
        }
        
        /// <summary> Records a command to remove component of type T from an entity.
        /// This method will work on any type, including mod created ones </summary>
        /// <remarks> Behavior at Playback: It is not an error if the entity doesn't have component T.
        /// Will throw an error if this entity is destroyed before playback,
        /// if this entity is still deferred, or if T is type Entity.</remarks>
        /// <param name="e"> The entity to have the component removed. </param>
        /// <typeparam name="T"> The type of component to remove. </typeparam>
        public static unsafe void RemoveModComponent<T>(this EntityCommandBuffer ecb, Entity e)
        {
            ecb.RemoveComponent(e, ComponentModule.ReadWrite<T>());
        }
        
        internal static unsafe void AddEntityComponentCommand<T>(EntityCommandBufferData* ecbData, EntityCommandBufferChain* chain, int sortKey, ECBCommand op, Entity e, T component) where T : unmanaged
        {
            var ctype = ComponentModule.ReadWrite<T>();
            if (ctype.IsZeroSized)
            {
                ecbData->AddEntityComponentTypeCommand(chain, sortKey, op, e, ctype);
                return;
            }

            // NOTE: This has to be sizeof not TypeManager.SizeInChunk since we use UnsafeUtility.CopyStructureToPtr
            //       even on zero size components.
            var typeSize = Unsafe.SizeOf<T>();
            var sizeNeeded = Align(sizeof(EntityComponentCommand) + typeSize, EntityCommandBufferData.ALIGN_64_BIT);

            ecbData->ResetCommandBatching(chain);
            var cmd = (EntityComponentCommand*)ecbData->Reserve(chain, sortKey, sizeNeeded);

            cmd->Header.Header.CommandType = op;
            cmd->Header.Header.TotalSize = sizeNeeded;
            cmd->Header.Header.SortKey = chain->m_LastSortKey;
            cmd->Header.Entity = e;
            cmd->Header.IdentityIndex = 0;
            cmd->Header.BatchCount = 1;
            cmd->ComponentTypeIndex = ctype.TypeIndex;
            cmd->ComponentSize = typeSize;

            byte* data = (byte*)(cmd + 1);
            Unsafe.Copy(data, ref component);

            if (ecbData->RequiresEntityFixUp(data, ctype.TypeIndex))
            {
                if (op == ECBCommand.AddComponent)
                    cmd->Header.Header.CommandType = ECBCommand.AddComponentWithEntityFixUp;
                else if (op == ECBCommand.SetComponent)
                    cmd->Header.Header.CommandType = ECBCommand.SetComponentWithEntityFixUp;
            }
        }
        
        internal static int Align(int size, int alignmentPowerOfTwo)
        {
            return (size + alignmentPowerOfTwo - 1) & ~(alignmentPowerOfTwo - 1);
        }
    }
}