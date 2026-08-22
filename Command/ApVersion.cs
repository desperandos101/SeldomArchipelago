using System;
using SeldomDespArchipelago.Systems;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ModLoader;

namespace SeldomDespArchipelago.Command
{
    public class ApVersion : ModCommand
    {
        public override string Command => "apversion";
        public override CommandType Type => CommandType.Chat;
        public override string Description => "Displays the APWorld version that this client is supposed to connect to.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            string version = ModContent.GetInstance<ArchipelagoSystem>().APversion.ToString();
            caller.Reply($"APWORLD VERSION: {version}");
        }
    }
}