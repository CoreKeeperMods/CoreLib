using Unity.Entities;

namespace CoreLib.Audio
{
    public interface IEffect
    {
        void PlayEffect(EffectEventCD effectEvent, Entity callerEntity, World world);
    }
}