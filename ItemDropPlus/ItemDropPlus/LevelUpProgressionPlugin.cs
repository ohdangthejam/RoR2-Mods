using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using LeTai.Asset.TranslucentImage;
using MiniRpcLib;
using MiniRpcLib.Action;
using On.RoR2;
using RoR2;
using RoR2.UI;
using System;
using System.IO;
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

    [BepInPlugin(ModGuid, "LevelUpProgressionPlugin", "1.0.0")]
    [BepInDependency(MiniRpcPlugin.Dependency)]
    public class LevelUpProgressionPlugin : BaseUnityPlugin
    {

        private const string ModGuid = "com.OhDangTheJam.LevelUpProgressionPlugin";

        private bool showUI = false;
        private static int levelsSpent = 0;
        private int levelsTotal = 0;
        private bool startMessage = true;
        private RoR2.CharacterMaster currentPlayer;
        private Button button1 = null;
        private Button button2 = null;
        private Button button3 = null;
        private GameObject uiObject = null;
        private List<RoR2.PickupIndex> saveList = new List<RoR2.PickupIndex>();

        public static ConfigWrapper<int> lowerLevel { get; set; }
        public static ConfigWrapper<int> upperLevel { get; set; }

        public static ConfigWrapper<float> lowerLevelTier1Weight { get; set; }
        public static ConfigWrapper<float> lowerLevelTier2Weight { get; set; }
        public static ConfigWrapper<float> lowerLevelTier3Weight { get; set; }
        public static ConfigWrapper<float> lowerLevelLunarWeight { get; set; }
        public static ConfigWrapper<float> lowerLevelEquipmentWeight { get; set; }

        public static ConfigWrapper<float> upperLevelTier1Weight { get; set; }
        public static ConfigWrapper<float> upperLevelTier2Weight { get; set; }
        public static ConfigWrapper<float> upperLevelTier3Weight { get; set; }
        public static ConfigWrapper<float> upperLevelLunarWeight { get; set; }
        public static ConfigWrapper<float> upperLevelEquipmentWeight { get; set; }

        public void initConfig()
        {

            LevelUpProgressionPlugin.lowerLevel = base.Config.Wrap<int>("CustomCommandPlugin lower level", "Lower Level", "Determines the level that rarity chances begin scaling. Default: 1.", 1);
            LevelUpProgressionPlugin.upperLevel = base.Config.Wrap<int>("CustomCommandPlugin upper level", "Upper Level", "Determines the level that rarity chances stop scaling. Default: 30.", 30);

            LevelUpProgressionPlugin.lowerLevelTier1Weight = base.Config.Wrap<float>("CustomCommandPlugin lower level tier 1 weight", "Lower Level Tier 1 Weight", "Determines the weight of tier 1 drops at the lower level. Default 60.", 60f);
            LevelUpProgressionPlugin.lowerLevelTier2Weight = base.Config.Wrap<float>("CustomCommandPlugin lower level tier 2 weight", "Lower Level Tier 2 Weight", "Determines the weight of tier 2 drops at the lower level. Default 25.", 25f);
            LevelUpProgressionPlugin.lowerLevelTier3Weight = base.Config.Wrap<float>("CustomCommandPlugin lower level tier 3 weight", "Lower Level Tier 3 Weight", "Determines the weight of tier 3 drops at the lower level. Default 0.5.", 0.5f);
            LevelUpProgressionPlugin.lowerLevelLunarWeight = base.Config.Wrap<float>("CustomCommandPlugin lower level lunar weight", "Lower Level Lunar Weight", "Determines the weight of lunar drops at the lower level. Default 4.5.", 4.5f);
            LevelUpProgressionPlugin.lowerLevelEquipmentWeight = base.Config.Wrap<float>("CustomCommandPlugin lower level equipment weight", "Lower Level Equipment Weight", "Determines the weight of equipment drops at the lower level. Default 10.", 10f);

            LevelUpProgressionPlugin.upperLevelTier1Weight = base.Config.Wrap<float>("CustomCommandPlugin upper level tier 1 weight", "Upper Level Tier 1 Weight", "Determines the weight of tier 1 drops at the upper level. Default 30.", 30f);
            LevelUpProgressionPlugin.upperLevelTier2Weight = base.Config.Wrap<float>("CustomCommandPlugin upper level tier 2 weight", "Upper Level Tier 2 Weight", "Determines the weight of tier 2 drops at the upper level. Default 35.", 35f);
            LevelUpProgressionPlugin.upperLevelTier3Weight = base.Config.Wrap<float>("CustomCommandPlugin upper level tier 3 weight", "Upper Level Tier 3 Weight", "Determines the weight of tier 3 drops at the upper level. Default 25.", 25f);
            LevelUpProgressionPlugin.upperLevelLunarWeight = base.Config.Wrap<float>("CustomCommandPlugin upper level lunar weight", "Upper Level Lunar Weight", "Determines the weight of lunar drops at the upper level. Default 5.", 5f);
            LevelUpProgressionPlugin.upperLevelEquipmentWeight = base.Config.Wrap<float>("CustomCommandPlugin upper level equipment weight", "Upper Level Equipment Weight", "Determines the weight of equipment drops at the upper level. Default 5.", 5f);

        }

        public void Start()
        {

            var miniRpc = MiniRpc.CreateInstance(ModGuid);
            this.initConfig();

        }

        public void Update()
        {

            if (Input.GetKeyDown(KeyCode.Alpha9))
                RoR2.PlayerCharacterMasterController.instances[0].master.GiveExperience(300000);
            if (Input.GetKeyDown(KeyCode.Alpha1) && button1 != null)
                button1.onClick.Invoke();
            if (Input.GetKeyDown(KeyCode.Alpha2) && button2 != null)
                button2.onClick.Invoke();
            if (Input.GetKeyDown(KeyCode.Alpha3) && button3 != null)
                button3.onClick.Invoke();
        }

        public void Awake()
        {

            On.RoR2.TeleporterInteraction.OnInteractionBegin += delegate (On.RoR2.TeleporterInteraction.orig_OnInteractionBegin orig, RoR2.TeleporterInteraction self, RoR2.Interactor interactor)
            {

                orig.Invoke(self, interactor);

            };

            //On Start
            On.RoR2.Run.Start += delegate (On.RoR2.Run.orig_Start orig, RoR2.Run self)
            {
                orig.Invoke(self);
                levelsSpent = 0;
                this.levelsTotal = 0;
                this.startMessage = true;
            };

            //On Spawn
            On.RoR2.Run.BeginStage += delegate (On.RoR2.Run.orig_BeginStage orig, RoR2.Run self)
            {

                orig.Invoke(self);

                showUI = false;
                button1 = null;
                button2 = null;
                button3 = null;

                if ((levelsTotal > 0 && levelsSpent < levelsTotal) || levelsTotal <= 0)
                {

                    if (showUI == false && startMessage == true)
                        StartCoroutine(ShowItemPickerCoroutine(3));
                    if (showUI == false && startMessage == false)
                        StartCoroutine(ShowItemPickerCoroutine(0.8f));

                }

            };

            // On Level Up
            On.RoR2.GlobalEventManager.OnTeamLevelUp += delegate (On.RoR2.GlobalEventManager.orig_OnTeamLevelUp orig, TeamIndex self)
            {

                orig.Invoke(self);
                levelsTotal = (int)RoR2.TeamManager.instance.GetTeamLevel(RoR2.PlayerCharacterMasterController.instances[0].master.teamIndex);

                if (levelsTotal > 0 && levelsSpent < levelsTotal)
                {

                    int count = RoR2.PlayerCharacterMasterController.instances.Count;
                    for (int i = 0; i < count; i++)
                    {

                        RoR2.CharacterMaster master = RoR2.PlayerCharacterMasterController.instances[i].master;
                        bool alive = master.hasBody;
                        if (alive)
                        {

                            if (showUI == false)
                                ShowItemPicker(GetAvailablePickups(), master);

                        }
                    }
                }
            };
        }

        IEnumerator ShowItemPickerCoroutine(float waitForSeconds)
        {

            if (waitForSeconds > 0)
                yield return new WaitForSeconds(waitForSeconds);

            RoR2.CharacterMaster master = GetCurrentPlayer();
            ShowItemPicker(GetAvailablePickups(), GetCurrentPlayer());

        }

        IEnumerator DestroyUICoroutine(float waitForSeconds, GameObject g)
        {

            if (waitForSeconds > 0)
                yield return new WaitForSeconds(waitForSeconds);

            UnityEngine.Object.Destroy(g);

        }

        public RoR2.CharacterMaster GetCurrentPlayer()
        {

            if (this.currentPlayer == null)
                this.currentPlayer = RoR2.PlayerCharacterMasterController.instances[0].master;
            return this.currentPlayer;

        }

        private List<RoR2.PickupIndex> GetAvailablePickups()
        {

            float difference = (float)upperLevel.Value - (float)lowerLevel.Value;
            float chanceScaling = ((float)levelsSpent - (float)lowerLevel.Value) / (float)upperLevel.Value;
            if (levelsSpent <= lowerLevel.Value) chanceScaling = 0;
            if (chanceScaling > 1) chanceScaling = 1;

            float totalMinimum = lowerLevelTier1Weight.Value + lowerLevelTier2Weight.Value + lowerLevelTier3Weight.Value + lowerLevelLunarWeight.Value + lowerLevelEquipmentWeight.Value;
            float totalMaximum = upperLevelTier1Weight.Value + upperLevelTier2Weight.Value + upperLevelTier3Weight.Value + upperLevelLunarWeight.Value + upperLevelEquipmentWeight.Value;
            float totalDifference = (float)totalMaximum - (float)totalMinimum;
            float currentTotal = totalMinimum + (chanceScaling * totalDifference);

            List <RoR2.PickupIndex> RollPickupList()
            {

                float roll = UnityEngine.Random.Range(0, currentTotal);
                List<RoR2.PickupIndex> dropList = new List<RoR2.PickupIndex>();
                float checkLevel = 0;
                checkLevel += lowerLevelTier1Weight.Value + (chanceScaling * (upperLevelTier1Weight.Value - lowerLevelTier1Weight.Value));
                if (roll <= checkLevel)
                {
                    dropList.AddRange(RoR2.Run.instance.availableTier1DropList);
                    return dropList;
                }
                checkLevel += lowerLevelTier2Weight.Value + (chanceScaling * (upperLevelTier2Weight.Value - lowerLevelTier2Weight.Value));
                if (roll <= checkLevel)
                {
                    dropList.AddRange(RoR2.Run.instance.availableTier2DropList);
                    return dropList;
                }
                checkLevel += lowerLevelTier3Weight.Value + (chanceScaling * (upperLevelTier3Weight.Value - lowerLevelTier3Weight.Value));
                if (roll <= checkLevel)
                {
                    dropList.AddRange(RoR2.Run.instance.availableTier3DropList);
                    return dropList;
                }
                checkLevel += lowerLevelLunarWeight.Value + (chanceScaling * (upperLevelLunarWeight.Value - lowerLevelLunarWeight.Value));
                if (roll <= checkLevel)
                {
                    dropList.AddRange(RoR2.Run.instance.availableLunarDropList);
                    return dropList;
                }
                checkLevel += lowerLevelEquipmentWeight.Value + (chanceScaling * (upperLevelEquipmentWeight.Value - lowerLevelEquipmentWeight.Value));
                if (roll <= checkLevel)
                {
                    dropList.AddRange(RoR2.Run.instance.availableEquipmentDropList);
                    return dropList;
                }
                else
                    return dropList;

            }

            RoR2.PickupIndex RandomFromList(List<RoR2.PickupIndex> list)
            {

                int selection = Mathf.RoundToInt((UnityEngine.Random.Range(0, list.Count - 1)));
                if (selection < 0) selection = 0;
                RoR2.PickupIndex selectedPickup = list[selection];

                return selectedPickup;

            }

            List<RoR2.PickupIndex> selectedPickups = new List<RoR2.PickupIndex>();
            while (selectedPickups.Count < 3)
            {
                List<RoR2.PickupIndex> tierList = RollPickupList();
                RoR2.PickupIndex attemptedPickup = RandomFromList(tierList);

                for (int i = 0; i <= selectedPickups.Count - 1; i++)
                {
                    if (attemptedPickup.itemIndex == selectedPickups[i].itemIndex)
                        attemptedPickup = RandomFromList(tierList);
                }

                selectedPickups.Add(attemptedPickup);
            }

            return selectedPickups;

        }

        public void ShowItemPicker(List<RoR2.PickupIndex> availablePickups, RoR2.CharacterMaster master)
        {

            showUI = true;

            var itemInventoryDisplay = GameObject.Find("ItemInventoryDisplay");

            float uiWidth = 220f;
            Logger.Log(LogLevel.Info, "Run started");
            var g = new GameObject();
            g.name = "LevelUpProgressionUI";
            g.layer = 5; // UI
            g.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            g.GetComponent<Canvas>().sortingOrder = -1; // Required or the UI will render over pause and tooltips.
            // g.AddComponent<CanvaschanceScaling>().scaleFactor = 10.0f;
            // g.GetComponent<CanvaschanceScaling>().dynamicPixelsPerUnit = 10f;
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

            if (startMessage)
            {
                header.GetComponent<HGTextMeshProUGUI>().text = "GOOD LUCK\n" + "Choose a starting perk.";
                startMessage = false;
            }
            else
                header.GetComponent<HGTextMeshProUGUI>().text = "LEVEL UP\n" + "Choose a perk for level " + (levelsSpent + 1) + ".";

            header.GetComponent<HGTextMeshProUGUI>().color = Color.white;
            header.GetComponent<HGTextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            header.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 1f);
            header.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);
            header.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
            header.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 40);

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

            foreach (RoR2.PickupIndex index in availablePickups)
            {
                var item = Instantiate<GameObject>(itemIconPrefab, itemCtr.transform).GetComponent<ItemIcon>();

                if (index.equipmentIndex == EquipmentIndex.None)
                {

                    item.SetItemIndex(index.itemIndex, 1);
                    var temp = index.value;

                    item.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                    {

                        //RoR2.Util.PlaySound("Item", currentPlayer.gameObject);
                        RoR2.Chat.AddMessage(currentPlayer.GetBody().GetUserName() + " chose " + index.GetPickupNameToken() + ".");
                        Logger.LogInfo("Item picked: " + index);
                        UnityEngine.Object.Destroy(g);
                        master.inventory.GiveItem(index.itemIndex);
                        showUI = false;
                        button1 = null;
                        button2 = null;
                        button3 = null;
                        levelsSpent += 1;
                        if (levelsSpent >= 0 && levelsSpent < levelsTotal)
                            ShowItemPicker(GetAvailablePickups(), master);
                    });
                }

                if (index.itemIndex == ItemIndex.None)
                {
                    var def = RoR2.EquipmentCatalog.GetEquipmentDef(index.equipmentIndex);
                    item.GetComponent<RawImage>().texture = def.pickupIconTexture;
                    item.stackText.enabled = false;
                    item.tooltipProvider.titleToken = def.nameToken;
                    item.tooltipProvider.titleColor = RoR2.ColorCatalog.GetColor(def.colorIndex);
                    item.tooltipProvider.bodyToken = def.pickupToken;
                    item.tooltipProvider.bodyColor = Color.gray;
                    var temp = index.value;
                    item.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                    {

                        if (master.inventory.GetEquipmentIndex() != null)
                            RoR2.PickupDropletController.CreatePickupDroplet(RoR2.PickupCatalog.FindPickupIndex(master.inventory.currentEquipmentIndex), master.GetBody().transform.position, new Vector3(0, 0, 1));

                        Logger.LogInfo("Equipment picked: " + index);
                        RoR2.Chat.AddMessage(currentPlayer.GetBody().GetUserName() + " chose " + def.nameToken + ".");
                        UnityEngine.Object.Destroy(g);
                        master.inventory.GiveEquipmentString(def.name);
                        showUI = false;
                        button1 = null;
                        button2 = null;
                        button3 = null;
                        levelsSpent += 1;
                        if (levelsSpent >= 0 && levelsSpent < levelsTotal)
                            ShowItemPicker(GetAvailablePickups(), master);
                    });
                }

                if (index == availablePickups[0])
                {
                    button1 = item.gameObject.GetComponent<Button>();
                }
                else if (index == availablePickups[1])
                {
                    button2 = item.gameObject.GetComponent<Button>();
                }
                else if (index == availablePickups[2])
                {
                    button3 = item.gameObject.GetComponent<Button>();
                }

            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(itemCtr.GetComponent<RectTransform>());
            ctr.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, itemCtr.GetComponent<RectTransform>().sizeDelta.y + 45f);

            //////////////////////////////////////////////////////////////////////////////////////////////
            ///Teleport out code
            //////////////////////////////////////////////////////////////////////////////////////////////

            
            /* Also activates on teleport in...
            On.RoR2.TeleportOutController.AddTPOutEffect += delegate (On.RoR2.TeleportOutController.orig_AddTPOutEffect orig, RoR2.CharacterModel characterModel, float beginAlpha, float endAlpha, float duration)
            {

                orig.Invoke(characterModel, beginAlpha, endAlpha, duration);

                StartCoroutine(DestroyUICoroutine(2, g));

                }
            };
            */

        }
    }
}