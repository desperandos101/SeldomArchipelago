using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace SeldomArchipelago.Config
{
    public class Config : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("Common")]

        [Label("Name")]
        [DefaultValue("")]
        public string name;

        [Label("Port")]
        [Range(0, 65535)]
        [DefaultValue(38281)]
        public int port;

        [Header("Advanced")]

        [Label("Server Address")]
        [DefaultValue("archipelago.gg")]
        public string address;

        [Label("Password")]
        [DefaultValue("")]
        public string password;

        [Header("Miscellaneous")]

        [Label("Receive Events as Items")]
        [DefaultValue(true)]
        public bool eventsAsItems;

        [Label("Receive Hardmode as Item")]
        [DefaultValue(true)]
        public bool hardmodeAsItem;

    }
}