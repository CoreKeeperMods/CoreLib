# Specifics guide
This guide has many sections dedicated to some specific kind of entity. What is explained here is only the difference from it's closest base



## Rotatable block
Rotatable blocks in their nature are blocks with certain amount of variations, that can be rotated using E key after being placed. To make such entity make 4 entity prefabs, which are configured to each rotation with correct variation (Reference other rotatable prefabs in vanilla game).

Note that you will need only one visual prefab. This prefab will have to modify itself, when user clicks to rotate the block.

<details><summary>Example</summary>

```csharp
public class MyRotatableBlock : ModEntityMonoBehavior
{
    public Il2CppReferenceField<SpriteRenderer> mainRenderer;
    public Il2CppReferenceField<SpriteSheetSkin> mainSkin;

    public Il2CppReferenceField<List<Sprite>> mainSprites;
    private GCHandle mainSpritesHandle;

    public MyRotatableBlock(IntPtr ptr) : base(ptr) { }

    public override bool Allocate()
    {
        bool shouldAllocate = base.Allocate();
        if (shouldAllocate)
        {
            mainSpritesHandle = GCHandle.Alloc(mainSprites.Value);
        }
        return shouldAllocate;
    }

    public override void OnOccupied()
    {
        this.CallBase<EntityMonoBehaviour>(nameof(OnOccupied));
        UpdateVisual();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        mainSpritesHandle.Free();
    }

    public void OnUse()
    {
        int newVariation = (variation + 1) % 4;
        SetVariation(newVariation);
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (mainSprites.Value != null && variation < mainSprites.Value.Count)
        {
            Sprite sprite = mainSprites.Value._items[variation];
            mainRenderer.Value.sprite = sprite;
        }
    }
}
```
`mainSprites` field should contain sprite for each rotation at that variation.

If you want to animate the entity do not bother to make arrays of arrays of sprites, just set the texture of the texture skin instead. 

During tests this code frequently crashed the game with no obvious reason and no logs left. If you use it, please test your mod thoroughly before releasing.
</details>

## Animated block
Doing animations here is as simple as replacing the texture used by sprite renderer at the correct time. Other than that setup is identical. You can even combine this with effects like rotation.

This works by hot swapping Sprite texture sheet, without modifying the sprite. If each sheet in animation is setup identically, sprites from first frame apply correctly.

<details><summary>Example</summary>

```cs
public class AnimatedBlock : ModEntityMonoBehavior
{
    public Il2CppReferenceField<SpriteSheetSkin> mainSkin;

    public Il2CppReferenceField<List<Texture2D>> frames;
    private GCHandle framesHandle;


    public AnimatedBlock(IntPtr ptr) : base(ptr) { }

    public override bool Allocate()
    {
        bool shouldAllocate = base.Allocate();
        if (shouldAllocate)
        {
            framesHandle = GCHandle.Alloc(frames.Value);
        }

        return shouldAllocate;
    }


    public override void OnDestroy()
    {
        base.OnDestroy();

        framesHandle.Free();
    }

    public override void ManagedLateUpdate()
    {
        this.CallBase<EntityMonoBehaviour>(nameof(ManagedLateUpdate));

        if (entityExist)
        {
            int frame = (int)(Time.time * 15) % frames.Value.Count;
            Texture2D currentFrame = frames.Value._items[frame];
            mainSkin.Value.skin = currentFrame;
        }
    }
}
```
</details>

## Projectile
Custom projectiles need to be derived from `ModProjectile`, which is almost identical to `ModEntityMonoBehaviour`. Other than this it's identical to block setup.

<details><summary>Example</summary>

```csharp
public class MyProjectile : ModProjectile
{
    public Il2CppReferenceField<Transform> directionTransform;
    public Il2CppReferenceField<ParticleSystem> projectileFx;
    public Il2CppReferenceField<ParticleSystem> fireballSmoke;
    public Il2CppReferenceField<ParticleSystem> fireballFireTrail;
    public Il2CppReferenceField<ParticleSystem> hit;
    public Il2CppReferenceField<PugLight> fireLight;

    public MyProjectile(IntPtr ptr) : base(ptr) { }

    public override void OnOccupied()
    {
        //this.CallBase<Projectile>(nameof(OnOccupied));
        int health = currentHealth;
        directionTransform.Value.gameObject.SetActive(health > 0);
        if (health <= 0) return;

        AudioManager.Sfx(SfxID.fireball, transform.position, 0.8f, 1, 0.1f);
        AudioManager.Sfx(SfxID.anicentDevicePowerUp, transform.position, 0.6f, 0.7f, 0.1f);
        ProjectileCD projectileCd = EntityUtility.GetComponentData<ProjectileCD>(entity, world);

        Vector3 dir = projectileCd.direction * 0.3f;
        Vector3 renderPos = ToRenderFromWorld(WorldPosition);
        Vector3 puffPos = renderPos + directionTransform.Value.localPosition + dir;
        
        Manager.effects.PlayPuff(PuffID.SmallEnergyExplosion, puffPos);

        dir = directionTransform.Value.position + (Vector3)projectileCd.direction;
        directionTransform.Value.transform.LookAt(dir, Vector3.up);
        
        projectileFx.Value.Play();
        if (fireballSmoke.Value != null)
            fireballSmoke.Value.Play();
        if (fireballFireTrail.Value != null)
            fireballFireTrail.Value.Play();
        fireLight.Value.gameObject.SetActive(true);
    }

    public override void OnDeath()
    {
        this.CallBase<Projectile>(nameof(OnDeath));
        
        if (projectileFx.Value != null && hit.Value != null)
        {
            projectileFx.Value.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (fireballSmoke.Value != null)
                fireballSmoke.Value.Stop();
            if (fireballFireTrail.Value != null)
                fireballFireTrail.Value.Stop();
            hit.Value.Play();
        }
        fireLight.Value.gameObject.SetActive(false);
        SpawnFadeOutLight(fireLight.Value.lightToOptimize);
    }
}
```
Note the removed base call in `OnOccupied()`. During experiments I found that this method for some reason is never called. So far I was not able to figure this out. For now you can bypass the issue by patching `Projectile` and redirecting the call.
```csharp
[HarmonyPatch(typeof(Projectile), nameof(Projectile.OnOccupied))]
[HarmonyPostfix]
public static void OnOccupied(Projectile __instance)
{
    MyProjectile myProjectile = __instance.TryCast<MyProjectile>();

    if (myProjectile != null)
    {
        myProjectile.OnOccupied();
    }
}
```
</details>

## Enemy
Custom enemies work almost identically to blocks. You can adjust their loot by adding a custom loot table. Most of things you will want to edit in their behavior should be configurable using components and their settings.

Currently defining where the enemy is spawned is not added by CoreLib, but it is planned. This is defined in `EnvironmentSpawnObjectsTable`, and editing it in your plugin should allow to set this.

<details><summary>Example</summary>

```csharp
public class MyCustomEnemy : ModEntityMonoBehavior
{
    public MyCustomEnemy(IntPtr ptr) : base(ptr) { }

    public void AE_AnticipationSound()
    {
        AudioManager.Sfx(SfxID.slimeAnticipation, transform.position, 0.8f, 1, 0.1f);
    }

    public void AE_Jump()
    {
        AudioManager.Sfx(SfxID.jump2, transform.position, 0.8f, 1, 0.1f);
    }

    public override void HandleAnimationTrigger(int animID)
    {
        this.CallBase<EntityMonoBehaviour, Action<int>>(nameof(HandleAnimationTrigger), animID);
        if (animID == AnimID.death)
        {
            Manager.effects.PlayPuff(PuffID.SlimeExplosion, transform.position, 30);
            if (shadow != null)
            {
                shadow.SetActive(false);
            }
        }
    }

    public override void HandleInitialAnimationTrigger(int animID)
    {
        this.CallBase<EntityMonoBehaviour, Action<int>>(nameof(HandleInitialAnimationTrigger), animID);

        if (animID == AnimID.death)
        {
            if (shadow != null)
            {
                shadow.SetActive(false);
            }
        }
    }

    public override bool ShouldPlayAnimTrigger(int animID)
    {
        bool result = (bool)this.CallBase<EntityMonoBehaviour, Func<int, bool>>(nameof(ShouldPlayAnimTrigger), animID);
        if (lastAnim == AnimID.idle || lastAnim == AnimID.move)
        {
            if (animID == AnimID.idle)
            {
                return false;
            }

            return result && animID != AnimID.move;
        }

        return result;
    }
}
```
Note methods called `AE_AnticipationSound()`. These are Animation Events and are called by animator at times marked in it's dope sheet.

This particular code is for a slime enemy.
</details>