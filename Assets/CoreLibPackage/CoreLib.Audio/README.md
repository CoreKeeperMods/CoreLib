# Audio Module
Audio Module is a CoreLib submodule that allows to add custom music and sound effects

## Usage example:
Make sure to call `CoreLibMod.LoadModules(typeof(AudioModule));` to in your mod `EarlyInit()` function, before using the module. This will load the submodule.

Now in your Unity project import needed assets into your mod folder. Next ensure that your mod bundles have been registered using `ResourcesModule`.

### Add custom music

In your mod `EarlyInit()` method write:
```cs
//You can add your own roster or use existing ones
MusicManager.MusicRosterType roster = AudioModule.AddCustomRoster();

//Now add music clip to a roster
var clipRef = "Assets/myamazingmod/Music/myEpicMusic".AsAddress<AudioClip>();
AudioModule.AddMusicToRoster(roster, clipRef);
```
To play the music you can use `AudioManager` class methods as usual.
```cs
Manager.music.SetNewMusicPlaylist(roster);
```

### Add custom sound effects

```cs
// After adding the sound effect make sure to remember the SfxID
var clip = ResourcesModule.LoadAsset<AudioClip>("Assets/myamazingmod/Music/my-sound-effect");
SfxID soundEffect = AudioModule.AddSoundEffect(clip);
```
To play your sound effect you can use `EffectsManager` class methods as usual.
```cs
AudioManager.Sfx(soundEffect, position);
```