using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using LeTai.Asset.TranslucentImage;
using MiniRpcLib;
using MiniRpcLib.Action;
using RoR2;
using RoR2.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Events;

namespace OhDangTheMods
{

    [BepInPlugin(ModGuid, "CustomCommandPlugin", "1.0.0")]
    [BepInDependency(MiniRpcPlugin.Dependency)]
    public class CustomCommandPlugin : BaseUnityPlugin
    {
        private const string ModGuid = "com.OhDangTheJam.OhDangTheMods";

        private int levelsSpent = 0;
        private int levelsTotal = 0;

        public static ConfigWrapper<int> choiceScaler { get; set; }
        public void initConfig()
        {
            CustomCommandPlugin.choiceScaler = base.Config.Wrap<int>("CustomCommandPlugin choice scaler", "Choice Scaler", "Determine how many items to choose from upon level up. Default: 3.", 3);
        }

        public void Start()
        {
            var miniRpc = MiniRpc.CreateInstance(ModGuid);
            RoR2.Chat.AddMessage("Level ups reward item drops.");
            this.initConfig();
        }

        public void Awake()
        {
            /* NOT WORKING - AWAKE IS NOT CALLED ON SCENE CHANGE
            if (levelsSpent >= 0 && levelsSpent < levelsTotal)
            {
                for (int i = levelsSpent; i == levelsTotal; i++)
                {
                    ShowItemPicker(GetAvailablePickups(), RoR2.PlayerCharacterMasterController.instances[0].master);
                }
            }
            */

            On.RoR2.GlobalEventManager.OnTeamLevelUp += delegate (On.RoR2.GlobalEventManager.orig_OnTeamLevelUp orig, TeamIndex self)
            {
                levelsTotal += 1;
                RoR2.Chat.AddMessage("Level up: " + levelsTotal);
                if (levelsTotal > 0 && levelsSpent < levelsTotal) // 
                {
                    orig.Invoke(self);
                    int count = RoR2.PlayerCharacterMasterController.instances.Count;
                    for (int i = 0; i < count; i++)
                    {
                        RoR2.CharacterMaster master = RoR2.PlayerCharacterMasterController.instances[i].master;
                        bool alive = master.hasBody;
                        if (alive)
                        {
                            ShowItemPicker(GetAvailablePickups(), master);
                        }
                    }
                }
            };
        }

        private List<PickupIndex> GetAvailablePickups()
        {
            var availablePickups = new List<PickupIndex>();
            var selectedPickups = new List<PickupIndex>();
            var tier = Mathf.RoundToInt(UnityEngine.Random.Range(0, 100));
            if (tier <= 50)
                selectedPickups.AddRange(Run.instance.availableTier1DropList);
            else if (tier <= 85)
                selectedPickups.AddRange(Run.instance.availableTier2DropList);
            else if (tier <= 90)
                selectedPickups.AddRange(Run.instance.availableTier3DropList);
            else if (tier <= 95)
                selectedPickups.AddRange(Run.instance.availableLunarDropList);
            else if (tier <= 100)
                selectedPickups.AddRange(Run.instance.availableEquipmentDropList);

            if (selectedPickups.Count > 0)
            {
                for (var i = 0; i < choiceScaler.Value; i++)
                {
                    int currentPickup = Mathf.RoundToInt((UnityEngine.Random.Range(0, selectedPickups.Count - 1)));
                    availablePickups.Add(selectedPickups[currentPickup]);
                }
            }

            return availablePickups;
        }

        public void ShowItemPicker(List<PickupIndex> availablePickups, RoR2.CharacterMaster master)
        {
            var itemInventoryDisplay = GameObject.Find("ItemInventoryDisplay");

            float uiWidth = 220f;

            Logger.Log(LogLevel.Info, "Run started");
            var g = new GameObject();
            g.name = "ChestItemsUI";
            g.layer = 5; // UI
            g.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            g.GetComponent<Canvas>().sortingOrder = -1; // Required or the UI will render over pause and tooltips.
            // g.AddComponent<CanvasScaler>().scaleFactor = 10.0f;
            // g.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
            g.AddComponent<GraphicRaycaster>();
            g.AddComponent<MPEventSystemProvider>().fallBackToMainEventSystem = true;
            g.AddComponent<MPEventSystemLocator>();
            //g.AddComponent<CursorOpener>();

            var ctr = new GameObject();
            ctr.name = "Container";
            ctr.transform.SetParent(g.transform, false);
            ctr.AddComponent<RectTransform>();
            ctr.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0f);
            ctr.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);
            ctr.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
            ctr.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, uiWidth);

            var bg2 = new GameObject();
            bg2.name = "Background";
            bg2.transform.SetParent(ctr.transform, false);
            bg2.AddComponent<TranslucentImage>().color = new Color(0f, 0f, 0f, 1f);
            bg2.GetComponent<TranslucentImage>().raycastTarget = true;
            bg2.GetComponent<TranslucentImage>().material = Resources.Load<GameObject>("Prefabs/UI/Tooltip").GetComponentInChildren<TranslucentImage>(true).material;
            bg2.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0f);
            bg2.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);
            bg2.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 0);

            var bg = new GameObject();
            bg.name = "Background";
            bg.transform.SetParent(ctr.transform, false);
            bg.AddComponent<Image>().sprite = itemInventoryDisplay.GetComponent<Image>().sprite;
            bg.GetComponent<Image>().type = Image.Type.Sliced;
            bg.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0f);
            bg.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);
            bg.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 0);

            var header = new GameObject();
            header.name = "Header";
            header.transform.SetParent(ctr.transform, false);
            header.transform.localPosition = new Vector2(0, 0);
            header.AddComponent<HGTextMeshProUGUI>().fontSize = 15;
            header.GetComponent<HGTextMeshProUGUI>().text = "LEVEL UP\n" + "Level " + (levelsSpent + 1);
            header.GetComponent<HGTextMeshProUGUI>().color = Color.white;
            header.GetComponent<HGTextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            header.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 1f);
            header.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);
            header.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
            header.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 35);

            var itemCtr = new GameObject();
            itemCtr.name = "Item Container";
            itemCtr.transform.SetParent(ctr.transform, false);
            itemCtr.transform.localPosition = new Vector2(0, -40f);
            itemCtr.AddComponent<GridLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
            itemCtr.GetComponent<GridLayoutGroup>().cellSize = new Vector2(50f, 50f);
            itemCtr.GetComponent<GridLayoutGroup>().spacing = new Vector2(4f, 4f);
            itemCtr.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 1f);
            itemCtr.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);
            itemCtr.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
            itemCtr.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 0);
            itemCtr.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var itemIconPrefab = itemInventoryDisplay.GetComponent<ItemInventoryDisplay>().itemIconPrefab;
            foreach (PickupIndex index in availablePickups)
            {
                if (index.itemIndex == ItemIndex.None)
                    continue;
                var item = Instantiate<GameObject>(itemIconPrefab, itemCtr.transform).GetComponent<ItemIcon>();
                item.SetItemIndex(index.itemIndex, 1);
                item.gameObject.AddComponent<Button>().onClick.AddListener(() => {
                    Logger.LogInfo("Item picked: " + index);
                    UnityEngine.Object.Destroy(g);
                    master.inventory.GiveItem(index.itemIndex);
                    levelsSpent += 1;
                    if (levelsSpent >= 0 && levelsSpent < levelsTotal)
                        ShowItemPicker(GetAvailablePickups(), master);
                    RoR2.Chat.AddMessage("Perks chosen: " + levelsSpent + " / " + levelsTotal);
                });
            }
            foreach (PickupIndex index in availablePickups)
            {
                if (index.equipmentIndex == EquipmentIndex.None)
                    continue;
                var def = EquipmentCatalog.GetEquipmentDef(index.equipmentIndex);
                var item = Instantiate<GameObject>(itemIconPrefab, itemCtr.transform).GetComponent<ItemIcon>();
                item.GetComponent<RawImage>().texture = def.pickupIconTexture;
                item.stackText.enabled = false;
                item.tooltipProvider.titleToken = def.nameToken;
                item.tooltipProvider.titleColor = ColorCatalog.GetColor(def.colorIndex);
                item.tooltipProvider.bodyToken = def.pickupToken;
                item.tooltipProvider.bodyColor = Color.gray;
                item.gameObject.AddComponent<Button>().onClick.AddListener(() => {
                    Logger.LogInfo("Equipment picked: " + index);
                    UnityEngine.Object.Destroy(g);
                    master.inventory.GiveEquipmentString(def.name);
                    levelsSpent += 1;
                    if (levelsSpent > 0 && levelsSpent < levelsTotal)
                        ShowItemPicker(GetAvailablePickups(), master);
                    RoR2.Chat.AddMessage("Perks chosen: " + levelsSpent + " / " + levelsTotal);
                });
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(itemCtr.GetComponent<RectTransform>());
            ctr.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, itemCtr.GetComponent<RectTransform>().sizeDelta.y + 40f);
        }

    }
}