using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;
using MyExtensions;
using Newtonsoft.Json.Linq;
using SeldomArchipelago.HardmodeItem;
using SeldomArchipelago.Locking;
using SeldomArchipelago;
using SeldomArchipelago.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.GameContent.Generation;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Social;
using Terraria.WorldBuilding;
using CalamityMod.NPCs.VanillaNPCAIOverrides.Bosses;
using CalamityMod.NPCs.Yharon;
using CalamityMod.Projectiles.Ranged;
using System.Security.Policy;

using SeldomArchipelago.ArchipelagoItem;
using Microsoft.Xna.Framework.Content;
using static SeldomArchipelago.Systems.ArchipelagoSystem.WorldState;
using System.Runtime.Intrinsics.Arm;
using Steamworks;
using System.Resources;
using System.Collections.Immutable;
using System.Diagnostics.Metrics;
using Microsoft.Build.Tasks;

namespace SeldomArchipelago.Systems
{
    public class ArchipelagoSystem : ModSystem

    {
        public enum FlagID
        {
            Hook,
            Rain,
            Wind,
            Forest,
            Snow,
            Desert,
            Jungle,
            JungleUpgrade,
            Ocean,
            Sky,
            Evil,
            Dungeon,
            DungeonUpgrade,
            Mushroom,
            Marble,
            Granite,
            Web,
            Underworld,
            BloodMoon,
            Dryad,
            Tavernkeep,
            Meteor,
            GoblinArmy,
            GoblinTinkerer,
            WitchDoctor,
            Clothier,
            Hardmode,
            Wizard,
            PirateInvasion,
            Pirate,
            Eclipse,
            EclipseUpgrade,
            Steampunker,
            Cyborg,
            PrismaticLacewing,
            Temple,
            PumpkinMoon,
            FrostMoon,
            Martians,
            Cultists,
            SantaClaus
        }

        // System that stores, manages, and processes flags
        public class FlagSystem : TagSerializable
        {
            #region Instance Data
            public static readonly Func<TagCompound, FlagSystem> DESERIALIZER = Load;

            private HashSet<FlagID> activeFlags = new HashSet<FlagID>() { FlagID.Forest };
            private List<int> ActiveIntFlags
            {
                get => (from id in activeFlags select (int)id).ToList();
            }
            public TagCompound SerializeData()
            {
                return new TagCompound
                {
                    ["activeFlags"] = ActiveIntFlags
                };
            }
            public static FlagSystem Load(TagCompound tag)
            {
                FlagSystem flagSystem = new();
                if (tag.ContainsKey("activeFlags"))
                {
                    foreach (int i in tag.GetList<int>("activeFlags")) flagSystem.activeFlags.Add((FlagID)i);
                }
                flagSystem.activeFlags.Add(FlagID.Forest);
                return flagSystem;
            }
            #endregion
            #region Activation Methods
            public Flag.ActivateResult UnlockFlag(string flagName, bool safeEvent, bool safeHardmode)
            {
                Flag flag = FlagSystemDatabase.locToFlag[flagName];
                bool safeUnlock = (safeEvent && flagName != "Hardmode") || (safeHardmode && flagName == "Hardmode");
                return flag.ActivateFlag(activeFlags, safeUnlock);
            }
            public void UnlockBiomesNormally() => activeFlags.UnionWith(FlagSystemDatabase.biomeFlags);
            public void UnlockWeatherNormally() => activeFlags.UnionWith(FlagSystemDatabase.weatherFlags);
            public void UnlockHookNormally() => activeFlags.Add(FlagID.Hook);
            #endregion
            #region Database Methods
            private static void GiveItem(int id) => SessionState.GiveItem(id);

            public static FlagID? GetChestRegion(int i, int j)
            {
                Terraria.Tile chest = Main.tile[i, j];
                int id = chest.IDChest();
                return FlagSystemDatabase.BiomeChestSet.UseAsDict(id);
            }
            public static void InitializeBannerSet()
            {
                FlagSystemDatabase.FlagNPCBannerSet = new (FlagID, int[])[FlagSystemDatabase.FlagNPCSet.Length];
                for (int i = 0; i < FlagSystemDatabase.FlagNPCBannerSet.Length; i++)
                {
                    (FlagID, int[]) tuple = FlagSystemDatabase.FlagNPCSet[i];
                    int[] bannerIDset = (from id in tuple.Item2 select Item.NPCtoBanner(id)).ToArray();
                    FlagSystemDatabase.FlagNPCBannerSet[i] = (tuple.Item1, bannerIDset);
                }
                FlagSystemDatabase.FlagNPCSet = null;
            }
            public static FlagID? GetNPCRegion(int npc) => FlagSystemDatabase.FlagNPCBannerSet.UseAsDict(Item.NPCtoBanner(npc));
            public static FlagID? GetNPCRegion(NPC npc) => GetNPCRegion(npc.BannerID());
            private static readonly Dictionary<int, FlagID> FreeNPCSet = new()
                {
                    {NPCID.Dryad, FlagID.Dryad },
                    {NPCID.WitchDoctor, FlagID.WitchDoctor },
                    {NPCID.Steampunker, FlagID.Steampunker },
                    {NPCID.Clothier, FlagID.Clothier },
                    {NPCID.Pirate, FlagID.Pirate },
                    {NPCID.Cyborg, FlagID.Cyborg },
                    {NPCID.SantaClaus, FlagID.SantaClaus },
                };
            #endregion
            public bool FlagIsActive(FlagID flag) => activeFlags.Contains(flag);
            public bool FlagIsActive(string flagName) => FlagIsActive(FlagSystemDatabase.locToFlag[flagName].id);
            public bool? FlagIsHardmode(string flag)
            {
                FlagID? flagID = TryGetFlag(flag);
                if (flagID is null) return null;
                return FlagSystemDatabase.hardmodeFlags.Contains((FlagID)flagID); 
            }
            public static FlagID? TryGetFlag(string flagLoc)
            {
                if (FlagSystemDatabase.locToFlag.TryGetValue(flagLoc, out Flag flag)) return flag.id;
                else return null;
            }
            public static string[] AllKeys
            {
                get { return FlagSystemDatabase.locToFlag.Keys.ToArray(); }
            }

            private List<string> HardmodeBacklog
            {
                get
                {
                    return ModContent.GetInstance<ArchipelagoSystem>().session.hardmodeBacklog;
                }
            }
            public void RedeemHardmodeBacklog()
            {
                List<string> hardmodeBacklog = HardmodeBacklog;
                foreach (string flagName in hardmodeBacklog)
                {
                    Flag.ActivateResult result = FlagSystemDatabase.locToFlag[flagName].ActivateFlag(activeFlags, true);
                }
                hardmodeBacklog.Clear();
            }
            #region General Checks
            public bool PlayerBiomeUnlocked(Player player)
            {
                bool[] biomeList = {
                    player.ZoneForest &&                            !FlagIsActive(FlagID.Forest),
                    player.ZoneSnow &&                              !FlagIsActive(FlagID.Snow),
                    player.ZoneDesert &&                            !FlagIsActive(FlagID.Desert),
                    player.ZoneJungle &&                            !FlagIsActive(FlagID.Jungle),
                    player.ZoneBeach &&                             !FlagIsActive(FlagID.Ocean),
                    player.ZoneSkyHeight &&                         !FlagIsActive(FlagID.Sky),
                    player.ZoneUnderworldHeight &&                  !FlagIsActive(FlagID.Underworld),
                    (player.ZoneCorrupt || player.ZoneCrimson) &&   !FlagIsActive(FlagID.Evil),
                    player.ZoneGlowshroom &&                        !FlagIsActive(FlagID.Mushroom),
                };
                return !biomeList.Any(p => p);
            }
            public bool IllegalDepth(int j) => j >= Main.UnderworldLayer && !FlagIsActive(FlagID.Underworld);
            #endregion
            #region Tile Checks
            

            public bool TileRegionUnlocked(int i, int j, Player player = null)
            {
                if (IllegalDepth(j)) return false;
                int id = ItemRef.IDTree(i, j);
                if (id != -1)
                {
                    FlagID? biome = FlagSystemDatabase.BiomeTreeSet.UseAsDict(id);
                    return biome is null || FlagIsActive((FlagID)biome);
                }

                id = ItemRef.IDHerb(i, j);
                if (id != -1)
                {
                    return FlagIsActive(FlagSystemDatabase.BiomeHerbSet[id]);
                }

                id = Main.tile[i, j].TileType;
                FlagID? tileBiome = FlagSystemDatabase.OtherTiles.UseAsDict(id);
                if (tileBiome is not null)
                {
                    return FlagIsActive((FlagID)tileBiome);
                }

                return player is null ? true : PlayerBiomeUnlocked(player);
            }
            public bool KillTileRegionUnlocked(int i, int j, bool computeDepth = false)
            {
                if (Main.gameMenu) return true;
                if (computeDepth && IllegalDepth(j)) return false;
                int type = Main.tile[i, j].TileType;
                FlagID? tileBiome = FlagSystemDatabase.OtherTiles.UseAsDict(type);
                if (tileBiome is not null)
                {
                    return FlagIsActive((FlagID)tileBiome);
                }
                return true;
            }
            #endregion
            #region Chest Checks
            
            public bool ChestRegionUnlocked(int i, int j)
            {
                if (IllegalDepth(j)) return false;
                FlagID? biome = GetChestRegion(i, j);
                return biome is null || FlagIsActive((FlagID)biome);
            }
            public static void UpdateChests()
            {
                var chestList = from chest in Main.chest
                                where chest != null
                                select chest;
                foreach (Chest chest in chestList)
                {
                    int i = chest.x;
                    int j = chest.y;
                    int blockUnderChestType = Main.tile[i, j + 2].TileType;
                    FlagID? blockBiome;
                    if (blockUnderChestType == TileID.Mud)
                    {
                        blockBiome = FlagID.Jungle; //Hardcoding Mud because making it a jungle-locked block is not advisable.
                    }
                    else
                    {
                        blockBiome = FlagSystemDatabase.OtherTiles.UseAsDict(blockUnderChestType);
                    }

                    if (blockBiome != FlagID.Forest && blockBiome is not null && Main.tile[i, j].IDChest() == 0) foreach ((FlagID, int[]) chestTuple in FlagSystemDatabase.BiomeChestSet)
                        {
                            if (chestTuple.Item1 == blockBiome) ItemRef.ChangeChest(i, j, chestTuple.Item2[0]);
                        }
                    FlagID? chestBiome = GetChestRegion(i, j);

                    if (chestBiome is FlagID notNullBiome)
                    {
                        int oldItem = chest.item[0].type;
                        int[] oldItemSet = ItemRef.GetItemSet(oldItem);
                        chest.item = chest.item.OffsetInventory(oldItemSet.Length, 1);

                        chest.item[0].SetDefaults(ModContent.ItemType<ArchipelagoItem.ArchipelagoItem>());
                        var archItem = (ArchipelagoItem.ArchipelagoItem)chest.item[0].ModItem;
                        archItem.SetCheckType(LocationSystem.GetChestName(notNullBiome));
                    }
                }
            }

            public int[] GetLegalHerbs()
            {
                List<int> herbList = new List<int>();
                foreach ((FlagID, int[]) tuple in FlagSystemDatabase.BiomeHerbItemIDSet)
                {
                    FlagID flag = tuple.Item1;
                    if (FlagIsActive(flag)) herbList.AddRange(tuple.Item2);
                }
                return herbList.ToArray();
            }
            #endregion
            #region NPC Checks

            public bool NPCRegionUnlocked(DropAttemptInfo info) => NPCRegionUnlocked(info.npc);
            public bool NPCRegionUnlocked(NPC npc)
            {
                FlagID? biome = GetNPCRegion(npc);
                return biome is null || FlagIsActive((FlagID)biome);
            }
            #endregion
            #region Bound NPC Spawn Checks (includes martian probe and prismatic lacewing)
            public bool NPCShouldDespawn(int id)
            {
                switch (id)
                {
                    case NPCID.SleepingAngler: return !FlagIsActive(FlagID.Ocean);
                    case NPCID.BoundGoblin: return !FlagIsActive(FlagID.GoblinTinkerer);
                    case NPCID.WebbedStylist: return !FlagIsActive(FlagID.Web);
                    case NPCID.BoundWizard: return !FlagIsActive(FlagID.Wizard);
                    case NPCID.BartenderUnconscious: return !FlagIsActive(FlagID.Tavernkeep);
                    case NPCID.MartianProbe: return !FlagIsActive(FlagID.Martians);
                    case NPCID.EmpressButterfly: return !FlagIsActive(FlagID.PrismaticLacewing);
                    default: return false;
                }
            }
            public bool BoundNPCFindable(int id, NPCSpawnInfo info = default)
            {
                if (!NPC.AnyNPCs(id) && info.Water)
                {
                    switch (id)
                    {
                        case NPCID.SleepingAngler: return (info.Player.ZoneBeach && !NPC.savedAngler && FlagIsActive(FlagID.Ocean));
                        case NPCID.BoundGoblin: return (info.Player.ZoneRockLayerHeight && !NPC.savedGoblin && FlagIsActive(FlagID.GoblinTinkerer));
                        case NPCID.BoundWizard: return (info.Player.ZoneRockLayerHeight && !NPC.savedWizard && FlagIsActive(FlagID.Wizard));
                        case NPCID.BartenderUnconscious: return (!NPC.savedBartender && FlagIsActive(FlagID.Tavernkeep));
                        case NPCID.MartianProbe: return (info.Player.ZoneSkyHeight && Math.Abs(info.Player.position.X - Main.spawnTileX) > Main.maxTilesX / 3 && FlagIsActive(FlagID.Martians));
                        case NPCID.EmpressButterfly: return (info.Player.ZoneHallow && info.Player.ZoneOverworldHeight && FlagIsActive(FlagID.PrismaticLacewing));
                        default: return false;
                    }
                    throw new Exception($"Didn't expect to check {id} for being a Bound NPC.");
                }
                return false;
            }
            public void SetBoundNPCsInSpawnDict(IDictionary<int, float> dict, NPCSpawnInfo info)
            {
                foreach (int id in FlagSystemDatabase.BoundNPCSet)
                {
                    dict[id] = BoundNPCFindable(id, info) ? 1f : 0f;
                }
            }

            public bool FreeNPCSpawnable(int npcID) => FlagIsActive(FreeNPCSet[npcID]) && !NPC.AnyNPCs(npcID);
            #endregion
            #region Item Checks
            public bool ItemIsUsable(int id)
            {
                switch (id)
                {
                    case ItemID.SolarTable: return FlagIsActive(FlagID.Eclipse);
                    case ItemID.BloodMoonStarter: return FlagIsActive(FlagID.BloodMoon);
                    case ItemID.GoblinBattleStandard: return FlagIsActive(FlagID.GoblinArmy);
                    case ItemID.PirateMap: return FlagIsActive(FlagID.PirateInvasion);
                    case ItemID.PumpkinMoonMedallion: return FlagIsActive(FlagID.PumpkinMoon);
                    case ItemID.NaughtyPresent: return FlagIsActive(FlagID.FrostMoon);
                    default: return true;
                }
            }
            #endregion
            public class Flag
            {
                public enum ActivateResult
                {
                    Activated,
                    ActivateOnHardmode
                }
                #region Instance Data & Methods
                public Action<bool> SideEffects;
                public Flag nestedFlag;
                public FlagID id;
                private string FlagName => Enum.GetName(typeof(Flag), id);
                // Handles the flag becoming active, which we differentiate from unlocking (or receiving) the flag
                public ActivateResult ActivateFlag(HashSet<FlagID> flagSet, bool unlockSafely)
                {
                    if (!flagSet.Contains(FlagID.Hardmode) && FlagSystemDatabase.hardmodeFlags.Contains(id)) return ActivateResult.ActivateOnHardmode;
                    if (flagSet.Contains(id) && nestedFlag is null)
                    {
                        throw new Exception($"Tried to activate flag {FlagName}, but it was already activated and had no progressive flag.");
                    }
                    else if (flagSet.Contains(id))
                    {
                        return nestedFlag.ActivateFlag(flagSet, unlockSafely);
                    }
                    if (SideEffects is not null) SideEffects(unlockSafely);
                    flagSet.Add(id);
                    return ActivateResult.Activated;
                }

                public Flag(FlagID theID, Action<bool> theSideEffects = null, Flag theNestedFlag = null)
                {
                    SideEffects = theSideEffects;
                    nestedFlag = theNestedFlag;
                    id = theID;
                }
                #endregion
            }
            private static class FlagSystemDatabase
            {
                public static readonly Dictionary<string, Flag> locToFlag = new Dictionary<string, Flag>()
                {
                    {"Grappling Hook",          new Flag(FlagID.Hook, theSideEffects: delegate(bool safe)
                    {
                        GiveItem(ItemID.GrapplingHook);
                    }
                    )},
                    {"Rain",                    new Flag(FlagID.Rain) },
                    {"Wind",                    new Flag(FlagID.Wind) },
                    {"Snow Biome",              new Flag(FlagID.Snow) },
                    {"Desert Biome",            new Flag(FlagID.Desert) },
                    {"Progressive Jungle",      new Flag(FlagID.Jungle, theNestedFlag: new Flag(FlagID.JungleUpgrade)) },
                    {"Jungle Upgrade",          new Flag(FlagID.JungleUpgrade) },
                    {"Ocean",                   new Flag(FlagID.Ocean) },
                    {"Sky and Floating Islands",new Flag(FlagID.Sky) },
                    {"Evil Biome",              new Flag(FlagID.Evil) },
                    {"Progressive Dungeon",     new Flag(FlagID.Dungeon, theSideEffects: delegate(bool safe)
                    {
                        NPC.downedBoss3 = true;
                    }, theNestedFlag: new Flag(FlagID.DungeonUpgrade, theSideEffects: delegate(bool safe)
                    {
                        NPC.downedPlantBoss = true;
                    }))
                    },
                    {"Mushroom Biome",          new Flag(FlagID.Mushroom) },
                    {"Marble Biome",            new Flag(FlagID.Marble) },
                    {"Granite Biome",           new Flag(FlagID.Granite) },
                    {"Spider Nest",             new Flag(FlagID.Web) },
                    {"Underworld",              new Flag(FlagID.Underworld, theSideEffects: delegate(bool safe)
                    {
                        var chestList = from chest in Main.chest
                                        where chest != null && Main.tile[chest.x, chest.y].IDChest() == 4
                                        select chest;
                        foreach (Chest chest in chestList) Chest.Unlock(chest.x, chest.y);
                    }) },
                    {"Blood Moon",              new Flag(FlagID.BloodMoon, theSideEffects: delegate(bool safe)
                    {
                        if (safe)
                        {
                            GiveItem(ItemID.BloodMoonStarter);
                        }
                        else
                        {
                            ModContent.GetInstance<SeldomArchipelago>().guaranteeBloodMoon = true;
                        }
                    }) },
                    {"Dryad",                   new Flag(FlagID.Dryad) },
                    {"Tavernkeep",              new Flag(FlagID.Tavernkeep) },
                    {"Meteor",                  new Flag(FlagID.Meteor, theSideEffects: delegate(bool safe)
                    {
                        WorldGen.dropMeteor();
                    }) },
                    {"Goblin Army",             new Flag(FlagID.GoblinArmy, theSideEffects: delegate(bool safe)
                    {
                        if (safe)
                        {
                            GiveItem(ItemID.GoblinBattleStandard);
                        }
                        else
                        {
                            InvasionLock.invasionList.Add(1);
                        }
                    }) },
                    {"Goblin Tinkerer",         new Flag(FlagID.GoblinTinkerer) },
                    {"Witch Doctor",            new Flag(FlagID.WitchDoctor) },
                    {"Clothier",                new Flag(FlagID.Clothier) },
                    {"Hardmode",                new Flag(FlagID.Hardmode, theSideEffects: delegate(bool safe)
                    {
                        if (safe)
                        {
                            GiveItem(ModContent.ItemType<HardmodeStarter>());
                        }
                        else
                        {
                            WorldGen.StartHardmode();
                        }
                    }) },
                    {"Wizard",                  new Flag(FlagID.Wizard) },
                    {"Pirate Invasion",         new Flag(FlagID.PirateInvasion, theSideEffects: delegate(bool safe)
                    {
                        if (safe)
                        {
                            GiveItem(ItemID.PirateMap);
                        }
                        else
                        {
                            InvasionLock.invasionList.Add(3);
                        }
                    }) },
                    {"Pirate",                  new Flag(FlagID.Pirate) },
                    {"Progressive Eclipse",     new Flag(FlagID.Eclipse, theSideEffects: delegate(bool safe)
                    {
                        if (safe)
                        {
                            GiveItem(ItemID.SolarTablet);
                        } else
                        {
                            ModContent.GetInstance<SeldomArchipelago>().guaranteeEclipse = true;
                        }
                    }, theNestedFlag: new Flag(FlagID.EclipseUpgrade, theSideEffects: delegate(bool safe)
                    {
                        if (!safe) ModContent.GetInstance<SeldomArchipelago>().guaranteeEclipse = true;
                    })) },
                    {"Steampunker",             new Flag(FlagID.Steampunker) },
                    {"Cyborg",                  new Flag(FlagID.Cyborg) },
                    {"Temple",                  new Flag(FlagID.Temple, theSideEffects: delegate(bool safe)
                    {
                        GiveItem(ItemID.TempleKey);
                    }) },
                    {"Pumpkin Moon Medallion",  new Flag(FlagID.PumpkinMoon, theSideEffects: delegate(bool safe)
                    {
                        GiveItem(ItemID.PumpkinMoonMedallion);
                    }) },
                    {"Naughty Present",         new Flag(FlagID.FrostMoon, theSideEffects: delegate(bool safe)
                    {
                        GiveItem(ItemID.NaughtyPresent);
                    }) },
                    {"Martian Madness",         new Flag(FlagID.Martians, theSideEffects: delegate(bool safe)
                    {
                        if (!safe) InvasionLock.invasionList.Add(4);
                    }) },
                    {"Cultists",                new Flag(FlagID.Cultists, theSideEffects: delegate(bool safe)
                    {
                        NPC.downedGolemBoss = true;
                    }) },
                };
                public static readonly FlagID[] hardmodeFlags = [
                    FlagID.Wizard,
                    FlagID.PirateInvasion,
                    FlagID.Pirate,
                    FlagID.Eclipse,
                    FlagID.JungleUpgrade,
                    FlagID.DungeonUpgrade,
                    FlagID.EclipseUpgrade,
                    FlagID.Steampunker,
                    FlagID.Cyborg,
                    FlagID.Temple,
                    FlagID.PumpkinMoon,
                    FlagID.FrostMoon,
                    FlagID.Martians,
                    FlagID.Cultists
                ];
                public static readonly FlagID[] biomeFlags = [
                    FlagID.Desert,
                    FlagID.Snow,
                    FlagID.Underworld,
                    FlagID.Jungle,
                    FlagID.Mushroom,
                    FlagID.Marble,
                    FlagID.Granite,
                    FlagID.Web,
                    FlagID.Ocean,
                    FlagID.Evil
                ];
                public static readonly FlagID[] weatherFlags = [
                    FlagID.Rain,
                FlagID.Wind
                    ];
                public static readonly (FlagID, int[])[] OtherTiles =
    [
            (FlagID.Forest, new int[] {TileID.Plants, TileID.Iron, TileID.Copper, TileID.Gold, TileID.Silver,
                                        TileID.Tin, TileID.Lead, TileID.Tungsten, TileID.Platinum,
                                        TileID.ExposedGems, TileID.Sapphire, TileID.Ruby, TileID.Emerald, TileID.Topaz, TileID.Amethyst, TileID.Diamond}),
                    (FlagID.Snow, new int[] {  TileID.SnowBlock, TileID.IceBlock, TileID.HallowedIce, TileID.CorruptIce, TileID.FleshIce}),
                    (FlagID.Evil, new int[] {  TileID.ShadowOrbs, TileID.CorruptPlants, TileID.CrimsonPlants, TileID.Demonite, TileID.Crimtane, TileID.Ebonstone, TileID.Crimstone, TileID.CorruptGrass, TileID.CrimsonGrass,
                                        TileID.Ebonsand, TileID.Crimsand, TileID.CorruptSandstone, TileID.CrimsonSandstone,
                                        TileID.CorruptIce, TileID.FleshIce}),
                    (FlagID.Dungeon, new int[] {TileID.Books}),
                    (FlagID.Desert, new int[] {    TileID.AmberStoneBlock,
                                            TileID.HardenedSand, TileID.CorruptHardenedSand, TileID.CrimsonHardenedSand, TileID.HallowHardenedSand,
                                            TileID.Sandstone, TileID.CorruptSandstone, TileID.CrimsonSandstone, TileID.HallowSandstone}),
                    (FlagID.Jungle, new int[] {    TileID.Hive, TileID.Larva, TileID.JunglePlants, TileID.JunglePlants2, TileID.JungleGrass, TileID.RichMahogany}),
                    (FlagID.JungleUpgrade, new int[] {TileID.Chlorophyte}),
                    (FlagID.Mushroom, new int[] {  TileID.MushroomBlock, TileID.MushroomGrass, TileID.MushroomPlants}),
                    (FlagID.Marble, new int[] { TileID.Marble}),
                    (FlagID.Granite, new int[] { TileID.Granite})
    ];
                public static readonly (FlagID, int[])[] BiomeTreeSet =
                {
                    (FlagID.Forest, new int[]         {ItemRef.ForestTree, ItemRef.SakuraTree, ItemRef.YellowWillowTree,
                                                       ItemRef.TopazTree, ItemRef.AmethystTree, ItemRef.SapphireTree, ItemRef.EmeraldTree, ItemRef.RubyTree, ItemRef.DiamondTree, ItemRef.AmberTree}),
                    (FlagID.Snow, new int[]            {ItemRef.SnowTree}),
                    (FlagID.Desert, new int[]          {ItemRef.Cactus}),
                    (FlagID.Jungle, new int[]          {ItemRef.JungleTree, ItemRef.UndergroundJungleTree}),
                    (FlagID.Ocean, new int[]   {ItemRef.PalmTree}),
                    (FlagID.Mushroom, new int[]        {ItemRef.GiantGlowingMushroom, ItemRef.GiantGlowingMushroomSurface}),
                    (FlagID.Underworld, new int[]            {ItemRef.AshTree}),
                    (FlagID.Evil, new int[]            {ItemRef.CorruptTree, ItemRef.CrimsonTree})
                };
                public static readonly Dictionary<int, FlagID> BiomeHerbSet = new Dictionary<int, FlagID>()
                {
                    {0, FlagID.Forest},
                    {1, FlagID.Jungle},
                    {2, FlagID.Forest},
                    {3, FlagID.Evil},
                    {4, FlagID.Desert},
                    {5, FlagID.Underworld},
                    {6, FlagID.Snow},
                };
                public static readonly (FlagID, int[])[] BiomeChestSet = {
                    (FlagID.Forest, new int[] {0, 12, 1, 56}),
                    (FlagID.Granite, new int [] {50}),
                    (FlagID.Marble, new int[] {51}),
                    (FlagID.Web, new int[] {15}),
                    (FlagID.Snow, new int[] {11}),
                    (FlagID.Desert, new int[] {62, 69}),
                    (FlagID.Jungle, new int[] {10, 8}),
                    (FlagID.Ocean, new int[] {17}),
                    (FlagID.Sky, new int[] {13}),
                    (FlagID.Mushroom, new int[] {32}),
                    (FlagID.Dungeon, new int[] {2}),
                    (FlagID.Underworld, new int[] {4}),
                };
                public static readonly int[] BoundNPCSet =
    {
                NPCID.SleepingAngler,
                NPCID.BoundGoblin,
                NPCID.WebbedStylist,
                NPCID.BoundWizard,
                NPCID.BartenderUnconscious,
                NPCID.MartianProbe,
                NPCID.EmpressButterfly,
                };
                public static readonly (FlagID, int[])[] BiomeHerbItemIDSet =
[
(FlagID.Forest, [ItemID.Daybloom, ItemID.DaybloomSeeds, ItemID.Blinkroot, ItemID.BlinkrootSeeds] ),
                (FlagID.Jungle, [ItemID.Moonglow, ItemID.MoonglowSeeds] ),
                (FlagID.Evil, [ItemID.Deathweed, ItemID.DeathweedSeeds] ),
                (FlagID.Desert, [ItemID.Waterleaf, ItemID.WaterleafSeeds] ),
                (FlagID.Underworld, [ItemID.Fireblossom, ItemID.FireblossomSeeds] ),
                (FlagID.Snow, [ItemID.Shiverthorn, ItemID.ShiverthornSeeds] ),
                ];
                public static (FlagID, int[])[] FlagNPCSet = [
                        (FlagID.Forest,new int[]   {NPCID.GreenSlime, NPCID.BlueSlime, NPCID.PurpleSlime, NPCID.Pinky, NPCID.Zombie, NPCID.DemonEye, NPCID.Raven, NPCID.GoblinScout, NPCID.KingSlime, NPCID.PossessedArmor, NPCID.WanderingEye, NPCID.Wraith, NPCID.Werewolf, NPCID.HoppinJack,
                                                NPCID.GiantWormHead, NPCID.RedSlime, NPCID.YellowSlime, NPCID.DiggerHead, NPCID.ToxicSludge,
                                                NPCID.BlackSlime, NPCID.MotherSlime, NPCID.BabySlime, NPCID.Skeleton, NPCID.CaveBat, NPCID.Salamander, NPCID.Crawdad, NPCID.GiantShelly, NPCID.UndeadMiner, NPCID.Tim, NPCID.Nymph, NPCID.CochinealBeetle,
                                                NPCID.Mimic, NPCID.ArmoredSkeleton, NPCID.GiantBat, NPCID.RockGolem, NPCID.SkeletonArcher, NPCID.RuneWizard}),
                    (FlagID.Marble, new int[]  {NPCID.GreekSkeleton, NPCID.Medusa}),
                    (FlagID.Granite, new int[] {NPCID.GraniteFlyer, NPCID.GraniteGolem}),
                    (FlagID.Web, new int[]     {NPCID.BlackRecluse, NPCID.WallCreeper}),
                    (FlagID.Snow,  new int[]   {NPCID.IceSlime, NPCID.ZombieEskimo, NPCID.CorruptPenguin, NPCID.CrimsonPenguin, NPCID.IceElemental, NPCID.Wolf, NPCID.IceGolem,
                                                NPCID.IceBat, NPCID.SnowFlinx, NPCID.SpikedIceSlime, NPCID.UndeadViking, NPCID.CyanBeetle, NPCID.ArmoredViking, NPCID.IceTortoise, NPCID.IceElemental, NPCID.IcyMerman, NPCID.IceMimic, NPCID.PigronCorruption, NPCID.PigronCrimson, NPCID.PigronHallow}),
                    (FlagID.Desert, new int[]  {NPCID.Vulture, NPCID.Antlion, NPCID.Mummy, NPCID.LightMummy, NPCID.DarkMummy, NPCID.BloodMummy,
                                                NPCID.Tumbleweed, NPCID.SandElemental, NPCID.SandShark, NPCID.SandsharkCorrupt, NPCID.SandsharkCrimson, NPCID.SandsharkHallow,
                                                NPCID.WalkingAntlion, NPCID.LarvaeAntlion, NPCID.FlyingAntlion, NPCID.GiantWalkingAntlion, NPCID.GiantFlyingAntlion, NPCID.SandSlime, NPCID.TombCrawlerHead,
                                                NPCID.DesertBeast, NPCID.DesertScorpionWalk, NPCID.DesertLamiaLight, NPCID.DesertLamiaDark, NPCID.DuneSplicerHead, NPCID.DesertGhoul, NPCID.DesertGhoulCorruption, NPCID.DesertGhoulCrimson, NPCID.DesertGhoulHallow, NPCID.DesertDjinn}),
                    (FlagID.Jungle, new int[]  {NPCID.JungleSlime, NPCID.JungleBat, NPCID.Snatcher, NPCID.DoctorBones, NPCID.Derpling, NPCID.GiantTortoise, NPCID.GiantFlyingFox, NPCID.Arapaima, NPCID.AngryTrapper,
                                                NPCID.Hornet, NPCID.ManEater, NPCID.SpikedJungleSlime, NPCID.LacBeetle, NPCID.JungleCreeper, NPCID.Moth, NPCID.MossHornet}),
                    (FlagID.Ocean, new int[]   {NPCID.BlueJellyfish, NPCID.PinkJellyfish, NPCID.GreenJellyfish, NPCID.Piranha, NPCID.AnglerFish, NPCID.Crab, NPCID.Squid, NPCID.SeaSnail, NPCID.Shark}),
                    (FlagID.Sky,   new int[]   {NPCID.Harpy, NPCID.WyvernHead}),
                    (FlagID.Underworld,new int[]{NPCID.Hellbat, NPCID.LavaSlime, NPCID.FireImp, NPCID.Demon, NPCID.VoodooDemon, NPCID.BoneSerpentHead, NPCID.Lavabat, NPCID.RedDevil}),
                    (FlagID.Evil,  new int[]   {NPCID.EaterofSouls, NPCID.CorruptGoldfish, NPCID.DevourerHead, NPCID.Corruptor, NPCID.CorruptSlime, NPCID.Slimeling, NPCID.Slimer, NPCID.Slimer2, NPCID.SeekerHead, NPCID.DarkMummy,
                                                NPCID.CursedHammer, NPCID.Clinger, NPCID.BigMimicCorruption, NPCID.DesertGhoulCorruption, NPCID.PigronCorruption,
                                                NPCID.BloodCrawler, NPCID.CrimsonGoldfish, NPCID.FaceMonster, NPCID.Crimera, NPCID.Herpling, NPCID.Crimslime, NPCID.BloodJelly, NPCID.BloodFeeder, NPCID.BloodMummy,
                                                NPCID.CrimsonAxe, NPCID.IchorSticker, NPCID.FloatyGross, NPCID.BigMimicCrimson, NPCID.DesertGhoulCrimson, NPCID.PigronCrimson}),
                    (FlagID.Mushroom, new int[] {NPCID.AnomuraFungus, NPCID.FungiBulb, NPCID.MushiLadybug, NPCID.SporeBat, NPCID.SporeSkeleton, NPCID.ZombieMushroom,
                                                NPCID.FungoFish, NPCID.GiantFungiBulb})
                    ];
                public static (FlagID, int[])[] FlagNPCBannerSet;
            }
        }

        // Data that's reset and specific between worlds
        public class WorldState : TagSerializable
        {
            public static readonly Func<TagCompound, WorldState> DESERIALIZER = Load;            // Achievements can be completed while loading into the world, but those complete before
            // `ArchipelagoPlayer::OnEnterWorld`, where achievements are reset, is run. So, this
            // keeps track of which achievements have been completed since `OnWorldLoad` was run, so
            // `ArchipelagoPlayer` knows not to clear them.
            public List<string> achieved = new List<string>();
            //Whether this world has had its chests randomized.
            public bool chestsRandomized = false;
            public TagCompound SerializeData()
            {
                return new TagCompound
                {
                    ["achieved"] = achieved,
                    ["chestsRandomized"] = chestsRandomized
                };
            }
            public static WorldState Load(TagCompound tag)
            {
                WorldState state = new WorldState();
                state.achieved = tag.GetList<string>("achieved").ToList();
                state.chestsRandomized = tag.GetBool("chestsRandomized");
                return state;
            }

        }

        // SessionState data that's saved to a world in case of offline play
        public class SessionMemory : TagSerializable
        {
            public static readonly Func<TagCompound, SessionMemory> DESERIALIZER = Load;
            public string seedName = "";
            public string slotName = "";
            public bool HasActiveMemory => seedName is not null && slotName is not null;
            public FlagSystem flagSystem = new();
            // Stores locations that were collected before Archipelago is started so they can be
            // queued once it's started
            public List<string> locationBacklog = new List<string>();
            // Multiple lists that contain the names of the items within enumerable location groups.
            // Indexed by base location names. The tuple at index zero of each list is the next one to be retrieved.
            // Item1 is the name of a location, and Item2 is the full name of the item at said location.
            public Dictionary<string, List<(string, string)>> locGroupRewardNames = new();
            // Number of items the player has collected in this world
            public int collectedItems = 0;
            // List of rewards received in this world, so they don't get reapplied. Saved in the
            // Terraria world instead of Archipelago data in case the player is, for example,
            // playing Hardcore and wants to receive all the rewards again when making a new player/
            // world.
            public List<int> receivedRewards = new List<int>();
            // Enemy names to kills required to cash in a check
            public Dictionary<string, int> enemyToKillCount = new Dictionary<string, int>();
            // Whether or not an enemy is a location
            public bool ArchipelagoEnemy(string name) => enemyToKillCount.TryGetValue(LocationSystem.GetNPCLocKey(name), out var count);
            // All Enemy-specific Items
            public HashSet<string> enemyItems = new();
            // All Enemy 
            // Backlog of hardmode-only items to be cashed in once Hardmode activates.
            public List<string> hardmodeBacklog = new();
            // Whether chests should be randomized.
            public bool randomizeChests = false;
            public TagCompound SerializeData()
            {
                TagCompound tag = new TagCompound
                {
                    [nameof(seedName)] = seedName,
                    [nameof(slotName)] = slotName,
                    [nameof(flagSystem)] = flagSystem,
                    [nameof(locationBacklog)] = locationBacklog,
                    [nameof(collectedItems)] = collectedItems,
                    [nameof(receivedRewards)] = receivedRewards,
                    [nameof(enemyToKillCount) + "Keys"] = enemyToKillCount.Keys.ToList(),
                    [nameof(enemyToKillCount) + "Values"] = enemyToKillCount.Values.ToList(),
                    [nameof(enemyItems)] = enemyItems.ToList(),
                    [nameof(hardmodeBacklog)] = hardmodeBacklog,
                    [nameof(randomizeChests)] = randomizeChests,
                    [nameof(locGroupRewardNames) + "Keys"] = locGroupRewardNames.Keys.ToList(),
                };
                foreach ((string key, List<(string, string)> values) in locGroupRewardNames)
                {
                    tag[key + "1"] = (from value in values select value.Item1).ToList();
                    tag[key + "2"] = (from value in values select value.Item2).ToList();
                }
                return tag;
            }
            public static SessionMemory Load(TagCompound tag) => Load(tag, null);
            public static SessionMemory Load(TagCompound tag, SessionState state = null)
            {
                SessionMemory sessionMemory = state ?? new SessionMemory();
                sessionMemory.seedName = tag.GetString(nameof(seedName));
                sessionMemory.slotName = tag.GetString(nameof(slotName));
                sessionMemory.flagSystem = tag.Get<FlagSystem>(nameof(flagSystem));
                sessionMemory.locationBacklog = tag.Get<List<string>>(nameof(locationBacklog));
                sessionMemory.collectedItems = tag.GetInt(nameof(collectedItems));
                sessionMemory.receivedRewards = tag.Get<List<int>>(nameof(receivedRewards));
                List<string> enemyKillKeys = tag.Get<List<string>>(nameof(enemyToKillCount) + "Keys");
                List<int> enemyKillValues = tag.Get<List<int>>(nameof(enemyToKillCount) + "Values");
                for (int i = 0; i < enemyKillKeys.Count; i++)
                {
                    sessionMemory.enemyToKillCount[enemyKillKeys[i]] = enemyKillValues[i];
                }
                sessionMemory.enemyItems = tag.Get<List<string>>(nameof(enemyItems)).ToHashSet();
                sessionMemory.hardmodeBacklog = tag.Get<List<string>>(nameof(hardmodeBacklog));
                sessionMemory.randomizeChests = tag.GetBool(nameof(randomizeChests));
                List<string> locKeyList = tag.Get<List<string>>(nameof(locGroupRewardNames) + "Keys");
                foreach (string key in locKeyList)
                {
                    if (!tag.ContainsKey(key + "1")) throw new Exception($"TAG ERROR: Key {key} missing the 1 value.");
                    if (!tag.ContainsKey(key + "2")) throw new Exception($"TAG ERROR: Key {key} missing the 2 value.");
                    List<string> values1 = tag.Get<List<string>>(key + "1");
                    List<string> values2 = tag.Get<List<string>>(key + "2");
                    if (values1.Count != values2.Count) throw new Exception($"TAG ERROR: Key {key} has value mismatch; List 1 has {values1.Count} items, List 2 has {values2.Count} items.");
                    List<(string, string)> completeValues = new();
                    for (int i = 0; i < values1.Count; i++)
                    {
                        completeValues.Append((values1[i], values2[i]));
                    }
                    sessionMemory.locGroupRewardNames[key] = completeValues;
                }
                return sessionMemory;
            }
            public static SessionState LoadState(TagCompound tag)
            {
                SessionState state = new();
                Load(tag, state); // No need to capture return value in this instance
                return state;
            }
        }

        // Data that's reset between Archipelago sessions
        public class SessionState : SessionMemory
        {
            public static new readonly Func<TagCompound, SessionState> DESERIALIZER = SessionMemory.LoadState;
            // List of locations that are currently being sent
            public List<Task<Dictionary<long, ScoutedItemInfo>>> locationQueue = new List<Task<Dictionary<long, ScoutedItemInfo>>>();
            public ArchipelagoSession session;
            public DeathLinkService deathlink;
            // Like `collectedItems`, but unique to this Archipelago session, and doesn't save, so
            // it starts at 0 each session. While less than `collectedItems`, it discards items
            // instead of collecting them. This is needed bc AP just gives us a list of items that
            // we have, and it's up to us to keep track of which ones we've already applied.
            public int currentItem;
            public List<string> collectedLocations = new List<string>();
            public List<string> goals = new List<string>();
            public bool victory;
            public int slot;
            public HashSet<string> hardmodeItems = new HashSet<string>();
            public bool eventAsItem;
            public bool hardmodeAsItem;
            public static void GiveItem(int? item, Action<Player> giveItem)
            {
                if (item != null)
                {
                    SessionMemory state = ModContent.GetInstance<ArchipelagoSystem>().Session;
                    state.receivedRewards.Add(item.Value);
                }

                for (var i = 0; i < Main.maxPlayers; i++)
                {
                    var player = Main.player[i];
                    if (player.active)
                    {
                        giveItem(player);
                        if (item != null)
                        {
                            if (Main.netMode == NetmodeID.Server)
                            {
                                var packet = ModContent.GetInstance<SeldomArchipelago>().GetPacket();
                                packet.Write("YouGotAnItem");
                                packet.Write(item.Value);
                                packet.Send(i);
                            }
                            else player.GetModPlayer<ArchipelagoPlayer>().ReceivedReward(item.Value);
                        }
                    }
                }
            }

            public static void GiveItem(int item) => GiveItem(item, player => player.QuickSpawnItem(player.GetSource_GiftOrReward(), item, 1));
            public static void GiveItem<T>() where T : ModItem => GiveItem(ModContent.ItemType<T>());
        }

        public WorldState world = new();
        public SessionState session;
        public SessionMemory sessionMemory = new();
        public SessionMemory Session
        {
            get => session ?? sessionMemory;
        }
        public bool SessionDisparity
        {
            get => (session is not null && sessionMemory is not null && sessionMemory.slotName != "" && (sessionMemory.slotName != session.slotName || sessionMemory.seedName != session.seedName));
        }

        public string[] CollectedLocations
        {
            get
            {
                return [.. Session.locationBacklog];
            }
        }
        // This method does everything that needs to be done with sessionMemory when
        // play switches from offline to online, then sets it to null.
        public void UseSessionMemory()
        {
            if (session is null) throw new Exception("UseSessionMemory: session is null!");
            if (sessionMemory is null) return;
            foreach (var location in sessionMemory.locationBacklog) QueueLocation(location);
            session.flagSystem = sessionMemory.flagSystem;
            sessionMemory.locationBacklog.Clear();
            sessionMemory = null;
        }
        public override void LoadWorldData(TagCompound tag)
        {
            world = tag.ContainsKey("WorldState") ? tag.Get<WorldState>("WorldState") : new();
            sessionMemory = tag.ContainsKey("SessionMemory") ? tag.Get<SessionMemory>("SessionMemory") : null;
            if (sessionMemory != null && session != null)
            {
                if (SessionDisparity)
                {
                    return;
                }
                UseSessionMemory();
            }
            if (!world.chestsRandomized && Session is not null && Session.randomizeChests)
            {
                FlagSystem.UpdateChests();
                world.chestsRandomized = true;
            }
        }

        public override void OnWorldLoad()
        {
            // Needed for achievements to work right
            typeof(SocialAPI).GetField("_mode", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, SocialMode.None);

            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            var config = ModContent.GetInstance<Config.Config>();

            LoginResult result;
            ArchipelagoSession newSession;
            try
            {
                newSession = ArchipelagoSessionFactory.CreateSession(config.address, config.port);

                result = newSession.TryConnectAndLogin(SeldomArchipelago.gameName, config.name, ItemsHandlingFlags.AllItems, null, null, null, config.password == "" ? null : config.password);
                if (result is LoginFailure)
                {
                    return;
                }
            }
            catch
            {
                return;
            }

            session = new();
            session.session = newSession;
            UseSessionMemory();
            FlagSystem flagSystem = session.flagSystem;
            session.eventAsItem = config.eventsAsItems;
            session.hardmodeAsItem = config.hardmodeAsItem;

            var locations = session.session.DataStorage[Scope.Slot, "CollectedLocations"].To<String[]>();
            if (locations != null)
            {
                session.collectedLocations = new List<string>(locations);
            }

            session.hardmodeBacklog = session.session.DataStorage[Scope.Slot, "HardmodeBacklog"].To<List<string>>();
            if (session.hardmodeBacklog is null) session.hardmodeBacklog = new List<string>();

            session.receivedRewards = session.session.DataStorage[Scope.Slot, "ReceivedRewards"].To<List<int>>();
            if (session.receivedRewards is null) session.receivedRewards = new List<int>();

            session.collectedItems = session.session.DataStorage[Scope.Slot, "CollectedItems"];

            var success = (LoginSuccessful)result;
            session.goals = new List<string>(((JArray)success.SlotData["goal"]).ToObject<string[]>());

            session.session.MessageLog.OnMessageReceived += (message) =>
            {
                var text = "";
                foreach (var part in message.Parts)
                {
                    text += part.Text;
                }
                Chat(text);
            };

            if ((bool)success.SlotData["deathlink"])
            {
                session.deathlink = session.session.CreateDeathLinkService();
                session.deathlink.EnableDeathLink();

                session.deathlink.OnDeathLinkReceived += ReceiveDeathlink;
            }

            if (!(bool)success.SlotData["biome_locks"])
            {
                flagSystem.UnlockBiomesNormally();
            }

            if (!(bool)success.SlotData["weather_locks"])
            {
                flagSystem.UnlockWeatherNormally();
            }

            if (!(bool)success.SlotData["grappling_hook_rando"])
            {
                flagSystem.UnlockHookNormally();
            }

            session.slot = success.Slot;
            session.seedName = newSession.RoomState.Seed;
            session.slotName = newSession.Players.GetPlayerName(session.slot);

            session.randomizeChests = (bool)success.SlotData["chest_loot"];

            String[] locKeys = session.session.DataStorage[Scope.Slot, "LocRewardNamesKeys"].To<String[]>();
            List<(string, string)>[] locValues = session.session.DataStorage[Scope.Slot, "LocRewardNamesValues"].To<List<(string, string)>[]>();
            session.locGroupRewardNames = new Dictionary<string, List<(string, string)>>();

            if (locKeys is not null)
            {
                for (int i = 0; i < locKeys.Length; i++)
                {
                    session.locGroupRewardNames[locKeys[i]] = locValues[i];
                }
            }
            else
            {
                object multiLocDictObject = success.SlotData["multi_loc_slot_dicts"];
                var multiLocDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(multiLocDictObject.ToString());
                var allBaseLocs = multiLocDict.Keys.ToList();
                while (allBaseLocs.Count > 0)
                {
                    string baseLoc = allBaseLocs[0];
                    session.locGroupRewardNames[baseLoc] = new List<(string, string)>();
                    string[] locs = multiLocDict[baseLoc].ToArray();
                    long[] itemIDs = (from loc in locs select session.session.Locations.GetLocationIdFromName(SeldomArchipelago.gameName, loc)).ToArray();
                    if (itemIDs.Contains(-1))
                    {
                        throw new Exception($"Some retrieved locations under {baseLoc} turned up -1 ids.");
                    }
                    var task = session.session.Locations.ScoutLocationsAsync(itemIDs);
                    if (!task.Wait(1000)) continue;
                    var itemInfoDictValues = task.Result.Values;
                    foreach (ScoutedItemInfo itemInfo in itemInfoDictValues)
                    {
                        string loc = itemInfo.LocationName;
                        if (session.collectedLocations.Contains(loc))
                            continue;
                        string itemName = $"{itemInfo.Player.Name}'s {itemInfo.ItemName}";
                        session.locGroupRewardNames[baseLoc].Add((loc, itemName));
                    }
                    allBaseLocs.RemoveAt(0);
                }
            }
            object enemyToKillCount = success.SlotData["enemy_to_kill_count"];
            session.enemyToKillCount = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int>>(enemyToKillCount.ToString());
            object enemyItems = success.SlotData["enemy_items"];
            session.enemyItems = Newtonsoft.Json.JsonConvert.DeserializeObject<HashSet<String>>(enemyItems.ToString());
            object hardmodeItems = success.SlotData["hardmode_items"];
            session.hardmodeItems = Newtonsoft.Json.JsonConvert.DeserializeObject<HashSet<String>>(hardmodeItems.ToString());

            int counter = -200;
            HashSet<string> locationNPCs = session.enemyToKillCount.Keys.ToHashSet();
            while (Lang.GetNPCNameValue(counter) is string npcName && counter < 10000)
            {
                locationNPCs.Remove(npcName);
                counter++;
            }

            Console.WriteLine("A FOUL SMELL FILLS THE AIR...");
        }

        public string[] Flags {
            get
            {
                List<string> flagList = FlagSystem.AllKeys.ToList();
                flagList.AddRange(["Post-OOA Tier 1", "Post-OOA Tier 2", "Post-OOA Tier 3"]);
                return flagList.ToArray();
            }
        }
        public bool CheckFlag(string flag)
        {
            if (FlagSystem.AllKeys.Contains(flag))
            {
                return session.flagSystem.FlagIsActive(flag);
            }
            return flag switch
            {
                "Post-OOA Tier 1" => DD2Event.DownedInvasionT1,
                "Post-OOA Tier 2" => DD2Event.DownedInvasionT2,
                "Post-OOA Tier 3" => DD2Event.DownedInvasionT3,
                _ => ModContent.GetInstance<CalamitySystem>()?.CheckCalamityFlag(flag) ?? false,
            };
        } 
        public void Collect(string item)
        {
            if (FlagSystem.AllKeys.Contains(item))
            {
                FlagSystem.Flag.ActivateResult result = session.flagSystem.UnlockFlag(item, session.eventAsItem, session.hardmodeAsItem);
                if (result == FlagSystem.Flag.ActivateResult.ActivateOnHardmode)
                {
                    session.hardmodeBacklog.Add(item);
                }
                return;
            }
            if (session.hardmodeItems.Contains(item))
            {
                session.hardmodeBacklog.Add(item);
                return;
            }
            Activate(item);
        }
        public void Activate(string item)
        {
            switch (item)
            {
                case "Reward: Torch God's Favor": SessionState.GiveItem(ItemID.TorchGodsFavor); break;
                case "Post-OOA Tier 1": DD2Event.DownedInvasionT1 = true; break;
                case "Post-OOA Tier 2": DD2Event.DownedInvasionT2 = true; break;
                case "Post-OOA Tier 3": DD2Event.DownedInvasionT3 = true; break;

                case "Reward: Coins": GiveCoins(); break;
                default:
                    string strippedItem = item.Replace("Reward: ", "");
                    if (SeldomArchipelago.englishLangToTypeDict.ContainsKey(strippedItem))
                    {
                        int itemID = SeldomArchipelago.englishLangToTypeDict[strippedItem];
                        SessionState.GiveItem(itemID);
                    }
                    else
                    {
                        Main.NewText($"Received unknown item: {item}");
                    }
                    break;
            }
        }

        public override void PostUpdateWorld()
        {
            if (session == null) return;

            if (!Session.flagSystem.FlagIsActive(FlagID.Wind))
            {
                Main.windSpeedCurrent = Main.windSpeedTarget = 0;
            }
            if (!session.session.Socket.Connected)
            {
                Chat("Disconnected from Archipelago. Reload the world to reconnect.");
                sessionMemory = session;
                session = null;
                return;
            }

            var unqueue = new List<int>();
            for (var i = 0; i < session.locationQueue.Count; i++)
            {
                var status = session.locationQueue[i].Status;

                if (status switch
                {
                    TaskStatus.RanToCompletion or TaskStatus.Canceled or TaskStatus.Faulted => true,
                    _ => false,
                })
                {
                    if (status == TaskStatus.RanToCompletion) foreach (var item in session.locationQueue[i].Result.Values) Chat($"Sent {item.ItemName} to {item.Player.Name}!");
                    else Chat("Sent an item to a player...but failed to get info about it!");

                    unqueue.Add(i);
                }
            }

            unqueue.Reverse();
            foreach (var i in unqueue) session.locationQueue.RemoveAt(i);

            while (session.session.Items.Any())
            {
                ItemInfo item = session.session.Items.DequeueItem();
                var itemName = item.ItemName;

                if (session.currentItem++ < Session.collectedItems)
                {
                    continue;
                }

                Collect(itemName);

                Session.collectedItems++;
            }

            if (ModLoader.HasMod("CalamityMod")) ModContent.GetInstance<CalamitySystem>().CalamityPostUpdateWorld();

            if (session.victory) return;

            foreach (var goal in session.goals) if (!session.collectedLocations.Contains(goal)) return;

            var victoryPacket = new StatusUpdatePacket()
            {
                Status = ArchipelagoClientState.ClientGoal,
            };
            session.session.Socket.SendPacket(victoryPacket);

            session.victory = true;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (session != null)
            {
                session.session.DataStorage[Scope.Slot, "CollectedLocations"] = session.collectedLocations.ToArray();
                session.session.DataStorage[Scope.Slot, "LocRewardNamesKeys"] = session.locGroupRewardNames.Keys.ToArray();
                session.session.DataStorage[Scope.Slot, "LocRewardNamesValues"] = session.locGroupRewardNames.Values.ToArray();
                session.session.DataStorage[Scope.Slot, "HardmodeBacklog"] = session.hardmodeBacklog.ToArray();
                session.session.DataStorage[Scope.Slot, "ReceivedRewards"] = session.receivedRewards.ToArray();
                session.session.DataStorage[Scope.Slot, "CollectedItems"] = session.collectedItems;
            }
            tag["WorldState"] = world;
            if (!SessionDisparity)
            {
                SessionMemory sess = Session; //explicit cast so it doesnt try to serialize SessionState
                tag["SessionMemory"] = sess;
            }
        }

        public void Reset()
        {
            typeof(SocialAPI).GetField("_mode", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, SocialMode.Steam);

            if (session != null) session.session.Socket.DisconnectAsync();
            session = null;
        }

        public override void OnWorldUnload()
        {
            world = new();
            session = null;
            sessionMemory = new();
            Reset();
        }
        public string[] Status()
        {
            if (session is not null && sessionMemory is not null
                && SessionDisparity)
            {
                return
                [
                    "WARNING: Disparity between current AP connection and world data detected!",
                    "Please update the mod's config with the correct credentials, or try a different world.",
                    $"CURRENT CONNECTION: {session.slotName} in APworld seed {session.seedName}",
                    $"WORLD DATA: {sessionMemory.slotName} in APworld seed {sessionMemory.seedName}",
                ];
            }
            else if (session is not null) return ["Archipelago is Active!"];
            else
            {
                string[] defaultText = [
                @"The world is not connected to Archipelago! Reload the world or run ""/apconnect"".",
                "If you are the host, check your config in the main menu at Workshop > Manage Mods > Config",
                "Or in-game at Settings > Mod Configuration",
                ];
                string[] sessionMemoryText = sessionMemory.HasActiveMemory ? [$"Currently playing in slot {sessionMemory.slotName} in APworld seed {sessionMemory.seedName}"] : [];
                return defaultText.Concat(sessionMemoryText).ToArray();
            }
            

        }

        public bool SendCommand(string command)
        {
            if (session == null) return false;

            var packet = new SayPacket()
            {
                Text = command,
            };
            session.session.Socket.SendPacket(packet);

            return true;
        }

        public string[] DebugInfo()
        {
            var info = new List<string>();

            if (world == null)
            {
                info.Add("The mod thinks you're not in a world, which should never happen");
            }
            else
            {
                info.Add("You are in a world");
                if (Session.locationBacklog.Count > 0)
                {
                    info.Add("You have locations in the backlog, which should only be the case if Archipelago is inactive");
                    info.Add($"Location backlog: [{string.Join("; ", Session.locationBacklog)}]");
                }
                else
                {
                    info.Add("No locations in the backlog, which is usually normal");
                }

                info.Add($"You've collected {Session.collectedItems} items");
            }

            if (session == null)
            {
                info.Add("You're not connected to Archipelago");
            }
            else
            {
                if (session.session.Socket.Connected)
                {
                    info.Add("You're connected to Archipelago");
                }
                else
                {
                    info.Add("You're not connected to Archipelago, but the mod thinks you are");
                }

                if (session.locationQueue.Count > 0)
                {
                    info.Add($"You have locations queued for sending. In normal circumstances, these locations will be sent ASAP.");

                    var statuses = new List<string>();
                    foreach (var location in session.locationQueue) statuses.Add(location.Status switch
                    {
                        TaskStatus.Created => "Created",
                        TaskStatus.WaitingForActivation => "Waiting for activation",
                        TaskStatus.WaitingToRun => "Waiting to run",
                        TaskStatus.Running => "Running",
                        TaskStatus.WaitingForChildrenToComplete => "Waiting for children to complete",
                        TaskStatus.RanToCompletion => "Completed",
                        TaskStatus.Canceled => "Canceled",
                        TaskStatus.Faulted => "Faulted",
                        _ => "Has a status that was added to C# after this code was written",
                    });

                    info.Add($"Location queue statuses: [{string.Join("; ", statuses)}]");
                }
                else
                {
                    info.Add("No locations in the queue, which is usually normal");
                }

                info.Add($"DeathLink is {(session.deathlink == null ? "dis" : "en")}abled");
                info.Add($"{session.currentItem} items have been applied");
                info.Add($"Collected locations: [{string.Join("; ", session.collectedLocations)}]");
                info.Add($"Goals: [{string.Join("; ", session.goals)}]");
                info.Add($"Victory has {(session.victory ? "been achieved! Hooray!" : "not been achieved. Alas.")}");
                info.Add($"You are slot {session.slot}");
            }

            return info.ToArray();
        }

        public void Chat(string message, int player = -1)
        {
            if (player == -1)
            {
                if (Main.netMode == NetmodeID.Server)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(message), Microsoft.Xna.Framework.Color.White);
                    Console.WriteLine(message);
                }
                else Main.NewText(message);
            }
            else ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral(message), Microsoft.Xna.Framework.Color.White, player);
        }

        public void Chat(string[] messages, int player = -1)
        {
            foreach (var message in messages) Chat(message, player);
        }

        public void QueueLocation(string locationName)
        {
            if (session == null)
            {
                sessionMemory.locationBacklog.Add(locationName);
                return;
            }

            var location = session.session.Locations.GetLocationIdFromName(SeldomArchipelago.gameName, locationName);
            if (location == -1 || !session.session.Locations.AllMissingLocations.Contains(location)) return;

            if (!session.collectedLocations.Contains(locationName))
            {
                session.locationQueue.Add(session.session.Locations.ScoutLocationsAsync(location));
                session.collectedLocations.Add(locationName);
            }

            session.session.Locations.CompleteLocationChecks(new[] { location });
        }

        public void QueueLocationClient(string locationName)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                QueueLocation(locationName);
                return;
            }

            var packet = ModContent.GetInstance<SeldomArchipelago>().GetPacket();
            packet.Write(locationName);
            packet.Send();
        }

        public void QueueLocationKey(string locType, string checkName = null)
        {
            bool locFoundAndRemoved = false;
            var locList = Session.locGroupRewardNames[locType];
            if (locList.Count == 0) return;
            if (checkName is null)
            {
                string loc = locList[0].Item1;
                locList.RemoveAt(0);
                QueueLocationClient(loc);
                Main.NewText($"Queued and removed {loc} under {locType}");
            } else
            {
                for (int i = 0; i < locList.Count; i++)
                {
                    if (locList[i].Item1 == checkName)
                    {
                        locList.RemoveAt(i);
                        QueueLocationClient(checkName);
                        locFoundAndRemoved = true;
                        Main.NewText($"Queued and removed {checkName} under {locType}");
                        break;
                    }
                }
                if (!locFoundAndRemoved)
                {
                    throw new Exception($"Location {checkName} not found in {locType} list.");
                }
            }
        }

        public void Achieved(string achievement)
        {
            world.achieved.Add(achievement);
        }

        public List<string> GetAchieved()
        {
            return world.achieved;
        }

        public void TriggerDeathlink(string message, int player)
        {
            if (session?.deathlink == null) return;

            var death = new DeathLink(session.session.Players.GetPlayerAlias(session.slot), message);
            session.deathlink.SendDeathLink(death);
            ReceiveDeathlink(death);
        }

        public void ReceiveDeathlink(DeathLink death)
        {
            var message = $"[DeathLink] {(death.Source == null ? "" : $"{death.Source} died")}{(death.Source != null && death.Cause != null ? ": " : "")}{(death.Cause == null ? "" : $"{death.Cause}")}";

            for (var i = 0; i < Main.maxPlayers; i++)
            {
                var player = Main.player[i];
                if (player.active && !player.dead) player.Hurt(PlayerDeathReason.ByCustomReason(message), 999999, 1);
            }

            if (Main.netMode == NetmodeID.SinglePlayer) return;

            var packet = ModContent.GetInstance<SeldomArchipelago>().GetPacket();
            packet.Write(message);
            packet.Send();
        }

        void BossFlag(ref bool flag, int boss)
        {
            BossFlag(boss);
            flag = true;
        }

        void BossFlag(Action set, int boss)
        {
            BossFlag(boss);
            set();
        }

        void BossFlag(int boss)
        {
            if (ModLoader.HasMod("CalamityMod")) ModContent.GetInstance<CalamitySystem>().VanillaBossKilled(boss);
        }

        int[] baseCoins = { 15, 20, 25, 30, 40, 50, 70, 100 };

        void GiveCoins()
        {
            var flagCount = 0;
            foreach (var flag in Flags) if (CheckFlag(flag)) flagCount++;
            var count = baseCoins[flagCount % 8] * (int)Math.Pow(10, flagCount / 8);

            var platinum = count / 10000;
            var gold = count % 10000 / 100;
            var silver = count % 100;
            SessionState.GiveItem(null, player =>
            {
                if (platinum > 0) player.QuickSpawnItem(player.GetSource_GiftOrReward(), ItemID.PlatinumCoin, platinum);
                if (gold > 0) player.QuickSpawnItem(player.GetSource_GiftOrReward(), ItemID.GoldCoin, gold);
                if (silver > 0) player.QuickSpawnItem(player.GetSource_GiftOrReward(), ItemID.SilverCoin, silver);
            });
        }

        public List<int> ReceivedRewards() => Session.receivedRewards;

        public override void ModifyHardmodeTasks(List<GenPass> list)
        {
            // If all mech boss flags are collected, but not Hardmode, there was no Hallow when
            // hallowed ore was generated, so no ore was generated. So, we generate new ore if this
            // is the case.
            list.Add(new PassLegacy("Hallowed Ore", (progress, config) =>
            {
                if (ModLoader.HasMod("CalamityMod")) ModContent.GetInstance<CalamitySystem>().CalamityStartHardmode();
            }));
        }
    }
}
