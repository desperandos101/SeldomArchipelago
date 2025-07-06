using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using System;
using SeldomArchipelago;
using Terraria.GameContent.Ambience;
using System.Collections.Generic;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net.Models;

using static SeldomArchipelago.Systems.ArchipelagoSystem;
using System.Linq;
using static SeldomArchipelago.SeldomArchipelago;
using SeldomArchipelago.Systems;
using Steamworks;
using rail;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;

namespace SeldomArchipelago.ArchipelagoItem
{
    public class ArchipelagoItem : ModItem
    {
        private const string inactive = "Inactive AP Item";
        public const string dummy = "null";
        private string? locType;
        private string? locName = null;
        public static List<(string, string, int)>[] chestMatrix = new List<(string, string, int)>[9];
        private bool CheckTypeExhausted => GetSession().locGroupRewardNames[locType].Count == 0;
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.maxStack = 999;
            Item.value = 100;
            Item.rare = ItemRarityID.Blue;
            // Set other Item.X values here
        }
        public void SetCheckType(string loc) => locType = loc;
        public void SetCheck()
        {
            if (locType == null) throw new Exception("Attempted to SetCheck an Architem with no locType assigned.");

            if (locName is not null) return;

            if (locType == dummy)
            {
                locName = "Dummy Archipelago Item";
                return;
            }
            
            SessionMemory state = GetSession();

            if (state is null)
            {
                throw new Exception("Attempted to activate an APitem in a non-AP world!");
            }

            if (!state.locGroupRewardNames.ContainsKey(locType))
            {
                throw new Exception("ArchItem was a locType that didn't exist in locGroupRewardNames.");
            }

            if (state.locGroupRewardNames[locType].Count == 0)
            {
                locName = inactive;
                Item.SetNameOverride(inactive);
                Item.TurnToAir();
                return;
            }
            (string, string) tuple = state.locGroupRewardNames[locType][0];

            locName = tuple.Item1;
            Item.SetNameOverride(tuple.Item1);
        }
        public void SetCheck(string loc)
        {
            SetCheckType(loc);
            SetCheck();
        }
        public void SetShopCheck(string locKey, string locationName, string itemName)
        {
            locType = locKey;
            locName = locationName;
            Item.SetNameOverride(itemName);
        }
        public static ArchipelagoItem CreateItem(string loc)
        {
            var item = new Item(ModContent.ItemType<ArchipelagoItem>());
            var archItem = item.ModItem as ArchipelagoItem;
            archItem.SetCheck(loc);
            return archItem;
        }
        public static ArchipelagoItem CreateDummyItem() => CreateItem(dummy);
        public override Microsoft.Xna.Framework.Color? GetAlpha(Microsoft.Xna.Framework.Color lightColor)
        {
            if (locName == inactive)
            {
                lightColor.R = 0;
                lightColor.G = 0;
                lightColor.B = 0;
                return lightColor;
            }
            return null;
        }
        public override void PostUpdate()
        {
            if (CheckTypeExhausted)
            {
                Item.TurnToAir();
                return;
            }
        }
        public override void UpdateInventory(Player player)
        {
            if (locType == dummy)
            {
                Main.NewText("Huzzah and forsooth, the dummy item has activated!");
            } else if (locName == inactive)
            {
                Main.NewText("Dumb Stupid Item Idiot");
            } else
            {
                ArchipelagoSystem system = ModContent.GetInstance<ArchipelagoSystem>();
                if (locName is not null) system.QueueLocationKey(locType, locName);
                else system.QueueLocationKey(locType);
            }
            Item.TurnToAir();
            /*Main.AmbienceServer.ForceEntitySpawn(new AmbienceServer.AmbienceSpawnInfo
            {
                skyEntityType = SkyEntityType.Meteor,
                targetPlayer = -1
            });*/
        }
        public override void SaveData(TagCompound tag)
        {
            tag[nameof(locType)] = (string)locType;
            if (locName is not null) tag[nameof(locName)] = locName;
        }
        public override void LoadData(TagCompound tag)
        {
            locType = tag.ContainsKey(nameof(locType)) ? tag.GetString(nameof(locType)) : null;
            locName = tag.ContainsKey(nameof(locName)) ? tag.GetString(nameof(locName)) : null;
        }
    }
}