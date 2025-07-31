# Mod Resources Module
Mod Resources Module is a CoreLib submodule that organises mod resources and facilitates loading of assets

## How to use the module:
This module does not require explicit loading.

### Register your mod bundles

In your plugin `EarlyInit()` method write:
```cs
// Register bundles
var modInfo = GetModInfo(this);
ResourcesModule.RegisterBundles(modInfo);
```

At this point you can use to load any assets in your mod. For example:

```cs
var clip = ResourcesModule.LoadAsset<AudioClip>("Assets/myamazingmod/Music/myEpicMusic");
```

If you need an `AssetReference` to pass to some game method, you can do this:

```csharp
var clipRef = "Assets/myamazingmod/Music/myEpicMusic".AsAddress<AudioClip>();
```