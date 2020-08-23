using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using On.RoR2;
using RoR2;
using UnityEngine;

namespace ArtifactOfSatisfaction
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.OhDangTheJam.ArtifactOfSatisfaction", "ArtifactOfSatisfaction", "1.0.0")]
    public class ArtifactOfSatisfaction : BaseUnityPlugin
    {
        public static ConfigWrapper<int> equipmentTimeLimit { get; set; }
        public static ConfigWrapper<int> tierTwoTimeLimit { get; set; }
        public static ConfigWrapper<int> tierThreeTimeLimit { get; set; }

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
                        int num = 0;
                        bool flag = time >= (float)ArtifactOfSatisfaction.tierThreeTimeLimit.Value * 60f;
                        if (flag)
                        {
                            num = UnityEngine.Random.Range(0, 4);
                        }
                        else
                        {
                            bool flag2 = time >= (float)ArtifactOfSatisfaction.tierTwoTimeLimit.Value * 60f;
                            if (flag2)
                            {
                                num = UnityEngine.Random.Range(0, 3);
                            }
                            else
                            {
                                bool flag3 = time >= (float)ArtifactOfSatisfaction.equipmentTimeLimit.Value * 60f;
                                if (flag3)
                                {
                                    num = UnityEngine.Random.Range(0, 2);
                                }
                            }
                        }
                        switch (num)
                        {
                            case 0:
                                this.spawnTier1Item(1f, master.GetBodyObject().transform);
                                break;
                            case 1:
                                this.spawnEquipment(0f, master.GetBodyObject().transform);
                                break;
                            case 2:
                                this.spawnTier2Item(0f, master.GetBodyObject().transform);
                                break;
                            case 3:
                                this.spawnTier3Item(0f, master.GetBodyObject().transform);
                                break;
                            default:
                                this.spawnTier1Item(0f, master.GetBodyObject().transform);
                                break;
                        }
                    }
                }
            };
        }

        public void initConfig()
        {
            ArtifactOfSatisfaction.tierThreeTimeLimit = base.Config.Wrap<int>("ArtifactOfSatisfaction time limits", "Tier Three Time Limit", "Upto and including Tier three items will start appearing after this time limit. IN MINUTES. (tier one, tier two, tier three, equipment)", 30);
            ArtifactOfSatisfaction.tierTwoTimeLimit = base.Config.Wrap<int>("ArtifactOfSatisfaction time limits", "Tier Two Time Limit", "Upto and including Tier two items will start appearing after this time limit. IN MINUTES. (tier one, tier two, equipment)", 10);
            ArtifactOfSatisfaction.equipmentTimeLimit = base.Config.Wrap<int>("ArtifactOfSatisfaction time limits", "Equipment Time Limit", "Upto and including equipment will start appearing after this time limit. IN MINUTES. (tier one, equipment)", 5);
        }

        public void spawnTier1Item(float offSet, Transform transform)
        {
            List<RoR2.PickupIndex> availableTier1DropList = RoR2.Run.instance.availableTier1DropList;
            int index = RoR2.Run.instance.treasureRng.RangeInt(0, availableTier1DropList.Count);
            RoR2.PickupDropletController.CreatePickupDroplet(availableTier1DropList[index], transform.position, transform.forward * (20f + offSet));
        }

        public void spawnTier2Item(float offSet, Transform transform)
        {
            List<RoR2.PickupIndex> availableTier2DropList = RoR2.Run.instance.availableTier2DropList;
            int index = RoR2.Run.instance.treasureRng.RangeInt(0, availableTier2DropList.Count);
            RoR2.PickupDropletController.CreatePickupDroplet(availableTier2DropList[index], transform.position, transform.forward * (20f + offSet));
        }

        public void spawnTier3Item(float offSet, Transform transform)
        {
            List<RoR2.PickupIndex> availableTier3DropList = RoR2.Run.instance.availableTier3DropList;
            int index = RoR2.Run.instance.treasureRng.RangeInt(0, availableTier3DropList.Count);
            RoR2.PickupDropletController.CreatePickupDroplet(availableTier3DropList[index], transform.position, transform.forward * (20f + offSet));
        }

        public void spawnLunarItem(float offSet, Transform transform)
        {
            List<RoR2.PickupIndex> availableLunarDropList = RoR2.Run.instance.availableLunarDropList;
            int index = RoR2.Run.instance.treasureRng.RangeInt(0, availableLunarDropList.Count);
            RoR2.PickupDropletController.CreatePickupDroplet(availableLunarDropList[index], transform.position, transform.forward * (20f + offSet));
        }

        public void spawnEquipment(float offSet, Transform transform)
        {
            List<RoR2.PickupIndex> availableEquipmentDropList = RoR2.Run.instance.availableEquipmentDropList;
            int index = RoR2.Run.instance.treasureRng.RangeInt(0, availableEquipmentDropList.Count);
            RoR2.PickupDropletController.CreatePickupDroplet(availableEquipmentDropList[index], transform.position, transform.forward * (20f + offSet));
        }

        public void Update()
        {
        }
    }
}
