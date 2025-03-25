using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Microsoft.Xna.Framework;
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
using static SeldomArchipelago.Systems.ArchipelagoSystem.WorldState.Flag;
using static SeldomArchipelago.Systems.ArchipelagoSystem.WorldState;
using System.Runtime.Intrinsics.Arm;
using Steamworks;

namespace SeldomArchipelago.Systems
{
    public class ArchipelagoSystem : ModSystem

    {
        public enum FlagID
        {
            Hook,
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
        public enum LocationID
        {

        }
        // Data that's reset between worlds
        public class WorldState
        {
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
                    if (!flagSet.Contains(FlagID.Hardmode) && hardmodeFlags.Contains(id)) return ActivateResult.ActivateOnHardmode;
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
            // Achievements can be completed while loading into the world, but those complete before
            // `ArchipelagoPlayer::OnEnterWorld`, where achievements are reset, is run. So, this
            // keeps track of which achievements have been completed since `OnWorldLoad` was run, so
            // `ArchipelagoPlayer` knows not to clear them.
            public List<string> achieved = new List<string>();
            // Stores locations that were collected before Archipelago is started so they can be
            // queued once it's started
            public List<string> locationBacklog = new List<string>();
            // Stores the flags of chest locations that were collected before Archipelago is started.
            public List<string> chestLocationFlagBacklog = new List<string>();
            // Number of items the player has collected in this world
            public int collectedItems;
            // List of rewards received in this world, so they don't get reapplied. Saved in the
            // Terraria world instead of Archipelago data in case the player is, for example,
            // playing Hardcore and wants to receive all the rewards again when making a new player/
            // world.
            public static List<int> receivedRewards = new List<int>();
            //Whether this world has had its chests randomized.
            public bool chestsRandomized;
            #region Flag and Item Distribution

            
            
            public static void GiveItem(int? item, Action<Player> giveItem)
            {
                if (item != null) receivedRewards.Add(item.Value);

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
            #endregion
            #region Flag Data Management
            public void LoadFlagData(TagCompound tag)
            {
                if (tag.ContainsKey("activeFlags"))
                    foreach (int i in tag.GetList<int>("activeFlags")) activeFlags.Add((FlagID)i);
                activeFlags.Add(FlagID.Forest);
            }
            public void SaveFlagData(TagCompound tag)
            {
                tag["activeFlags"] = (from flag in activeFlags select (int)flag).ToList();
            }
            public bool IsFlagUnlocked(FlagID flag) => activeFlags.Contains(flag);
            #endregion
            private HashSet<FlagID> activeFlags = new HashSet<FlagID>();

            public bool FlagIsActive(FlagID flag) => activeFlags.Contains(flag);
            public static Dictionary<string, Flag> locToFlag = new Dictionary<string, Flag>()
                {
                    {"Grappling Hook",          new Flag(FlagID.Hook, theSideEffects: delegate(bool safe)
                    {
                        GiveItem(ItemID.GrapplingHook);
                    }
                    )},
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
            private static readonly FlagID[] hardmodeFlags = [
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
            private static readonly FlagID[] biomeFlags = [
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
            public ActivateResult UnlockFlag(string flagName, int receiveItemAs)
            {
                Flag flag = locToFlag[flagName];
                bool safeUnlock = receiveItemAs == 2 || (receiveItemAs == 1 && flagName == "Hardmode");
                return flag.ActivateFlag(activeFlags, safeUnlock);
            }
            public void UnlockBiomesNormally() => activeFlags.UnionWith(biomeFlags);
            public void UnlockHookNormally() => activeFlags.Add(FlagID.Hook);
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
                    ActivateResult result = locToFlag[flagName].ActivateFlag(activeFlags, true);
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
            private static readonly (FlagID, int[])[] OtherTiles =
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

            private static readonly (FlagID, int[])[] BiomeTreeSet =
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
            private static readonly Dictionary<int, FlagID> BiomeHerbSet = new Dictionary<int, FlagID>()
                {
                    {0, FlagID.Forest},
                    {1, FlagID.Jungle},
                    {2, FlagID.Forest},
                    {3, FlagID.Evil},
                    {4, FlagID.Desert},
                    {5, FlagID.Underworld},
                    {6, FlagID.Snow},
                };

            public bool TileRegionUnlocked(int i, int j, Player player = null)
            {
                if (IllegalDepth(j)) return false;
                int id = ItemRef.IDTree(i, j);
                if (id != -1)
                {
                    FlagID? biome = BiomeTreeSet.UseAsDict(id);
                    return biome is null || FlagIsActive((FlagID)biome);
                }

                id = ItemRef.IDHerb(i, j);
                if (id != -1)
                {
                    return FlagIsActive(BiomeHerbSet[id]);
                }

                id = Main.tile[i, j].TileType;
                FlagID? tileBiome = OtherTiles.UseAsDict(id);
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
                FlagID? tileBiome = OtherTiles.UseAsDict(type);
                if (tileBiome is not null)
                {
                    return FlagIsActive((FlagID)tileBiome);
                }
                return true;
            }
            #endregion
            #region Chest Checks
            private static readonly (FlagID, int[])[] BiomeChestSet = {
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
            public List<int> chestChecked = new();
            public static FlagID? GetChestRegion(int i, int j)
            {
                Terraria.Tile chest = Main.tile[i, j];
                int id = chest.IDChest();
                return BiomeChestSet.UseAsDict(id);
            }
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
                        blockBiome = OtherTiles.UseAsDict(blockUnderChestType);
                    }

                    if (blockBiome != FlagID.Forest && blockBiome is not null && Main.tile[i, j].IDChest() == 0) foreach ((FlagID, int[]) chestTuple in BiomeChestSet)
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
            private static readonly (FlagID, int[])[] BiomeHerbItemIDSet =
                [
                (FlagID.Forest, [ItemID.Daybloom, ItemID.DaybloomSeeds, ItemID.Blinkroot, ItemID.BlinkrootSeeds] ),
                (FlagID.Jungle, [ItemID.Moonglow, ItemID.MoonglowSeeds] ),
                (FlagID.Evil, [ItemID.Deathweed, ItemID.DeathweedSeeds] ),
                (FlagID.Desert, [ItemID.Waterleaf, ItemID.WaterleafSeeds] ),
                (FlagID.Underworld, [ItemID.Fireblossom, ItemID.FireblossomSeeds] ),
                (FlagID.Snow, [ItemID.Shiverthorn, ItemID.ShiverthornSeeds] ),
                ];
            public int[] GetLegalHerbs()
            {
                List<int> herbList = new List<int>();
                foreach ((FlagID, int[]) tuple in BiomeHerbItemIDSet)
                {
                    FlagID flag = tuple.Item1;
                    if (IsFlagUnlocked(flag)) herbList.AddRange(tuple.Item2);
                }
                return herbList.ToArray();
            }
            #endregion
            #region NPC Checks
            private static readonly (FlagID, int[])[] FlagNPCSet = [
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
            public static FlagID? GetNPCRegion(NPC npc) => FlagNPCSet.UseAsDict(npc.IDNPC());
            public bool NPCRegionUnlocked(DropAttemptInfo info)
            {
                FlagID? biome = GetNPCRegion(info.npc);
                return biome is null || FlagIsActive((FlagID)biome);
            }
            #endregion
            #region Bound NPC Spawn Checks (includes martian probe and prismatic lacewing)
            private static readonly int[] BoundNPCSet =
                {
                NPCID.SleepingAngler,
                NPCID.BoundGoblin,
                NPCID.WebbedStylist,
                NPCID.BoundWizard,
                NPCID.BartenderUnconscious,
                NPCID.MartianProbe,
                NPCID.EmpressButterfly,
                };
            public bool NPCShouldDespawn(int id)
            {
                switch (id)
                {
                    case NPCID.SleepingAngler:          return !FlagIsActive(FlagID.Ocean);
                    case NPCID.BoundGoblin:             return !FlagIsActive(FlagID.GoblinTinkerer);
                    case NPCID.WebbedStylist:           return !FlagIsActive(FlagID.Web);
                    case NPCID.BoundWizard:             return !FlagIsActive(FlagID.Wizard);
                    case NPCID.BartenderUnconscious:    return !FlagIsActive(FlagID.Tavernkeep);
                    case NPCID.MartianProbe:            return !FlagIsActive(FlagID.Martians);
                    case NPCID.EmpressButterfly:        return !FlagIsActive(FlagID.PrismaticLacewing);
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
                foreach (int id in BoundNPCSet)
                {
                    dict[id] = BoundNPCFindable(id, info) ? 1f : 0f;
                }
            }
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
            public bool FreeNPCSpawnable(int npcID) => FlagIsActive(FreeNPCSet[npcID]) && !NPC.AnyNPCs(npcID);
            #endregion
            #region Item Checks
            public bool ItemIsUsable(int id)
            {
                switch (id)
                {
                    case ItemID.SolarTable:             return FlagIsActive(FlagID.Eclipse);
                    case ItemID.BloodMoonStarter:       return FlagIsActive(FlagID.BloodMoon);
                    case ItemID.GoblinBattleStandard:   return FlagIsActive(FlagID.GoblinArmy);
                    case ItemID.PirateMap:              return FlagIsActive(FlagID.PirateInvasion);
                    case ItemID.PumpkinMoonMedallion:   return FlagIsActive(FlagID.PumpkinMoon);
                    case ItemID.NaughtyPresent:         return FlagIsActive(FlagID.FrostMoon);
                    default: return true;
                }
            }
            #endregion

        }

        // Data that's reset between Archipelago sessions
        public class SessionState
        {
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

            public int safeUnlock;
            public bool ReceiveHardmodeAsItem => safeUnlock == 1;
            public bool ReceiveAllEventsAsItems => safeUnlock == 2;
            // Multiple lists that contain the names of the items within enumerable location groups.
            // Indexed by base location names. The tuple at index zero of each list is the next one to be retrieved.
            // Item1 is the name of a location, and Item2 is the full name of the item at said location.
            public Dictionary<string, List<(string, string)>> locGroupRewardNames;
            // Backlog of hardmode-only items to be cashed in once Hardmode activates.
            public List<string> hardmodeBacklog = null;
            // Whether chests should be randomized.
            public bool randomizeChests;
        }

        public WorldState world = new();
        public SessionState session;

        public string[] CollectedLocations
        {
            get
            {
                return session is null ? [.. world.locationBacklog] : [.. session.collectedLocations];
            }
        }

        public override void LoadWorldData(TagCompound tag)
        {
            world.collectedItems = tag.ContainsKey("ApCollectedItems") ? tag.Get<int>("ApCollectedItems") : 0;
            WorldState.receivedRewards = tag.ContainsKey("ApReceivedRewards") ? tag.Get<List<int>>("ApReceivedRewards") : new();
            world.locationBacklog = tag.ContainsKey("ApLocationBacklog") ? tag.Get<List<string>>("ApLocationBacklog") : new();

            world.chestsRandomized = tag.ContainsKey("ApChestsRandomized") ? tag.GetBool("ApChestsRandomized") : false;
            if (!world.chestsRandomized && session is not null && session.randomizeChests)
            {
                WorldState.UpdateChests();
                world.chestsRandomized = true;
            }
            else if (world.chestsRandomized && session is not null && !session.randomizeChests)
            {
                Chat("WARNING: You have connected to a slot that has chest randomization disabled,");
                Chat("but the chests in this world have had their items randomized.");
                Chat("Please load a new world.");
            }

            world.LoadFlagData(tag);
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

            var locations = session.session.DataStorage[Scope.Slot, "CollectedLocations"].To<String[]>();
            if (locations != null)
            {
                session.collectedLocations = new List<string>(locations);
            }

            session.hardmodeBacklog = session.session.DataStorage[Scope.Slot, "HardmodeBacklog"].To<List<string>>();
            if (session.hardmodeBacklog is null) session.hardmodeBacklog = new List<string>();

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
                world.UnlockBiomesNormally();
            }

            if (!(bool)success.SlotData["grappling_hook_rando"])
            {
                world.UnlockHookNormally();
            }

            session.slot = success.Slot;

            session.randomizeChests = (bool)success.SlotData["chest_loot"];

            foreach (var location in world.locationBacklog) QueueLocation(location);
            world.locationBacklog.Clear();
            foreach (string baseName in world.chestLocationFlagBacklog)
            {
                int counter = 1;
                while (true)
                {
                    string locName = $"{baseName} {counter}";
                    if (session.collectedLocations.Contains(locName))
                    {
                        counter++;
                    }
                    else if (session.session.Locations.GetLocationIdFromName(SeldomArchipelago.gameName, FullName) != -1)
                    {
                        QueueLocation(locName);
                        break;
                    }
                    else { break; }
                }
            }

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
                foreach (string baseLoc in multiLocDict.Keys)
                {
                    session.locGroupRewardNames[baseLoc] = new List<(string, string)>();
                    string[] locs = multiLocDict[baseLoc].ToArray();
                    long[] itemIDs = (from loc in locs select session.session.Locations.GetLocationIdFromName(SeldomArchipelago.gameName, loc)).ToArray();
                    if (itemIDs.Contains(-1)) throw new Exception($"Some retrieved locations under {baseLoc} turned up -1 ids.");
                    var itemInfoDictValues = session.session.Locations.ScoutLocationsAsync(itemIDs).Result.Values;
                    foreach (ScoutedItemInfo itemInfo in itemInfoDictValues)
                    {
                        string loc = itemInfo.LocationName;
                        if (session.collectedLocations.Contains(loc))
                            continue;
                        string itemName = $"{itemInfo.Player.Name}'s {itemInfo.ItemName}";
                        session.locGroupRewardNames[baseLoc].Add((loc, itemName));
                    }
                }
            }
            Console.WriteLine("A FOUL SMELL FILLS THE AIR...");
        }

        public string[] Flags {
            get
            {
                List<string> flagList = locToFlag.Keys.ToList();
                flagList.AddRange(["Post-OOA Tier 1", "Post-OOA Tier 2", "Post-OOA Tier 3"]);
                return flagList.ToArray();
            }
        }
        public bool CheckFlag(string flag)
        {
            if (locToFlag.ContainsKey(flag))
            {
                return world.IsFlagUnlocked(locToFlag[flag].id);
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
            if (locToFlag.ContainsKey(item))
            {
                ActivateResult result = world.UnlockFlag(item, session.safeUnlock);
                if (result == ActivateResult.ActivateOnHardmode)
                {
                    session.hardmodeBacklog.Add(item);
                }
                return;
            }
            switch (item)
            {
                case "Reward: Torch God's Favor": WorldState.GiveItem(ItemID.TorchGodsFavor); break;
                case "Post-OOA Tier 1": DD2Event.DownedInvasionT1 = true; break;
                case "Post-OOA Tier 2": DD2Event.DownedInvasionT2 = true; break;
                case "Post-OOA Tier 3": DD2Event.DownedInvasionT3 = true; break;

                case "Reward: Coins": GiveCoins(); break;
                default:
                    string strippedItem = item.Replace("Reward: ", "");
                    if (SeldomArchipelago.englishLangToTypeDict.ContainsKey(strippedItem))
                    {
                        int itemID = SeldomArchipelago.englishLangToTypeDict[strippedItem];
                        WorldState.GiveItem(itemID);
                    }
                    else
                    {
                        Main.NewText($"Received unknown item: {item}");
                    }
                    break;
                    /*
                    case "Reward: Torch God's Favor": GiveItem(ItemID.TorchGodsFavor); break;
                    case "Post-King Slime": BossFlag(ref NPC.downedSlimeKing, NPCID.KingSlime); break;
                    case "Post-Eye of Cthulhu": BossFlag(ref NPC.downedBoss1, NPCID.EyeofCthulhu); break;
                    case "Post-Evil Boss": BossFlag(ref NPC.downedBoss2, NPCID.EaterofWorldsHead); break;
                    case "Post-Old One's Army Tier 1": DD2Event.DownedInvasionT1 = true; break;
                    case "Post-Goblin Army": NPC.downedGoblins = true; break;
                    case "Post-Queen Bee": BossFlag(ref NPC.downedQueenBee, NPCID.QueenBee); break;
                    case "Post-Skeletron": BossFlag(ref NPC.downedBoss3, NPCID.SkeletronHead); break;
                    case "Post-Deerclops": BossFlag(ref NPC.downedDeerclops, NPCID.Deerclops); break;
                    case "Hardmode":
                        BossFlag(NPCID.WallofFlesh);
                        WorldGen.StartHardmode();
                        break;
                    case "Post-Pirate Invasion": NPC.downedPirates = true; break;
                    case "Post-Queen Slime": BossFlag(ref NPC.downedQueenSlime, NPCID.QueenSlimeBoss); break;
                    case "Post-The Twins":
                        Action set = () => NPC.downedMechBoss2 = NPC.downedMechBossAny = true;
                        if (NPC.AnyNPCs(NPCID.Retinazer))
                        {
                            if (NPC.AnyNPCs(NPCID.Spazmatism))
                            {
                                // If the player is fighting The Twins, it would mess with the `CalamityGlobalNPC.OnKill` logic, so we have a fallback
                                if (ModLoader.HasMod("CalamityMod")) ModContent.GetInstance<CalamitySystem>().SpawnMechOres();
                                NPC.downedMechBoss2 = NPC.downedMechBossAny = true;
                            }
                            else BossFlag(set, NPCID.Retinazer);
                        }
                        else BossFlag(set, NPCID.Spazmatism);
                        break;
                    case "Post-Old One's Army Tier 2": DD2Event.DownedInvasionT2 = true; break;
                    case "Post-The Destroyer": BossFlag(() => NPC.downedMechBoss1 = NPC.downedMechBossAny = true, NPCID.TheDestroyer); break;
                    case "Post-Skeletron Prime": BossFlag(() => NPC.downedMechBoss3 = NPC.downedMechBossAny = true, NPCID.SkeletronPrime); break;
                    case "Post-Plantera": BossFlag(ref NPC.downedPlantBoss, NPCID.Plantera); break;
                    case "Post-Golem": BossFlag(ref NPC.downedGolemBoss, NPCID.Golem); break;
                    case "Post-Old One's Army Tier 3": DD2Event.DownedInvasionT3 = true; break;
                    case "Post-Martian Madness": NPC.downedMartians = true; break;
                    case "Post-Duke Fishron": BossFlag(ref NPC.downedFishron, NPCID.DukeFishron); break;
                    case "Post-Mourning Wood": BossFlag(ref NPC.downedHalloweenTree, NPCID.MourningWood); break;
                    case "Post-Pumpking": BossFlag(ref NPC.downedHalloweenKing, NPCID.Pumpking); break;
                    case "Post-Everscream": BossFlag(ref NPC.downedChristmasTree, NPCID.Everscream); break;
                    case "Post-Santa-NK1": BossFlag(ref NPC.downedChristmasSantank, NPCID.SantaNK1); break;
                    case "Post-Ice Queen": BossFlag(ref NPC.downedChristmasIceQueen, NPCID.IceQueen); break;
                    case "Post-Frost Legion": NPC.downedFrost = true; break;
                    case "Post-Empress of Light": BossFlag(ref NPC.downedEmpressOfLight, NPCID.HallowBoss); break;
                    case "Post-Lunatic Cultist": BossFlag(ref NPC.downedAncientCultist, NPCID.CultistBoss); break;
                    case "Post-Lunar Events": NPC.downedTowerNebula = NPC.downedTowerSolar = NPC.downedTowerStardust = NPC.downedTowerVortex = true; break;
                    case "Post-Moon Lord": BossFlag(ref NPC.downedMoonlord, NPCID.MoonLordCore); break;
                    case "Post-Desert Scourge": ModContent.GetInstance<CalamitySystem>().CalamityOnKillDesertScourge(); break;
                    case "Post-Giant Clam": ModContent.GetInstance<CalamitySystem>().CalamityOnKillGiantClam(false); break;
                    case "Post-Acid Rain Tier 1": ModContent.GetInstance<CalamitySystem>().CalamityAcidRainTier1Downed(); break;
                    case "Post-Crabulon": ModContent.GetInstance<CalamitySystem>().CalamityOnKillCrabulon(); break;
                    case "Post-The Hive Mind": ModContent.GetInstance<CalamitySystem>().CalamityOnKillTheHiveMind(); break;
                    case "Post-The Perforators": ModContent.GetInstance<CalamitySystem>().CalamityOnKillThePerforators(); break;
                    case "Post-The Slime God": ModContent.GetInstance<CalamitySystem>().CalamityOnKillTheSlimeGod(); break;
                    case "Post-Dreadnautilus": ModContent.GetInstance<CalamitySystem>().CalamityDreadnautilusDowned(); break;
                    case "Post-Hardmode Giant Clam": ModContent.GetInstance<CalamitySystem>().CalamityOnKillGiantClam(true); break;
                    case "Post-Aquatic Scourge": ModContent.GetInstance<CalamitySystem>().CalamityOnKillAquaticScourge(); break;
                    case "Post-Cragmaw Mire": ModContent.GetInstance<CalamitySystem>().CalamityOnKillCragmawMire(); break;
                    case "Post-Acid Rain Tier 2": ModContent.GetInstance<CalamitySystem>().CalamityAcidRainTier2Downed(); break;
                    case "Post-Brimstone Elemental": ModContent.GetInstance<CalamitySystem>().CalamityOnKillBrimstoneElemental(); break;
                    case "Post-Cryogen": ModContent.GetInstance<CalamitySystem>().CalamityOnKillCryogen(); break;
                    case "Post-Calamitas Clone": ModContent.GetInstance<CalamitySystem>().CalamityOnKillCalamitasClone(); break;
                    case "Post-Great Sand Shark": ModContent.GetInstance<CalamitySystem>().CalamityOnKillGreatSandShark(); break;
                    case "Post-Leviathan and Anahita": ModContent.GetInstance<CalamitySystem>().CalamityOnKillLeviathanAndAnahita(); break;
                    case "Post-Astrum Aureus": ModContent.GetInstance<CalamitySystem>().CalamityOnKillAstrumAureus(); break;
                    case "Post-The Plaguebringer Goliath": ModContent.GetInstance<CalamitySystem>().CalamityOnKillThePlaguebringerGoliath(); break;
                    case "Post-Ravager": ModContent.GetInstance<CalamitySystem>().CalamityOnKillRavager(); break;
                    case "Post-Astrum Deus": ModContent.GetInstance<CalamitySystem>().CalamityOnKillAstrumDeus(); break;
                    case "Post-Profaned Guardians": ModContent.GetInstance<CalamitySystem>().CalamityOnKillProfanedGuardians(); break;
                    case "Post-The Dragonfolly": ModContent.GetInstance<CalamitySystem>().CalamityOnKillTheDragonfolly(); break;
                    case "Post-Providence, the Profaned Goddess": ModContent.GetInstance<CalamitySystem>().CalamityOnKillProvidenceTheProfanedGoddess(); break;
                    case "Post-Storm Weaver": ModContent.GetInstance<CalamitySystem>().CalamityOnKillStormWeaver(); break;
                    case "Post-Ceaseless Void": ModContent.GetInstance<CalamitySystem>().CalamityOnKillCeaselessVoid(); break;
                    case "Post-Signus, Envoy of the Devourer": ModContent.GetInstance<CalamitySystem>().CalamityOnKillSignusEnvoyOfTheDevourer(); break;
                    case "Post-Polterghast": ModContent.GetInstance<CalamitySystem>().CalamityOnKillPolterghast(); break;
                    case "Post-Mauler": ModContent.GetInstance<CalamitySystem>().CalamityOnKillMauler(); break;
                    case "Post-Nuclear Terror": ModContent.GetInstance<CalamitySystem>().CalamityOnKillNuclearTerror(); break;
                    case "Post-The Old Duke": ModContent.GetInstance<CalamitySystem>().CalamityOnKillTheOldDuke(); break;
                    case "Post-The Devourer of Gods": ModContent.GetInstance<CalamitySystem>().CalamityOnKillTheDevourerOfGods(); break;
                    case "Post-Yharon, Dragon of Rebirth": ModContent.GetInstance<CalamitySystem>().CalamityOnKillYharonDragonOfRebirth(); break;
                    case "Post-Exo Mechs": ModContent.GetInstance<CalamitySystem>().CalamityOnKillExoMechs(); break;
                    case "Post-Supreme Witch, Calamitas": ModContent.GetInstance<CalamitySystem>().CalamityOnKillSupremeWitchCalamitas(); break;
                    case "Post-Primordial Wyrm": ModContent.GetInstance<CalamitySystem>().CalamityPrimordialWyrmDowned(); break;
                    case "Post-Boss Rush": ModContent.GetInstance<CalamitySystem>().CalamityBossRushDowned(); break;
                    case "Reward: Hermes Boots": GiveItem(ItemID.HermesBoots); break;
                    case "Reward: Magic Mirror": GiveItem(ItemID.MagicMirror); break;
                    case "Reward: Demon Conch": GiveItem(ItemID.DemonConch); break;
                    case "Reward: Magic Conch": GiveItem(ItemID.MagicConch); break;
                    case "Reward: Grappling Hook": GiveItem(ItemID.GrapplingHook); break;
                    case "Reward: Cloud in a Bottle": GiveItem(ItemID.CloudinaBottle); break;
                    case "Reward: Climbing Claws": GiveItem(ItemID.ClimbingClaws); break;
                    case "Reward: Ancient Chisel": GiveItem(ItemID.AncientChisel); break;
                    case "Reward: Fledgling Wings": GiveItem(ItemID.CreativeWings); break;
                    case "Reward: Rod of Discord": GiveItem(ItemID.RodofDiscord); break;
                    case "Reward: Aglet": GiveItem(ItemID.Aglet); break;
                    case "Reward: Anklet of the Wind": GiveItem(ItemID.AnkletoftheWind); break;
                    case "Reward: Ice Skates": GiveItem(ItemID.IceSkates); break;
                    case "Reward: Lava Charm": GiveItem(ItemID.LavaCharm); break;
                    case "Reward: Water Walking Boots": GiveItem(ItemID.WaterWalkingBoots); break;
                    case "Reward: Flipper": GiveItem(ItemID.Flipper); break;
                    case "Reward: Frog Leg": GiveItem(ItemID.FrogLeg); break;
                    case "Reward: Shoe Spikes": GiveItem(ItemID.ShoeSpikes); break;
                    case "Reward: Tabi": GiveItem(ItemID.Tabi); break;
                    case "Reward: Black Belt": GiveItem(ItemID.BlackBelt); break;
                    case "Reward: Flying Carpet": GiveItem(ItemID.FlyingCarpet); break;
                    case "Reward: Moon Charm": GiveItem(ItemID.MoonCharm); break;
                    case "Reward: Neptune's Shell": GiveItem(ItemID.NeptunesShell); break;
                    case "Reward: Compass": GiveItem(ItemID.Compass); break;
                    case "Reward: Depth Meter": GiveItem(ItemID.DepthMeter); break;
                    case "Reward: Radar": GiveItem(ItemID.Radar); break;
                    case "Reward: Tally Counter": GiveItem(ItemID.TallyCounter); break;
                    case "Reward: Lifeform Analyzer": GiveItem(ItemID.LifeformAnalyzer); break;
                    case "Reward: DPS Meter": GiveItem(ItemID.DPSMeter); break;
                    case "Reward: Stopwatch": GiveItem(ItemID.Stopwatch); break;
                    case "Reward: Metal Detector": GiveItem(ItemID.MetalDetector); break;
                    case "Reward: Fisherman's Pocket Guide": GiveItem(ItemID.FishermansGuide); break;
                    case "Reward: Weather Radio": GiveItem(ItemID.WeatherRadio); break;
                    case "Reward: Sextant": GiveItem(ItemID.Sextant); break;
                    case "Reward: Band of Regeneration": GiveItem(ItemID.BandofRegeneration); break;
                    case "Reward: Celestial Magnet": GiveItem(ItemID.CelestialMagnet); break;
                    case "Reward: Nature's Gift": GiveItem(ItemID.NaturesGift); break;
                    case "Reward: Philosopher's Stone": GiveItem(ItemID.PhilosophersStone); break;
                    case "Reward: Cobalt Shield": GiveItem(ItemID.CobaltShield); break;
                    case "Reward: Armor Polish": GiveItem(ItemID.ArmorPolish); break;
                    case "Reward: Vitamins": GiveItem(ItemID.Vitamins); break;
                    case "Reward: Bezoar": GiveItem(ItemID.Bezoar); break;
                    case "Reward: Adhesive Bandage": GiveItem(ItemID.AdhesiveBandage); break;
                    case "Reward: Megaphone": GiveItem(ItemID.Megaphone); break;
                    case "Reward: Nazar": GiveItem(ItemID.Nazar); break;
                    case "Reward: Fast Clock": GiveItem(ItemID.FastClock); break;
                    case "Reward: Trifold Map": GiveItem(ItemID.TrifoldMap); break;
                    case "Reward: Blindfold": GiveItem(ItemID.Blindfold); break;
                    case "Reward: Pocket Mirror": GiveItem(ItemID.PocketMirror); break;
                    case "Reward: Paladin's Shield": GiveItem(ItemID.PaladinsShield); break;
                    case "Reward: Frozen Turtle Shell": GiveItem(ItemID.FrozenTurtleShell); break;
                    case "Reward: Flesh Knuckles": GiveItem(ItemID.FleshKnuckles); break;
                    case "Reward: Putrid Scent": GiveItem(ItemID.PutridScent); break;
                    case "Reward: Feral Claws": GiveItem(ItemID.FeralClaws); break;
                    case "Reward: Cross Necklace": GiveItem(ItemID.CrossNecklace); break;
                    case "Reward: Star Cloak": GiveItem(ItemID.StarCloak); break;
                    case "Reward: Titan Glove": GiveItem(ItemID.TitanGlove); break;
                    case "Reward: Obsidian Rose": GiveItem(ItemID.ObsidianRose); break;
                    case "Reward: Magma Stone": GiveItem(ItemID.MagmaStone); break;
                    case "Reward: Shark Tooth Necklace": GiveItem(ItemID.SharkToothNecklace); break;
                    case "Reward: Magic Quiver": GiveItem(ItemID.MagicQuiver); break;
                    case "Reward: Rifle Scope": GiveItem(ItemID.RifleScope); break;
                    case "Reward: Brick Layer": GiveItem(ItemID.BrickLayer); break;
                    case "Reward: Extendo Grip": GiveItem(ItemID.ExtendoGrip); break;
                    case "Reward: Paint Sprayer": GiveItem(ItemID.PaintSprayer); break;
                    case "Reward: Portable Cement Mixer": GiveItem(ItemID.PortableCementMixer); break;
                    case "Reward: Treasure Magnet": GiveItem(ItemID.TreasureMagnet); break;
                    case "Reward: Step Stool": GiveItem(ItemID.PortableStool); break;
                    case "Reward: Discount Card": GiveItem(ItemID.DiscountCard); break;
                    case "Reward: Gold Ring": GiveItem(ItemID.GoldRing); break;
                    case "Reward: Lucky Coin": GiveItem(ItemID.LuckyCoin); break;
                    case "Reward: High Test Fishing Line": GiveItem(ItemID.HighTestFishingLine); break;
                    case "Reward: Angler Earring": GiveItem(ItemID.AnglerEarring); break;
                    case "Reward: Tackle Box": GiveItem(ItemID.TackleBox); break;
                    case "Reward: Lavaproof Fishing Hook": GiveItem(ItemID.LavaFishingHook); break;
                    case "Reward: Red Counterweight": GiveItem(ItemID.RedCounterweight); break;
                    case "Reward: Yoyo Glove": GiveItem(ItemID.YoYoGlove); break;
                    case "Reward: Coins": GiveCoins(); break;
                    case "Reward: Cosmolight": ModContent.GetInstance<CalamitySystem>().GiveCosmolight(); break;
                    case "Reward: Diving Helmet": GiveItem(ItemID.DivingHelmet); break;
                    case "Reward: Jellyfish Necklace": GiveItem(ItemID.JellyfishNecklace); break;
                    case "Reward: Corrupt Flask": ModContent.GetInstance<CalamitySystem>().GiveCorruptFlask(); break;
                    case "Reward: Crimson Flask": ModContent.GetInstance<CalamitySystem>().GiveCrimsonFlask(); break;
                    case "Reward: Craw Carapace": ModContent.GetInstance<CalamitySystem>().GiveCrawCarapace(); break;
                    case "Reward: Giant Shell": ModContent.GetInstance<CalamitySystem>().GiveGiantShell(); break;
                    case "Reward: Life Jelly": ModContent.GetInstance<CalamitySystem>().GiveLifeJelly(); break;
                    case "Reward: Vital Jelly": ModContent.GetInstance<CalamitySystem>().GiveVitalJelly(); break;
                    case "Reward: Cleansing Jelly": ModContent.GetInstance<CalamitySystem>().GiveCleansingJelly(); break;
                    case "Reward: Giant Tortoise Shell": ModContent.GetInstance<CalamitySystem>().GiveGiantTortoiseShell(); break;
                    case "Reward: Coin of Deceit": ModContent.GetInstance<CalamitySystem>().GiveCoinOfDeceit(); break;
                    case "Reward: Ink Bomb": ModContent.GetInstance<CalamitySystem>().GiveInkBomb(); break;
                    case "Reward: Voltaic Jelly": ModContent.GetInstance<CalamitySystem>().GiveVoltaicJelly(); break;
                    case "Reward: Wulfrum Battery": ModContent.GetInstance<CalamitySystem>().GiveWulfrumBattery(); break;
                    case "Reward: Luxor's Gift": ModContent.GetInstance<CalamitySystem>().GiveLuxorsGift(); break;
                    case "Reward: Raider's Talisman": ModContent.GetInstance<CalamitySystem>().GiveRaidersTalisman(); break;
                    case "Reward: Rotten Dogtooth": ModContent.GetInstance<CalamitySystem>().GiveRottenDogtooth(); break;
                    case "Reward: Scuttler's Jewel": ModContent.GetInstance<CalamitySystem>().GiveScuttlersJewel(); break;
                    case "Reward: Unstable Granite Core": ModContent.GetInstance<CalamitySystem>().GiveUnstableGraniteCore(); break;
                    case "Reward: Amidias' Spark": ModContent.GetInstance<CalamitySystem>().GiveAmidiasSpark(); break;
                    case "Reward: Ursa Sergeant": ModContent.GetInstance<CalamitySystem>().GiveUrsaSergeant(); break;
                    case "Reward: Trinket of Chi": ModContent.GetInstance<CalamitySystem>().GiveTrinketOfChi(); break;
                    case "Reward: The Transformer": ModContent.GetInstance<CalamitySystem>().GiveTheTransformer(); break;
                    case "Reward: Rover Drive": ModContent.GetInstance<CalamitySystem>().GiveRoverDrive(); break;
                    case "Reward: Marnite Repulsion Shield": ModContent.GetInstance<CalamitySystem>().GiveMarniteRepulsionShield(); break;
                    case "Reward: Frost Barrier": ModContent.GetInstance<CalamitySystem>().GiveFrostBarrier(); break;
                    case "Reward: Ancient Fossil": ModContent.GetInstance<CalamitySystem>().GiveAncientFossil(); break;
                    case "Reward: Spelunker's Amulet": ModContent.GetInstance<CalamitySystem>().GiveSpelunkersAmulet(); break;
                    case "Reward: Fungal Symbiote": ModContent.GetInstance<CalamitySystem>().GiveFungalSymbiote(); break;
                    case "Reward: Gladiator's Locket": ModContent.GetInstance<CalamitySystem>().GiveGladiatorsLocket(); break;
                    case "Reward: Wulfrum Acrobatics Pack": ModContent.GetInstance<CalamitySystem>().GiveWulfrumAcrobaticsPack(); break;
                    case "Reward: Depths Charm": ModContent.GetInstance<CalamitySystem>().GiveDepthsCharm(); break;
                    case "Reward: Anechoic Plating": ModContent.GetInstance<CalamitySystem>().GiveAnechoicPlating(); break;
                    case "Reward: Iron Boots": ModContent.GetInstance<CalamitySystem>().GiveIronBoots(); break;
                    case "Reward: Sprit Glyph": ModContent.GetInstance<CalamitySystem>().GiveSpritGlyph(); break;
                    case "Reward: Abyssal Amulet": ModContent.GetInstance<CalamitySystem>().GiveAbyssalAmulet(); break;
                    case "Reward: Life Crystal": GiveItem(ItemID.LifeCrystal); break;
                    case "Reward: Enchanted Sword": GiveItem(ItemID.EnchantedSword); break;
                    case "Reward: Starfury": GiveItem(ItemID.Starfury); break;
                    case "Reward: Defender Medal": GiveItem(ItemID.DefenderMedal); break;
                    case null: break;
                    default: Chat($"Received unknown item: {item}"); break;
                    */
            }
        }

        public override void PostUpdateWorld()
        {
            if (session == null) return;

            if (!session.session.Socket.Connected)
            {
                Chat("Disconnected from Archipelago. Reload the world to reconnect.");
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

                if (session.currentItem++ < world.collectedItems)
                {
                    continue;
                }

                Collect(itemName);

                world.collectedItems++;
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
            tag["ApCollectedItems"] = world.collectedItems;
            if (session != null)
            {
                session.session.DataStorage[Scope.Slot, "CollectedLocations"] = session.collectedLocations.ToArray();
                session.session.DataStorage[Scope.Slot, "LocRewardNamesKeys"] = session.locGroupRewardNames.Keys.ToArray();
                session.session.DataStorage[Scope.Slot, "LocRewardNamesValues"] = session.locGroupRewardNames.Values.ToArray();
                session.session.DataStorage[Scope.Slot, "HardmodeBacklog"] = session.hardmodeBacklog.ToArray();
            }
            tag["ApReceivedRewards"] = WorldState.receivedRewards;
            WorldState.receivedRewards.Clear();
            tag["ApLocationBacklog"] = world.locationBacklog;
            tag["ApChestsRandomized"] = world.chestsRandomized;

            world.SaveFlagData(tag);
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
            Reset();
        }

        public string[] Status() => (session == null) switch
        {
            true => new[] {
                @"The world is not connected to Archipelago! Reload the world or run ""/apconnect"".",
                "If you are the host, check your config in the main menu at Workshop > Manage Mods > Config",
                "Or in-game at Settings > Mod Configuration",
            },
            false => new[] { "Archipelago is active!" },
        };

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
                if (world.locationBacklog.Count > 0)
                {
                    info.Add("You have locations in the backlog, which should only be the case if Archipelago is inactive");
                    info.Add($"Location backlog: [{string.Join("; ", world.locationBacklog)}]");
                }
                else
                {
                    info.Add("No locations in the backlog, which is usually normal");
                }

                info.Add($"You've collected {world.collectedItems} items");
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
                world.locationBacklog.Add(locationName);
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
            WorldState.GiveItem(null, player =>
            {
                if (platinum > 0) player.QuickSpawnItem(player.GetSource_GiftOrReward(), ItemID.PlatinumCoin, platinum);
                if (gold > 0) player.QuickSpawnItem(player.GetSource_GiftOrReward(), ItemID.GoldCoin, gold);
                if (silver > 0) player.QuickSpawnItem(player.GetSource_GiftOrReward(), ItemID.SilverCoin, silver);
            });
        }

        public List<int> ReceivedRewards() => WorldState.receivedRewards;

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
