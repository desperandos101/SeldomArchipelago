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
        ImmutableHashSet<string> sentLocations;
        ImmutableHashSet<string> receivedItems;
        public List<string> locationBacklog;
        OfflineCache() {}
        public OfflineCache(SessionState sess)
        {
            slotData = sess.slotData;
            sentLocations = (from loc in sess.session.Locations.AllLocationsChecked select sess.session.Locations.GetLocationNameFromId(loc)).ToImmutableHashSet();
            receivedItems = (from item in sess.session.Items.AllItemsReceived select item.ItemName).ToImmutableHashSet();
            locationBacklog = [];
        }
        public void QueueLocation(string loc) => locationBacklog.Add(loc);
        public bool Sent(string loc) => sentLocations.Contains(loc) || locationBacklog.Contains(loc);
        public bool Received(string item) => receivedItems.Contains(item);
        public TagCompound SerializeData()
        {
            return new TagCompound
            {
                [nameof(slotData)] = slotData,
                [nameof(sentLocations)] = sentLocations.ToList(),
                [nameof(receivedItems)] = receivedItems.ToList(),
                [nameof(locationBacklog)] = locationBacklog,
            };
        }
        public static readonly Func<TagCompound, OfflineCache> DESERIALIZER = (tag) =>
        {
            return new OfflineCache
            {
                slotData = tag.Get<SlotData>(nameof(slotData)),
                sentLocations = tag.GetList<string>(nameof(sentLocations)).ToImmutableHashSet(),
                receivedItems = tag.GetList<string>(nameof(receivedItems)).ToImmutableHashSet(),
                locationBacklog = tag.Get<List<string>>(nameof(locationBacklog)),
            };
        };
    }
}