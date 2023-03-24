# System Module
System Module is a submodule that allows creating systems that interact with ECS components.

Currently we are unable to create proper ECS systems. However Pseudo Systems provide means of performing similar things.

# Usage
Make sure to add `[CoreLibSubmoduleDependency(nameof(SystemModule))]` to your plugin attributes. This will load the submodule.

## Create Pseudo System Class
Pseudo Systems are MonoBehaviors in their nature. They use their `Update` and `FixedUpdate` methods to perform calculations.

```csharp
public class MyPseudoSystem : MonoBehaviour, IPseudoServerSystem
{
    internal World serverWorld;
    
    public MyPseudoSystem(IntPtr ptr) : base(ptr) { }

    // This will be called once the world is ready
    public void OnServerStarted(World world)
    {
        serverWorld = world;
    }

    // At this point the world is about to be destroyed, remove reference
    public void OnServerStopped()
    {
        serverWorld = null;
    }

    private void Update()
    {
        // Make sure the world exists
        if (serverWorld == null) return;
        
        // Use Entity Query to grab needed entities. You can request multiple components here.
        // Every entity returned will have them all
        EntityQuery query = serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<InventoryCD>());

        NativeArray<Entity> result = query.ToEntityArray(Allocator.Temp);

        // Itterate over the results and do your thing
        foreach (Entity entity in result)
        {
            // do something
        }
    }
}
```

Now in your `Load()` method add this code. System will be created automatically
```csharp
SystemModule.RegisterSystem<MyPseudoSystem>();
```

## Create Entity State Requester
State Requester is a system that determines what state should entity have. Typically you should have one per unique state.

It is recommended to create a [custom component](../Component/README.md#Creating-custom-ECS-components) to house your state information and determine if entity should have that behaviour

Register State Requester using `SystemModule.RegisterStateRequester<MyStateRequester>();` 

You are allowed to house state requester in one class with a pseudo system, however you should be careful not to make these parts interact, as each have their own lifecycle. In this case register the class as a pseudo system.
<details>
<summary>Slime flee example</summary>

This example will add a new behavior to slimes with our `SlimeFleeStateCD` component:
```csharp
[Il2CppImplements(typeof(IComponentData))]
public struct SlimeFleeStateCD
{
    public float fleeAtHealthRatio;
    public float leaveFleeAtHealthRatio;
    public float fleeSpeed;
}
```
(Authoring component code is skipped for brevity)

```csharp
public class SlimeFleeStateRequester : IStateRequester
{
    // Use a unique id to identify your custom state
    public const string FLEE_STATE_ID = "MyMod:FleeState";
    public static StateID fleeState = SystemModule.GetModStateId(FLEE_STATE_ID);

    // This class will help us get our component
    private ModComponentDataFromEntity<SlimeFleeStateCD> slimeStateGroup;

    // In case of multiple requesters changing same entity state one with higher priority will get executed first
    public int priority => SystemModule.NORMAL_PRIORITY;

    // Use this method to intialize your state
    public void OnCreate(World world)
    {
        slimeStateGroup = new ModComponentDataFromEntity<SlimeFleeStateCD>(world.EntityManager);
    }

    // The method will be called for each entity. 
    // You need to determine if your entity should have the state
    // This is usually done by checking if your component exists
    public bool OnUpdate(Entity entity, EntityCommandBuffer ecb, ref StateRequestData data, ref StateRequestContainers containers, ref StateInfoCD stateInfo)
    {
        // Does this entity have our custom component?
        if (!slimeStateGroup.HasComponent(entity)) return false;

        // Get needed data
        HealthCD healthCd = containers._healthGroup[entity];
        SlimeFleeStateCD slimeFleeStateCd = slimeStateGroup[entity];
        float healthPercent = healthCd.health / (float) healthCd.maxHealth;
        
        // If the entity has too low HP enter flee state
        if (stateInfo.currentState == StateID.RandomWalking &&
            healthPercent < slimeFleeStateCd.fleeAtHealthRatio)
        {
            stateInfo.newState = fleeState;
            // By returning true here we signal that the 'stateInfo' field has changed
            return true;
        }
        
        // The entity is fleeing 
        if (stateInfo.currentState == fleeState)
        {
            stateInfo.newState = fleeState;
            // Determine if it should keep fleeing
            if (healthPercent > slimeFleeStateCd.leaveFleeAtHealthRatio)
            {
                stateInfo.newState = StateID.RandomWalking;
            }
            // By returning true here we signal that the 'stateInfo' field has changed
            return true;
        }
        
        // Nothing changed
        return false;
    }
}
```

Now that the slime will have our custom state when it needs to we need to implement the system that will implement slime behavior when it's in our state:
```csharp
public class SlimeFleeStateSystem : MonoBehaviour, IPseudoServerSystem
{
    private World serverWorld;
    private EntityQuery entityQuery;

    public void OnServerStarted(World world)
    {
        // Prepare our state. This entity query will return only entities that match our components
        serverWorld = world;
        entityQuery = serverWorld.EntityManager.CreateEntityQuery(
            ComponentModule.ReadOnly<SlimeFleeStateCD>(),
            ComponentModule.ReadWrite<Translation>(),
            ComponentModule.ReadOnly<LastAttackerCD>(),
            ComponentModule.ReadOnly<StateInfoCD>());
    }

    public void OnServerStopped()
    {
        // The game is about to stop, clear our reference to World
        serverWorld = null;
    }

    private void FixedUpdate()
    {
        if (serverWorld == null) return;
        
        // Execute our query and itterate the entities
        var entities = entityQuery.ToEntityArray(Allocator.Temp);

        foreach (Entity entity in entities)
        {
            // Get StateInfoCD component, and see if entity is in our state
            StateInfoCD stateInfo = serverWorld.EntityManager.GetModComponentData<StateInfoCD>(entity);
            if (stateInfo.currentState == SlimeFleeStateRequester.fleeState)
            {
                // Fetch needed components
                SlimeFleeStateCD fleeStateCd = serverWorld.EntityManager.GetModComponentData<SlimeFleeStateCD>(entity);
                Translation translation = serverWorld.EntityManager.GetModComponentData<Translation>(entity);
                
                // Find last attacker entity
                LastAttackerCD lastAttackerCd = serverWorld.EntityManager.GetModComponentData<LastAttackerCD>(entity);
                Entity attackerEntity = lastAttackerCd.Value;

                // If attacker exists, do our thing
                if (serverWorld.EntityManager.Exists(attackerEntity))
                {
                    Translation attackerTranslation = serverWorld.EntityManager.GetModComponentData<Translation>(attackerEntity);

                    // This will result in a very basic straight line movement behavior
                    float3 dir = math.normalize(translation.Value - attackerTranslation.Value);
                    translation.Value += dir * fleeStateCd.fleeSpeed * Time.fixedDeltaTime;

                    serverWorld.EntityManager.SetModComponentData(entity, translation);
                }
            }
        }
    }
}
```

Make sure to register these and add our component to your target entity. You can do that by either [entity modification](../Entity/README.md#Modifying-existing-entities) or creating a custom entity with the component.

</details>