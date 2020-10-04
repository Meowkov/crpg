﻿using System;
using System.IO;
using System.Threading.Tasks;
using Crpg.GameMod.Api;
using Crpg.GameMod.Api.Models;
using Crpg.GameMod.Common;
using Crpg.GameMod.Helpers;
using Newtonsoft.Json;
using Steamworks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using Module = TaleWorlds.MountAndBlade.Module;

namespace Crpg.GameMod.DefendTheVirgin
{
    public class DefendTheVirginGameManager : MBGameManager
    {
        private static readonly Random Rng = new Random();

        private readonly ICrpgClient _crpgClient = new CrpgHttpClient("eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJuYW1laWQiOjAsInJvbGUiOiJnYW1lIiwibmJmIjoxNjAxMDQ4NzEyLCJleHAiOiIxNjAyMDAwMDAwIiwiaWF0IjoxNjAxMDQ4NzEyfQ.8GuUTu3Gs5-JH3_oHCuFWuNxF6ChjWK4P4vfs_2KFuE");

        private Task<CrpgUser>? _getUserTask;
        private WaveGroup[][]? _waves;

        protected override void DoLoadingForGameManager(
            GameManagerLoadingSteps gameManagerLoadingStep,
            out GameManagerLoadingSteps nextStep)
        {
            nextStep = GameManagerLoadingSteps.None;
            switch (gameManagerLoadingStep)
            {
                case GameManagerLoadingSteps.PreInitializeZerothStep:
                    LoadModuleData(false);
                    _getUserTask = GetUserAsync();
                    _waves = LoadWaves();
                    MBGlobals.InitializeReferences();
                    Game.CreateGame(new DefendTheVirginGame(), this).DoLoading();
                    nextStep = GameManagerLoadingSteps.FirstInitializeFirstStep;
                    break;
                case GameManagerLoadingSteps.FirstInitializeFirstStep:
                    bool flag = true;
                    foreach (MBSubModuleBase subModule in Module.CurrentModule.SubModules)
                    {
                        flag = flag && subModule.DoLoading(Game.Current);
                    }

                    nextStep = flag
                        ? GameManagerLoadingSteps.WaitSecondStep
                        : GameManagerLoadingSteps.FirstInitializeFirstStep;
                    break;
                case GameManagerLoadingSteps.WaitSecondStep:
                    StartNewGame();
                    nextStep = GameManagerLoadingSteps.SecondInitializeThirdState;
                    break;
                case GameManagerLoadingSteps.SecondInitializeThirdState:
                    nextStep = Game.Current.DoLoading()
                        ? GameManagerLoadingSteps.PostInitializeFourthState
                        : GameManagerLoadingSteps.SecondInitializeThirdState;
                    break;
                case GameManagerLoadingSteps.PostInitializeFourthState:
                    nextStep = _getUserTask!.IsCompleted
                        ? GameManagerLoadingSteps.FinishLoadingFifthStep
                        : GameManagerLoadingSteps.PostInitializeFourthState;
                    break;
                case GameManagerLoadingSteps.FinishLoadingFifthStep:
                    nextStep = GameManagerLoadingSteps.None;
                    break;
            }
        }

        public override void OnLoadFinished()
        {
            base.OnLoadFinished();

            string scene = GetRandomVillageScene();
            AtmosphereInfo atmosphereInfoForMission = GetRandomAtmosphere();

            InformationManager.DisplayMessage(new InformationMessage($"Map is {scene}."));
            InformationManager.DisplayMessage(new InformationMessage("Visit c-rpg.eu to upgrade your character."));

            var waveController = new WaveController(_waves!.Length);
            var waveSpawnLogic = new WaveSpawnLogic(waveController, _waves!, CreateCharacter(_getUserTask!.Result.Character));
            var crpgLogic = new CrpgLogic(waveController, _crpgClient, _waves!, _getUserTask!.Result);

            MissionState.OpenNew("DefendTheVirgin", new MissionInitializerRecord(scene)
            {
                DoNotUseLoadingScreen = false,
                PlayingInCampaignMode = false,
                AtmosphereOnCampaign = atmosphereInfoForMission,
                SceneLevels = string.Empty,
                TimeOfDay = 6f,
            }, missionController => new MissionBehaviour[]
            {
                new MissionCombatantsLogic(),
                waveController,
                waveSpawnLogic,
                crpgLogic,
                new AgentBattleAILogic(),
                new MissionHardBorderPlacer(),
                new MissionBoundaryPlacer(),
                new MissionBoundaryCrossingHandler(),
                new AgentFadeOutLogic(),
            });
        }

        private async Task<CrpgUser> GetUserAsync()
        {
            var steamId = SteamUser.GetSteamID();
            string name = SteamFriends.GetFriendPersonaName(steamId);

            var res = await _crpgClient.Update(new CrpgGameUpdateRequest
            {
                GameUserUpdates = new[]
                {
                    new CrpgGameUserUpdate { PlatformUserId = steamId.ToString(), CharacterName = name },
                },
            });

            return res.Users[0];
        }

        private static WaveGroup[][] LoadWaves()
        {
            string path = BasePath.Name + "Modules/cRPG/ModuleData/waves.json";
            var waves = JsonConvert.DeserializeObject<WaveGroup[][]>(File.ReadAllText(path));
            foreach (var wave in waves)
            {
                foreach (var group in wave)
                {
                    // In case count was not set
                    group.Count = Math.Max(group.Count, 1);
                }
            }

            return waves;
        }

        private static string GetRandomVillageScene()
        {
            string[] scenes =
            {
                "aserai_village_e", // 127m
                // "aserai_village_i", // 200m
                // "aserai_village_j", // 257m
                // "aserai_village_l", // 296m
                // "battania_village_h", // 252m
                "battania_village_i", // 120m
                "battania_village_j", // 120m
                // "battania_village_k", // 218m
                // "battania_village_l", // 340m
                // "empire_village_002", // 170m
                "empire_village_003", // 152m
                // "empire_village_004", // 300m
                "empire_village_007", // 150m
                // "empire_village_008", // CRASH WITH NO EXCEPTION
                // "empire_village_p", // CRASH WITH NO EXCEPTION
                // "empire_village_x", // CRASH WITH NO EXCEPTION
                "khuzait_village_a", // 169m
                // "khuzait_village_i", // 229m
                // "khuzait_village_j", // 214m
                // "khuzait_village_k", // 250m
                "khuzait_village_l", // 103m
                "sturgia_village_e", // 153m
                // "sturgia_village_f", // 160m
                "sturgia_village_g", // 100m
                // "sturgia_village_h", // 279m
                // "sturgia_village_j", // 291m
                // "sturgia_village_l", // 120m
                // "vlandia_village_g", // 270m
                // "vlandia_village_i", // 292m
                // "vlandia_village_k", // 300m
                // "vlandia_village_l", // 280m
                // "vlandia_village_m", // 342m
                // "vlandia_village_n", // 278m
            };

            return scenes[Rng.Next(0, scenes.Length)];
        }

        private static AtmosphereInfo GetRandomAtmosphere()
        {
            string[] atmospheres =
            {
                "TOD_01_00_SemiCloudy",
                "TOD_02_00_SemiCloudy",
                "TOD_03_00_SemiCloudy",
                "TOD_04_00_SemiCloudy",
                "TOD_05_00_SemiCloudy",
                "TOD_06_00_SemiCloudy",
                "TOD_07_00_SemiCloudy",
                "TOD_08_00_SemiCloudy",
                "TOD_09_00_SemiCloudy",
                "TOD_10_00_SemiCloudy",
                "TOD_11_00_SemiCloudy",
                "TOD_12_00_SemiCloudy"
            };
            string atmosphere = atmospheres[Rng.Next(0, atmospheres.Length)];

            string[] seasons =
            {
                "spring",
                "summer",
                "fall",
                "winter",
            };
            int seasonId = Rng.Next(0, seasons.Length);

            return new AtmosphereInfo
            {
                AtmosphereName = atmosphere,
                TimeInfo = new TimeInformation { Season = seasonId },
            };
        }

        private static BasicCharacterObject CreateCharacter(CrpgCharacter crpgCharacter)
        {
            var skills = new CharacterSkills();
            skills.SetPropertyValue(CrpgSkills.Strength, crpgCharacter.Statistics.Attributes.Strength);
            skills.SetPropertyValue(CrpgSkills.Agility, crpgCharacter.Statistics.Attributes.Agility);

            skills.SetPropertyValue(CrpgSkills.IronFlesh, crpgCharacter.Statistics.Skills.IronFlesh);
            skills.SetPropertyValue(CrpgSkills.PowerStrike, crpgCharacter.Statistics.Skills.PowerStrike);
            skills.SetPropertyValue(CrpgSkills.PowerDraw, crpgCharacter.Statistics.Skills.PowerDraw);
            skills.SetPropertyValue(CrpgSkills.PowerThrow, crpgCharacter.Statistics.Skills.PowerThrow);
            skills.SetPropertyValue(DefaultSkills.Athletics, crpgCharacter.Statistics.Skills.Athletics * 20 + crpgCharacter.Statistics.Attributes.Agility);
            skills.SetPropertyValue(DefaultSkills.Riding, crpgCharacter.Statistics.Skills.Riding * 20);
            skills.SetPropertyValue(CrpgSkills.WeaponMaster, crpgCharacter.Statistics.Skills.WeaponMaster);
            skills.SetPropertyValue(CrpgSkills.HorseArchery, crpgCharacter.Statistics.Skills.HorseArchery);

            skills.SetPropertyValue(DefaultSkills.OneHanded, (int)(crpgCharacter.Statistics.WeaponProficiencies.OneHanded * 1.5));
            skills.SetPropertyValue(DefaultSkills.TwoHanded, (int)(crpgCharacter.Statistics.WeaponProficiencies.TwoHanded * 1.5));
            skills.SetPropertyValue(DefaultSkills.Polearm, (int)(crpgCharacter.Statistics.WeaponProficiencies.Polearm * 1.5));
            skills.SetPropertyValue(DefaultSkills.Bow, (int)(crpgCharacter.Statistics.WeaponProficiencies.Bow * 1.5));
            skills.SetPropertyValue(DefaultSkills.Crossbow, (int)(crpgCharacter.Statistics.WeaponProficiencies.Crossbow * 1.5));
            skills.SetPropertyValue(DefaultSkills.Throwing, (int)(crpgCharacter.Statistics.WeaponProficiencies.Throwing * 1.5));

            var equipment = new Equipment();
            AddEquipment(equipment, EquipmentIndex.Head, crpgCharacter.Items.HeadItem?.MbId, skills);
            AddEquipment(equipment, EquipmentIndex.Cape, crpgCharacter.Items.CapeItem?.MbId, skills);
            AddEquipment(equipment, EquipmentIndex.Body, crpgCharacter.Items.BodyItem?.MbId, skills);
            AddEquipment(equipment, EquipmentIndex.Gloves, crpgCharacter.Items.HandItem?.MbId, skills);
            AddEquipment(equipment, EquipmentIndex.Leg, crpgCharacter.Items.LegItem?.MbId, skills);
            AddEquipment(equipment, EquipmentIndex.HorseHarness, crpgCharacter.Items.HorseHarnessItem?.MbId, skills);
            AddEquipment(equipment, EquipmentIndex.Horse, crpgCharacter.Items.HorseItem?.MbId, skills);
            AddEquipment(equipment, EquipmentIndex.Weapon0, crpgCharacter.Items.Weapon1Item?.MbId, skills);
            AddEquipment(equipment, EquipmentIndex.Weapon1, crpgCharacter.Items.Weapon2Item?.MbId, skills);
            AddEquipment(equipment, EquipmentIndex.Weapon2, crpgCharacter.Items.Weapon3Item?.MbId, skills);
            AddEquipment(equipment, EquipmentIndex.Weapon3, crpgCharacter.Items.Weapon4Item?.MbId, skills);

            var characterTemplate = MBObjectManager.Instance.GetObject<BasicCharacterObject>("villager_empire");
            return new CrpgCharacterObject(new TextObject(crpgCharacter.Name), characterTemplate, skills, equipment);
        }

        private static void AddEquipment(Equipment equipments, EquipmentIndex idx, string? itemId, CharacterSkills skills)
        {
            var itemObject = itemId != null ? MBObjectManager.Instance.GetObject<ItemObject>(itemId) : null;
            var itemModifier = itemObject != null ? GetItemModifier(itemObject.Type, skills) : null;
            var equipmentElement = new EquipmentElement(itemObject, itemModifier);
            equipments.AddEquipmentToSlotWithoutAgent(idx, equipmentElement);
        }

        // TODO: it seems like ItemModifier.Damage doesn't have any effect
        // TODO: use ItemModifier for looming
        private static ItemModifier GetItemModifier(ItemObject.ItemTypeEnum itemType, CharacterSkills skills)
        {
            var itemModifier = new ItemModifier();

            // Not sure if needed
            ReflectionHelper.SetProperty(itemModifier, nameof(ItemModifier.IsInitialized), true);
            ReflectionHelper.SetProperty(itemModifier, "IsReady", true);

            // Set default values for the properties which have a default value different than their type default (e.g. different than 0f for floats)
            ReflectionHelper.SetProperty(itemModifier, nameof(ItemModifier.WeightMultiplier), 1f);
            ReflectionHelper.SetProperty(itemModifier, nameof(ItemModifier.PriceMultiplier), 1f);

            int value;
            switch (itemType)
            {
                case ItemObject.ItemTypeEnum.OneHandedWeapon:
                case ItemObject.ItemTypeEnum.TwoHandedWeapon:
                case ItemObject.ItemTypeEnum.Polearm:
                    value = skills.GetPropertyValue(CrpgSkills.PowerStrike) * 8 + 500;
                    ReflectionHelper.SetProperty(itemModifier, nameof(ItemModifier.Damage), value);
                    break;
                case ItemObject.ItemTypeEnum.Shield:
                    // TODO: Shield skill
                    break;
                case ItemObject.ItemTypeEnum.Bow:
                    value = skills.GetPropertyValue(CrpgSkills.PowerDraw) * 14;
                    ReflectionHelper.SetProperty(itemModifier, nameof(ItemModifier.Damage), value);
                    break;
                case ItemObject.ItemTypeEnum.Thrown:
                    value = skills.GetPropertyValue(CrpgSkills.PowerDraw) * 10;
                    ReflectionHelper.SetProperty(itemModifier, nameof(ItemModifier.Damage), value);
                    break;
                case ItemObject.ItemTypeEnum.HeadArmor:
                case ItemObject.ItemTypeEnum.Cape:
                case ItemObject.ItemTypeEnum.BodyArmor:
                case ItemObject.ItemTypeEnum.ChestArmor:
                case ItemObject.ItemTypeEnum.LegArmor:
                case ItemObject.ItemTypeEnum.HandArmor:
                case ItemObject.ItemTypeEnum.Crossbow:
                case ItemObject.ItemTypeEnum.HorseHarness:
                case ItemObject.ItemTypeEnum.Horse:
                case ItemObject.ItemTypeEnum.Arrows:
                case ItemObject.ItemTypeEnum.Bolts:
                case ItemObject.ItemTypeEnum.Goods:
                case ItemObject.ItemTypeEnum.Pistol:
                case ItemObject.ItemTypeEnum.Musket:
                case ItemObject.ItemTypeEnum.Bullets:
                case ItemObject.ItemTypeEnum.Animal:
                case ItemObject.ItemTypeEnum.Book:
                case ItemObject.ItemTypeEnum.Banner:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null);
            }

            return itemModifier;
        }
    }
}