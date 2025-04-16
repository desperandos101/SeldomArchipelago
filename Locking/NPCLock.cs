using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SeldomArchipelago.Systems;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static SeldomArchipelago.Systems.ArchipelagoSystem;

namespace SeldomArchipelago.Locking
{
    public class NPCDropLock : GlobalItem
    {
        public override void OnSpawn(Item item, IEntitySource source)
        {
            if (source is EntitySource_Loot lootSource && lootSource.Entity is NPC)
            {
                var system = ModContent.GetInstance<ArchipelagoSystem>();
                if (system.Session().enemyItems.Contains(item.Name))
                {
                    Main.NewText("ITEM BLOCKED: " + item.Name);
                    item.TurnToAir();
                }
            }
        }
    }
    public class NPCLock : GlobalNPC
    {
        public class RegionLockCondition(FlagID? region = null, bool evaluateBiomeOnCondition = false) : IItemDropRuleCondition
        {
            private FlagID? region = region;
            public bool CanDrop(DropAttemptInfo info)
            {
                FlagSystem flags = ModContent.GetInstance<ArchipelagoSystem>().Session().flagSystem;
                if (evaluateBiomeOnCondition) return flags.NPCRegionUnlocked(info);
                return flags.FlagIsActive((FlagID)region);
            } 
            public string GetConditionDescription()
            {
                return "Checks if the NPC can drop any items, according to whether its corresponding biome is locked.";
            }
            public bool CanShowItemDropInUI() => true;
        }
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            FlagID? npcBiome = FlagSystem.GetNPCRegion(npc.netID);
            if (npcBiome is not null)
            {
                List<IItemDropRule> ruleList = npcLoot.Get(false);
                npcLoot.RemoveWhere(rule => true, false);
                LeadingConditionRule megaRule = new LeadingConditionRule(new RegionLockCondition((FlagID)npcBiome));
                foreach (IItemDropRule rule in ruleList)
                {
                    megaRule.OnSuccess(rule);
                }
                npcLoot.Add(megaRule);
            }
        }
        public override void ModifyGlobalLoot(GlobalLoot globalLoot)
        {
            List<IItemDropRule> ruleList = globalLoot.Get(false);
            globalLoot.RemoveWhere(rule => true, false);
            LeadingConditionRule megaRule = new LeadingConditionRule(new RegionLockCondition(evaluateBiomeOnCondition: true));
            foreach (IItemDropRule rule in ruleList)
            {
                megaRule.OnSuccess(rule);
            }
            globalLoot.Add(megaRule);
        }
        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo) {
            FlagSystem flags = ModContent.GetInstance<ArchipelagoSystem>().Session().flagSystem;
            flags.SetBoundNPCsInSpawnDict(pool, spawnInfo);
            if (spawnInfo.SpawnTileY <= Main.worldSurface && Main.dayTime && Main.eclipse)
            {
                pool[0] = 0f;
                (int, float)[] dictSet = flags.FlagIsActive(FlagID.EclipseUpgrade) ? ItemRef.eclipseWeights2 : ItemRef.eclipseWeights1;
                foreach ((int, float) tuple in dictSet) pool[tuple.Item1] = tuple.Item2;
                if (flags.FlagIsActive(FlagID.EclipseUpgrade) && !NPC.AnyNPCs(NPCID.Mothron)) pool[NPCID.Mothron] = 1f;
            }
        }
        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            FlagSystem flags = ModContent.GetInstance<ArchipelagoSystem>().Session().flagSystem;
            if (flags.NPCShouldDespawn(npc.type)) npc.EncourageDespawn(0);
        }
        public override void ModifyShop(NPCShop shop)
        {
            List<NPCShop.Entry> entriesToAdd = new List<NPCShop.Entry>();
            foreach (NPCShop.Entry entry in shop.Entries)
            {
                foreach (Condition cond in entry.Conditions)
                {
                    if (cond.Description == Language.GetOrRegister("Conditions.DownedPlantera")) // TODO: Find a better way to identify conditions
                    {
                        entry.Disable();
                        int itemID = entry.Item.type;
                        List<Condition> conds = entry.Conditions.ToList();
                        conds.Remove(cond);
                        conds.Add(Condition.Hardmode);
                        entriesToAdd.Add(new NPCShop.Entry(itemID, conds.ToArray()));

                        break;
                    }
                }
            }
            shop.Add(entriesToAdd.ToArray());
        }
    }
}
