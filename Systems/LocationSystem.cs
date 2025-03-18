﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SeldomArchipelago.Systems.ArchipelagoSystem;

namespace SeldomArchipelago.Systems
{
    public static class LocationSystem
    {
        #region Data
        private static Dictionary<FlagID, string> biomeToChestLocationName = new Dictionary<FlagID, string>()
        {
            {FlagID.Forest, "Gold or Wooden"},
            {FlagID.Snow, "Frozen"},
            {FlagID.Desert, "Sandstone"},
            {FlagID.Jungle, "Ivy or Mahogany"},
            {FlagID.Ocean, "Water"},
            {FlagID.Sky, "Floating Island" },
            {FlagID.Mushroom, "Mushroom Biome"},
            {FlagID.Dungeon, "Dungeon"},
            {FlagID.Underworld, "Shadow"},
        };
        public const string EvilOrb = "Shadow/Crimson Orb";
        #endregion
        public static FlagID[] GetChestFlags() => biomeToChestLocationName.Keys.ToArray();
        public static string GetChestName(FlagID flag) => $"{biomeToChestLocationName[flag]} Chest";
    }
}
