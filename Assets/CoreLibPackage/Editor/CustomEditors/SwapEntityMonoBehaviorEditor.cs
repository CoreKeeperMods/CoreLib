using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Editor
{
    [CustomEditor(typeof(EntityMonoBehaviour), true)]
    public class SwapEntityMonoBehaviorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            if (GUILayout.Button("Swap Component", GUILayout.Height(25)))
                SwapEntityMonoBehaviorWindow.ShowWindow((EntityMonoBehaviour)target);
        }
    }

    public class SwapEntityMonoBehaviorWindow : EditorWindow
    {
        private const string WindowTitle = "Swap Component";

        private EntityMonoBehaviour _originalComponent;
        
        [SerializeField, ClassReferenceDropdown(typeof(EntityMonoBehaviour))]
        public string swapTo;
        
        private SerializedObject _serializedObject;

        public static void ShowWindow(EntityMonoBehaviour target)
        {
            var window = GetWindow<SwapEntityMonoBehaviorWindow>(WindowTitle);
            window.position = new Rect(Screen.width, Screen.height / 2f, 450, 150);
            window.OnOpen(target);
        }

        private void OnOpen(EntityMonoBehaviour target)
        {
            _originalComponent = target;
            _serializedObject = new SerializedObject(this);
            swapTo = "";
        }

        private void OnGUI()
        {
            if (_serializedObject == null) Close();
            EditorGUILayout.Separator();
            EditorGUILayout.HelpBox(
                $"To perform a swap of {_originalComponent.GetType().Name} on {_originalComponent.gameObject.name} " +
                $"select a new EntityMonoBehaviour derived class below:", MessageType.Info);
            EditorGUILayout.Separator();
            if (_serializedObject == null) return;
            var swapToProperty = _serializedObject.FindProperty("swapTo");
            EditorGUILayout.PropertyField(swapToProperty);
            _serializedObject.ApplyModifiedProperties();
            if (!ClassReferenceDropdownAttribute.ALL_CLASS_TYPES.Exists(x => x != null && x.Name == swapTo)) return;
            var centeredStyle = GUI.skin.GetStyle("HelpBox");
            centeredStyle.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.Separator();
            EditorGUILayout.HelpBox($"You have selected {swapTo}!", MessageType.None);
            if (GUILayout.Button("Swap"))
                SwapComponent();
        }

        private void SwapComponent()
        {
            if (swapTo != null)
            {
                var gameObject = _originalComponent.gameObject;
                var type = ClassReferenceDropdownAttribute.ALL_CLASS_TYPES.Find(x => x != null && x.Name == swapTo);
                var newComponent = gameObject.AddComponent(type);

                var originalFields = _originalComponent.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var newFields = newComponent.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
                foreach (var field in originalFields)
                {
                    var newField = newFields.FirstOrDefault(info => info.Name.Equals(field.Name));
                    if (newField == null) continue;
                    newField.SetValue(newComponent, field.GetValue(_originalComponent));
                }
                newComponent.MoveToTop();
                InternalEditorUtility.SetIsInspectorExpanded(newComponent, true);
                Undo.DestroyObjectImmediate(_originalComponent);
            }
            else
            {
                Debug.LogError("Swap component was not selected!");
            }
            Close();
        }
    }
}