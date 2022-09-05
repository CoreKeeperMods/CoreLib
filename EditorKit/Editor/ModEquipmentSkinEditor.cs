using CoreLib.Components;
using UnityEditor;

namespace EditorKit.Editor
{
    [CustomEditor(typeof(ModEquipmentSkinCDAuthoring))]
    public class ModEquipmentSkinEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            ModEquipmentSkinCDAuthoring authoring = (ModEquipmentSkinCDAuthoring)target;
            EntityMonoBehaviourData entity = authoring.GetComponent<EntityMonoBehaviourData>();

            if (entity == null)
            {
                base.OnInspectorGUI();
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ModEquipmentSkinCDAuthoring.skinTexture)));
                switch (entity.objectInfo.objectType)
                {
                    case ObjectType.Helm:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ModEquipmentSkinCDAuthoring.helmHairType)));
                        break;
                    case ObjectType.BreastArmor:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ModEquipmentSkinCDAuthoring.shirtVisibility)));
                        break;
                    case ObjectType.PantsArmor:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ModEquipmentSkinCDAuthoring.pantsVisibility)));
                        break;
                }
            }
        }
    }
}