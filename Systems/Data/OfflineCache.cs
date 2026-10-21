using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Terraria.ModLoader.IO;
using System.Linq;
using SeldomDespArchipelago.Systems.Data;
using System.Data;

namespace SeldomDespArchipelago.Systems.Data
{
    /// <summary>
    /// Backup of room data used to facillitate offline play.
    /// </summary>
    public class OfflineCache : TagSerializable
    {
        public SlotData slotData;
        public ImmutableHashSet<string> sentLocations;
        public List<string> receivedItems;
        public ImmutableHashSet<string> locationBacklog;
        OfflineCache() {}
        public OfflineCache(SessionState sess)
        {
            slotData = sess.slotData;
            sentLocations = (from loc in sess.session.Locations.AllLocationsChecked select sess.session.Locations.GetLocationNameFromId(loc)).ToImmutableHashSet();
            receivedItems = (from item in sess.session.Items.AllItemsReceived select item.ItemName).ToList();
            locationBacklog = [];
        }
        public void QueueLocation(string loc)
        {
            if (!receivedItems.Contains(loc)) locationBacklog.Add(loc);
        }
        public bool Sent(string loc) => sentLocations.Contains(loc) || locationBacklog.Contains(loc);
        public int ReceivedAmount(string item) => receivedItems.Count(i => i == item);
        public bool Received(string item) => receivedItems.Contains(item);
        public TagCompound SerializeData()
        {
            return new TagCompound
            {
                [nameof(slotData)] = slotData,
                [nameof(sentLocations)] = sentLocations.ToList(),
                [nameof(receivedItems)] = receivedItems.ToList(),
                [nameof(locationBacklog)] = locationBacklog.ToList(),
            };
        }
        public static readonly Func<TagCompound, OfflineCache> DESERIALIZER = (tag) =>
        {
            return new OfflineCache
            {
                slotData = tag.Get<SlotData>(nameof(slotData)),
                sentLocations = tag.GetList<string>(nameof(sentLocations)).ToImmutableHashSet(),
                receivedItems = tag.GetList<string>(nameof(receivedItems)).ToList(),
                locationBacklog = tag.Get<List<string>>(nameof(locationBacklog)).ToImmutableHashSet(),
            };
        };
    }
}