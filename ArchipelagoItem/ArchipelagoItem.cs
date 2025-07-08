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
        private SimpleItemInfo info = null;
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

            if (info is not null) return;

            if (locType == dummy)
            {
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
                Item.SetNameOverride(inactive);
                Item.TurnToAir();
                return;
            }
            info = state.locGroupRewardNames[locType][0];

            Item.SetNameOverride($"{info.player}'s {info.itemName}");
        }
        public void SetCheck(string loc)
        {
            SetCheckType(loc);
            SetCheck();
        }
        public void SetShopCheck(string locKey, SimpleItemInfo info)
        {
            locType = locKey;
            this.info = info;
            Item.SetNameOverride($"{info.player}'s {info.itemName}");
            if (info.player == GetSession().SlotName && info.itemName == "Reward: Coins")
            {
                Item.value = 1;
                return;
            }
            switch (info.flag)
            {
                case Archipelago.MultiClient.Net.Enums.ItemFlags.NeverExclude: Item.value = 5000; break;
                case Archipelago.MultiClient.Net.Enums.ItemFlags.Advancement: Item.value = 50000; break;
                default: Item.value = 100; break;
            }
        }
        public static ArchipelagoItem CreateItem(string loc)
        {
            var item = new Item(ModContent.ItemType<ArchipelagoItem>());
            var archItem = item.ModItem as ArchipelagoItem;
            archItem.SetCheck(loc);
            return archItem;
        }
        public static ArchipelagoItem CreateDummyItem() => CreateItem(dummy);
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
            } 
            {
                ArchipelagoSystem system = ModContent.GetInstance<ArchipelagoSystem>();
                if (info.locName is not null) system.QueueLocationKey(locType, info);
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
            if (info is not null) tag[nameof(info)] = info;
        }
        public override void LoadData(TagCompound tag)
        {
            locType = tag.ContainsKey(nameof(locType)) ? tag.GetString(nameof(locType)) : null;
            info = tag.ContainsKey(nameof(info)) ? tag.Get<SimpleItemInfo>(nameof(info)) : null;
        }
    }
}