using Humanizer;
using Microsoft.Xna.Framework;
using MyExtensions;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks.Sources;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using static System.Random;

namespace SeldomArchipelago {
    public static class ItemRef {
        public static HashSet<int> mundaneCrateIDs = new HashSet<int>() {ItemID.WoodenCrate, ItemID.WoodenCrateHard, ItemID.IronCrate, ItemID.IronCrateHard, ItemID.GoldenCrate, ItemID.GoldenCrateHard};
        private static Random rnd = new Random();
        #region Private Datasets
        private static readonly (int, int[])[] NPCIDSets = {
            (3, new int[] {132, 186, 187, 188, 189, 200, 223, 161, 254, 255, 52, 53, 536, 319, 320, 321, 332, 436, 431, 432, 433, 434, 435, 331, 430, 590}),
            (1, new int[] {302, 333, 334, 335, 336}),
            (494, new int[] {495}),
            (496, new int[] {497}),
            (498, new int[] {499, 500, 501, 502, 503, 504, 505, 506}),
            (42, new int[] {-16, -17, 231, -56, -57, 232, -58, -59, 233, -60, -61, 234, -62, -63, 235, -64, -65}),
            (176, new int[] {-18, -19, -20, -21}),
            (21, new int[] {449, -46, -47, 201, -48, -49, 202, -50, -51, 203, -52, -53, 322, 323, 324, 635}),
            (3187, new int[] {3188, 3189}),
            (580, new int[] {508}),
            (581, new int[] {509}),
            (195, new int[] {196}),
            (NPCID.RustyArmoredBonesAxe, new int[] {NPCID.RustyArmoredBonesFlail, NPCID.RustyArmoredBonesSword, NPCID.RustyArmoredBonesSwordNoArmor}),
            (NPCID.BlueArmoredBones, new int[] {NPCID.BlueArmoredBonesMace, NPCID.BlueArmoredBonesNoPants, NPCID.BlueArmoredBonesSword}),
            (NPCID.HellArmoredBones, new int[] {NPCID.HellArmoredBonesMace, NPCID.HellArmoredBonesSpikeShield, NPCID.HellArmoredBonesSword}),
            (NPCID.DiabolistRed, new int[] {NPCID.DiabolistWhite}),
            (NPCID.Necromancer, new int[] {NPCID.NecromancerArmored}),
            (NPCID.RaggedCaster, new int[] {NPCID.RaggedCasterOpenCoat}),
            (NPCID.Vampire, new int[] {NPCID.VampireBat}),
            (NPCID.MartianSaucer, new int[] {NPCID.MartianSaucerCore}),
            (NPCID.BlackRecluse, new int[] {NPCID.BlackRecluseWall}),
            (NPCID.JungleCreeper, new int[] {NPCID.JungleCreeperWall}),
            (NPCID.DesertScorpionWalk, new int[] {NPCID.DesertScorpionWall}),
            (NPCID.WallCreeper, new int[] {NPCID.WallCreeperWall}),
            (NPCID.BloodCrawler, new int[] {NPCID.BloodCrawlerWall}),
        };
        public static int[] eowIDs = new int[] {13, 14, 15};
        public static (int, float)[] eclipseWeights1 = new (int, float)[]
        {
            (NPCID.Eyezor, 1f),
            (NPCID.Vampire, 3f),
            (NPCID.ThePossessed, 6f),
            (NPCID.SwampThing, 10f),
            (NPCID.Fritz, 10f),
            (NPCID.Frankenstein, 10f),
            (NPCID.CreatureFromTheDeep, 10f),
        };
        public static readonly (int, float)[] eclipseWeights2 = new (int, float)[]
        {
            (NPCID.Eyezor, 3f),
            (NPCID.Vampire, 3f),
            (NPCID.ThePossessed, 3f),
            (NPCID.SwampThing, 3f),
            (NPCID.Fritz, 3f),
            (NPCID.Frankenstein, 3f),
            (NPCID.CreatureFromTheDeep, 3f),
            (NPCID.Reaper, 3f),
            (NPCID.Butcher, 3f),
            (NPCID.DeadlySphere, 3f),
            (NPCID.DrManFly, 3f),
            (NPCID.Nailhead, 3f),
            (NPCID.Psycho, 3f),
        };
                private static readonly Dictionary<int, int[]> ItemSetDict = new Dictionary<int, int[]> {
            {6, new int[] {-11, -12}},
            {ItemID.FlareGun, new int[] {ItemID.Flare}},
            {ItemID.PharaohsMask, new int[] {ItemID.PharaohsRobe}},
            {ItemID.AncientCobaltHelmet, new int[] {ItemID.AncientCobaltBreastplate, ItemID.AncientCobaltLeggings}},
            {954, new int[] {81, 77}},
            {955, new int[] {83, 79}},
            {956, new int[] {957, 958}},
            {410, new int[] {411}},
            {ItemID.AnglerHat, new int[] {ItemID.AnglerVest, ItemID.AnglerPants}},
            {ItemID.SeashellHairpin, new int[] {ItemID.MermaidAdornment, ItemID.MermaidTail}},
            {ItemID.FishCostumeMask, new int[] {ItemID.FishCostumeShirt, ItemID.FishCostumeFinskirt}},
            {ItemID.NinjaHood, new int[] {ItemID.NinjaShirt, ItemID.NinjaPants}},
            {ItemID.GladiatorHelmet, new int[] {ItemID.GladiatorBreastplate, ItemID.GladiatorLeggings}},
            {4982, new int[] {4983, 4984}},
            {ItemID.Stynger, new int[] {ItemID.StyngerBolt}},
            {ItemID.StakeLauncher, new int[] {ItemID.Stake}},
            {ItemID.GrenadeLauncher, new int[] {ItemID.RocketI}}
        };
        private static readonly Dictionary<int, int> ChestDict = new Dictionary<int, int> {
            {21, 0},
            {467, 52}
        };
        private static readonly Dictionary<(int, int), int> WallOverride = new Dictionary<(int, int), int> {
            {(1, 34), 69} //Pyramid Chests
        };
        private static readonly Dictionary<int, (int, int)> ItemQuantDict = new Dictionary<int, (int, int)> {
            {931, (25, 50)},
            {ItemID.StyngerBolt, (60, 99)},
            {ItemID.Stake, (30, 60)}
        };
        private static readonly HashSet<int> Sellables = new HashSet<int> {
            931, 97, ItemID.Stake, ItemID.StyngerBolt, ItemID.RocketI
        };
        #region Tree Constants
        public const int ForestTree = 0;
        public const int CorruptTree = 1;
        public const int JungleTree = 2;
        public const int HallowTree = 3;
        public const int SnowTree = 4;
        public const int CrimsonTree = 5;
        public const int UndergroundJungleTree = 6;
        public const int GiantGlowingMushroomSurface = 7;
        public const int GiantGlowingMushroom = 8;
        public const int Cactus = 9;
        public const int PalmTree = 10;
        public const int BambooTree = 11;
        public const int TopazTree = 12;
        public const int AmethystTree = 13;
        public const int SapphireTree = 14;
        public const int EmeraldTree = 15;
        public const int RubyTree = 16;
        public const int DiamondTree = 17;
        public const int AmberTree = 18;
        public const int SakuraTree = 19;
        public const int YellowWillowTree = 20;
        public const int AshTree = 21;
        #endregion
        private static readonly Dictionary<int, int> TreeDict = new Dictionary<int, int>()
        {
            {TileID.MushroomTrees,          GiantGlowingMushroom},
            {TileID.Cactus,                 Cactus},
            {TileID.PalmTree,               PalmTree},
            {TileID.Bamboo,                 BambooTree},
            {TileID.TreeTopaz,              TopazTree},
            {TileID.TreeAmethyst,           AmethystTree},
            {TileID.TreeSapphire,           SapphireTree},
            {TileID.TreeEmerald,            EmeraldTree},
            {TileID.TreeRuby,               RubyTree},
            {TileID.TreeDiamond,            DiamondTree},
            {TileID.TreeAmber,              AmberTree},
            {TileID.VanityTreeSakura,       SakuraTree},
            {TileID.VanityTreeYellowWillow, YellowWillowTree},
            {TileID.TreeAsh,                AshTree}
        };
        private static readonly Dictionary<int, int> TreeSourceDict = new Dictionary<int, int>()
        {
            {TileID.Grass,          ForestTree},
            {TileID.CorruptGrass,   CorruptTree},
            {TileID.JungleGrass,    JungleTree},
            {TileID.HallowedGrass,  HallowTree},
            {TileID.SnowBlock,      SnowTree},
            {TileID.CrimsonGrass,   CrimsonTree},
            {TileID.MushroomGrass,  GiantGlowingMushroomSurface}

        };
        #endregion
        #region Grappling Hook ID
        private static readonly int[] grapplingHooks = new int[]
        {
            ItemID.GrapplingHook,
            ItemID.AmethystHook,
            ItemID.SquirrelHook,
            ItemID.TopazHook,
            ItemID.SapphireHook,
            ItemID.EmeraldHook,
            ItemID.RubyHook,
            ItemID.AmberHook,
            ItemID.DiamondHook,
            ItemID.WebSlinger,
            ItemID.SkeletronHand,
            ItemID.SlimeHook,
            ItemID.FishHook,
            ItemID.IvyWhip,
            ItemID.BatHook,
            ItemID.CandyCaneHook,
            ItemID.DualHook,
            ItemID.QueenSlimeHook,
            ItemID.ThornHook,
            ItemID.IlluminantHook,
            ItemID.WormHook,
            ItemID.TendonHook,
            ItemID.AntiGravityHook,
            ItemID.SpookyHook,
            ItemID.ChristmasHook,
            ItemID.LunarHook,
            ItemID.StaticHook
        };
        public static bool IsGrapplingHook(this Item item) => grapplingHooks.Contains(item.type);
        #endregion
        public static int GetQuant(this int itemID)
        {
            if (ItemQuantDict.ContainsKey(itemID))
            {
                int lowerBound = ItemQuantDict[itemID].Item1;
                int upperBound = ItemQuantDict[itemID].Item2;
                return rnd.Next(lowerBound, upperBound);
            }
            return 1;
        }
        public static int[] GetItemSet(int itemID, bool stripShop = false)
        {
            if (itemID == 0)
                return new int[0];
            int[] ItemSetsNew = new int[] {itemID};
            
            if (ItemSetDict.ContainsKey(itemID)) {
                int[] ItemSets = ItemSetDict[itemID];
                return ItemSetsNew.Concat(ItemSets).ToArray();
            }
            if(stripShop) {
                ItemSetsNew = StripSellables(ItemSetsNew);
            }
            return ItemSetsNew;
        }
        public static int[] AddExtrasToItemSet(int[] itemSet) {
            int[] newSet = new int[] {};
            foreach (int item in itemSet) {
                newSet = newSet.Concat(GetItemSet(item)).ToArray();
            }
            return newSet;
        }
        public static int[] StripSellables(int[] itemSet) => (from item in itemSet where !Sellables.Contains(item) select item).ToArray();
        public static Item[] OffsetInventory(this Item[] inventory, int oldSetLength, int newSetLength) {
            if (oldSetLength != newSetLength) {
                (int, int)[] inventoryTypes = (from item in inventory where item != null select (item.type, item.stack)).ToArray();
                int offset = newSetLength - oldSetLength;
                for (int i = 0; i < inventoryTypes.Length; i++)
                {
                    if (i + offset < 0 || i + offset > inventory.Length - 1)
                    {
                        continue;
                    }
                    
                    if (inventory[i + offset] == null) {
                        inventory[i + offset] = new Item();
                    }
                    inventory[i + offset].SetDefaults(inventoryTypes[i].Item1, false);
                    inventory[i + offset].stack = inventoryTypes[i].Item2;
                }
            }
            return inventory;
					
        }
        public static int IDChest(this Terraria.Tile chest) {
            int chestTileID = chest.TileType;
			int chestType = chest.TileFrameX;
			int chestWall = chest.WallType;
            if (!ChestDict.ContainsKey(chestTileID)) {
                return -1;
            }
            if (chestType % 36 != 0) {
                return -1;
            }
            int ChestID = chestType / 36 + ChestDict[chestTileID];
            if (WallOverride.Keys.Contains((ChestID, chestWall))) {
                return WallOverride[(ChestID, chestWall)];
            }
            return ChestID;
        }
        public static int IDChest(int i, int j) => Main.tile[i, j].IDChest();
        public static bool ChangeChest(int i, int j, int newID)
        {
            ushort tileID;
            if (newID / 52 == 1)
            {
                tileID = TileID.Containers2;
                newID -= 52;
            }
            else
            {
                tileID = TileID.Containers;
            }
            short tileFrameX = (short)(newID * 36);
            for (int i2 = 0; i2 < 2; i2++) for (int j2 = 0; j2 < 2; j2++)
                {
                    Terraria.Tile chestTile = Main.tile[i + i2, j + j2];
                    chestTile.ResetToType(tileID);
                    chestTile.TileFrameX = (short)(tileFrameX + i2 * 18);
                    chestTile.TileFrameY = (short)(j2 * 18);
                }
            return true;
        }
        
        public static int IDTree(int i, int j)
        {
            int treeID = Main.tile[i, j].TileType;
            if (TreeDict.ContainsKey(treeID))
                return TreeDict[treeID];
            if (treeID == 5)
            {
                for (int i2 = 0; i2 < 20; i2++) //counter runtime is arbitrary
                {
                    int belowBlock = Main.tile[i, j + 1].TileType;
                    if (TreeSourceDict.ContainsKey(belowBlock)) return TreeSourceDict[belowBlock];
                    j++;
                }
            }
            return -1;
        }
        
        public static int IDHerb(int i, int j)
        {
            Terraria.Tile herb = Main.tile[i, j];
            int TileType = herb.TileType;
            if (TileType < 82 || 84 < TileType) return -1;
            return herb.TileFrameX / 18;
        }
        public static int IDNPC(this NPC npc) => IDNPC(npc.netID);
        public static int IDNPC(this int id) {
            int? newID = NPCIDSets.UseAsDict(id);
            return newID ?? id;
        }
    }
}