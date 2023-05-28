using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BepInEx.Configuration;
using CoreLib.Submodules.ChatCommands;
using CoreLib.Submodules.Security.Patches;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Random = UnityEngine.Random;

namespace CoreLib.Submodules.Security
{
    [CoreLibSubmodule]
    public static class SecurityModule
    {
        #region PublicInterface

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded
        {
            get => _loaded;
            internal set => _loaded = value;
        }

        public static unsafe void SendMessageTo(string message, Entity connection)
        {
            World world = Manager.ecs.ServerWorld;
            InitArchetypes();

            NetworkCommMessageRPC networkCommMessageRPC = new NetworkCommMessageRPC
            {
                messageNumber = Random.Range(int.MinValue, 0),
                messageType = NetworkCommMessageType.System
            };
            var requestComponent = new SendRpcCommandRequestComponent
            {
                TargetConnection = connection
            };
            NetworkCommDataMessageRPC networkCommDataMessageRPC = default;

            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            int messageLength = messageBytes.Length;
            int entityCount = (messageLength - 1) / networkCommDataMessageRPC.messagePart.Size + 1;

            networkCommMessageRPC.totalSize = messageLength;
            Entity messageEntity = world.EntityManager.CreateEntity(messageArchetype);
            world.EntityManager.SetComponentData(messageEntity, networkCommMessageRPC);
            world.EntityManager.SetComponentData(messageEntity, requestComponent);

            NativeArray<Entity> messageParts = world.EntityManager.CreateEntity(messageDataArchetype, entityCount, Allocator.Temp);

            networkCommDataMessageRPC.messageNumber = networkCommMessageRPC.messageNumber;

            fixed (byte* ptr = messageBytes)
            {
                int remainingBytes = messageLength;
                ushort i = 0;
                while (i < messageParts.Length)
                {
                    networkCommDataMessageRPC.startByte = i * networkCommDataMessageRPC.messagePart.Size;
                    UnsafeUtility.MemCpy(networkCommDataMessageRPC.messagePart.GetUnsafePtr(), ptr + networkCommDataMessageRPC.startByte,
                        math.min(networkCommDataMessageRPC.messagePart.Size, remainingBytes));
                    world.EntityManager.SetComponentData(messageParts[i], networkCommDataMessageRPC);
                    world.EntityManager.SetComponentData(messageParts[i], requestComponent);
                    remainingBytes -= networkCommDataMessageRPC.messagePart.Size;
                    i += 1;
                }
            }
        }

        #endregion

        #region PrivateImplementation

        private static bool _loaded;
        internal static ConfigEntry<bool> allowCommandExecution;
        internal static ConfigEntry<bool> allowSelfCheats;
        internal static ConfigEntry<bool> allowUnknownCommands;

        private static EntityArchetype messageArchetype;
        private static EntityArchetype messageDataArchetype;
        private static bool archetypesInited;

        private static Dictionary<Entity, long> lastWarned = new Dictionary<Entity, long>();
        private static int warnTimeout = 5;
        
        private static void InitArchetypes()
        {
            if (archetypesInited) return;

            World world = Manager.ecs.ServerWorld;

            messageArchetype = world.EntityManager.CreateArchetype(ComponentType.ReadOnly<NetworkCommMessageRPC>(), ComponentType.ReadOnly<SendRpcCommandRequestComponent>());
            messageDataArchetype = world.EntityManager.CreateArchetype(ComponentType.ReadOnly<NetworkCommDataMessageRPC>(), ComponentType.ReadOnly<SendRpcCommandRequestComponent>());

            archetypesInited = true;
        }

        internal static void ThrowIfNotLoaded()
        {
            if (!Loaded)
            {
                Type submoduleType = MethodBase.GetCurrentMethod().DeclaringType;
                string message = $"{submoduleType.Name} is not loaded. Please use [{nameof(CoreLibSubmoduleDependency)}(nameof({submoduleType.Name})]";
                throw new InvalidOperationException(message);
            }
        }


        [CoreLibSubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks()
        {
            CoreLibPlugin.harmony.PatchAll(typeof(PlayerCommandServerSystem_Patch));
            CoreLibPlugin.harmony.PatchAll(typeof(PlayerCommandClientSystem_Patch));
            
            CoreLibPlugin.harmony.PatchAll(typeof(InventoryHandlerServerSystem_Patch));
            CoreLibPlugin.harmony.PatchAll(typeof(InventoryHandlerClientSystem_Patch));
        }

        [CoreLibSubmoduleInit(Stage = InitStage.Load)]
        internal static void Load()
        {
            allowCommandExecution = CoreLibPlugin.Instance.Config.Bind("Security", "AllowCommandExecution", true, "Should commands be allowed to be executed?");
            allowSelfCheats = CoreLibPlugin.Instance.Config.Bind("Security", "AllowSelfCheats", true, "Should self cheats (IE applies only to the player) be allowed?");
            allowUnknownCommands = CoreLibPlugin.Instance.Config.Bind("Security", "AllowUnknownCommands", true, "Should unknown commands be allowed to be executed?");
        }

        private static int GetAdminLevel(this ComponentDataFromEntity<ConnectionAdminLevelCD> fromEntity, Entity connectionEntity)
        {
            if (!fromEntity.HasComponent(connectionEntity))
            {
                return -1;
            }

            return fromEntity[connectionEntity].Value;
        }

        private static void IssueWarn(string message, Entity connectionEntity)
        {
            if (lastWarned.ContainsKey(connectionEntity))
            {
                long lastTimeWarned = lastWarned[connectionEntity];
                long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                if (now - lastTimeWarned > warnTimeout)
                {
                    SendMessageTo(message, connectionEntity);
                    lastWarned[connectionEntity] = now;
                }
            }
            else
            {
                long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                SendMessageTo(message, connectionEntity);
                lastWarned[connectionEntity] = now;
            }
        }

        internal static void CheckCommandPermission(Entity commandEntity, Entity connection, string additionalData, EntityCommandBuffer ecb,
            ComponentDataFromEntity<ConnectionAdminLevelCD> componentDataFromEntity)
        {
            if (string.IsNullOrEmpty(additionalData)) return;

            if (!CommandInfo.TryParseInfoString(additionalData, out CommandInfo commandInfo))
            {
                CoreLibPlugin.Logger.LogWarning($"Received invalid command info: '{additionalData}'!");
                return;
            }

            if (!allowCommandExecution.Value)
            {
                ecb.DestroyEntity(commandEntity);
                IssueWarn("Commands are not allowed on this server!", connection);
                return;
            }

            if (CommandsModule.Loaded)
            {
                CommandPair pair = CommandsModule.FindCommand(commandInfo);
                if (pair.handler == null)
                {
                    CoreLibPlugin.Logger.LogWarning("Received rpc with unknown command!");
                    if (!allowUnknownCommands.Value)
                    {
                        ecb.DestroyEntity(commandEntity);
                        IssueWarn("This server restricted executing unknown commands!", connection);
                    }

                    return;
                }

                commandInfo.kind = CommandsModule.DetermineCommandKind(pair);
            }

            bool isAdmin = componentDataFromEntity.GetAdminLevel(connection) > 0;

            if (commandInfo.kind == CommandKind.SelfCheat)
            {
                if (allowSelfCheats.Value || isAdmin) return;

                ecb.DestroyEntity(commandEntity);
                IssueWarn("Not enough permissions!", connection);
            }
            else if (commandInfo.kind == CommandKind.Cheat && !isAdmin)
            {
                ecb.DestroyEntity(commandEntity);
                IssueWarn("Not enough permissions!", connection);
            }
        }
        
        #endregion
    }
}