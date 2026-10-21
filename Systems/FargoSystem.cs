using System;
using System.Collections.Generic;
using System.Reflection;
using FargowiltasSouls;
using FargowiltasSouls.Core.Globals;
using FargowiltasSouls.Core.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using FargowiltasSouls.Content.Bosses.TrojanSquirrel;
using FargowiltasSouls.Content.Bosses.CursedCoffin;
using FargowiltasSouls.Content.Bosses.DeviBoss;
using FargowiltasSouls.Content.Bosses.BanishedBaron;
using FargowiltasSouls.Content.Bosses.Lifelight;
using FargowiltasSouls.Content.Bosses.VanillaEternity;
using FargowiltasSouls.Content.Bosses.Champions.Timber;
using FargowiltasSouls.Content.Bosses.Champions.Terra;
using FargowiltasSouls.Content.Bosses.Champions.Earth;
using FargowiltasSouls.Content.Bosses.Champions.Nature;
using FargowiltasSouls.Content.Bosses.Champions.Life;
using FargowiltasSouls.Content.Bosses.Champions.Shadow;
using FargowiltasSouls.Content.Bosses.Champions.Will;
using FargowiltasSouls.Content.Bosses.Champions.Cosmos;
using FargowiltasSouls.Content.Bosses.AbomBoss;
using FargowiltasSouls.Content.Bosses.MutantBoss;
using FargowiltasSouls.Content.Bosses.Champions.Spirit;
using FargowiltasSouls.Content.Items.Accessories.Masomode;
using FargowiltasSouls.Content.Projectiles.Minions;
using FargowiltasSouls.Content.Patreon.ParadoxWolf;
using FargowiltasSouls.Content.Patreon.Potato;
using FargowiltasSouls.Content.Items.Accessories.Souls;


namespace SeldomDespArchipelago.Systems
{
    // Direct usage of Fargo must happen in here, else the mod won't compile without Fargo Souls
    [ExtendsFromMod("FargowiltasSouls")]
    public static class FargoSystem
    {
        public static readonly Dictionary<string, int> calItemToType = new()
        {
            {"Concentrated Rainbow Matter", ModContent.ItemType<ConcentratedRainbowMatter>()},
            {"Crystal Skull", ModContent.ItemType<SkullCharm>()},
            {"Frigid Grasp", ModContent.ItemType<FrigidGemstone>()},
            {"Mystic Skull", ModContent.ItemType<MysticSkull>()},
            {"Nymph\'s Perfume", ModContent.ItemType<NymphsPerfume>()},
            {"Paradox Wolf Soul", ModContent.ItemType<ParadoxWolfSoul>()},
            {"Razor Container", ModContent.ItemType<RazorContainer>()},
            {"Squeaky Toy", ModContent.ItemType<SqueakyToy>()},
            {"Tim\'s Conconction", ModContent.ItemType<TimsConcoction>()},
            {"Tribal Charm", ModContent.ItemType<TribalCharm>()},
            {"Wretched Pouch", ModContent.ItemType<WretchedPouch>()},
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

        public static bool? CheckFargoFlag(string flag) => flag switch
        {
            "Post-Trojan Squirrel" => WorldSavingSystem.DownedBoss[(int)WorldSavingSystem.Downed.TrojanSquirrel],
            "Post-Cursed Coffin" => WorldSavingSystem.DownedBoss[(int)WorldSavingSystem.Downed.CursedCoffin],
            "Post-Deviantt" => WorldSavingSystem.DownedDevi,
            "Post-Banished Baron" => WorldSavingSystem.DownedBoss[(int)WorldSavingSystem.Downed.BanishedBaron],
            "Post-Lifelight" => WorldSavingSystem.DownedBoss[(int)WorldSavingSystem.Downed.Lifelight],
            "Post-Betsy" => WorldSavingSystem.DownedBetsy,
            "Post-Champion of Timber" => WorldSavingSystem.DownedBoss[(int)WorldSavingSystem.Downed.TimberChampion],
            "Post-Champion of Terra" => WorldSavingSystem.DownedBoss[(int)WorldSavingSystem.Downed.TerraChampion],
            "Post-Champion of Earth" => WorldSavingSystem.DownedBoss[(int)WorldSavingSystem.Downed.EarthChampion],
            "Post-Champion of Nature" => WorldSavingSystem.DownedBoss[(int)WorldSavingSystem.Downed.NatureChampion],
            "Post-Champion of Life" => WorldSavingSystem.DownedBoss[(int)WorldSavingSystem.Downed.LifeChampion],
            "Post-Champion of Death" => WorldSavingSystem.DownedBoss[(int)WorldSavingSystem.Downed.ShadowChampion],
            "Post-Champion of Spirit" => WorldSavingSystem.DownedBoss[(int)WorldSavingSystem.Downed.SpiritChampion],
            "Post-Champion of Will" => WorldSavingSystem.DownedBoss[(int)WorldSavingSystem.Downed.WillChampion],
            "Post-Eridanus, Champion of Cosmos" => WorldSavingSystem.DownedBoss[(int)WorldSavingSystem.Downed.CosmosChampion],
            "Post-Abominationn" => WorldSavingSystem.DownedAbom,
            "Post-Mutant" => WorldSavingSystem.DownedMutant,
            _ => null,
        };

        private static bool soulOfEternityChecked;

        public static void FargoPostUpdateWorld()
        {
            if (soulOfEternityChecked) return;

            int soulOfEternity = ModContent.ItemType<EternitySoul>();

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];

                if (!player.active)
                    continue;

                if (!player.HasItem(soulOfEternity))
                    continue;

                ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Soul of Eternity");

                soulOfEternityChecked = true;
                break;
            }
        }

        public static void VanillaBossKilled(int boss)
        {
            var npc = new NPC();
            npc.SetDefaults(boss);
            npc.lastInteraction = (byte)Main.myPlayer;
            var fargoNpc = new FargoSoulsGlobalNPC();
            int globalIndex = ModContent.GetInstance<FargoSoulsGlobalNPC>().PerEntityIndex;
            GlobalNPC[] dummyArray = new GlobalNPC[globalIndex + 1];
            dummyArray[globalIndex] = fargoNpc;
            typeof(NPC).GetField("_globals", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(npc, dummyArray);
            var seldomArchipelago = ModContent.GetInstance<SeldomArchipelago>();
            seldomArchipelago.Temp = true;
            fargoNpc.OnKill(npc);
            seldomArchipelago.Temp = false;
        }

        public static void FargoOnKill<T>() where T : ModNPC, new()
        {
            var npc = new T();
            var entity = new NPC
            {
                type = ModContent.NPCType<T>(),
                target = 0
            };
            var fargoNpc = new FargoSoulsGlobalNPC();
            int globalIndex = ModContent.GetInstance<FargoSoulsGlobalNPC>().PerEntityIndex;
            GlobalNPC[] dummyArray = new GlobalNPC[globalIndex + 1];
            dummyArray[globalIndex] = fargoNpc;
            typeof(NPC).GetField("_globals", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(entity, dummyArray);
            typeof(ModType<NPC>).GetProperty("Entity").SetValue(npc, entity);
            var seldomArchipelago = ModContent.GetInstance<SeldomArchipelago>();
            seldomArchipelago.Temp = true;
            npc.OnKill();
            seldomArchipelago.Temp = false;
        }

        public static void FargoOnKillBetsy()
        {
            var entity = new NPC();
            entity.SetDefaults(NPCID.DD2Betsy);
            entity.target = 0;

            var betsy = new Betsy();

            var seldomArchipelago = ModContent.GetInstance<SeldomArchipelago>();
            seldomArchipelago.Temp = true;
            betsy.OnKill(entity);
            seldomArchipelago.Temp = false;
        }

        public static void FargoOnKillTrojanSquirrel() => FargoOnKill<TrojanSquirrel>();
        public static void FargoOnKillCursedCoffin() => FargoOnKill<CursedCoffin>();
        public static void FargoOnKillDeviantt() => FargoOnKill<DeviBoss>();
        public static void FargoOnKillBanishedBaron() => FargoOnKill<BanishedBaron>();
        public static void FargoOnKillLifelight() => FargoOnKill<LifeChallenger>();
        public static void FargoOnKillTimberChampion() => FargoOnKill<TimberChampion>();
        public static void FargoOnKillTerraChampion() => FargoOnKill<TerraChampion>();
        public static void FargoOnKillEarthChampion() => FargoOnKill<EarthChampion>();
        public static void FargoOnKillNatureChampion() => FargoOnKill<NatureChampion>();
        public static void FargoOnKillLifeChampion() => FargoOnKill<LifeChampion>();
        public static void FargoOnKillDeathChampion() => FargoOnKill<ShadowChampion>();
        public static void FargoOnKillSpiritChampion() => FargoOnKill<SpiritChampion>();
        public static void FargoOnKillWillChampion() => FargoOnKill<WillChampion>();
        public static void FargoOnKillCosmosChampion() => FargoOnKill<CosmosChampion>();
        public static void FargoOnKillAbominationn() => FargoOnKill<AbomBoss>();
        public static void FargoOnKillMutant() => FargoOnKill<MutantBoss>();
    }
}