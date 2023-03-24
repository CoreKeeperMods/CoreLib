# Component Module

Component module enables working with ECS components without restrictions posed by lack of AOT compilation. Also allows to inject custom ECS components and use them.

# Usage
Make sure to add `[CoreLibSubmoduleDependency(nameof(ComponentModule))]` to your plugin attributes. This will load the submodule.

## Using Mod versions of ECS methods
Type `ECSExtensions` contains a number of extension methods that behave very similarly to those found in `EntityManager`. However they will work on ANY type that is a valid ECS component, including modded ones. These can be used as follows:
```csharp
EntityManager entityManager = world.EntityManager;

bool value = entityManager.HasModComponent<TemplateBlockCD>(entity);
TemplateBlockCD templateBlockCd = entityManager.GetModComponentData<TemplateBlockCD>(entity);
entityManager.SetModComponentData(entity, templateBlockCd);
```
### Additional helpers
Some other helper methods exists that also will work on any valid ECS components.

- `ComponentModule` feature a number of methods which mirror those found in `PugDatabase` and `ComponentType`
- `CommandBufferExtensions` feature a number of methods to work with `EntityCommandBuffer` class
- `EntityQueryExtensions` feature a number of methods to work with `EntityQuery` class
- `ModComponentDataFromEntity` is a class which allows to have faster cached access to data of a single component type


# Creating custom ECS components

Creating custom ECS components is not much different from creating [wrapper components](../Entity/README.md#Creating-wrapper-components). Custom ECS components need two things:
- ECS component struct - this will be holding your data
- component authoring - this will perform `conversion` from authoring world to game world.

<details><summary>Example</summary>

```csharp
[Il2CppImplements(typeof(IComponentData))]
public struct MyECSComponentCD
{
    public int value;
    public int3 position;
}

[Il2CppImplements(typeof(IConvertGameObjectToEntity))]
public class MyECSComponentCDAuthoring : ModCDAuthoringBase
{
    public Il2CppValueField<int> value;
    public Il2CppValueField<int3> position;

    public MyECSComponentCDAuthoring(IntPtr ptr) : base(ptr) { }
    
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddModComponentData(entity, new MyECSComponentCD()
        {
            value = value,
            position = position
        });
    }
}
```

</details>

## Valid types note

When creating ECS component remember that components can only hold value types. That includes primitives such as `byte`, `int`, `float`, `char` and `bool` and any `blittable` structs that contain only other value types.

For example your ECS component **CAN'T** contain these types: `object`, `string`, `Type`, `GameObject`, `MonoBehavior`, etc. It also can't contain any `non blittable` structs which use these types.

However your authoring component **CAN** contain non value types, but you will need to perform some kind of conversion to value types. 

## Registering custom ECS component

To register your component you must register **BOTH** the component itself, and it's authoring class:
```csharp
ComponentModule.RegisterECSComponent<MyECSComponentCD>();
ComponentModule.RegisterECSComponent<MyECSComponentCDAuthoring>();
```