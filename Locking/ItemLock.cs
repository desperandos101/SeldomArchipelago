using SeldomArchipelago.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static SeldomArchipelago.Systems.ArchipelagoSystem;

namespace SeldomArchipelago.Locking
{
    internal class ItemLock : GlobalItem
    {
        public override bool CanUseItem(Item item, Player player) {
            var flags = GetSession().flagSystem;
            if (!flags.ItemIsUsable(item.type)) {
                Main.NewText("You have not unlocked this event yet!");
                return false;
            }
            if (item.IsGrapplingHook() && !flags.FlagIsActive(FlagID.Hook))
            {
                Main.NewText("You cannot use a grappling hook until you receive the Grappling Hook item.");
                return false;
            }
            return true;

        }
        public override bool ConsumeItem(Item item, Player player) => !new int[] {ItemID.PumpkinMoonMedallion, ItemID.NaughtyPresent}.Contains(item.type);
    }
}
