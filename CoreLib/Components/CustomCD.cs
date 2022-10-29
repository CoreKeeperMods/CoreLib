using CoreLib.Util.Extensions;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using Unity.Entities;
using UnityEngine;

namespace CoreLib.Components
{
   // [Il2CppImplements(typeof(IComponentData))]
    public struct CustomCD
    {
        public int value;

        public CustomCD()
        {
            value = 0;
        }

        public CustomCD(int value)
        {
            this.value = value;
        }
    }

    [Il2CppImplements(typeof(IConvertGameObjectToEntity))]
    public class CustomCDAuthoring : MonoBehaviour
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentDataRaw(entity, new LevelCD(){level = 8});
        }
        
        public int value;
    }
}