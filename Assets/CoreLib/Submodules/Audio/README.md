# Audio Module
Audio Module is a submodule that allows to add custom music and sound effects

## Usage example:
Make sure to add `[CoreLibSubmoduleDependency(nameof(AudioModule))]` to your plugin attributes. This will load the submodule.

Before continuing follow guide on [Resource Module](../ModResources/README.md) page to setup your asset bundle.

Now in your Unity project import needed assets into your folder.

### Add custom music

In your plugin `Load()` method write:
```cs
//You can add your own roster or use existing ones
MusicManager.MusicRosterType roster = AudioModule.AddCustomRoster();

//Now add music clip to a roster
AudioModule.AddMusicToRoster(roster, "Assets/myamazingmod/Music/myEpicMusic");
```
To play the music you can use `AudioManager` class methods as usual.
```cs
Manager.music.SetNewMusicPlaylist(roster);
```

### Add custom sound effects

```cs
// After adding the sound effect make sure to remember the SfxID
SfxID soundEffect = AudioModule.AddSoundEffect("Assets/myamazingmod/Music/my-sound-effect");
```
To play your sound effect you can use `EffectsManager` class methods as usual.
```cs
AudioManager.Sfx(soundEffect, position);
```