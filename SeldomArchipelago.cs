using Mono.Cecil.Cil;
using MonoMod.Cil;
using MyExtensions;
using SeldomArchipelago.NPCs;
using SeldomArchipelago.Players;
using SeldomArchipelago.Systems;
using SeldomArchipelago.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Achievements;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;
using static SeldomArchipelago.Systems.ArchipelagoSystem;

namespace SeldomArchipelago
{
    // TODO Use a data-oriented approach to get rid of all this repetition
    public class SeldomArchipelago : Mod
    {
        public const string gameName = "Terraria";
        // We reuse some parts of Terraria's code for multiple purposes in this mod. For example,
        // when you kill a boss, we have to prevent that code from making permanent world changes
        // and instead send a location, but we reuse that same code when making permanent changes
        // after receiving a boss flag as an item, so we have to not prevent the code from making
        // such changes in that case. So, we use this flag to determine whether the code is run by
        // the game naturally (false) or run by us (true). Terraria is single-threaded, don't worry.
        public bool temp;

        public bool guaranteeBloodMoon;
        public bool guaranteeEclipse;

        public static Dictionary<string, int> englishLangToTypeDict = new Dictionary<string, int>();

        public static MethodInfo desertScourgeHeadOnKill = null;
        public static MethodInfo giantClamOnKill = null; // downedCLAM and downedCLAMHardMode
        public static MethodInfo cragmawMireOnKill = null;
        public static MethodInfo acidRainEventUpdateInvasion = null; // downedEoCAcidRain and downedAquaticScourgeAcidRain
        public static MethodInfo crabulonOnKill = null;
        public static MethodInfo hiveMindOnKill = null;
        public static MethodInfo perforatorHiveOnKill = null;
        public static MethodInfo slimeGodCoreOnKill = null;
        public static MethodInfo calamityGlobalNpcOnKill = null;
        public static MethodInfo aquaticScourgeHeadOnKill = null;
        public static MethodInfo maulerOnKill = null;
        public static MethodInfo brimstoneElementalOnKill = null;
        public static MethodInfo cryogenOnKill = null;
        public static MethodInfo calamitasCloneOnKill = null;
        public static MethodInfo greatSandSharkOnKill = null;
        public static MethodInfo leviathanRealOnKill = null;
        public static MethodInfo astrumAureusOnKill = null;
        public static MethodInfo plaguebringerGoliathOnKill = null;
        public static MethodInfo ravagerBodyOnKill = null;
        public static MethodInfo astrumDeusHeadOnKill = null;
        public static MethodInfo profanedGuardianCommanderOnKill = null;
        public static MethodInfo bumblefuckOnKill = null;
        public static MethodInfo providenceOnKill = null;
        public static MethodInfo stormWeaverHeadOnKill = null;
        public static MethodInfo ceaselessVoidOnKill = null;
        public static MethodInfo signusOnKill = null;
        public static MethodInfo polterghastOnKill = null;
        public static MethodInfo nuclearTerrorOnKill = null;
        public static MethodInfo oldDukeOnKill = null;
        public static MethodInfo devourerofGodsHeadOnKill = null;
        public static MethodInfo yharonOnKill = null;
        public static MethodInfo aresBodyOnKill = null;
        public static MethodInfo apolloOnKill = null;
        public static MethodInfo thanatosHeadOnKill = null;
        public static MethodInfo supremeCalamitasOnKill = null;
        public static MethodInfo calamityGlobalNpcSetNewBossJustDowned = null;

        FlagSystem GetFlags() => ModContent.GetInstance<ArchipelagoSystem>().Session.flagSystem;
        public override void Load()
        {
            int counter = 1;

            // TODO: Initialize this on connection instead and compare with SlotData to get rid of unnecessary items
            while (Lang.GetItemNameValue(counter) is string itemName && counter < 10000)
            {
                englishLangToTypeDict[itemName] = counter;
                counter++;
            }

            FlagSystem.InitializeBannerSet();

            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();

            // Begin cursed IL editing

            // Torch God reward Terraria.Player:13794
            IL_Player.TorchAttack += il =>
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(i => i.MatchLdsfld(typeof(Main).GetField(nameof(Main.netMode))));
                cursor.EmitDelegate(() => archipelagoSystem.QueueLocationClient("Torch God"));
                cursor.Emit(OpCodes.Ret);
            };

            // Allow Torch God even if you have `unlockedBiomeTorches`
            IL_Player.UpdateTorchLuck_ConsumeCountersAndCalculate += il =>
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(i => i.MatchLdfld(typeof(Player).GetField(nameof(Player.unlockedBiomeTorches))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_0);

                cursor.GotoNext(i => i.MatchLdcI4(ItemID.TorchGodsFavor));
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_0);
            };

            // General event clear locations
            IL_NPC.SetEventFlagCleared += il =>
            {
                var cursor = new ILCursor(il);

                var label = cursor.DefineLabel();
                cursor.MarkLabel(label);
                cursor.Emit(OpCodes.Pop);
                cursor.Index--;
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.Emit(OpCodes.Dup);
                cursor.Emit(OpCodes.Ldc_I4_M1);
                cursor.Emit(OpCodes.Beq, label);
                cursor.EmitDelegate((int id) =>
                {
                    var location = id switch
                    {
                        GameEventClearedID.DefeatedSlimeKing => "King Slime",
                        GameEventClearedID.DefeatedEyeOfCthulu => "Eye of Cthulhu",
                        GameEventClearedID.DefeatedEaterOfWorldsOrBrainOfChtulu => "Evil Boss",
                        GameEventClearedID.DefeatedGoblinArmy => "Goblin Army",
                        GameEventClearedID.DefeatedQueenBee => "Queen Bee",
                        GameEventClearedID.DefeatedSkeletron => "Skeletron",
                        GameEventClearedID.DefeatedDeerclops => "Deerclops",
                        GameEventClearedID.DefeatedWallOfFleshAndStartedHardmode => "Wall of Flesh",
                        GameEventClearedID.DefeatedPirates => "Pirate Invasion",
                        GameEventClearedID.DefeatedQueenSlime => "Queen Slime",
                        GameEventClearedID.DefeatedTheTwins => "The Twins",
                        GameEventClearedID.DefeatedDestroyer => "The Destroyer",
                        GameEventClearedID.DefeatedSkeletronPrime => "Skeletron Prime",
                        GameEventClearedID.DefeatedPlantera => "Plantera",
                        GameEventClearedID.DefeatedGolem => "Golem",
                        GameEventClearedID.DefeatedMartians => "Martian Madness",
                        GameEventClearedID.DefeatedFishron => "Duke Fishron",
                        GameEventClearedID.DefeatedHalloweenTree => "Mourning Wood",
                        GameEventClearedID.DefeatedHalloweenKing => "Pumpking",
                        GameEventClearedID.DefeatedChristmassTree => "Everscream",
                        GameEventClearedID.DefeatedSantank => "Santa-NK1",
                        GameEventClearedID.DefeatedIceQueen => "Ice Queen",
                        GameEventClearedID.DefeatedEmpressOfLight => "Empress of Light",
                        GameEventClearedID.DefeatedAncientCultist => "Lunatic Cultist",
                        GameEventClearedID.DefeatedMoonlord => "Moon Lord",
                        _ => null,
                    };
                    switch (id)
                    {
                        case GameEventClearedID.DefeatedSlimeKing: NPC.downedSlimeKing = true; break;
                        case GameEventClearedID.DefeatedEyeOfCthulu: NPC.downedBoss1 = true; break;
                        case GameEventClearedID.DefeatedEaterOfWorldsOrBrainOfChtulu: NPC.downedBoss2 = true; break;
                        case GameEventClearedID.DefeatedGoblinArmy: NPC.downedGoblins = true; break;
                        case GameEventClearedID.DefeatedQueenBee: NPC.downedQueenBee = true; break;
                        case GameEventClearedID.DefeatedSkeletron: break;
                        case GameEventClearedID.DefeatedDeerclops: NPC.downedDeerclops = true; break;
                        case GameEventClearedID.DefeatedWallOfFleshAndStartedHardmode: break;
                        case GameEventClearedID.DefeatedPirates: NPC.downedPirates = true; break;
                        case GameEventClearedID.DefeatedQueenSlime: NPC.downedQueenSlime = true; break;
                        case GameEventClearedID.DefeatedTheTwins: NPC.downedMechBoss2 = true; break;
                        case GameEventClearedID.DefeatedDestroyer: NPC.downedMechBoss1 = true; break;
                        case GameEventClearedID.DefeatedSkeletronPrime: NPC.downedMechBoss3 = true; break;
                        case GameEventClearedID.DefeatedPlantera:
                        case GameEventClearedID.DefeatedGolem: break;
                        case GameEventClearedID.DefeatedMartians: NPC.downedMartians = true; break;
                        case GameEventClearedID.DefeatedFishron: NPC.downedFishron = true; break;
                    };

                    if (location != null) archipelagoSystem.QueueLocation(location);
                });
                cursor.Emit(OpCodes.Ret);
            };
            // Daytime Events
            Terraria.IL_Main.UpdateTime_StartDay += il =>
            {
                var cursor = new ILCursor(il);

                cursor.OverrideField(typeof(NPC).GetField(nameof(NPC.downedMechBossAny)),
                    () => GetFlags().FlagIsActive(FlagID.Eclipse));

                cursor.GotoNext(i => i.MatchCallvirt(out var m));
                cursor.Index++;
                cursor.EmitDelegate((int chance) =>
                {
                    if (guaranteeEclipse)
                    {
                        guaranteeEclipse = false;
                        return 0;
                    }
                    return chance;
                });

                cursor.OverrideField(typeof(WorldGen).GetField(nameof(WorldGen.shadowOrbSmashed)),
                    () => GetFlags().FlagIsActive(FlagID.GoblinArmy));

                cursor.OverrideField(typeof(WorldGen).GetField(nameof(WorldGen.altarCount)),
                    () => GetFlags().FlagIsActive(FlagID.PirateInvasion));
            };

            // Nighttime Events
            Terraria.IL_Main.UpdateTime_StartNight += il =>
            {
                var cursor = new ILCursor(il);

                cursor.OverrideField(typeof(NPC).GetField(nameof(NPC.downedBoss2)),
                    () => GetFlags().FlagIsActive(FlagID.Meteor));

                cursor.GotoNext(i => i.MatchLdsfld(typeof(Main).GetField(nameof(Main.tenthAnniversaryWorld))));
                cursor.GotoNext(i => i.MatchCallvirt(out var m));
                cursor.Index++;
                cursor.EmitDelegate((int chance) =>
                {
                    if (guaranteeBloodMoon)
                    {
                        guaranteeBloodMoon = false;
                        return 0;
                    }
                    return chance;
                });
                cursor.GotoNext(i => i.MatchCallvirt(out var m));
                cursor.Index++;
                cursor.EmitPop();
                cursor.EmitDelegate(() => {
                    return GetFlags().FlagIsActive(FlagID.BloodMoon) ? 5 : 0;
                });
            };

            // Rain
            Terraria.IL_Main.StartRain += il =>
            {
                var cursor = new ILCursor(il);
                var label = cursor.DefineLabel();
                cursor.EmitDelegate(() =>
                {
                    return GetFlags().FlagIsActive(FlagID.Rain);
                });
                cursor.EmitBrtrue(label);
                cursor.EmitRet();
                cursor.MarkLabel(label);
            };

            // Sandstorm
            Terraria.GameContent.Events.IL_Sandstorm.StartSandstorm += il =>
            {
                var cursor = new ILCursor(il);
                var label = cursor.DefineLabel();
                cursor.EmitDelegate(() =>
                {
                    return GetFlags().FlagIsActive(FlagID.Wind);
                });
                cursor.EmitBrtrue(label);
                cursor.EmitRet();
                cursor.MarkLabel(label);
            };

            // Journey Mode Weather Manipulation
            Terraria.GameContent.UI.States.IL_UICreativePowersMenu.WeatherCategoryButtonClick += il =>
            {
                var cursor = new ILCursor(il);
                cursor.GotoNext(i => i.MatchBrtrue(out var m));
                cursor.EmitDelegate(() =>
                {
                    if (!GetFlags().FlagIsActive(FlagID.Rain))
                    {
                        Main.NewText("You cannot modify the rain level until you receive the 'Rain' item!");
                        return 0;
                    }
                    else
                    {
                        return 1;
                    }
                });
                cursor.EmitAnd();
                cursor.Index++;
                cursor.GotoNext(i => i.MatchBrtrue(out var m));
                cursor.EmitDelegate(() =>
                {
                    if (!GetFlags().FlagIsActive(FlagID.Wind))
                    {
                        Main.NewText("You cannot modify the wind level until you receive the 'Wind' item!");
                        return 0;
                    }
                    else
                    {
                        return 1;
                    }
                });
                cursor.EmitAnd();
            };

            // Jungle Upgrade
            Terraria.IL_WorldGen.UpdateWorld_GrassGrowth += il =>
            {
                var cursor = new ILCursor(il);
                var label = cursor.DefineLabel();

                cursor.OverrideField(typeof(NPC).GetField(nameof(NPC.downedMechBoss1)),
                    () => GetFlags().FlagIsActive(FlagID.JungleUpgrade));
                cursor.EmitBrtrue(label);
                cursor.EmitLdcI4(0);
                cursor.GotoNext(i => i.MatchCall(out var m));
                cursor.MarkLabel(label);
            };

            // Invasion Item Trigger
            Terraria.IL_Main.CanStartInvasion += il =>
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(i => i.MatchCgt());
                cursor.Index++;
                cursor.EmitPop();
                cursor.EmitLdarg0();
                cursor.EmitDelegate((int invasionType) =>
                {
                    switch (invasionType)
                    {
                        case 1: return GetFlags().FlagIsActive(FlagID.GoblinArmy);
                        case 3: return GetFlags().FlagIsActive(FlagID.PirateInvasion);
                        default: return false;
                    }
                });
            };

            Terraria.IL_Main.StartInvasion += il =>
            {
                var cursor = new ILCursor(il);
                var label = il.DefineLabel();

                cursor.GotoNext(i => i.MatchRet());
                cursor.Index += 2;
                cursor.EmitPop();
                cursor.EmitBr(label);
                cursor.GotoNext(i => i.MatchLdarg0());
                cursor.MarkLabel(label);
            };

            // Meteor Trigger on Evil Boss Death
            Terraria.IL_NPC.DoDeathEvents += il =>
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(i => i.MatchStsfld(typeof(WorldGen).GetField(nameof(WorldGen.spawnMeteor))));
                cursor.EmitPop();
                cursor.EmitLdcI4(0);
            };

            // Old One's Army locations
            IL_DD2Event.WinInvasionInternal += il =>
            {
                var cursor = new ILCursor(il);

                foreach (var (flagName, tier) in new Tuple<string, int>[] {
                    Tuple.Create(nameof(DD2Event.DownedInvasionT1), 1),
                    Tuple.Create(nameof(DD2Event.DownedInvasionT2), 2),
                    Tuple.Create(nameof(DD2Event.DownedInvasionT3), 3),
                })
                {
                    var flag = typeof(DD2Event).GetField(flagName);
                    cursor.GotoNext(i => i.MatchStsfld(flag));
                    cursor.EmitDelegate<Action>(() => temp = (bool)flag.GetValue(null));
                    cursor.Index++;
                    cursor.EmitDelegate(() =>
                    {
                        flag.SetValue(null, temp);
                        archipelagoSystem.QueueLocation($"Old One's Army Tier {tier}");
                    });
                }
            };

            IL_NPC.DoDeathEvents += il =>
            {
                var cursor = new ILCursor(il);

                // Prevent NPC.downedTower* from being set
                foreach (var flag in new string[] { nameof(NPC.downedTowerSolar), nameof(NPC.downedTowerVortex), nameof(NPC.downedTowerNebula), nameof(NPC.downedTowerStardust) })
                {
                    var field = typeof(NPC).GetField(flag, BindingFlags.Static | BindingFlags.Public);
                    cursor.GotoNext(i => i.MatchStsfld(field));
                    // Crimes
                    cursor.EmitDelegate<Action>(() => temp = (bool)field.GetValue(null));
                    cursor.Index++;
                    cursor.EmitDelegate(() => field.SetValue(null, temp));
                }

                // Prevent NPC.downedMechBossAny from being set
                while (cursor.TryGotoNext(i => i.MatchStsfld(typeof(NPC).GetField(nameof(NPC.downedMechBossAny)))))
                {
                    cursor.EmitDelegate<Action>(() => temp = NPC.downedMechBossAny);
                    cursor.Index++;
                    cursor.EmitDelegate<Action>(() => NPC.downedMechBossAny = temp);
                }

                // Prevent Hardmode generation Terraria.NPC:69104
                cursor.GotoNext(i => i.MatchCall(typeof(WorldGen).GetMethod(nameof(WorldGen.StartHardmode))));
                cursor.EmitDelegate(() =>
                {
                    temp = Main.hardMode;
                    Main.hardMode = true;
                });
                cursor.Index++;
                cursor.EmitDelegate<Action>(() => Main.hardMode = temp);
            };

            IL_WorldGen.UpdateLunarApocalypse += il =>
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(i => i.MatchCall(typeof(WorldGen).GetMethod(nameof(WorldGen.StartImpendingDoom))));
                cursor.Index--;
                cursor.EmitDelegate(() => archipelagoSystem.QueueLocation("Lunar Events"));
            };

            // Stop loading achievements from disk
            IL_AchievementManager.Load += il =>
            {
                var cursor = new ILCursor(il);
                cursor.Emit(OpCodes.Ret);
            };

            if (Main.netMode != NetmodeID.Server) Main.Achievements.OnAchievementCompleted += OnAchievementCompleted;

            // Change Town NPCs' Spawn Conditions

            Terraria.IL_Main.UpdateTime_SpawnTownNPCs += il =>
            {
                var cursor = new ILCursor(il);

                List<int> overridenNPCids = new List<int>();

                void OverrideCondition(FieldInfo fieldInfo, int npcID)
                {
                    var label = il.DefineLabel();

                    cursor.GotoNext(i => i.MatchLdsfld(fieldInfo));
                    cursor.Index++;
                    cursor.EmitPop();
                    cursor.EmitBr(label);

                    cursor.GotoNext(i => i.MatchLdsfld(typeof(Main).GetField(nameof(Main.townNPCCanSpawn))));
                    cursor.MarkLabel(label);
                    cursor.Index += 3;
                    cursor.EmitPop();
                    cursor.EmitDelegate(() =>
                    {
                        return GetFlags().FreeNPCSpawnable(npcID);
                    });

                    overridenNPCids.Add(npcID);
                }

                OverrideCondition(typeof(NPC).GetField(nameof(NPC.downedBoss1)), NPCID.Dryad);
                OverrideCondition(typeof(NPC).GetField(nameof(NPC.downedBoss3)), NPCID.Clothier);
                OverrideCondition(typeof(NPC).GetField(nameof(NPC.downedFrost)), NPCID.SantaClaus);
                OverrideCondition(typeof(Main).GetField(nameof(Main.tenthAnniversaryWorld)), NPCID.Steampunker);
                OverrideCondition(typeof(NPC).GetField(nameof(NPC.downedQueenBee)), NPCID.WitchDoctor);
                OverrideCondition(typeof(NPC).GetField(nameof(NPC.downedPirates)), NPCID.Pirate);
                cursor.GotoNext(i => i.MatchLdsfld(typeof(Main).GetField(nameof(Main.hardMode))));
                cursor.Index++; //We skip the Main.hardMode flag in the truffle's condition. I'm so sorry for this
                OverrideCondition(typeof(Main).GetField(nameof(Main.hardMode)), NPCID.Cyborg);

                cursor.GotoNext(i => i.MatchLdsfld(typeof(WorldGen).GetField(nameof(WorldGen.prioritizedTownNPCType))));
                cursor.Index++;
                cursor.EmitDelegate<Func<int, int>>((priorityNPC) =>
                {
                    return overridenNPCids.Find(i => !NPC.AnyNPCs(i));
                });


            };

            // Unmaintainable reflection

            if (!ModLoader.HasMod("CalamityMod")) return;
            var calamity = ModLoader.GetMod("CalamityMod");

            var calamityAssembly = calamity.GetType().Assembly;
            foreach (var type in calamityAssembly.GetTypes()) switch (type.Name)
                {
                    case "DesertScourgeHead": desertScourgeHeadOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "GiantClam": giantClamOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "CragmawMire": cragmawMireOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "AcidRainEvent": acidRainEventUpdateInvasion = type.GetMethod("UpdateInvasion", BindingFlags.Static | BindingFlags.Public); break;
                    case "Crabulon": crabulonOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "HiveMind": hiveMindOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "PerforatorHive": perforatorHiveOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "SlimeGodCore": slimeGodCoreOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "CalamityGlobalNPC":
                        calamityGlobalNpcOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public);
                        calamityGlobalNpcSetNewBossJustDowned = type.GetMethod("SetNewBossJustDowned", BindingFlags.Static | BindingFlags.Public);
                        break;
                    case "AquaticScourgeHead": aquaticScourgeHeadOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Mauler": maulerOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "BrimstoneElemental": brimstoneElementalOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Cryogen": cryogenOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "CalamitasClone": calamitasCloneOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "GreatSandShark": greatSandSharkOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Leviathan": leviathanRealOnKill = type.GetMethod("RealOnKill", BindingFlags.Static | BindingFlags.Public); break;
                    case "AstrumAureus": astrumAureusOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "PlaguebringerGoliath": plaguebringerGoliathOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "RavagerBody": ravagerBodyOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "AstrumDeusHead": astrumDeusHeadOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "ProfanedGuardianCommander": profanedGuardianCommanderOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Bumblefuck": bumblefuckOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Providence": providenceOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "StormWeaverHead": stormWeaverHeadOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "CeaselessVoid": ceaselessVoidOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Signus": signusOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Polterghast": polterghastOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "NuclearTerror": nuclearTerrorOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "OldDuke": oldDukeOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "DevourerofGodsHead": devourerofGodsHeadOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Yharon": yharonOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "AresBody": aresBodyOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Apollo": apolloOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "ThanatosHead": thanatosHeadOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "SupremeCalamitas": supremeCalamitasOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                }

            onDesertScourgeHeadOnKill += OnDesertScourgeHeadOnKill;
            onGiantClamOnKill += OnGiantClamOnKill;
            onCragmawMireOnKill += OnCragmawMireOnKill;
            editAcidRainEventUpdateInvasion += EditAcidRainEventUpdateInvasion;
            onCrabulonOnKill += OnCrabulonOnKill;
            onHiveMindOnKill += OnHiveMindOnKill;
            onPerforatorHiveOnKill += OnPerforatorHiveOnKill;
            onSlimeGodCoreOnKill += OnSlimeGodCoreOnKill;
            onCalamityGlobalNpcOnKill += OnCalamityGlobalNpcOnKill;
            editCalamityGlobalNPCOnKill += EditCalamityGlobalNPCOnKill;
            onAquaticScourgeHeadOnKill += OnAquaticScourgeHeadOnKill;
            onMaulerOnKill += OnMaulerOnKill;
            onBrimstoneElementalOnKill += OnBrimstoneElementalOnKill;
            onCryogenOnKill += OnCryogenOnKill;
            onCalamitasCloneOnKill += OnCalamitasCloneOnKill;
            onGreatSandSharkOnKill += OnGreatSandSharkOnKill;
            onLeviathanRealOnKill += OnLeviathanRealOnKill;
            onAstrumAureusOnKill += OnAstrumAureusOnKill;
            onPlaguebringerGoliathOnKill += OnPlaguebringerGoliathOnKill;
            onRavagerBodyOnKill += OnRavagerBodyOnKill;
            onAstrumDeusHeadOnKill += OnAstrumDeusHeadOnKill;
            onProfanedGuardianCommanderOnKill += OnProfanedGuardianCommanderOnKill;
            onBumblefuckOnKill += OnBumblefuckOnKill;
            onProvidenceOnKill += OnProvidenceOnKill;
            onStormWeaverHeadOnKill += OnStormWeaverHeadOnKill;
            onCeaselessVoidOnKill += OnCeaselessVoidOnKill;
            onSignusOnKill += OnSignusOnKill;
            onPolterghastOnKill += OnPolterghastOnKill;
            onNuclearTerrorOnKill += OnNuclearTerrorOnKill;
            onOldDukeOnKill += OnOldDukeOnKill;
            onDevourerofGodsHeadOnKill += OnDevourerofGodsHeadOnKill;
            onYharonOnKill += OnYharonOnKill;
            onAresBodyOnKill += OnAresBodyOnKill;
            onApolloOnKill += OnApolloOnKill;
            onThanatosHeadOnKill += OnThanatosHeadOnKill;
            onSupremeCalamitasOnKill += OnSupremeCalamitasOnKill;
            onCalamityGlobalNpcSetNewBossJustDowned += OnCalamityGlobalNpcSetNewBossJustDowned;
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            var message = reader.ReadString();
            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();

            // The way we handle packets kind of sucks. It's using string IDs with some special
            // cases.
            if (message == "") archipelagoSystem.Chat(archipelagoSystem.Status(), whoAmI);
            else if (message.StartsWith("deathlink")) archipelagoSystem.TriggerDeathlink(message.Substring(9), whoAmI);
            else if (message.StartsWith("[DeathLink]"))
            {
                var player = Main.player[Main.myPlayer];
                if (player.active && !player.dead) player.Hurt(PlayerDeathReason.ByCustomReason(message), 999999, 1);
            }
            else if (message == "YouGotAnItem") Main.LocalPlayer.GetModPlayer<ArchipelagoPlayer>().ReceivedReward(reader.ReadInt32());
            else if (message == "RecievedRewardsForSetupShop")
            {
                var rewards = archipelagoSystem.ReceivedRewards();

                var packet = GetPacket();
                packet.Write("SetupShop");
                foreach (var reward in rewards) packet.Write(reward);
                packet.Write(-1);
                var player = Main.player[whoAmI];
                var position = player.position;
                var npc = NPC.NewNPC(new EntitySource_Misc("Open collection"), (int)position.X, (int)position.Y, ModContent.NPCType<CollectionNPC>(), 0, whoAmI, reader.ReadInt32());
                player.SetTalkNPC(npc);
                packet.Write(npc);
                packet.Send(whoAmI);
            }
            else if (message == "SetupShop")
            {
                var items = new List<int>();

                while (true)
                {
                    var item = reader.ReadInt32();
                    if (item == -1) break;
                    items.Add(item);
                }

                CollectionButton.SetupShop(items, reader.ReadInt32());
            }
            else archipelagoSystem.QueueLocation(message);
        }

        public override void Unload()
        {
            if (Main.netMode != NetmodeID.Server) Main.Achievements.OnAchievementCompleted -= OnAchievementCompleted;

            if (!ModLoader.HasMod("CalamityMod")) return;

            onDesertScourgeHeadOnKill -= OnDesertScourgeHeadOnKill;
            onGiantClamOnKill -= OnGiantClamOnKill;
            onCragmawMireOnKill -= OnCragmawMireOnKill;
            editAcidRainEventUpdateInvasion -= EditAcidRainEventUpdateInvasion;
            onCrabulonOnKill -= OnCrabulonOnKill;
            onHiveMindOnKill -= OnHiveMindOnKill;
            onPerforatorHiveOnKill -= OnPerforatorHiveOnKill;
            onSlimeGodCoreOnKill -= OnSlimeGodCoreOnKill;
            onCalamityGlobalNpcOnKill -= OnCalamityGlobalNpcOnKill;
            editCalamityGlobalNPCOnKill -= EditCalamityGlobalNPCOnKill;
            onAquaticScourgeHeadOnKill -= OnAquaticScourgeHeadOnKill;
            onMaulerOnKill -= OnMaulerOnKill;
            onBrimstoneElementalOnKill -= OnBrimstoneElementalOnKill;
            onCryogenOnKill -= OnCryogenOnKill;
            onCalamitasCloneOnKill -= OnCalamitasCloneOnKill;
            onGreatSandSharkOnKill -= OnGreatSandSharkOnKill;
            onLeviathanRealOnKill -= OnLeviathanRealOnKill;
            onAstrumAureusOnKill -= OnAstrumAureusOnKill;
            onPlaguebringerGoliathOnKill -= OnPlaguebringerGoliathOnKill;
            onRavagerBodyOnKill -= OnRavagerBodyOnKill;
            onAstrumDeusHeadOnKill -= OnAstrumDeusHeadOnKill;
            onProfanedGuardianCommanderOnKill -= OnProfanedGuardianCommanderOnKill;
            onBumblefuckOnKill -= OnBumblefuckOnKill;
            onProvidenceOnKill -= OnProvidenceOnKill;
            onStormWeaverHeadOnKill -= OnStormWeaverHeadOnKill;
            onCeaselessVoidOnKill -= OnCeaselessVoidOnKill;
            onSignusOnKill -= OnSignusOnKill;
            onPolterghastOnKill -= OnPolterghastOnKill;
            onNuclearTerrorOnKill -= OnNuclearTerrorOnKill;
            onOldDukeOnKill -= OnOldDukeOnKill;
            onDevourerofGodsHeadOnKill -= OnDevourerofGodsHeadOnKill;
            onYharonOnKill -= OnYharonOnKill;
            onAresBodyOnKill -= OnAresBodyOnKill;
            onApolloOnKill -= OnApolloOnKill;
            onThanatosHeadOnKill -= OnThanatosHeadOnKill;
            onSupremeCalamitasOnKill -= OnSupremeCalamitasOnKill;
            onCalamityGlobalNpcSetNewBossJustDowned -= OnCalamityGlobalNpcSetNewBossJustDowned;
        }

        void OnAchievementCompleted(Achievement achievement)
        {
            var name = achievement.Name switch
            {
                "TIMBER" => "Timber!!",
                "BENCHED" => "Benched",
                "OBTAIN_HAMMER" => "Stop! Hammer Time!",
                "MATCHING_ATTIRE" => "Matching Attire",
                "FASHION_STATEMENT" => "Fashion Statement",
                "OOO_SHINY" => "Ooo! Shiny!",
                "NO_HOBO" => "No Hobo",
                "HEAVY_METAL" => "Heavy Metal",
                "FREQUENT_FLYER" => "The Frequent Flyer",
                "GET_GOLDEN_DELIGHT" => "Feast of Midas",
                "DYE_HARD" => "Dye Hard",
                "LUCKY_BREAK" => "Lucky Break",
                "STAR_POWER" => "Star Power",
                "YOU_CAN_DO_IT" => "You Can Do It!",
                "DRINK_BOTTLED_WATER_WHILE_DROWNING" => "Unusual Survival Strategies",
                "TURN_GNOME_TO_STATUE" => "Heliophobia",
                "ARCHAEOLOGIST" => "Archaeologist",
                "PET_THE_PET" => "Feeling Petty",
                "FLY_A_KITE_ON_A_WINDY_DAY" => "A Rather Blustery Day",
                "PRETTY_IN_PINK" => "Pretty in Pink",
                "MARATHON_MEDALIST" => "Marathon Medalist",
                "SERVANT_IN_TRAINING" => "Servant-in-Training",
                "GOOD_LITTLE_SLAVE" => "10 Fishing Quests",
                "TROUT_MONKEY" => "Trout Monkey",
                "GLORIOUS_GOLDEN_POLE" => "Glorious Golden Pole",
                "FAST_AND_FISHIOUS" => "Fast and Fishious",
                "SUPREME_HELPER_MINION" => "Supreme Helper Minion!",
                "INTO_ORBIT" => "Into Orbit",
                "WATCH_YOUR_STEP" => "Watch Your Step!",
                "THROWING_LINES" => "Throwing Lines",
                "VEHICULAR_MANSLAUGHTER" => "Vehicular Manslaughter",
                "FIND_A_FAIRY" => "Hey! Listen!",
                "I_AM_LOOT" => "I Am Loot!",
                "HEART_BREAKER" => "Heart Breaker",
                "HOLD_ON_TIGHT" => "Hold on Tight!",
                "LIKE_A_BOSS" => "Like a Boss",
                "JEEPERS_CREEPERS" => "Jeepers Creepers",
                "FUNKYTOWN" => "Funkytown",
                "DECEIVER_OF_FOOLS" => "Deceiver of Fools",
                "DIE_TO_DEAD_MANS_CHEST" => "Dead Men Tell No Tales",
                "BULLDOZER" => "Bulldozer",
                "THERE_ARE_SOME_WHO_CALL_HIM" => "There are Some Who Call Him...",
                "THROW_A_PARTY" => "Jolly Jamboree",
                "TRANSMUTE_ITEM" => "A Shimmer In The Dark",
                "ITS_GETTING_HOT_IN_HERE" => "It's Getting Hot in Here",
                "ROCK_BOTTOM" => "Rock Bottom",
                "SMASHING_POPPET" => "Smashing, Poppet!",
                "TALK_TO_NPC_AT_MAX_HAPPINESS" => "Leading Landlord",
                "COMPLETELY_AWESOME" => "Completely Awesome",
                "STICKY_SITUATION" => "Sticky Situation",
                "THE_CAVALRY" => "The Cavalry",
                "BLOODBATH" => "Bloodbath",
                "TIL_DEATH" => "Til Death...",
                "FOUND_GRAVEYARD" => "Quiet Neighborhood",
                "PURIFY_ENTIRE_WORLD" => "And Good Riddance!",
                "MINER_FOR_FIRE" => "Miner for Fire",
                "GO_LAVA_FISHING" => "Hot Reels!",
                "GET_TERRASPARK_BOOTS" => "Boots of the Hero",
                "WHERES_MY_HONEY" => "Where's My Honey?",
                "NOT_THE_BEES" => "Not the Bees!",
                "DUNGEON_HEIST" => "Dungeon Heist",
                "GET_CELL_PHONE" => "Black Mirror",
                "BEGONE_EVIL" => "Begone, Evil!",
                "EXTRA_SHINY" => "Extra Shiny!",
                "GET_ANKH_SHIELD" => "Ankhumulation Complete",
                "GELATIN_WORLD_TOUR" => "Gelatin World Tour",
                "HEAD_IN_THE_CLOUDS" => "Head in the Clouds",
                "DEFEAT_DREADNAUTILUS" => "Don't Dread on Me",
                "IT_CAN_TALK" => "It Can Talk?!",
                "ALL_TOWN_SLIMES" => "The Great Slime Mitosis",
                "PRISMANCER" => "Prismancer",
                "GET_A_LIFE" => "Get a Life",
                "TOPPED_OFF" => "Topped Off",
                "BUCKETS_OF_BOLTS" => "Buckets of Bolts",
                "MECHA_MAYHEM" => "Mecha Mayhem",
                "DRAX_ATTAX" => "Drax Attax",
                "PHOTOSYNTHESIS" => "Photosynthesis",
                "YOU_AND_WHAT_ARMY" => "You and What Army?",
                "TO_INFINITY_AND_BEYOND" => "To Infinity... and Beyond!",
                "REAL_ESTATE_AGENT" => "Real Estate Agent",
                "ROBBING_THE_GRAVE" => "Robbing the Grave",
                "BIG_BOOTY" => "Big Booty",
                "RAINBOWS_AND_UNICORNS" => "Rainbows and Unicorns",
                "TEMPLE_RAIDER" => "Temple Raider",
                "SWORD_OF_THE_HERO" => "Sword of the Hero",
                "KILL_THE_SUN" => "Kill the Sun",
                "BALEFUL_HARVEST" => "Baleful Harvest",
                "ICE_SCREAM" => "Ice Scream",
                "SLAYER_OF_WORLDS" => "Slayer of Worlds",
                "SICK_THROW" => "Sick Throw",
                "GET_ZENITH" => "Infinity + 1 Sword",
                _ => null,
            };

            if (name != null) ModContent.GetInstance<ArchipelagoSystem>().QueueLocationClient(name);
            ModContent.GetInstance<ArchipelagoSystem>().Achieved(achievement.Name);
        }

        delegate void OnKill(ModNPC self);

        void OnDesertScourgeHeadOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Desert Scourge");
        }

        void OnGiantClamOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else
            {
                ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Giant Clam");
                if (Main.hardMode) ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Hardmode Giant Clam");
            }
        }

        void OnCragmawMireOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Cragmaw Mire");
        }

        void EditAcidRainEventUpdateInvasion(ILContext il)
        {
            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();
            var calamitySystem = ModContent.GetInstance<CalamitySystem>();
            var cursor = new ILCursor(il);

            cursor.GotoNext(i => i.MatchLdarg(0));
            cursor.Index++;
            cursor.EmitDelegate<Action<bool>>(won =>
            {
                if (won)
                {
                    archipelagoSystem.QueueLocation("Acid Rain Tier 1");
                    if (calamitySystem.DownedAquaticScourge()) archipelagoSystem.QueueLocation("Acid Rain Tier 2");
                }
            });
            cursor.Emit(OpCodes.Ldc_I4_0);
        }

        void OnCrabulonOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Crabulon");
        }

        void OnHiveMindOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("The Hive Mind");
        }

        void OnPerforatorHiveOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("The Perforators");
        }

        void OnSlimeGodCoreOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("The Slime God");
        }

        int[] vanillaBosses = { NPCID.KingSlime, NPCID.EyeofCthulhu, NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail, NPCID.BrainofCthulhu, NPCID.QueenBee, NPCID.SkeletronHead, NPCID.Deerclops, NPCID.WallofFlesh, NPCID.BloodNautilus, NPCID.QueenSlimeBoss, NPCID.Retinazer, NPCID.Spazmatism, NPCID.TheDestroyer, NPCID.SkeletronPrime, NPCID.Plantera, NPCID.Golem, NPCID.DukeFishron, NPCID.MourningWood, NPCID.Pumpking, NPCID.Everscream, NPCID.SantaNK1, NPCID.IceQueen, NPCID.HallowBoss, NPCID.CultistBoss, NPCID.MoonLordCore };

        delegate void CalamityGlobalNpcOnKill(object self, NPC npc);
        void OnCalamityGlobalNpcOnKill(CalamityGlobalNpcOnKill orig, object self, NPC npc)
        {
            if (temp || !vanillaBosses.Contains(npc.type)) orig(self, npc);
            else ModContent.GetInstance<CalamitySystem>().HandleBossRush(npc);
        }

        void EditCalamityGlobalNPCOnKill(ILContext il)
        {
            var SeldomArchipelago = ModContent.GetInstance<ArchipelagoSystem>();
            var cursor = new ILCursor(il);

            cursor.GotoNext(i => i.MatchLdcI4(NPCID.WallofFlesh));
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldc_I4_0);
        }

        void OnAquaticScourgeHeadOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Aquatic Scourge");
        }

        void OnMaulerOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Mauler");
        }

        void OnBrimstoneElementalOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Brimstone Elemental");
        }

        void OnCryogenOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Cryogen");
        }

        void OnCalamitasCloneOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Calamitas Clone");
        }

        void OnGreatSandSharkOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Great Sand Shark");
        }

        delegate void RealOnKill(NPC npc);
        void OnLeviathanRealOnKill(RealOnKill orig, NPC npc)
        {
            if (temp) orig(npc);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Leviathan and Anahita");
        }

        void OnAstrumAureusOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Astrum Aureus");
        }

        void OnPlaguebringerGoliathOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("The Plaguebringer Goliath");
        }

        void OnRavagerBodyOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Ravager");
        }

        void OnAstrumDeusHeadOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Astrum Deus");
        }

        void OnProfanedGuardianCommanderOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Profaned Guardians");
        }

        void OnBumblefuckOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("The Dragonfolly");
        }

        void OnProvidenceOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Providence, the Profaned Goddess");
        }

        void OnStormWeaverHeadOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Storm Weaver");
        }

        void OnCeaselessVoidOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Ceaseless Void");
        }

        void OnSignusOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Signus, Envoy of the Devourer");
        }

        void OnPolterghastOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Polterghast");
        }

        void OnNuclearTerrorOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Nuclear Terror");
        }

        void OnOldDukeOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("The Old Duke");
        }

        void OnDevourerofGodsHeadOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("The Devourer of Gods");
        }

        void OnYharonOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Yharon, Dragon of Rebirth");
        }

        void OnAresBodyOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else if (ModContent.GetInstance<CalamitySystem>().AreExosDead(0)) ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Exo Mechs");
        }

        void OnApolloOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else if (ModContent.GetInstance<CalamitySystem>().AreExosDead(1)) ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Exo Mechs");
        }

        void OnThanatosHeadOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else if (ModContent.GetInstance<CalamitySystem>().AreExosDead(2)) ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Exo Mechs");
        }

        void OnSupremeCalamitasOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Supreme Witch, Calamitas");
        }

        delegate void CalamityGlobalNpcSetNewBossJustDowned(NPC npc);
        void OnCalamityGlobalNpcSetNewBossJustDowned(CalamityGlobalNpcSetNewBossJustDowned orig, NPC npc) { }

        delegate void OnOnKill(OnKill orig, ModNPC self);

        static event OnOnKill onDesertScourgeHeadOnKill
        {
            add => MonoModHooks.Add(desertScourgeHeadOnKill, value);
            remove { }
        }

        static event OnOnKill onGiantClamOnKill
        {
            add => MonoModHooks.Add(giantClamOnKill, value);
            remove { }
        }

        static event OnOnKill onCragmawMireOnKill
        {
            add => MonoModHooks.Add(cragmawMireOnKill, value);
            remove { }
        }

        static event ILContext.Manipulator editAcidRainEventUpdateInvasion
        {
            add => MonoModHooks.Modify(acidRainEventUpdateInvasion, value);
            remove { }
        }

        static event OnOnKill onCrabulonOnKill
        {
            add => MonoModHooks.Add(crabulonOnKill, value);
            remove { }
        }

        static event OnOnKill onHiveMindOnKill
        {
            add => MonoModHooks.Add(hiveMindOnKill, value);
            remove { }
        }

        static event OnOnKill onPerforatorHiveOnKill
        {
            add => MonoModHooks.Add(perforatorHiveOnKill, value);
            remove { }
        }

        static event OnOnKill onSlimeGodCoreOnKill
        {
            add => MonoModHooks.Add(slimeGodCoreOnKill, value);
            remove { }
        }

        delegate void OnCalamityGlobalNpcOnKillTy(CalamityGlobalNpcOnKill orig, object self, NPC npc);
        static event OnCalamityGlobalNpcOnKillTy onCalamityGlobalNpcOnKill
        {
            add => MonoModHooks.Add(calamityGlobalNpcOnKill, value);
            remove { }
        }

        static event ILContext.Manipulator editCalamityGlobalNPCOnKill
        {
            add => MonoModHooks.Modify(calamityGlobalNpcOnKill, value);
            remove { }
        }

        static event OnOnKill onAquaticScourgeHeadOnKill
        {
            add => MonoModHooks.Add(aquaticScourgeHeadOnKill, value);
            remove { }
        }

        static event OnOnKill onMaulerOnKill
        {
            add => MonoModHooks.Add(maulerOnKill, value);
            remove { }
        }

        static event OnOnKill onBrimstoneElementalOnKill
        {
            add => MonoModHooks.Add(brimstoneElementalOnKill, value);
            remove { }
        }

        static event OnOnKill onCryogenOnKill
        {
            add => MonoModHooks.Add(cryogenOnKill, value);
            remove { }
        }

        static event OnOnKill onCalamitasCloneOnKill
        {
            add => MonoModHooks.Add(calamitasCloneOnKill, value);
            remove { }
        }

        static event OnOnKill onGreatSandSharkOnKill
        {
            add => MonoModHooks.Add(greatSandSharkOnKill, value);
            remove { }
        }

        delegate void OnRealOnKill(RealOnKill orig, NPC npc);
        static event OnRealOnKill onLeviathanRealOnKill
        {
            add => MonoModHooks.Add(leviathanRealOnKill, value);
            remove { }
        }

        static event OnOnKill onAstrumAureusOnKill
        {
            add => MonoModHooks.Add(astrumAureusOnKill, value);
            remove { }
        }

        static event OnOnKill onPlaguebringerGoliathOnKill
        {
            add => MonoModHooks.Add(plaguebringerGoliathOnKill, value);
            remove { }
        }

        static event OnOnKill onRavagerBodyOnKill
        {
            add => MonoModHooks.Add(ravagerBodyOnKill, value);
            remove { }
        }

        static event OnOnKill onAstrumDeusHeadOnKill
        {
            add => MonoModHooks.Add(astrumDeusHeadOnKill, value);
            remove { }
        }

        static event OnOnKill onProfanedGuardianCommanderOnKill
        {
            add => MonoModHooks.Add(profanedGuardianCommanderOnKill, value);
            remove { }
        }

        static event OnOnKill onBumblefuckOnKill
        {
            add => MonoModHooks.Add(bumblefuckOnKill, value);
            remove { }
        }

        static event OnOnKill onProvidenceOnKill
        {
            add => MonoModHooks.Add(providenceOnKill, value);
            remove { }
        }

        static event OnOnKill onStormWeaverHeadOnKill
        {
            add => MonoModHooks.Add(stormWeaverHeadOnKill, value);
            remove { }
        }

        static event OnOnKill onCeaselessVoidOnKill
        {
            add => MonoModHooks.Add(ceaselessVoidOnKill, value);
            remove { }
        }

        static event OnOnKill onSignusOnKill
        {
            add => MonoModHooks.Add(signusOnKill, value);
            remove { }
        }

        static event OnOnKill onPolterghastOnKill
        {
            add => MonoModHooks.Add(polterghastOnKill, value);
            remove { }
        }

        static event OnOnKill onNuclearTerrorOnKill
        {
            add => MonoModHooks.Add(nuclearTerrorOnKill, value);
            remove { }
        }

        static event OnOnKill onOldDukeOnKill
        {
            add => MonoModHooks.Add(oldDukeOnKill, value);
            remove { }
        }

        static event OnOnKill onDevourerofGodsHeadOnKill
        {
            add => MonoModHooks.Add(devourerofGodsHeadOnKill, value);
            remove { }
        }

        static event OnOnKill onYharonOnKill
        {
            add => MonoModHooks.Add(yharonOnKill, value);
            remove { }
        }

        static event OnOnKill onAresBodyOnKill
        {
            add => MonoModHooks.Add(aresBodyOnKill, value);
            remove { }
        }

        static event OnOnKill onApolloOnKill
        {
            add => MonoModHooks.Add(apolloOnKill, value);
            remove { }
        }

        static event OnOnKill onThanatosHeadOnKill
        {
            add => MonoModHooks.Add(thanatosHeadOnKill, value);
            remove { }
        }

        static event OnOnKill onSupremeCalamitasOnKill
        {
            add => MonoModHooks.Add(supremeCalamitasOnKill, value);
            remove { }
        }

        delegate void OnCalamityGlobalNpcSetNewBossJustDownedTy(CalamityGlobalNpcSetNewBossJustDowned orig, NPC npc);
        static event OnCalamityGlobalNpcSetNewBossJustDownedTy onCalamityGlobalNpcSetNewBossJustDowned
        {
            add => MonoModHooks.Add(calamityGlobalNpcSetNewBossJustDowned, value);
            remove { }
        }
    }
}
