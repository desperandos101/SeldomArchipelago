using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using SeldomDespArchipelago.Systems;
using System.Runtime.Serialization;
using Terraria.ModLoader.Config;
using static SeldomDespArchipelago.Systems.ArchipelagoSystem;

namespace SeldomDespArchipelago.Config
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
        [Header("Chat")]

        [Label("Color AP Text")]
        [DefaultValue(true)]
        public bool colorText;

        [Label("Adjust Filter")]
        [Dropdown]
        public ChatSetting chatSettings;

        [Header("Miscellaneous")]

        [Label("Receive Flag As Item")]
        [DefaultListValue("Hardmode")]
        public List<string> manualFlags = [];
        [Header("Debug")]
        [Label("Force NPC Randomization Off")]
        [DefaultValue(false)]
        public bool forceOffNPC;

        [OnDeserialized]
        internal void CheckItems(StreamingContext _)
        {
            name = name.Trim(' ', '\n', '\t');
            address = address.Trim(' ', '\n', '\t');
            password = password.Trim(' ', '\n', '\t');

            string[] flags = ArchipelagoSystem.flags;
            if (manualFlags is null) return;
            int counter = 0;
            HashSet<string> registeredFlags = new();
            string[] lowercaseFlags = (from x in flags select x.ToLower()).ToArray();

            while (counter < manualFlags.Count)
            {
                string item = manualFlags[counter];

                bool itemFound = FindFlag(item, out string fuzzy);

                if (!itemFound)
                {
                    manualFlags[counter] = "???";
                    counter++;
                    continue;
                }

                if (registeredFlags.Contains(item))
                {
                    manualFlags.RemoveAt(counter);
                    continue;
                }

                item = fuzzy ?? item;
                manualFlags[counter] = item;
                registeredFlags.Add(item);
                counter++;
            }
        }
    }
    public enum ChatSetting
    {
        All,
        Grey,
        Filter,
        Disable
    }
}