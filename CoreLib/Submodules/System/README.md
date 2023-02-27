# System Module
System Module is a submodule that allows creating Pseudo Systems that interact with ECS components.

Currently we are unable to create proper ECS systems. However Pseudo Systems provide means of performing similar things.

## Usage
Make sure to add `[CoreLibSubmoduleDependency(nameof(SystemModule))]` to your plugin attributes. This will load the submodule.

### Create Pseudo System Class
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