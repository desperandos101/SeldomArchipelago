using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using MyExtensions;
using static SeldomArchipelago.Systems.ArchipelagoSystem;

namespace SeldomArchipelago.Systems
{
    public static class LocationSystem
    {
        #region Data
        private static Dictionary<FlagID, string> biomeToChestLocationName = new Dictionary<FlagID, string>()
        {
            {FlagID.Forest, "Gold or Wooden"},
            {FlagID.Granite, "Granite" },
            {FlagID.Marble, "Marble" },
            {FlagID.Web, "Web Covered" },
            {FlagID.Snow, "Frozen"},
            {FlagID.Desert, "Sandstone"},
            {FlagID.Jungle, "Ivy or Mahogany"},
            {FlagID.Ocean, "Water"},
            {FlagID.Sky, "Floating Island" },
            {FlagID.Mushroom, "Mushroom"},
            {FlagID.Dungeon, "Dungeon"},
            {FlagID.Underworld, "Shadow"},
        };
        public const string EvilOrb = "Shadow/Crimson Orb";
        public static readonly (string, string[])[] npcNameToArchName = new (string, string[])[] {
            ("Jellyfish", new string[] {"Green Jellyfish", "Blue Jellyfish", "Pink Jellyfish"}),
            ("Crawdads, Shellies, and Salamanders", new string[] {"Crawdad", "Giant Shelly", "Salamander"}),
            ("Desert Spirit or Sand Poacher", new string[] {"Desert Spirit", "Sand Poacher"})
        };
        #endregion
        public static FlagID[] GetChestFlags() => biomeToChestLocationName.Keys.ToArray();
        public static string GetChestName(FlagID flag) => $"{biomeToChestLocationName[flag]} Chest";
        public static string GetNPCLocKey(string name) => npcNameToArchName.UseAsDict(name) ?? name;

        public static string[] GetAllLocNames()
        {
            List<String> list = new List<string>();
            foreach (String chestLoc in biomeToChestLocationName.Values)
            {
                list.Add($"{chestLoc} Chest");
            }
            list.Add(EvilOrb);
            return list.ToArray();
        }
    }
}
