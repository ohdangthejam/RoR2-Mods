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

        private GameObject currentUIObject;
        private bool showUI = false;
        private static int levelsSpent = 0;
        private int levelsTotal = 0;
        private bool startMessage = true;
        private RoR2.CharacterMaster currentPlayer;
        private List<RoR2.PickupIndex> currentPerkSelection;

        public UnityEvent onHotKey1Handler = new UnityEvent();
        public UnityEvent onHotKey2Handler = new UnityEvent();
        public UnityEvent onHotKey3Handler = new UnityEvent();
        public UnityAction HotKey1Action = null;
        public UnityAction HotKey2Action = null;
        public UnityAction HotKey3Action = null;

        public static ConfigWrapper<int> choiceScaler { get; set; }
        public void initConfig()
        {

            LevelUpProgressionPlugin.choiceScaler = base.Config.Wrap<int>("CustomCommandPlugin choice scaler", "Choice Scaler", "Determine how many items to choose from upon level up. Default: 3.", 3);

        }

        public void Start()
        {

            var miniRpc = MiniRpc.CreateInstance(ModGuid);
            RoR2.Chat.AddMessage("Level up progression activated.");
            this.initConfig();

            HotKey1Action = new UnityAction(delegate ()
            {
                RoR2.Chat.AddMessage("HotKey1 Confirmed");
                onHotKey1Handler.RemoveListener(HotKey1Action);
                ConfirmSelectionHotkey(1);
            });

            HotKey2Action = new UnityAction(delegate ()
            {
                RoR2.Chat.AddMessage("HotKey2 Confirmed");
                onHotKey2Handler.RemoveListener(HotKey2Action);
                ConfirmSelectionHotkey(2);
            });

            HotKey3Action = new UnityAction(delegate ()
            {
                RoR2.Chat.AddMessage("HotKey3 Confirmed");
                onHotKey3Handler.RemoveListener(HotKey3Action);
                ConfirmSelectionHotkey(3);
            });
        }

        void ConfirmSelectionHotkey( int hotkey )
        {

            if (currentPerkSelection.Count < 3)
                return;

            RoR2.PickupIndex index = currentPerkSelection[0];
            if (hotkey == 1)
                index = currentPerkSelection[0];
            if (hotkey == 2)
                index = currentPerkSelection[1];
            if (hotkey == 3)
                index = currentPerkSelection[2];

            Logger.LogInfo("Item picked: " + index);
            UnityEngine.Object.Destroy(currentUIObject);
            GetCurrentPlayer().inventory.GiveItem(index.itemIndex);

            showUI = false;
            levelsSpent += 1;
            if (levelsSpent >= 0 && levelsSpent < levelsTotal)
                ShowItemPicker(GetAvailablePickups(), GetCurrentPlayer());
        }


        public void Update()
        {

            if (showUI)
            {

                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    RoR2.Chat.AddMessage("Hotkey 1 pressed.");
                    onHotKey1Handler.Invoke();
                }
                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    RoR2.Chat.AddMessage("Hotkey 2 pressed.");
                    onHotKey2Handler.Invoke();
                }
                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    RoR2.Chat.AddMessage("Hotkey 3 pressed.");
                    onHotKey3Handler.Invoke();
                }
                
            }
            if (Input.GetKeyDown(KeyCode.Alpha9))
                RoR2.PlayerCharacterMasterController.instances[0].master.GiveExperience(10000);
        }

        public void Awake()
        {
            On.RoR2.Run.Start += delegate (On.RoR2.Run.orig_Start orig, RoR2.Run self)
            {
                orig.Invoke(self);
                levelsSpent = 0;
                this.levelsTotal = 0;
                this.startMessage = true;
            };

            On.RoR2.TeleportOutController.AddTPOutEffect += delegate (On.RoR2.TeleportOutController.orig_AddTPOutEffect orig, RoR2.CharacterModel characterModel, float beginAlpha, float endAlpha, float duration)
            {
                if (characterModel.body.master.isLocalPlayer)
                {
                    orig.Invoke(characterModel, beginAlpha, endAlpha, duration);

                    var levelUpUI = GameObject.Find("LevelUpProgressionUI");
                    if (levelUpUI != null)
                        UnityEngine.Object.Destroy(levelUpUI);

                }
            };

            //On Spawn
            On.RoR2.Run.BeginStage += delegate (On.RoR2.Run.orig_BeginStage orig, RoR2.Run self)
            {

                orig.Invoke(self);

                showUI = false;

                RoR2.Chat.AddMessage("1. BeginStage");
                if ((levelsTotal > 0 && levelsSpent < levelsTotal) || levelsTotal <= 0)
                {
                    RoR2.Chat.AddMessage("2. Points unspent. levelsSpent: " + levelsSpent + " levelsTotal: " + levelsTotal);
                    if (showUI == false)
                        StartCoroutine(ShowItemPickerCoroutine(3));
                }
                else
                    RoR2.Chat.AddMessage("2. No remaining points. levelsSpent: " + levelsSpent + " levelsTotal: " + levelsTotal);
            };

            // On Level Up
            On.RoR2.GlobalEventManager.OnTeamLevelUp += delegate (On.RoR2.GlobalEventManager.orig_OnTeamLevelUp orig, TeamIndex self)
            {
                orig.Invoke(self);

                levelsTotal = (int)RoR2.TeamManager.instance.GetTeamLevel(RoR2.PlayerCharacterMasterController.instances[0].master.teamIndex);

                //RoR2.Chat.AddMessage("test spent: " + levelsSpent);
                if (levelsTotal > 0 && levelsSpent < levelsTotal) // 
                {
                    //RoR2.Chat.AddMessage("Level up: " + levelsTotal);
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
            RoR2.Chat.AddMessage("3. Show item picker");
        }

        public RoR2.CharacterMaster GetCurrentPlayer()
        {
            if (this.currentPlayer == null)
            {
                this.currentPlayer = RoR2.PlayerCharacterMasterController.instances[0].master;
            }
            return this.currentPlayer;
        }

        private List<RoR2.PickupIndex> GetAvailablePickups()
        {
            var availablePickups = new List<RoR2.PickupIndex>();
            var selectedPickups = new List<RoR2.PickupIndex>();
            var tier = Mathf.RoundToInt(UnityEngine.Random.Range(0, 100));
            if (tier <= 50)
                selectedPickups.AddRange(RoR2.Run.instance.availableTier1DropList);
            else if (tier <= 85)
                selectedPickups.AddRange(RoR2.Run.instance.availableTier2DropList);
            else if (tier <= 90)
                selectedPickups.AddRange(RoR2.Run.instance.availableTier3DropList);
            else if (tier <= 95)
                selectedPickups.AddRange(RoR2.Run.instance.availableLunarDropList);
            else if (tier <= 100)
                selectedPickups.AddRange(RoR2.Run.instance.availableEquipmentDropList);

            if (selectedPickups.Count > 0)
            {
                for (var i = 0; i < choiceScaler.Value; i++)
                {
                    int currentPickup = Mathf.RoundToInt((UnityEngine.Random.Range(0, selectedPickups.Count - 1)));
                    availablePickups.Add(selectedPickups[currentPickup]);
                }
            }
            currentPerkSelection.Clear();
            currentPerkSelection.AddRange( availablePickups );
            return availablePickups;
        }



        public void ShowItemPicker(List<RoR2.PickupIndex> availablePickups, RoR2.CharacterMaster master)
        {

            showUI = true;

            var itemInventoryDisplay = GameObject.Find("ItemInventoryDisplay");

            float uiWidth = 220f;
            Logger.Log(LogLevel.Info, "Run started");
            var g = new GameObject();
            currentUIObject = g;
            g.name = "LevelUpProgressionUI";
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
                if (index.itemIndex == ItemIndex.None)
                    continue;
                var item = Instantiate<GameObject>(itemIconPrefab, itemCtr.transform).GetComponent<ItemIcon>();
                item.SetItemIndex(index.itemIndex, 1);
                item.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    ConfirmSelection();
                });

                if (index == availablePickups[0])
                {
                    RoR2.Chat.AddMessage("Listener Added: " + availablePickups[0].GetPickupNameToken() + " (1).");
                    onHotKey1Handler.AddListener(HotKey1Action);
                }

                if (index == availablePickups[1])
                {
                    RoR2.Chat.AddMessage("Listener Added: " + availablePickups[1].GetPickupNameToken() + " (2).");
                    onHotKey2Handler.AddListener(HotKey2Action);
                }

                if (index == availablePickups[2])
                {
                    RoR2.Chat.AddMessage("Listener Added: " + availablePickups[2].GetPickupNameToken() + " (3).");
                    onHotKey3Handler.AddListener(HotKey3Action);
                }

                void ConfirmSelection()
                {
                    Logger.LogInfo("Item picked: " + index);
                    UnityEngine.Object.Destroy(g);
                    master.inventory.GiveItem(index.itemIndex);

                    showUI = false;
                    levelsSpent += 1;
                    if (levelsSpent >= 0 && levelsSpent < levelsTotal)
                        ShowItemPicker(GetAvailablePickups(), master);
                }
            }
            foreach (RoR2.PickupIndex index in availablePickups)
            {
                if (index.equipmentIndex == EquipmentIndex.None)
                    continue;
                var def = RoR2.EquipmentCatalog.GetEquipmentDef(index.equipmentIndex);
                var item = Instantiate<GameObject>(itemIconPrefab, itemCtr.transform).GetComponent<ItemIcon>();
                item.GetComponent<RawImage>().texture = def.pickupIconTexture;
                item.stackText.enabled = false;
                item.tooltipProvider.titleToken = def.nameToken;
                item.tooltipProvider.titleColor = RoR2.ColorCatalog.GetColor(def.colorIndex);
                item.tooltipProvider.bodyToken = def.pickupToken;
                item.tooltipProvider.bodyColor = Color.gray;
                item.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    ConfirmSelection();
                });

                if (index == availablePickups[0])
                {
                    RoR2.Chat.AddMessage("Listener Added: " + availablePickups[0].GetPickupNameToken() + " (1).");
                    onHotKey1Handler.AddListener(HotKey1Action);
                }

                if (index == availablePickups[1])
                {
                    RoR2.Chat.AddMessage("Listener Added: " + availablePickups[1].GetPickupNameToken() + " (2).");
                    onHotKey2Handler.AddListener(HotKey2Action);
                }

                if (index == availablePickups[2])
                {
                    RoR2.Chat.AddMessage("Listener Added: " + availablePickups[2].GetPickupNameToken() + " (3).");
                    onHotKey3Handler.AddListener(HotKey3Action);
                }
                    

                void ConfirmSelection()
                {
                    Logger.LogInfo("Equipment picked: " + index);
                    UnityEngine.Object.Destroy(g);
                    master.inventory.GiveEquipmentString(def.name);

                    showUI = false;
                    levelsSpent += 1;
                    if (levelsSpent >= 0 && levelsSpent < levelsTotal)
                        ShowItemPicker(GetAvailablePickups(), master);
                }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(itemCtr.GetComponent<RectTransform>());
            ctr.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, itemCtr.GetComponent<RectTransform>().sizeDelta.y + 45f);

        }

    }
}