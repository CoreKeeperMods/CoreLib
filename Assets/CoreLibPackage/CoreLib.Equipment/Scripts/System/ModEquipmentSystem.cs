using Inventory;
using PlayerEquipment;
using PlayerState;
using PugProperties;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

namespace CoreLib.Equipment.System
{
    /// <summary>
    /// ModEquipmentSystem is a system responsible for handling the simulation and management
    /// of modifiable equipment mechanics within the game.
    /// </summary>
    /// <remarks>
    /// - It operates within the EquipmentUpdateSystemGroup and executes prior to the EquipmentUpdateSystem.
    /// - This system is manually instantiated and not automatically created due to the DisableAutoCreation attribute.
    /// - Uses WorldSystemFilter to specify execution environments as both client-side and server-side simulation contexts.
    /// </remarks>
    /// <example>
    /// This system requires specific components such as PhysicsWorldSingleton, WorldInfoCD, and TileWithTilesetToObjectDataMapCD to update successfully.
    /// </example>
    /// <seealso cref="PugSimulationSystemBase"/>
    /// <seealso cref="EquipmentUpdateSystemGroup"/>
    /// <seealso cref="EquipmentUpdateSystem"/>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(EquipmentUpdateSystemGroup))]
    [UpdateBefore(typeof(EquipmentUpdateSystem))]
    [DisableAutoCreation]
    public partial class ModEquipmentSystem : PugSimulationSystemBase
    {
        /// <summary>
        /// Represents the tick rate for the ModEquipmentSystem, determining the frequency
        /// of system updates. This value is initialized based on the simulation tick rate
        /// provided by the networking platform.
        /// </summary>
        private uint _tickRate;

        /// <summary>
        /// Holds the archetype for achievement-related entities within the ModEquipmentSystem.
        /// This archetype is used to define the structural layout for entities involved in handling achievements.
        /// </summary>
        private EntityArchetype _achievementArchetype;

        /// <summary>
        /// Initializes the ModEquipmentSystem during its creation phase.
        /// This method sets up essential parameters and archetypes required for the functioning of the system.
        /// </summary>
        /// <remarks>
        /// - Retrieves the simulation tick rate for the current platform and assigns it to the system.
        /// - Generates the achievement RPC archetype using the AchievementSystem.
        /// - Specifies the required components that must exist in the world for this system to update.
        /// - Ensures the database is initialized properly.
        /// - Calls the base implementation of the OnCreate method for additional setup.
        /// </remarks>
        protected override void OnCreate()
        {
            _tickRate = (uint)NetworkingManager.GetSimulationTickRateForPlatform();
            _achievementArchetype = AchievementSystem.GetRpcArchetype(EntityManager);

            RequireForUpdate<PhysicsWorldSingleton>();
            RequireForUpdate<WorldInfoCD>();
            RequireForUpdate<TileWithTilesetToObjectDataMapCD>();

            NeedDatabase();
            base.OnCreate();
        }

        /// Executes the system's update logic in the simulation loop at each frame or fixed interval,
        /// according to the system group's scheduling. This method is typically overridden to define
        /// the specific behavior or processing logic for this system.
        /// Note: This system has a WorldSystemFilter applied for both ClientSimulation and ServerSimulation,
        /// and is scheduled to update before the `EquipmentUpdateSystem` within the `EquipmentUpdateSystemGroup`.
        /// It is also marked with `DisableAutoCreation`, meaning it won't be automatically created
        /// unless explicitly added to a world.
        protected override void OnUpdate()
        {
            var worldInfo = SystemAPI.GetSingleton<WorldInfoCD>();
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            var currentTick = networkTime.ServerTick;

            var databaseBank = SystemAPI.GetSingleton<PugDatabase.DatabaseBankCD>();

            var cooldownLookup = GetComponentLookup<CooldownCD>(true);
            var ecb = CreateCommandBuffer();
            var tileAccessor = CreateTileAccessor();

            var equipmentShared = new EquipmentUpdateSharedData
            {
                currentTick = currentTick,
                databaseBank = databaseBank,
                worldInfoCD = worldInfo,
                tickRate = _tickRate,
                physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
                physicsWorldHistory = SystemAPI.GetSingleton<PhysicsWorldHistorySingleton>(),
                inventoryUpdateBufferEntity = SystemAPI.GetSingletonEntity<InventoryChangeBuffer>(),
                tileUpdateBufferEntity = SystemAPI.GetSingletonEntity<TileUpdateBuffer>(),
                tileAccessor = tileAccessor,
                tileWithTilesetToObjectDataMapCD = SystemAPI.GetSingleton<TileWithTilesetToObjectDataMapCD>(),
                colliderCacheCD = SystemAPI.GetSingleton<ColliderCacheCD>(),
                isServer = isServer,
                ecb = ecb,
                isFirstTimeFullyPredictingTick = networkTime.IsFirstTimeFullyPredictingTick,
                achievementArchetype = _achievementArchetype
            };

            var lookupData = new LookupEquipmentUpdateData
            {
                secondaryUseLookup = SystemAPI.GetComponentLookup<SecondaryUseCD>(),
                cooldownLookup = SystemAPI.GetComponentLookup<CooldownCD>(),
                consumeManaLookup = SystemAPI.GetComponentLookup<ConsumesManaCD>(),
                levelLookup = SystemAPI.GetComponentLookup<LevelCD>(),
                levelEntitiesLookup = SystemAPI.GetBufferLookup<LevelEntitiesBuffer>(),
                parchementRecipeLookup = SystemAPI.GetComponentLookup<ParchmentRecipeCD>(),
                objectDataLookup = SystemAPI.GetComponentLookup<ObjectDataCD>(),
                attackWithEquipmentLookup = SystemAPI.GetComponentLookup<AttackWithEquipmentTag>(),
                inventoryUpdateBuffer = SystemAPI.GetBufferLookup<InventoryChangeBuffer>(),
                cattleLookup = SystemAPI.GetComponentLookup<CattleCD>(),
                petCandyLookup = SystemAPI.GetComponentLookup<PetCandyCD>(),
                potionLookup = SystemAPI.GetComponentLookup<PotionCD>(),
                localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
                petLookup = SystemAPI.GetComponentLookup<PetCD>(),
                playAnimationStateLookup = SystemAPI.GetComponentLookup<PlayAnimationStateCD>(),
                simulateLookup = SystemAPI.GetComponentLookup<Simulate>(),
                waitingForEatableSlotConsumeResultLookup = SystemAPI.GetComponentLookup<WaitingForEatableSlotConsumeResultCD>(),
                tileUpdateBufferLookup = SystemAPI.GetBufferLookup<TileUpdateBuffer>(),
                tileLookup = SystemAPI.GetComponentLookup<TileCD>(),
                objectPropertiesLookup = SystemAPI.GetComponentLookup<ObjectPropertiesCD>(),
                adaptiveEntityBufferLookup = SystemAPI.GetBufferLookup<AdaptiveEntityBuffer>(),
                directionBasedOnVariationLookup = SystemAPI.GetComponentLookup<DirectionBasedOnVariationCD>(),
                directionLookup = SystemAPI.GetComponentLookup<DirectionCD>(),
                sizeVariationLookup = SystemAPI.GetComponentLookup<ResizableTileSizeCD>(),
                playerGhostLookup = SystemAPI.GetComponentLookup<PlayerGhost>(),
                minionLookup = SystemAPI.GetComponentLookup<MinionCD>(),
                indestructibleLookup = SystemAPI.GetComponentLookup<IndestructibleCD>(),
                plantLookup = SystemAPI.GetComponentLookup<PlantCD>(),
                critterLookup = SystemAPI.GetComponentLookup<CritterCD>(),
                fireflyLookup = SystemAPI.GetComponentLookup<FireflyCD>(),
                requiresDrillLookup = SystemAPI.GetComponentLookup<RequiresDrillCD>(),
                surfacePriorityLookup = SystemAPI.GetComponentLookup<SurfacePriorityCD>(),
                electricityLookup = SystemAPI.GetComponentLookup<ElectricityCD>(),
                eventTerminalLookup = SystemAPI.GetComponentLookup<EventTerminalCD>(),
                waterSourceLookup = SystemAPI.GetComponentLookup<WaterSourceCD>(),
                paintToolLookup = SystemAPI.GetComponentLookup<PaintToolCD>(),
                paintableObjectLookup = SystemAPI.GetComponentLookup<PaintableObjectCD>(),
                growingLookup = SystemAPI.GetComponentLookup<GrowingCD>(),
                healthLookup = SystemAPI.GetComponentLookup<HealthCD>(),
                summarizedConditionsBufferLookup = SystemAPI.GetBufferLookup<SummarizedConditionsBuffer>(),
                reduceDurabilityOfEquippedTagLookup = SystemAPI.GetComponentLookup<ReduceDurabilityOfEquippedTriggerCD>(),
                summarizedConditionEffectsBufferLookup = SystemAPI.GetBufferLookup<SummarizedConditionEffectsBuffer>(),
                entityDestroyedLookup = SystemAPI.GetComponentLookup<EntityDestroyedCD>(),
                dontDropSelfLookup = SystemAPI.GetComponentLookup<DontDropSelfCD>(),
                dontDropLootLookup = SystemAPI.GetComponentLookup<DontDropLootCD>(),
                killedByPlayerLookup = SystemAPI.GetComponentLookup<KilledByPlayerCD>(),
                destructibleLookup = SystemAPI.GetComponentLookup<DestructibleObjectCD>(),
                canBeRemovedByWaterLookup = SystemAPI.GetComponentLookup<CanBeRemovedByWaterCD>(),
                groundDecorationLookup = SystemAPI.GetComponentLookup<GroundDecorationCD>(),
                diggableLookup = SystemAPI.GetComponentLookup<DiggableCD>(),
                pseudoTileLookup = SystemAPI.GetComponentLookup<PseudoTileCD>(),
                dontBlockDiggingLookup = SystemAPI.GetComponentLookup<DontBlockDiggingCD>(),
                fullnessLookup = SystemAPI.GetComponentLookup<FullnessCD>(),
                godModeLookup = SystemAPI.GetComponentLookup<GodModeCD>(),
                containedObjectsBufferLookup = SystemAPI.GetBufferLookup<ContainedObjectsBuffer>(),
                anvilLookup = SystemAPI.GetComponentLookup<AnvilCD>(),
                waypointLookup = SystemAPI.GetComponentLookup<WayPointCD>(),
                craftingLookup = SystemAPI.GetComponentLookup<CraftingCD>(),
                triggerAnimationOnDeathLookup = SystemAPI.GetComponentLookup<TriggerAnimationOnDeathCD>(),
                moveToPredictedByEntityDestroyedLookup = SystemAPI.GetComponentLookup<MoveToPredictedByEntityDestroyedCD>(),
                hasExplodedLookup = SystemAPI.GetComponentLookup<HasExplodedCD>()
            };

            foreach (var slotInfo in EquipmentModule.slots.Values)
            {
                slotInfo.logic.CreateLookups(ref CheckedStateRef);    
            }

            Entities.ForEach((
                    EquipmentUpdateAspect equipmentAspect,
                    in ClientInput clientInput
                ) =>
                {
                    var slotType = equipmentAspect.equipmentSlotCD.ValueRO.slotType;
                    var slotTypeNum = (int)slotType;
                    
                    if (slotTypeNum < EquipmentModule.ModSlotTypeIdStart) return;
                    if (!EquipmentModule.slots.TryGetValue(slotType, out var slotInfo)) return;
                    var logic = slotInfo.logic;
                    
                    bool interactHeldRaw = clientInput.IsButtonStateSet(CommandInputButtonStateNames.Interact_HeldDown);
                    bool secondInteractHeldRaw = clientInput.IsButtonStateSet(CommandInputButtonStateNames.SecondInteract_HeldDown);
                    if (!CurrentStateAllowInteractions(
                            worldInfo, equipmentAspect.playerGhost.ValueRO,
                            equipmentAspect.playerStateCD.ValueRO,
                            equipmentAspect.equipmentSlotCD.ValueRO, logic, secondInteractHeldRaw && !interactHeldRaw))
                    {
                        return;
                    }

                    bool interactHeld = interactHeldRaw | equipmentAspect.equipmentSlotCD.ValueRW.interactIsPendingToBeUsed;
                    bool secondInteractHeld = secondInteractHeldRaw | equipmentAspect.equipmentSlotCD.ValueRW.secondInteractIsPendingToBeUsed;

                    bool onCooldown = EquipmentSlot.IsItemOnCooldown(
                        equipmentAspect.equippedObjectCD.ValueRO,
                        databaseBank,
                        cooldownLookup,
                        equipmentAspect.syncedSharedCooldownTimers,
                        equipmentAspect.localPlayerSharedCooldownTimers, currentTick);
                    if (onCooldown)
                    {
                        return;
                    }
                    
                    var success = logic.Update(
                        equipmentAspect,
                        equipmentShared,
                        lookupData,
                        interactHeld,
                        secondInteractHeld
                    );

                    if (!success) return;

                    equipmentAspect.equipmentSlotCD.ValueRW.secondInteractIsPendingToBeUsed = false;
                })
                .WithoutBurst()
                .Schedule();

            base.OnUpdate();
        }

        /// <summary>
        /// Determines whether the system is operating in guest mode based on the provided world and player information.
        /// </summary>
        /// <param name="worldInfo">The current world information containing guest mode configuration.</param>
        /// <param name="playerGhost">The player ghost data, including privilege level and metadata.</param>
        /// <returns>
        /// Returns true if the system is in guest mode and the player does not have sufficient administrative privileges.
        /// Returns false otherwise.
        /// </returns>
        private static bool IsGuestMode(in WorldInfoCD worldInfo, in PlayerGhost playerGhost)
        {
            return worldInfo.guestMode && playerGhost.adminPrivileges < 1;
        }

        /// <summary>
        /// Determines whether the player is allowed to interact based on the current state and context.
        /// This method evaluates various constraints, including player state, equipment logic, and interaction type,
        /// to decide whether an interaction is permissible.
        /// </summary>
        /// <param name="worldInfo">Information about the current game world, such as mode or default settings.</param>
        /// <param name="playerGhost">Details about the player's ghost representation in the game.</param>
        /// <param name="playerState">The current state of the player, including active or passive states.</param>
        /// <param name="equippedSlot">Information about the equipment currently held or used by the player.</param>
        /// <param name="logic">The logic interface responsible for determining equipment-related behavior or restrictions.</param>
        /// <param name="isTryingToUseSecondInteract">Specifies whether the interaction is a secondary interaction.</param>
        /// <param name="isTryingToInteractWithObject">Optional parameter indicating if the interaction involves an object.</param>
        /// <returns>
        /// A boolean value indicating whether the interaction is allowed. Returns true if the interaction satisfies all conditions; otherwise, false.
        /// </returns>
        private static bool CurrentStateAllowInteractions(
            in WorldInfoCD worldInfo,
            in PlayerGhost playerGhost,
            in PlayerStateCD playerState,
            in EquipmentSlotCD equippedSlot,
            IEquipmentLogic logic,
            bool isTryingToUseSecondInteract,
            bool isTryingToInteractWithObject = false)
        {
            if (IsGuestMode(in worldInfo, in playerGhost))
                return false;
            if (playerState.HasAnyState(PlayerStateEnum.Sitting))
            {
                if (isTryingToInteractWithObject)
                    return true;
                if (!isTryingToUseSecondInteract)
                    return false;
                return logic.CanUseWhileSitting;
            }

            if (playerState.HasAnyState(PlayerStateEnum.VehicleRiding))
                return isTryingToUseSecondInteract && equippedSlot.GetSlotType() == EquipmentSlotType.EatableSlot;

            if (playerState.HasAnyState(PlayerStateEnum.Teleporting) || playerState.HasAnyState(PlayerStateEnum.Release) ||
                !playerState.HasNoneState(PlayerStateEnum.SpawningFromCore | PlayerStateEnum.Death | PlayerStateEnum.Sleep))
                return false;

            return isTryingToUseSecondInteract && logic.CanUseWhileOnBoat || !isTryingToUseSecondInteract ||
                   playerState.HasNoneState(PlayerStateEnum.MinecartRiding | PlayerStateEnum.BoatRiding);
        }
    }
}