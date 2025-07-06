using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;
using SeldomArchipelago.Systems;
using Terraria.ID;
using static SeldomArchipelago.Systems.ArchipelagoSystem;

namespace SeldomArchipelago.Locking
{
    internal class CrateLock : GlobalItem
    {
        public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
        {
            if (item.type == ItemID.HerbBag)
            {
                itemLoot.RemoveWhere(i => true);
                itemLoot.Add(new HerbBagDropsItemDropFlagRule());
            }
        }
    }
    public class HerbBagDropsItemDropFlagRule : IItemDropRule // I COPY PASTED THIS FROM SOURCE CODE DON'T LOOK
    {
        public new int[] dropIds() {
            var flags = GetFlags();
            if (flags == null) return FlagSystem.GetAllHerbs();

            return flags.GetLegalHerbs();
    }

        public List<IItemDropRuleChainAttempt> ChainedRules
        {
            get;
            private set;
        }

        public HerbBagDropsItemDropFlagRule()
        {
            ChainedRules = new List<IItemDropRuleChainAttempt>();
        }

        public bool CanDrop(DropAttemptInfo info) => true;

        public ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info)
        {
            ItemDropAttemptResult result;

            int amount = Main.rand.Next(2, 5);
            if (Main.rand.Next(3) == 0)
                amount++;

            for (int i = 0; i < amount; i++)
            {
                int stack = Main.rand.Next(2, 5);
                if (Main.rand.Next(3) == 0)
                    stack += Main.rand.Next(1, 5);
                int[] ids = dropIds();
                CommonCode.DropItem(info, ids[info.rng.Next(ids.Length)], stack);
            }

            result = default(ItemDropAttemptResult);
            result.State = ItemDropAttemptResultState.Success;
            return result;
        }

        public void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo)
        {
            float num = (float)1f / (float)1f;
            float num2 = num * ratesInfo.parentDroprateChance;
            int[] ids = dropIds();
            float dropRate = 1f / (float)(ids.Length + 3.83f) * num2;
            for (int i = 0; i < ids.Length; i++)
            {
                drops.Add(new DropRateInfo(ids[i], 1, 1, dropRate, ratesInfo.conditions));
            }

            Chains.ReportDroprates(ChainedRules, num, drops, ratesInfo);
        }
    }
}
