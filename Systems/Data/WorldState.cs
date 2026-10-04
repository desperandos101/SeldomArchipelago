using System;
using System.Collections.Generic;
using Terraria.ModLoader.IO;
using System.Linq;

namespace SeldomDespArchipelago.Systems.Data
{
    /// <summary>
    /// Contains state information about a specific world's past interaction with a multiworld room.
    /// </summary>
    public class WorldState : TagSerializable
    {
        public static readonly Func<TagCompound, WorldState> DESERIALIZER = LoadFromTagCompound;
        // Achievements can be completed while loading into the world, but those complete before
        // `ArchipelagoPlayer::OnEnterWorld`, where achievements are reset, is run. So, this
        // keeps track of which achievements have been completed since `OnWorldLoad` was run, so
        // `ArchipelagoPlayer` knows not to clear them.
        public List<string> achieved = new List<string>();
        // Number of items the player has collected in this world
        public int collectedItems;
        // List of rewards received in this world, so they don't get reapplied. Saved in the
        // Terraria world instead of Archipelago data in case the player is, for example,
        // playing Hardcore and wants to receive all the rewards again when making a new player/
        // world.
        public List<int> receivedRewards = new List<int>();
        // List of flags that have been received but not triggered
        public HashSet<string> suspendedFlags = new HashSet<string>();
        // Contains all ghosts that are available to spawn.
        public Queue<int> ghostNPCqueue = new();
        public TagCompound SerializeData()
        {
            var tag = new TagCompound
            {
                [nameof(collectedItems)] = collectedItems,
                [nameof(receivedRewards)] = receivedRewards,
                [nameof(suspendedFlags)] = suspendedFlags.ToList(),
            };
            return tag;
        }
        public static WorldState LoadFromTagCompound(TagCompound tag)
        {
            var world = new WorldState();
            world.collectedItems = tag.GetInt(nameof(collectedItems));
            world.receivedRewards = tag.Get<List<int>>(nameof(receivedRewards));
            world.suspendedFlags = tag.Get<List<string>>(nameof(suspendedFlags)).ToHashSet();
            return world;
        }
    }
}