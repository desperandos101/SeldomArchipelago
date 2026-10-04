using Archipelago.MultiClient.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.Linq;
using System.Data;
using static SeldomDespArchipelago.Systems.ArchipelagoSystem;

namespace SeldomDespArchipelago.Systems.Data
{
    /// <summary>
    /// Contains static information about an Archipelago slot. 
    /// </summary>
    public readonly struct SlotData : TagSerializable
    {
        public int Slot {get; init; }
        public string Name {get; init; }
        public string Seed {get; init; }
        public ImmutableArray<string> Goals {get; init; }
        public bool Deathlink {get; init;}
        public bool NpcRando {get; init; }
        public ImmutableHashSet<int> RandomizedNPCs {get; init; }
        // Dict of loc npc ids to item npc ids, if a player's npc item happens to be placed in one of their npc locations.
        // If this is the case, we can transform the ghost/bound npc into the item npc as soon as it is activated, for both expediency and cuteness.
        public ImmutableDictionary<int, int> ItemsByNPC {get; init; }
        public bool Calamity {get; init; }
        public bool Fargo {get; init; }
        public SlotData() {}
        public SlotData(ArchipelagoSession session, LoginSuccessful login)
        {
            Slot = login.Slot;
            Name = session.Players.GetPlayerName(Slot);
            Seed = session.RoomState.Seed;
            Goals = ((JArray)login.SlotData["goal"]).ToObject<string[]>().ToImmutableArray();
            bool isEnabled(string key) => (long)login.SlotData[key] == 1;
            Deathlink = (bool)login.SlotData["deathlink"];
            NpcRando = isEnabled("npc_rando");
            Calamity = isEnabled("calamity");
            Fargo = isEnabled("fargo");
            RandomizedNPCs = null;
            var itemsByNPC = new Dictionary<int, int>();
            if (NpcRando)
            {
                string[] randomizedNPCnames = ((JArray)login.SlotData["randomize_npcs"]).ToObject<string[]>();
                RandomizedNPCs = (from name in randomizedNPCnames select npcNameToID[name]).ToImmutableHashSet();
                string[] allNPCnames = npcNameToID.Keys.ToArray();
                var locNamesByID = new Dictionary<long, string>();
                foreach (string loc in allNPCnames)
                {
                    locNamesByID[session.Locations.GetLocationIdFromName(APWorldName, loc)] = loc;
                }
                if (locNamesByID.ContainsKey(-1))
                {
                    throw new Exception($"Some retrieved NPC locations turned up -1 ids.");
                }
                var task = session.Locations.ScoutLocationsAsync(locNamesByID.Keys.ToArray());
                if (task.Wait(1000))
                {
                    var locByID = task.Result;
                    foreach (long key in locByID.Keys)
                    {
                        var itemInfo = locByID[key];
                        if (itemInfo.Player.Slot == Slot && allNPCnames.Contains(itemInfo.ItemName))
                        {
                            int npcType = npcNameToID[locNamesByID[key]];
                            itemsByNPC[npcType] = npcNameToID[itemInfo.ItemName];
                        }
                    }
                }
                else  // TODO: Have better backup plan
                {
                    ModContent.GetInstance<ArchipelagoSystem>().Mod.Logger.Info("Failed to properly initialize " + nameof(ItemsByNPC));
                }
            }
            ItemsByNPC = NpcRando ? itemsByNPC.ToImmutableDictionary() : null;
        }
        public TagCompound SerializeData()
        {
            var tag = new TagCompound
            {
                [nameof(Slot)] = Slot,
                [nameof(Name)] = Name,
                [nameof(Seed)] = Seed,
                [nameof(Goals)] = Goals.ToList(),
                [nameof(Deathlink)] = Deathlink,
                [nameof(NpcRando)] = NpcRando,
                [nameof(Calamity)] = Calamity,
                [nameof(Fargo)] = Fargo,
            };
            if (NpcRando)
            {
                tag[nameof(RandomizedNPCs)] = RandomizedNPCs.ToList();
                tag[nameof(ItemsByNPC)+"Keys"] = ItemsByNPC.Keys.ToList();
                tag[nameof(ItemsByNPC)+"Values"] = ItemsByNPC.Values.ToList();
            }
            return tag;
        }
        public static readonly Func<TagCompound, SlotData> DESERIALIZER = (tag) =>
        {
            // Outside constructor for slightly enhanced readability
            bool npcRando = tag.GetBool(nameof(NpcRando));
            var npcs = npcRando ? tag.Get<List<int>>(nameof(ItemsByNPC)+"Keys") : null;
            var items = npcRando ? tag.Get<List<int>>(nameof(ItemsByNPC)+"Values") : null;
            return new SlotData()
            {
                Slot = tag.GetInt(nameof(Slot)),
                Name = tag.GetString(nameof(Name)),
                Seed = tag.GetString(nameof(Seed)),
                Goals = tag.GetList<string>(nameof(Goals)).ToImmutableArray(),
                Deathlink = tag.GetBool(nameof(Deathlink)),
                NpcRando = npcRando,
                RandomizedNPCs = npcRando ? tag.GetList<int>(nameof(RandomizedNPCs)).ToImmutableHashSet() : null,
                ItemsByNPC = npcRando ? npcs.Zip(items, (k, v) => new { Key = k, Value = v}).ToImmutableDictionary(x => x.Key, x => x.Value) : null,
                Calamity = tag.GetBool(nameof(Calamity)),
                Fargo = tag.GetBool(nameof(Fargo)),
            };
        };
        /*
        public override bool Equals(object obj)
        {
            if (obj is not SlotData otherSlot) return false;
            return Name == otherSlot.Name && Seed == otherSlot.Seed;
        }
        */
    }
}