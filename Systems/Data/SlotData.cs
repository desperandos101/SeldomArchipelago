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
using System.IO;
using System.Configuration;
using System.Text;

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
        public bool DepthRando {get; init; }
        public bool Calamity {get; init; }
        public bool Fargo {get; init; }
        public SlotData() {}
        public SlotData(ArchipelagoSession session, LoginSuccessful login)
        {
            Slot = login.Slot;
            Name = session.Players.GetPlayerName(Slot);
            Seed = session.RoomState.Seed;
            Goals = ((JArray)login.SlotData["goal"]).ToObject<string[]>().ToImmutableArray();
            bool isEnabled(string key) => (long)login.SlotData[key] > 0;
            Deathlink = (bool)login.SlotData["deathlink"];
            NpcRando = isEnabled("npc_rando");
            DepthRando = isEnabled("depth_rando");
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
                    locNamesByID[session.Locations.GetLocationIdFromName(SessionState.APWorldName, loc)] = loc;
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
                    throw new Exception("Failed to properly initialize " + nameof(ItemsByNPC) + ". Please reconnect.");
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
                [nameof(DepthRando)] = DepthRando,
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
                DepthRando = tag.GetBool(nameof(DepthRando)),
                Calamity = tag.GetBool(nameof(Calamity)),
                Fargo = tag.GetBool(nameof(Fargo)),
            };
        };
        public void Write(BinaryWriter writer)
        {
            writer.Write(Slot);
            writer.Write(Name);
            writer.Write(Seed);
            writer.Write(string.Join(';', Goals));
            writer.Write(Deathlink);
            writer.Write(NpcRando);
            writer.Write(string.Join(';', RandomizedNPCs));
            writer.Write(string.Join(';', ItemsByNPC.Keys));
            writer.Write(string.Join(';', ItemsByNPC.Values));
            writer.Write(DepthRando);
            writer.Write(Calamity);
            writer.Write(Fargo);
        }
        public static SlotData? ReadSlotData(BinaryReader reader)
        {
            int slot = reader.ReadInt32();
            string name = reader.ReadString();
            string seed = reader.ReadString();
            ImmutableArray<string> goals = reader.ReadString().Split(';').ToImmutableArray();
            bool deathlink = reader.ReadBoolean();
            bool npcRando = reader.ReadBoolean();
            ImmutableHashSet<int> randomizedNPCs = (from i in reader.ReadString().Split(';') select int.Parse(i)).ToImmutableHashSet();
            int[] itemsByNPCKeys = [];
            int[] itemsByNPCValues = [];
            string keyString = reader.ReadString();
            string valueString = reader.ReadString();
            if (!string.IsNullOrEmpty(keyString))
            {
                itemsByNPCKeys = (from i in reader.ReadString().Split(';') select int.Parse(i)).ToArray();
                itemsByNPCValues = (from i in reader.ReadString().Split(';') select int.Parse(i)).ToArray();
            }
            bool depthRando = reader.ReadBoolean();
            bool calamity = reader.ReadBoolean();
            bool fargo = reader.ReadBoolean();
            return new SlotData
            {
                Slot = slot,
                Name = name,
                Seed = seed,
                Goals = goals,
                Deathlink = deathlink,
                NpcRando = npcRando,
                RandomizedNPCs = randomizedNPCs,
                ItemsByNPC = itemsByNPCKeys.Zip(itemsByNPCValues, (k, v) => KeyValuePair.Create(k, v)).ToImmutableDictionary(),
                DepthRando = depthRando,
                Calamity = calamity,
                Fargo = fargo,
            };
        }
        public override string ToString()
        {
            string EnabledOrDisabled(bool b) => b ? "enabled" : "disabled";
            return
            $"SLOT {Slot}, NAME {Name}, SEED {Seed}\n" +
            $"GOALS: {string.Join("; ", Goals)}\n" +
            $"Deathlink is {EnabledOrDisabled(Deathlink)}\n" +
            $"NPC rando is {EnabledOrDisabled(NpcRando)}\n" +
            $"Randomized NPCs: {string.Join("; ", from id in RandomizedNPCs select npcIDtoName[id])}\n" +
            $"NPC to NPC placements: {string.Join("; ", from pair in ItemsByNPC select $"{npcIDtoName[pair.Key]} to {npcIDtoName[pair.Value]}")}\n" +
            $"Depth rando is {EnabledOrDisabled(DepthRando)}\n" +
            $"Calamity is {EnabledOrDisabled(Calamity)}\n" +
            $"Fargo is {EnabledOrDisabled(Fargo)}";
        }
    }
}