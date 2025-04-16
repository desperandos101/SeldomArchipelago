using SeldomArchipelago.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static SeldomArchipelago.Systems.ArchipelagoSystem;

namespace SeldomArchipelago.HardmodeItem
{
    public class HardmodeStarter : ModItem
    {
        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.DemonHeart);
        }

        public override bool CanUseItem(Player player) => ModContent.GetInstance<ArchipelagoSystem>().Session().flagSystem.FlagIsActive(FlagID.Hardmode) && !Main.hardMode;

        public override bool? UseItem(Player player)
        {
            ModContent.GetInstance<ArchipelagoSystem>().Session().ActivateHardmode();
            return true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            List<string> lockedLocations = ModContent.GetInstance<ArchipelagoSystem>().Session().hardmodeBacklog;
            tooltips.Add(new TooltipLine(Mod, "Tooltip0", "The following items will be received on activation:"));
            int counter = 0;
            while (lockedLocations.Count > counter)
            {
                tooltips.Add(new TooltipLine(Mod, $"Tooltip{counter+1}", lockedLocations[counter]));
                counter++;
            }
        }
    }
}
