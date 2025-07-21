using HarmonyLib;
using UnityEngine;

namespace CoreLib.UserInterface.Patches
{
    public class UIManager_Patch
    {
        [HarmonyPatch(typeof(UIManager), nameof(UIManager.Init))]
        [HarmonyPostfix]
        public static void OnInit(UIManager __instance)
        {
            var UITransform = __instance.chestInventoryUI.transform.parent;
            var inventoryUI = __instance.playerInventoryUI;

            foreach (ModUIAuthoring interfacePrefab in UserInterfaceModule.interfacePrefabs)
            {
                var interfaceGameObject = Object.Instantiate(interfacePrefab.gameObject, UITransform);
                var interfaceComponent = interfaceGameObject.GetComponent<IModUI>();
                interfaceGameObject.transform.localPosition = interfacePrefab.initialInterfacePosition;
                UserInterfaceModule.modInterfaces.Add(interfacePrefab.modInterfaceID, interfaceComponent);

                var myLink = interfaceGameObject.GetComponent<LinkToPlayerInventory>();
                MakeLink(myLink, inventoryUI);
                
                var links = interfaceGameObject.GetComponentsInChildren<LinkToPlayerInventory>(true);
                foreach (var link in links)
                {
                    MakeLink(link, inventoryUI);
                }
            }
        }

        private static void MakeLink(LinkToPlayerInventory link, ItemSlotsUIContainer inventoryUI)
        {
            if (link == null) return;
            
            var uiElement = link.gameObject.GetComponent<UIelement>();
            if (uiElement == null) return;

            uiElement.bottomUIElements.Add(inventoryUI);
            if (link.createReverseLink)
            {
                inventoryUI.topUIElements.Add(uiElement);
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
        
        [HarmonyPatch(typeof(UIManager), nameof(UIManager.isAnyInventoryShowing), MethodType.Getter)]
        [HarmonyPostfix]
        public static void OnIsAnyMenuActive(ref bool __result)
        {
            if (__result) return;

            __result |= UserInterfaceModule.currentInterface != null;
        }
    }
}