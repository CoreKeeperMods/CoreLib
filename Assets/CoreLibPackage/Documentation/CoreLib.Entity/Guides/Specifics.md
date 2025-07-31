# Specifics guide
This guide has many sections dedicated to some specific kind of entity. What is explained here is only the difference from it's closest base

## Animated block
Doing animations here is as simple as replacing the texture used by sprite renderer at the correct time. Other than that setup is identical.

This works by hot swapping Sprite texture sheet, without modifying the sprite. If each sheet in animation is setup identically, sprites from first frame apply correctly.

<details><summary>Example</summary>

```cs
public class AnimatedBlock : EntityMonoBehavior
{
    public SpriteSheetSkin mainSkin;

    public List<Texture2D> frames;

    public override void ManagedLateUpdate()
    {
        base.ManagedLateUpdate();

        if (entityExist)
        {
            int frame = (int)(Time.time * 15) % frames.Count;
            mainSkin.skin = frames[frame];
        }
    }
}
```
</details>

## Projectile
Custom projectiles need to be derived from `Projectile`. Other than this it's identical to block setup.

<details><summary>Example</summary>

```csharp
public class MyProjectile : Projectile
{
    public Transform> directionTransform;
    public ParticleSystem projectileFx;
    public ParticleSystem fireballSmoke;
    public ParticleSystem fireballFireTrail;
    public ParticleSystem hit;
    public PugLight fireLight;

    public override void OnOccupied()
    {
        base.OnOccupied();
        int health = currentHealth;
        directionTransform.gameObject.SetActive(health > 0);
        if (health <= 0) return;

        AudioManager.Sfx(SfxID.fireball, transform.position, 0.8f, 1, 0.1f);
        AudioManager.Sfx(SfxID.anicentDevicePowerUp, transform.position, 0.6f, 0.7f, 0.1f);
        ProjectileCD projectileCd = EntityUtility.GetComponentData<ProjectileCD>(entity, world);

        Vector3 dir = projectileCd.direction * 0.3f;
        Vector3 renderPos = ToRenderFromWorld(WorldPosition);
        Vector3 puffPos = renderPos + directionTransform.localPosition + dir;
        
        Manager.effects.PlayPuff(PuffID.SmallEnergyExplosion, puffPos);

        dir = directionTransform.position + (Vector3)projectileCd.direction;
        directionTransform.transform.LookAt(dir, Vector3.up);
        
        projectileFx.Play();
        if (fireballSmoke != null)
            fireballSmoke.Play();
        if (fireballFireTrail != null)
            fireballFireTrail.Play();
        fireLight.gameObject.SetActive(true);
    }

    public override void OnDeath()
    {
        base.OnDeath();
        
        if (projectileFx != null && hit != null)
        {
            projectileFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (fireballSmoke != null)
                fireballSmoke.Stop();
            if (fireballFireTrail != null)
                fireballFireTrail.Stop();
            hit.Play();
        }
        fireLight.gameObject.SetActive(false);
        SpawnFadeOutLight(fireLight.lightToOptimize);
    }
}
```
</details>

## Enemy
Custom enemies work almost identically to blocks. You can adjust their loot by adding a custom loot table. Most of things you will want to edit in their behavior should be configurable using components and their settings.

Currently defining where the enemy is spawned is not added by CoreLib, but it is planned. This is defined in `EnvironmentSpawnObjectsTable`, and editing it in your plugin should allow to set this.

<details><summary>Example</summary>

```csharp
public class MyCustomEnemy : EntityMonoBehavior
{
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
        base.HandleAnimationTrigger(animID);
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
        base.HandleInitialAnimationTrigger(animID);

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
        bool result = base.ShouldPlayAnimTrigger(animID);
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