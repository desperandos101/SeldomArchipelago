using SeldomArchipelago.Systems;
using Terraria;
using Terraria.ModLoader;

namespace SeldomArchipelago.Command
{
    public class ApUnlockBiomeCommand : ModCommand
    {
        public override string Command => "unlockbiome";
        public override CommandType Type => CommandType.World;
        public override string Description => "Unlocks all biomes";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>().world;
            archipelagoSystem.UnlockBiomesNormally();
            Main.NewText("Biomes unlocked.");
        }
    }
}