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

        public static ConfigWrapper<int> choiceScaler { get; set; }
        public void initConfig()
        {
            CustomCommandPlugin.choiceScaler = base.Config.Wrap<int>("CustomCommandPlugin choice scaler", "Choice Scaler", "Determine how many items to choose from upon level up. Default: 3.", 3);
        }

        public void Start()
        {
            var miniRpc = MiniRpc.CreateInstance(ModGuid);
        }

        public void Awake()
        {
            RoR2.Chat.AddMessage("Artifact of Satisfaction activated: Level ups reward item drops.");
            this.initConfig();
            On.RoR2.GlobalEventManager.OnTeamLevelUp += delegate (On.RoR2.GlobalEventManager.orig_OnTeamLevelUp orig, TeamIndex self)
            {
                orig.Invoke(self);
                int count = RoR2.PlayerCharacterMasterController.instances.Count;
                float time = RoR2.Run.instance.time;
                for (int i = 0; i < count; i++)
                {
                    RoR2.CharacterMaster master = RoR2.PlayerCharacterMasterController.instances[i].master;
                    bool alive = master.isActiveAndEnabled;
                    if (alive)
                    {
                        ShowItemPicker(GetAvailablePickups(), master);
                    }
                }
            };
        }

        public void giveTier1Item(float offSet, RoR2.CharacterMaster master)
        {
            List<RoR2.PickupIndex> availableTier1DropList = RoR2.Run.instance.availableTier1DropList;
            int index = RoR2.Run.instance.treasureRng.RangeInt(0, availableTier1DropList.Count);
            master.inventory.GiveItem(availableTier1DropList[index].itemIndex);
        }

        public void giveTier2Item(float offSet, RoR2.CharacterMaster master)
        {
            List<RoR2.PickupIndex> availableTier2DropList = RoR2.Run.instance.availableTier2DropList;
            int index = RoR2.Run.instance.treasureRng.RangeInt(0, availableTier2DropList.Count);
            master.inventory.GiveItem(availableTier2DropList[index].itemIndex);
        }

        public void giveTier3Item(float offSet, RoR2.CharacterMaster master)
        {
            List<RoR2.PickupIndex> availableTier3DropList = RoR2.Run.instance.availableTier3DropList;
            int index = RoR2.Run.instance.treasureRng.RangeInt(0, availableTier3DropList.Count);
            master.inventory.GiveItem(availableTier3DropList[index].itemIndex);
        }

        public void giveLunarItem(float offSet, RoR2.CharacterMaster master)
        {
            List<RoR2.PickupIndex> availableLunarDropList = RoR2.Run.instance.availableLunarDropList;
            int index = RoR2.Run.instance.treasureRng.RangeInt(0, availableLunarDropList.Count);
            master.inventory.GiveItem(availableLunarDropList[index].itemIndex);
        }

        public void giveEquipment(float offSet, RoR2.CharacterMaster master)
        {
            List<RoR2.PickupIndex> availableEquipmentDropList = RoR2.Run.instance.availableEquipmentDropList;
            int index = RoR2.Run.instance.treasureRng.RangeInt(0, availableEquipmentDropList.Count);
            master.inventory.GiveItem(availableEquipmentDropList[index].itemIndex);
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

            float uiWidth = 300f;
            if (availablePickups.Count > 8 * 5) // at least 5 rows of 8 items
                uiWidth = 500f;
            if (availablePickups.Count > 10 * 5) // at least 5 rows of 10 items
                uiWidth = 600f;

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
            g.AddComponent<CursorOpener>();

            var ctr = new GameObject();
            ctr.name = "Container";
            ctr.transform.SetParent(g.transform, false);
            ctr.AddComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, uiWidth);

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
            header.GetComponent<HGTextMeshProUGUI>().text = "LEVEL UP\n Choose 1 upgrade.";
            header.GetComponent<HGTextMeshProUGUI>().color = Color.white;
            header.GetComponent<HGTextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            header.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 1f);
            header.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);
            header.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
            header.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 80);

            var itemCtr = new GameObject();
            itemCtr.name = "Item Container";
            itemCtr.transform.SetParent(ctr.transform, false);
            itemCtr.transform.localPosition = new Vector2(0, -100f);
            itemCtr.AddComponent<GridLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
            itemCtr.GetComponent<GridLayoutGroup>().cellSize = new Vector2(75f, 75f);
            itemCtr.GetComponent<GridLayoutGroup>().spacing = new Vector2(8f, 8f);
            itemCtr.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 1f);
            itemCtr.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);
            itemCtr.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
            itemCtr.GetComponent<RectTransform>().sizeDelta = new Vector2(-16f, 0);
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
                });
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(itemCtr.GetComponent<RectTransform>());
            ctr.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, itemCtr.GetComponent<RectTransform>().sizeDelta.y + 100f + 20f);
        }

    }
}