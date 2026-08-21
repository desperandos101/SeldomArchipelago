using System;
using System.Collections.Generic;
using System.Reflection;
using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Items.Accessories;
using CalamityMod.Items.Fishing.AstralCatches;
using CalamityMod.NPCs;
using CalamityMod.Tiles.Ores;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SeldomArchipelagoBeta.Systems
{
    // Direct usage of Calamity must happen in here, else the mod won't compile without Calamity
    [ExtendsFromMod("CalamityMod")]
    public static class CalamitySystem
    {
        public static bool DownedAquaticScourge() => CalamityMod.DownedBossSystem.downedAquaticScourge;
        public static readonly Dictionary<string, int> calItemToType = new()
        {
            {"Cosmolight", ModContent.ItemType<CalamityMod.Items.Tools.ClimateChange.Cosmolight>()},
            {"Unholy Tonic", ModContent.ItemType<UnholyTonic>()},
            {"Vicious Tonic", ModContent.ItemType<ViciousTonic>()},
            {"Craw Carapace", ModContent.ItemType<CrawCarapace>()},
            {"Giant Shell", ModContent.ItemType<GiantShell>()},
            {"Life Jelly", ModContent.ItemType<LifeJelly>()},
            {"Vital Jelly", ModContent.ItemType<VitalJelly>()},
            {"Cleansing Jelly", ModContent.ItemType<CleansingJelly>()},
            {"Giant Tortoise Shell", ModContent.ItemType<GiantTortoiseShell>()},
            {"Coin of Deceit", ModContent.ItemType<CoinofDeceit>()},
            {"Ink Bomb", ModContent.ItemType<InkBomb>()},
            {"Voltaic Jelly", ModContent.ItemType<VoltaicJelly>()},
            {"Wulfrum Battery", ModContent.ItemType<WulfrumBattery>()},
            {"Luxor's Gift", ModContent.ItemType<LuxorsGift>()},
            {"Raider's Talisman", ModContent.ItemType<RaidersTalisman>()},
            {"Rotten Dogtooth", ModContent.ItemType<RottenDogtooth>()},
            {"Scuttler's Jewel", ModContent.ItemType<ScuttlersJewel>()},
            {"Unstable Granite Core", ModContent.ItemType<UnstableGraniteCore>()},
            {"Ilmeris' Spark", ModContent.ItemType<IlmerisSpark>()},
            {"Ursa Sergeant", ModContent.ItemType<UrsaSergeant>()},
            {"Trinket of Chi", ModContent.ItemType<TrinketofChi>()},
            {"The Transformer", ModContent.ItemType<TheTransformer>()},
            {"Rover Drive", ModContent.ItemType<RoverDrive>()},
            {"Marnite Repulsion Shield", ModContent.ItemType<MarniteRepulsionShield>()},
            {"Frost Barrier", ModContent.ItemType<FrostBarrier>()},
            {"Ancient Fossil", ModContent.ItemType<AncientFossil>()},
            {"Spelunker's Amulet", ModContent.ItemType<SpelunkersAmulet>()},
            {"Fungal Symbiote", ModContent.ItemType<FungalSymbiote>()},
            {"Gladiator's Locket", ModContent.ItemType<GladiatorsLocket>()},
            {"Wulfrum Acrobatics Pack", ModContent.ItemType<WulfrumAcrobaticsPack>()},
            {"Depths Charm", ModContent.ItemType<DepthCharm>()},
            {"Anechoic Plating", ModContent.ItemType<AnechoicPlating>()},
            {"Iron Boots", ModContent.ItemType<IronBoots>()},
            {"Sprit Glyph", ModContent.ItemType<SpiritGlyph>()},
            {"Sea Spirit Amulet", ModContent.ItemType<SeaSpiritAmulet>()},
        };
        public static bool GiveItem(string fullItem)
        {
            string item = fullItem.Replace("Reward: ", "");
            if (calItemToType.TryGetValue(item, out int type))
            {
                ModContent.GetInstance<ArchipelagoSystem>().GiveItem(type);
                return true;
            }
            return false;
        }
        // TODO: this field seems problematic
        public static bool ranBossRush = false;

        public static void CalamityPostUpdateWorld()
        {
            if (CalamityMod.DownedBossSystem.downedBossRush && !ranBossRush)
            {
                ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Boss Rush");
                ranBossRush = true;
            }
        }

        public static bool? CheckCalamityFlag(string flag) => flag switch
        {
            "Post-Desert Scourge" => CalamityMod.DownedBossSystem.downedDesertScourge,
            "Post-Giant Clam" => CalamityMod.DownedBossSystem.downedCLAM,
            "Post-Acid Rain Tier 1" => CalamityMod.DownedBossSystem.downedEoCAcidRain,
            "Post-Crabulon" => CalamityMod.DownedBossSystem.downedCrabulon,
            "Post-The Hive Mind" => CalamityMod.DownedBossSystem.downedHiveMind,
            "Post-The Perforators" => CalamityMod.DownedBossSystem.downedPerforator,
            "Post-The Slime God" => CalamityMod.DownedBossSystem.downedSlimeGod,
            "Post-Dreadnautilus" => CalamityMod.DownedBossSystem.downedDreadnautilus,
            "Post-Hardmode Giant Clam" => CalamityMod.DownedBossSystem.downedCLAMHardMode,
            "Post-Aquatic Scourge" => CalamityMod.DownedBossSystem.downedAquaticScourge,
            "Post-Cragmaw Mire" => CalamityMod.DownedBossSystem.downedCragmawMire,
            "Post-Acid Rain Tier 2" => CalamityMod.DownedBossSystem.downedAquaticScourgeAcidRain,
            "Post-Brimstone Elemental" => CalamityMod.DownedBossSystem.downedBrimstoneElemental,
            "Post-Cryogen" => CalamityMod.DownedBossSystem.downedCryogen,
            "Post-Calamitas Clone" => CalamityMod.DownedBossSystem.downedCalamitasClone,
            "Post-Great Sand Shark" => CalamityMod.DownedBossSystem.downedGSS,
            "Post-Leviathan and Anahita" => CalamityMod.DownedBossSystem.downedLeviathan,
            "Post-Astrum Aureus" => CalamityMod.DownedBossSystem.downedAstrumAureus,
            "Post-The Plaguebringer Goliath" => CalamityMod.DownedBossSystem.downedPlaguebringer,
            "Post-Ravager" => CalamityMod.DownedBossSystem.downedRavager,
            "Post-Astrum Deus" => CalamityMod.DownedBossSystem.downedAstrumDeus,
            "Post-Profaned Guardians" => CalamityMod.DownedBossSystem.downedGuardians,
            "Post-Dragonfolly" => CalamityMod.DownedBossSystem.downedDragonfolly,
            "Post-Providence, the Profaned Goddess" => CalamityMod.DownedBossSystem.downedProvidence,
            "Post-Storm Weaver" => CalamityMod.DownedBossSystem.downedStormWeaver,
            "Post-Ceaseless Void" => CalamityMod.DownedBossSystem.downedCeaselessVoid,
            "Post-Signus, Envoy of the Devourer" => CalamityMod.DownedBossSystem.downedSignus,
            "Post-Polterghast" => CalamityMod.DownedBossSystem.downedPolterghast,
            "Post-Mauler" => CalamityMod.DownedBossSystem.downedMauler,
            "Post-Nuclear Terror" => CalamityMod.DownedBossSystem.downedNuclearTerror,
            "Post-The Old Duke" => CalamityMod.DownedBossSystem.downedBoomerDuke,
            "Post-The Devourer of Gods" => CalamityMod.DownedBossSystem.downedDoG,
            "Post-Yharon, Dragon of Rebirth" => CalamityMod.DownedBossSystem.downedYharon,
            "Post-Exo Mechs" => CalamityMod.DownedBossSystem.downedExoMechs,
            "Post-Supreme Witch, Calamitas" => CalamityMod.DownedBossSystem.downedCalamitas,
            "Post-Primordial Wyrm" => CalamityMod.DownedBossSystem.downedPrimordialWyrm,
            "Post-Boss Rush" => CalamityMod.DownedBossSystem.downedBossRush,
            _ => null,
        };

        public static void CalamityStartHardmode()
        {
            if (CalamityServerConfig.Instance.EarlyHardmodeProgressionRework && NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3) SpawnMechOres();
        }

        public static void VanillaBossKilled(int boss)
        {
            var npc = new NPC { type = boss };
            var calamityNpc = new CalamityGlobalNPC();
            int globalIndex = ModContent.GetInstance<CalamityGlobalNPC>().PerEntityIndex;
            GlobalNPC[] dummyArray = new GlobalNPC[globalIndex + 1];
            dummyArray[globalIndex] = calamityNpc;
            typeof(NPC).GetField("_globals", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(npc, dummyArray);
            var seldomArchipelago = ModContent.GetInstance<SeldomArchipelago>();
            seldomArchipelago.Temp = true;
            calamityNpc.OnKill(npc);
            seldomArchipelago.Temp = false;
        }

        public static void CalamityOnKill<T>() where T : ModNPC, new() => CalamityOnKill<T>(new float[] { 0, 0, 0, 0 });

        public static void CalamityOnKill<T>(float[] newAi) where T : ModNPC, new()
        {
            var npc = new T();
            var entity = new NPC
            {
                type = ModContent.NPCType<T>(),
                target = 0
            };
            var calamityNpc = new CalamityGlobalNPC
            {
                newAI = newAi
            };
            int globalIndex = ModContent.GetInstance<CalamityGlobalNPC>().PerEntityIndex;
            GlobalNPC[] dummyArray = new GlobalNPC[globalIndex + 1];
            dummyArray[globalIndex] = calamityNpc;
            typeof(NPC).GetField("_globals", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(entity, dummyArray);
            typeof(ModType<NPC>).GetProperty("Entity").SetValue(npc, entity);
            var seldomArchipelago = ModContent.GetInstance<SeldomArchipelago>();
            seldomArchipelago.Temp = true;
            npc.OnKill();
            seldomArchipelago.Temp = false;
        }

        public static void CalamityOnKillGiantClam(bool hardmode)
        {
            var downed = hardmode ? CalamityMod.DownedBossSystem.downedCLAM : CalamityMod.DownedBossSystem.downedCLAMHardMode;
            var isHardmode = Main.hardMode;
            Main.hardMode = hardmode;
            CalamityOnKill<CalamityMod.NPCs.SunkenSea.GiantClam>();
            if (hardmode) CalamityMod.DownedBossSystem.downedCLAM = downed;
            else CalamityMod.DownedBossSystem.downedCLAMHardMode = downed;
            Main.hardMode = isHardmode;
        }

        public static void CalamityOnKillLeviathanAndAnahita()
        {
            var seldomArchipelago = ModContent.GetInstance<SeldomArchipelago>();
            seldomArchipelago.Temp = true;
            SeldomArchipelago.leviathanRealOnKill.Invoke(new CalamityMod.NPCs.Leviathan.Leviathan(), new object[] { new NPC() { type = ModContent.NPCType<CalamityMod.NPCs.Leviathan.Leviathan>() } });
            seldomArchipelago.Temp = false;
        }

        public static void CalamityOnKillCryogen()
        {
            CalamityOnKill<CalamityMod.NPCs.Cryogen.Cryogen>();
            if (Main.netMode == NetmodeID.SinglePlayer) CalamityUtils.SpawnOre(ModContent.TileType<CryonicOre>(), 0.00015, 0.45f, 0.7f, 3, 8, new int[] {
                147,
                161,
                163,
                200,
                164,
                0,
                0,
            });
        }
        public static void CalamityOnKillExoMechs()
        {
            CalamityMod.DownedBossSystem.downedExoMechs = CalamityMod.DownedBossSystem.downedAres = CalamityMod.DownedBossSystem.downedArtemisAndApollo = CalamityMod.DownedBossSystem.downedThanatos = true;
        }

        public static void CalamityAcidRainTier1Downed() => CalamityMod.DownedBossSystem.downedEoCAcidRain = true;
        public static void CalamityDreadnautilusDowned() => CalamityMod.DownedBossSystem.downedDreadnautilus = true;
        public static void CalamityAcidRainTier2Downed() => CalamityMod.DownedBossSystem.downedAquaticScourgeAcidRain = true;
        public static void CalamityPrimordialWyrmDowned() => CalamityMod.DownedBossSystem.downedPrimordialWyrm = true;
        public static void CalamityBossRushDowned() => CalamityMod.DownedBossSystem.downedBossRush = true;

        public static void CalamityOnKillDesertScourge() => CalamityOnKill<CalamityMod.NPCs.DesertScourge.DesertScourgeHead>();
        public static void CalamityOnKillCrabulon() => CalamityOnKill<CalamityMod.NPCs.Crabulon.Crabulon>();
        public static void CalamityOnKillTheHiveMind() => CalamityOnKill<CalamityMod.NPCs.HiveMind.HiveMind>();
        public static void CalamityOnKillThePerforators() => CalamityOnKill<CalamityMod.NPCs.Perforator.PerforatorHive>();
        public static void CalamityOnKillTheSlimeGod() => CalamityOnKill<CalamityMod.NPCs.SlimeGod.SlimeGodCore>();
        public static void CalamityOnKillAquaticScourge() => CalamityOnKill<CalamityMod.NPCs.AquaticScourge.AquaticScourgeHead>();
        public static void CalamityOnKillCragmawMire() => CalamityOnKill<CalamityMod.NPCs.AcidRain.CragmawMire>();
        public static void CalamityOnKillBrimstoneElemental() => CalamityOnKill<CalamityMod.NPCs.BrimstoneElemental.BrimstoneElemental>();
        public static void CalamityOnKillCalamitasClone() => CalamityOnKill<CalamityMod.NPCs.CalClone.CalamitasClone>();
        public static void CalamityOnKillGreatSandShark() => CalamityOnKill<CalamityMod.NPCs.GreatSandShark.GreatSandShark>();
        public static void CalamityOnKillAstrumAureus() => CalamityOnKill<CalamityMod.NPCs.AstrumAureus.AstrumAureus>();
        public static void CalamityOnKillThePlaguebringerGoliath() => CalamityOnKill<CalamityMod.NPCs.PlaguebringerGoliath.PlaguebringerGoliath>();
        public static void CalamityOnKillRavager() => CalamityOnKill<CalamityMod.NPCs.Ravager.RavagerBody>();
        public static void CalamityOnKillAstrumDeus() => CalamityOnKill<CalamityMod.NPCs.AstrumDeus.AstrumDeusHead>(new float[] { 3, 0, 0, 0 });
        public static void CalamityOnKillProfanedGuardians() => CalamityOnKill<CalamityMod.NPCs.ProfanedGuardians.ProfanedGuardianCommander>();
        public static void CalamityOnKillTheDragonfolly() => CalamityOnKill<CalamityMod.NPCs.Bumblebirb.Dragonfolly>();
        public static void CalamityOnKillProvidenceTheProfanedGoddess() => CalamityOnKill<CalamityMod.NPCs.Providence.Providence>();
        public static void CalamityOnKillStormWeaver() => CalamityOnKill<CalamityMod.NPCs.StormWeaver.StormWeaverHead>();
        public static void CalamityOnKillCeaselessVoid() => CalamityOnKill<CalamityMod.NPCs.CeaselessVoid.CeaselessVoid>();
        public static void CalamityOnKillSignusEnvoyOfTheDevourer() => CalamityOnKill<CalamityMod.NPCs.Signus.Signus>();
        public static void CalamityOnKillPolterghast() => CalamityOnKill<CalamityMod.NPCs.Polterghast.Polterghast>();
        public static void CalamityOnKillMauler() => CalamityOnKill<CalamityMod.NPCs.AcidRain.Mauler>();
        public static void CalamityOnKillNuclearTerror() => CalamityOnKill<CalamityMod.NPCs.AcidRain.NuclearTerror>();
        public static void CalamityOnKillTheOldDuke() => CalamityOnKill<CalamityMod.NPCs.OldDuke.OldDuke>();
        public static void CalamityOnKillTheDevourerOfGods() => CalamityOnKill<CalamityMod.NPCs.DevourerofGods.DevourerofGodsHead>();
        public static void CalamityOnKillYharonDragonOfRebirth() => CalamityOnKill<CalamityMod.NPCs.Yharon.Yharon>();
        public static void CalamityOnKillSupremeWitchCalamitas() => CalamityOnKill<CalamityMod.NPCs.SupremeCalamitas.SupremeCalamitas>();
        public static void SpawnHardOres()
        {
            if (!CalamityMod.CalamityServerConfig.Instance.EarlyHardmodeProgressionRework) return;
            CalamityMod.CalamityUtils.SpawnOre(107, 0.00012, 0.45f, 0.7f, 3, 8, Array.Empty<int>());
            CalamityMod.CalamityUtils.SpawnOre(221, 0.00012, 0.45f, 0.7f, 3, 8, Array.Empty<int>());
        }

        public static void SpawnMechOres()
        {
            if (!CalamityMod.CalamityServerConfig.Instance.EarlyHardmodeProgressionRework) return;
            typeof(CalamityMod.NPCs.CalamityGlobalNPC).GetMethod("SpawnMechBossHardmodeOres", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(new CalamityMod.NPCs.CalamityGlobalNPC(), null);
        }

        // I'm using an int because I'm a hater
        public static bool AreExosDead(int thisExoIsDead)
        {
            return (thisExoIsDead == 0 || !(CalamityGlobalNPC.draedonExoMechPrime != -1 && Main.npc[CalamityGlobalNPC.draedonExoMechPrime].active)) && (thisExoIsDead == 1 || !(CalamityGlobalNPC.draedonExoMechTwinGreen != -1 && Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen].active)) && (thisExoIsDead == 2 || !(CalamityGlobalNPC.draedonExoMechWorm != -1 && Main.npc[CalamityGlobalNPC.draedonExoMechWorm].active));
        }

        public static void HandleBossRush(NPC npc)
        {
            if (BossRushEvent.BossRushActive) typeof(BossRushEvent).GetMethod("OnBossKill", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { npc, ModContent.GetInstance<CalamityMod.CalamityMod>() });
        }
    }
}
