using HarmonyLib;
using UnityEngine;

namespace CoreLib.UserInterface.Patches
{
    [HarmonyPatch]
    public class UIManager_Patch
    {
        [HarmonyPatch(typeof(UIManager), nameof(UIManager.Init))]
        [HarmonyPostfix]
        public static void OnInit(UIManager __instance)
        {
            var UITransform = __instance.chestInventoryUI.transform.parent;

            foreach (ModUIAuthoring interfacePrefab in UserInterfaceModule.interfacePrefabs)
            {
                var interfaceGameObject = Object.Instantiate(interfacePrefab.gameObject, UITransform);
                var interfaceComponent = interfaceGameObject.GetComponent<IModUI>();
                interfaceGameObject.transform.localPosition = interfacePrefab.initialInterfacePosition;
                UserInterfaceModule.modInterfaces.Add(interfacePrefab.modInterfaceID, interfaceComponent);
            }
        }
        
        [HarmonyPatch(typeof(UIManager), nameof(UIManager.HideAllInventoryAndCraftingUI))]
        [HarmonyPostfix]
        public static void OnHide()
        {
            foreach (IModUI modUI in UserInterfaceModule.modInterfaces.Values)
            {
                modUI.HideUI();
            }

            UserInterfaceModule.ClearModUIData();
        }
    }
}