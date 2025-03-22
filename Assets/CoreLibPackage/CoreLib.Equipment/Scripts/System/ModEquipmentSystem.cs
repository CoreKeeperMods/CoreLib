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
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(EquipmentUpdateSystemGroup))]
    [UpdateBefore(typeof(EquipmentUpdateSystem))]
    [DisableAutoCreation]
    public partial class ModEquipmentSystem : PugSimulationSystemBase
    {
        private uint _tickRate;
        private EntityArchetype _achievementArchetype;

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

        private static bool IsGuestMode(in WorldInfoCD worldInfo, in PlayerGhost playerGhost)
        {
            return worldInfo.guestMode && playerGhost.adminPrivileges < 1;
        }

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