using CalamityMod.Items.LabFinders;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.Projectiles.Boss;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SeldomArchipelagoBeta.NPCs;
using SeldomArchipelagoBeta.Players;
using SeldomArchipelagoBeta.Systems;
using SeldomArchipelagoBeta.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Terraria;
using Terraria.Achievements;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Achievements;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

// Note: When pulling commits from here to main, this namespace needs to be refactored.
namespace SeldomArchipelagoBeta
{
    // TODO Use a data-oriented approach to get rid of all this repetition

    public class SeldomArchipelago : Mod
    {
        // We reuse some parts of Terraria's code for multiple purposes in this mod. For example,
        // when you kill a boss, we have to prevent that code from making permanent world changes
        // and instead send a location, but we reuse that same code when making permanent changes
        // after receiving a boss flag as an item, so we have to not prevent the code from making
        // such changes in that case. So, we use this flag to determine whether the code is run by
        // the game naturally (false) or run by us (true). Terraria is single-threaded, don't worry.
        public bool tempValue;
        public bool Temp
        {
            get => tempValue;
            set
            {
                Logger.Info($"TEMP VALUE SET TO: {value}");
                tempValue = value;
            }
        }

        public static MethodInfo leviathanRealOnKill = null;
        private static readonly (Version Min, Version Max) calVersionRange = (new Version(2, 2, 2), new Version(2, 2, 4));
        private static readonly (Version Min, Version Max) fargoVersionRange = (new Version(1, 7, 3), new Version(1, 7, 3, 9));
        private static readonly (Version Min, Version Max) fargoCalVersionRange = (new Version(1, 2, 0, 27), new Version(1, 2, 0, 28));
        private static void CheckCompatibility(Mod mod, (Version Min, Version Max) versionRange)
        {
            if (mod.Version < versionRange.Min)
            {
                throw new Exception($"You are using an older version of {mod.DisplayNameClean}.\nPlease reload with version {versionRange.Item2}.");
            }

            if (mod.Version > versionRange.Max)
            {
                throw new Exception($"You are using a newer version of {mod.DisplayNameClean}.\nThis is probably because the mod recently received an update.\nPlease downpatch to version {versionRange.Item2}.");
            }
        }
        private static Mod CheckMod(string modName, (Version Min, Version Max) versionRange)
        {
            if (!ModLoader.HasMod(modName)) return null;

            Mod mod = ModLoader.GetMod(modName);
            CheckCompatibility(mod, versionRange);
            return mod;
        }

        public override void Load()
        {
            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();
            if (ModLoader.HasMod("SeldomArchipelago") || ModLoader.HasMod("SeldomArchipelagoExtended") || ModLoader.HasMod("SpikersArchipelago"))
            {
                throw new Exception("Multiple Archipelago mods detected! Make sure only one is enabled at a time.");
            }

            // Begin cursed IL editing
            #region Vanilla IL Edits
            // Old Man helper method
            // Note that the two IL edits pertaining to the Old Man are currently redundant.
            // However they will be preserved in case of future changes to his spawn conditions
            bool OldManSpawnBlocked() => NPC.downedBoss3;

            // Manage Town/Ghost NPC Spawn Conditions
            IL_Main.UpdateTime_SpawnTownNPCs += il =>
            {
                var cursor = new ILCursor(il);

                // SKip Active NPC Count Loop (Prevents local bools from getting set)
                cursor.GotoNext(i => i.MatchLdsfld(typeof(Main).GetField(nameof(Main.npc))));
                cursor.GotoNext(i => i.MatchLdfld(typeof(Entity).GetField(nameof(Entity.active))));
                cursor.Index++;
                cursor.EmitPop();
                cursor.EmitLdcI4(0);

                // Disable Static Bools
                cursor.GotoNext(i => i.MatchLdsfld(typeof(WorldGen).GetField(nameof(WorldGen.prioritizedTownNPCType))));
                cursor.Index++;
                cursor.EmitDelegate((int prioritizedType) =>
                {
                    NPC.unlockedArmsDealerSpawn = false;
                    NPC.unlockedDemolitionistSpawn = false;
                    NPC.unlockedDyeTraderSpawn = false;
                    NPC.unlockedMerchantSpawn = false;
                    NPC.unlockedNurseSpawn = false;
                    NPC.unlockedPartyGirlSpawn = false;
                    NPC.unlockedPrincessSpawn = false;
                    NPC.unlockedTruffleSpawn = false;
                    return prioritizedType;
                });

                // Count NPCs
                cursor.GotoNext(i => i.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedBoss3))));
                cursor.EmitDelegate(() =>
                {
                    int count = 0;
                    for (int i = 0; i < Main.npc.Length; i++)
                    {
                        if (Main.npc[i].active && Main.npc[i].townNPC && Main.npc[i].ModNPC is not GhostNPC)
                        {
                            count++;
                        }
                    }
                    return count;
                });
                cursor.EmitStloc(40);  // NPC Count

                // Old Man
                cursor.Index++;
                cursor.EmitPop();
                cursor.EmitDelegate(() =>
                {
                    return OldManSpawnBlocked() || NPC.AnyNPCs(NPCID.OldMan);
                });

                // Town NPCs
                cursor.GotoNext(i => i.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.unlockedSlimeGreenSpawn))));
                cursor.GotoNext(i => i.MatchLdsfld(typeof(WorldGen).GetField(nameof(WorldGen.prioritizedTownNPCType))));
                cursor.Index++;
                cursor.EmitPop();
                cursor.EmitLdloc(40);
                cursor.EmitDelegate((int npcCount) =>
                {
                    // Collect NPCs & Ghosts
                    HashSet<int> existingTownTypes = new();
                    HashSet<int> existingGhostTypes = new();
                    foreach (NPC npc in Main.npc)
                    {
                        if (!npc.active) continue;
                        if (npc.townNPC)
                        {
                            existingTownTypes.Add(npc.type);
                        }
                        if (npc.ModNPC is GhostNPC ghost)
                        {
                            existingGhostTypes.Add(ghost.GhostType);
                        }
                    }
                    // Nurse, Demolitionist, Princess Overrides
                    if (existingTownTypes.Contains(NPCID.Merchant))
                    {
                        Main.townNPCCanSpawn[NPCID.Nurse] = NPC.SpawnAllowed_Nurse();
                        Main.townNPCCanSpawn[NPCID.Demolitionist] = NPC.SpawnAllowed_Demolitionist();
                        HashSet<int> princessNPCs =
                        [
                            NPCID.Guide,
                            NPCID.Merchant,
                            NPCID.Nurse,
                            NPCID.Demolitionist,
                            NPCID.DyeTrader,
                            NPCID.Angler,
                            NPCID.BestiaryGirl,
                            NPCID.Dryad,
                            NPCID.Painter,
                            NPCID.Golfer,
                            NPCID.ArmsDealer,
                            NPCID.DD2Bartender,
                            NPCID.Stylist,
                            NPCID.GoblinTinkerer,
                            NPCID.WitchDoctor,
                            NPCID.Clothier,
                            NPCID.Mechanic,
                            NPCID.PartyGirl,
                            NPCID.Wizard,
                            NPCID.TaxCollector,
                            NPCID.Truffle,
                            NPCID.Pirate,
                            NPCID.Steampunker,
                            NPCID.Cyborg
                        ];
                        if (princessNPCs.IsSubsetOf(existingTownTypes))
                        {
                            Main.townNPCCanSpawn[NPCID.Princess] = true;
                        }
                    }
                    if (archipelagoSystem.world.NPCRandoActive())
                    {
                        // Evaluate and Build
                        HashSet<int> validGhostTypes = new();
                        foreach (int type in archipelagoSystem.world.randomizedNPCs)
                        {
                            if (Main.townNPCCanSpawn[type] && GhostNPC.GhostableType(type) && !existingGhostTypes.Contains(type))
                                validGhostTypes.Add(type);
                            Main.townNPCCanSpawn[type] = archipelagoSystem.world.receivedNPCs.Contains(type);
                        }

                        // Enqueue Ghosts
                        if (archipelagoSystem.session is not null)
                            foreach (int type in validGhostTypes)
                            {
                                long npcAsLoc = archipelagoSystem.session.session.Locations.GetLocationIdFromName(ArchipelagoSystem.APWorldName, ArchipelagoSystem.npcIDtoName[type]);
                                if (!archipelagoSystem.world.ghostNPCqueue.Contains(type) && !archipelagoSystem.session.session.Locations.AllLocationsChecked.Contains(npcAsLoc))
                                    archipelagoSystem.world.ghostNPCqueue.Enqueue(type);
                            }
                    }
                    // Block Duplicate Normal NPCs
                    foreach (int type in existingTownTypes)
                    {
                        Main.townNPCCanSpawn[type] = false;
                    }
                    // Check Modded NPCs and set prioritizedNPC if Modded NPC can spawn
                    NPCLoader.CanTownNPCSpawn(npcCount);
                    if (WorldGen.prioritizedTownNPCType > 0) return;
                    // Set prioritizedNPC if Vanilla NPC can spawn
                    for (int i = 0; i < Main.townNPCCanSpawn.Length; i++)
                    {
                        if (Main.townNPCCanSpawn[i])
                        {
                            WorldGen.prioritizedTownNPCType = i;
                            break;
                        }
                    }
                    // Set prioritizedNPC if Ghost NPC is next
                    int firstGhost = archipelagoSystem.world.ghostNPCqueue.FirstOrDefault();
                    if (archipelagoSystem.world.ghostNPCqueue.Count == 1 && ArchipelagoSystem.specialSpawnGhosts.Contains(firstGhost))
                    {
                        Main.townNPCCanSpawn[firstGhost] = true;
                        WorldGen.prioritizedTownNPCType = firstGhost;
                    }
                    else if (archipelagoSystem.world.ghostNPCqueue.Count > 0)
                    {
                        Main.townNPCCanSpawn[NPCID.BlueSlime] = true;
                        WorldGen.prioritizedTownNPCType = NPCID.BlueSlime;
                        return;
                    }
                    ;
                });
                cursor.EmitRet();
            };

            // Bypass AnyNPCs Blocks
            // The method we subscribe here checks multiple times whether an NPC of the type prioritizedTownNPCType exists.
            // If any of those return true, it blocks that NPC from spawning, which is a problem especially for special spawn ghosts that need to spawn while their real counterpart is alive.
            On_WorldGen.IsThereASpawnablePrioritizedTownNPC += (On_WorldGen.orig_IsThereASpawnablePrioritizedTownNPC orig, int x, int y, ref bool canSpawn) =>
            {
                int temp = WorldGen.prioritizedTownNPCType;
                if (ArchipelagoSystem.specialSpawnGhosts.Contains(temp))
                {
                    bool result = orig(x, y, ref canSpawn);
                    if (result && !canSpawn) // This specific combo tells us the house is suited to the special NPC, and that AnyNPCs was tripped
                    {
                        canSpawn = true;
                    }
                    return result;
                }
                else if (temp == NPCID.BlueSlime)
                {
                    WorldGen.prioritizedTownNPCType = 0;
                    bool result = orig(x, y, ref canSpawn);
                    WorldGen.prioritizedTownNPCType = temp;
                    return result;
                }

                return orig(x, y, ref canSpawn);
            };

            // Manage Ghost Spawning and Block No Hobo Achievement For Ghosts
            IL_WorldGen.SpawnTownNPC += il =>
            {
                var cursor = new ILCursor(il);
                var npcLabel = il.DefineLabel();
                var ghostLabel = il.DefineLabel();

                cursor.GotoNext(i => i.MatchCall(out var mref) && mref.Name == "NotifyProgressionEvent");
                cursor.EmitLdloc0();
                cursor.EmitDelegate((int achievement, int npcIndex) =>
                {
                    NPC npc = Main.npc[npcIndex];
                    if (npc.ModNPC is not GhostNPC)
                    {
                        AchievementsHelper.NotifyProgressionEvent(achievement);
                    }
                });
                cursor.Remove();

                cursor.GotoNext(i => i.MatchCall(out var mref) && mref.Name == "NewNPC");
                cursor.Remove();
                cursor.EmitDelegate((IEntitySource source, int x, int y, int type, int mysteryNumber, float _, float _, float _, float _, int target) => // whatever floats ur boat, dude
                {
                    int itemsChecked = 0;
                    while (itemsChecked < archipelagoSystem.world.ghostNPCqueue.Count)
                    {
                        int ghostID = archipelagoSystem.world.ghostNPCqueue.Dequeue();
                        if (type != ghostID && ArchipelagoSystem.specialSpawnGhosts.Contains(ghostID))
                        {
                            archipelagoSystem.world.ghostNPCqueue.Enqueue(ghostID);
                        }
                        else
                        {
                            int ghostIndex = NPC.NewNPC(source, WorldGen.bestX * 16, WorldGen.bestY * 16, ModContent.NPCType<GhostNPC>(), mysteryNumber, 0f, 0f, 0f, 0f, target);
                            NPC ghost = Main.npc[ghostIndex];
                            ghost.homeTileX = WorldGen.bestX;
                            ghost.homeTileY = WorldGen.bestY;
                            GhostNPC modGhost = ghost.ModNPC as GhostNPC;
                            modGhost.SetGhostType(ghostID);
                            return ghostIndex;
                        }
                        itemsChecked++;
                    }
                    return NPC.NewNPC(source, x, y, type, mysteryNumber, 0f, 0f, 0f, 0f, target);
                });
                cursor.GotoNext(i => i.MatchCallvirt(out var mref) && mref.Name == "get_FullName");
                cursor.EmitDelegate((NPC npc) =>
                {
                    if (npc.ModNPC is GhostNPC ghost)
                    {
                        string message = $"The {Lang.GetNPCName(ghost.GhostType)} has arrived...?";
                        if (Main.netMode == NetmodeID.SinglePlayer)
                            Main.NewText(message, 0, 255, 100);
                        else if (Main.netMode == NetmodeID.Server)
                        {
                            ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(message), new Color(0, 255, 100));
                        }
                        return 0;
                    }
                    else
                    {
                        npc.netUpdate = true;
                        string fullName = npc.FullName;
                        if (Main.netMode == NetmodeID.SinglePlayer)
                            Main.NewText(Language.GetTextValue("Announcement.HasArrived", fullName), 50, 125);
                        else if (Main.netMode == NetmodeID.Server)
                            ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Announcement.HasArrived", npc.GetFullNetName()), new Color(50, 125, 255));
                        if (archipelagoSystem.session.goals.Contains(npc.TypeName)) archipelagoSystem.QueueLocation(npc.TypeName);  // For Princess + Future Single NPC Goals W/O NPC Randomization
                        return 1;
                    }
                });
                cursor.EmitBrtrue(npcLabel);
                cursor.EmitBr(ghostLabel);
                cursor.GotoNext(i => i.MatchCall(out var mref) && mref.Name == "NotifyProgressionEvent");
                cursor.Index--;
                cursor.MarkLabel(npcLabel);
                cursor.GotoNext(i => i.MatchLdsfld(out var _));
                cursor.MarkLabel(ghostLabel);
            };

            // Manage Ghost Redeeming
            On_Player.SetTalkNPC += (On_Player.orig_SetTalkNPC orig, Player player, int index, bool fromNet) =>
            {
                if (-1 < index && index <= Main.npc.Length && Main.npc[index].ModNPC is GhostNPC ghost)
                {
                    GhostNPC.RedeemGhost(index);
                }
                orig(player, index, fromNet);
            };

            // Manage Ghost Occupying Rooms
            On_WorldGen.IsRoomConsideredAlreadyOccupied += (On_WorldGen.orig_IsRoomConsideredAlreadyOccupied orig, int i, int j, int type) =>
            {
                GhostNPC[] existingGhosts = [.. (from npc in Main.npc where npc.ModNPC is GhostNPC select npc.ModNPC as GhostNPC)];
                foreach (var ghost in existingGhosts)
                {
                    if (ghost.NPC.active && ghost.NPC.homeTileX == i && ghost.NPC.homeTileY == j) return true;
                }
                bool result = orig(i, j, type);
                return result;
            };

            On_WorldGen.ScoreRoom_IsThisRoomOccupiedBySomeone += (On_WorldGen.orig_ScoreRoom_IsThisRoomOccupiedBySomeone orig, int ignoreNPC, int npcTypeAsking) =>
            {
                GhostNPC[] existingGhosts = [.. (from npc in Main.npc where npc.active && npc.ModNPC is GhostNPC select npc.ModNPC as GhostNPC)];
                foreach (var ghost in existingGhosts)
                {
                    for (int i = 0; i < WorldGen.numRoomTiles; i++)
                    {
                        if (ghost.NPC.homeTileX == WorldGen.roomX[i] && ghost.NPC.homeTileY - 1 == WorldGen.roomY[i])
                        {
                            return true;
                        }
                    }
                }
                return orig(ignoreNPC, npcTypeAsking);
            };

            // Bypass Spawn Blocking for Santa & Bound NPCs
            Terraria.IL_NPC.AI_007_TownEntities += il =>
            {
                var cursor = new ILCursor(il);

                // Santa
                cursor.GotoNext(i => i.MatchLdsfld(typeof(Main).GetField(nameof(Main.xMas))));
                cursor.Index++;
                cursor.EmitPop();
                cursor.EmitLdcI4(1);

                // Bound NPCs
                // Specifically, this stops the existence of town NPCs from preventing their bound counterparts.
                void SkipInstruction(string varName)
                {
                    var label = il.DefineLabel();
                    cursor.GotoNext(i => i.MatchStsfld(typeof(NPC).GetField(varName)));
                    cursor.EmitPop();
                    cursor.EmitBr(label);
                    cursor.GotoNext(i => i.MatchBr(out var _));
                    cursor.MarkLabel(label);
                }

                SkipInstruction(nameof(NPC.savedGolfer));
                SkipInstruction(nameof(NPC.savedTaxCollector));
                SkipInstruction(nameof(NPC.savedGoblin));
                SkipInstruction(nameof(NPC.savedWizard));
                SkipInstruction(nameof(NPC.savedMech));
                SkipInstruction(nameof(NPC.savedStylist));
                SkipInstruction(nameof(NPC.savedAngler));
                SkipInstruction(nameof(NPC.savedBartender));

                cursor.GotoNext(i => i.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedBoss3))));
                cursor.Index++;
                cursor.EmitPop();
                cursor.EmitDelegate(OldManSpawnBlocked);
            };

            // Add Checks To Bound NPCs + Enable Saved Bools For Vanilla

            Terraria.IL_NPC.AI_000_TransformBoundNPC += il =>
            {
                var cursor = new ILCursor(il);
                var skipRando = il.DefineLabel();

                cursor.EmitLdarg(2);
                cursor.EmitDelegate((int npcType) =>
                {
                    switch (npcType)
                    {
                        case NPCID.Angler: NPC.savedAngler = true; break;
                        case NPCID.Golfer: NPC.savedGolfer = true; break;
                        case NPCID.DD2Bartender: NPC.savedBartender = true; break;
                        case NPCID.Stylist: NPC.savedStylist = true; break;
                        case NPCID.GoblinTinkerer: NPC.savedGoblin = true; break;
                        case NPCID.Mechanic: NPC.savedMech = true; break;
                        case NPCID.Wizard: NPC.savedWizard = true; break;
                        default: throw new Exception($"NPC type {npcType} unaccounted for in TransformBoundNPC");
                    }
                });

                cursor.EmitDelegate(() =>
                {
                    return archipelagoSystem.world.NPCRandoActive();
                });
                cursor.EmitLdcI4(1);
                cursor.EmitBlt(skipRando);

                cursor.EmitLdarg(2);
                cursor.EmitDelegate((int npcType) =>
                {
                    int boundNPCtype;
                    string locName;
                    switch (npcType)
                    {
                        case NPCID.Angler: boundNPCtype = NPCID.SleepingAngler; locName = "Angler"; break;
                        case NPCID.Golfer: boundNPCtype = NPCID.GolferRescue; locName = "Golfer"; break;
                        case NPCID.DD2Bartender: boundNPCtype = NPCID.BartenderUnconscious; locName = "Tavernkeep"; break;
                        case NPCID.Stylist: boundNPCtype = NPCID.WebbedStylist; locName = "Stylist"; break;
                        case NPCID.GoblinTinkerer: boundNPCtype = NPCID.BoundGoblin; locName = "Goblin Tinkerer"; break;
                        case NPCID.Mechanic: boundNPCtype = NPCID.BoundMechanic; locName = "Mechanic"; break;
                        case NPCID.Wizard: boundNPCtype = NPCID.BoundWizard; locName = "Wizard"; break;
                        default: throw new Exception($"NPC type {npcType} unaccounted for in TransformBoundNPC. Also, {npcType} somehow changed value mid-exec. Dial 911 as fast as you can");
                    }
                    if (!archipelagoSystem.world.randomizedNPCs.Contains(npcType)) return npcType;
                    archipelagoSystem.QueueLocationClient(locName);
                    if (archipelagoSystem.world.npcLocTypeToNpcItemType is not null && archipelagoSystem.world.npcLocTypeToNpcItemType.TryGetValue(npcType, out int newNpcType) && !NPC.AnyNPCs(newNpcType))
                        return newNpcType;
                    NPC npc = Main.npc[NPC.FindFirstNPC(boundNPCtype)];
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        NetMessage.SendStrikeNPC(npc, new NPC.HitInfo() { InstantKill = true });
                    }
                    else
                    {
                        npc.StrikeInstantKill();
                    }
                    return 0;
                });
                cursor.EmitStarg(2);
                cursor.EmitLdarg(2);
                cursor.EmitLdcI4(1);
                cursor.EmitBge(skipRando);
                cursor.EmitRet();
                cursor.MarkLabel(skipRando);
                ;
            };

            // Add Check to Purifying Tax Collector
            // The method we edit for this is suspiciously large yet not identified as one that gets affected by garbage collection. If this randomly stops working in gameplay, find a different way to do this.

            Terraria.IL_Projectile.Damage += il =>
            {
                var cursor = new ILCursor(il);
                var label = il.DefineLabel();

                cursor.GotoNext(i => i.MatchCallvirt(typeof(NPC).GetMethod(nameof(NPC.Transform))));
                cursor.Index += 2;
                cursor.MarkLabel(label);
                cursor.Index -= 3;
                cursor.EmitDelegate<Func<NPC, bool>>((NPC npc) =>
                {
                    NPC.savedTaxCollector = true;
                    if (!archipelagoSystem.world.NPCRandoActive()) return false;
                    archipelagoSystem.QueueLocationClient("Tax Collector");

                    if (archipelagoSystem.world.npcLocTypeToNpcItemType.TryGetValue(NPCID.TaxCollector, out int type))
                    {
                        npc.Transform(type);
                        return true;
                    }

                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        NetMessage.SendStrikeNPC(npc, new NPC.HitInfo() { InstantKill = true });
                    }
                    else
                    {
                        npc.StrikeInstantKill();
                    }
                    return true;
                });
                cursor.EmitLdcI4(1);
                cursor.EmitBge(label);
                cursor.EmitDelegate<Func<NPC>>(() => Main.npc[NPC.FindFirstNPC(NPCID.DemonTaxCollector)]);
            };

            // Correct Skeletron-spawning Behavior
            // If the Old Man and the Clothier exist, this method cannot differentiate whether it was summoned via Old Man or voodoo doll.
            IL_NPC.SpawnSkeletron += il =>
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(i => i.MatchCall(out var mref) && mref.Name == "NewNPC");
                cursor.Remove();
                cursor.EmitLdarg0();
                cursor.EmitDelegate((IEntitySource _, int x, int y, int _, int _, float _, float _, float _, float _, int _, int onWho) =>
                {
                    void Broadcast(string msg) => ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(msg), Color.White);

                    int oldManIndex = -1;
                    int clothierIndex = -1;
                    for (int i = 0; i < Main.npc.Length; i++)
                    {
                        if (Main.npc[i].type == NPCID.OldMan) oldManIndex = i;
                        else if (Main.npc[i].type == NPCID.Clothier) clothierIndex = i;
                    }
                    if (oldManIndex != -1 && clothierIndex != -1)
                    {
                        NPC trueSummoner = Main.player[onWho].killClothier ? Main.npc[clothierIndex] : Main.npc[oldManIndex];
                        return NPC.NewNPC(NPC.GetBossSpawnSource(onWho), (int)trueSummoner.position.X + trueSummoner.width / 2, (int)trueSummoner.position.Y + trueSummoner.height / 2, 35);
                    }
                    else
                    {
                        return NPC.NewNPC(NPC.GetBossSpawnSource(onWho), x, y, 35);
                    }
                });
            };

            // Torch God reward Terraria.Player:13794
            IL_Player.TorchAttack += il =>
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(i => i.MatchLdsfld(typeof(Main).GetField(nameof(Main.netMode))));
                cursor.EmitDelegate(() => archipelagoSystem.QueueLocationClient("Torch God"));
                cursor.Emit(OpCodes.Ret);
            };

            // Allow Torch God even if you have `unlockedBiomeTorches`
            IL_Player.UpdateTorchLuck_ConsumeCountersAndCalculate += il =>
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(i => i.MatchLdfld(typeof(Player).GetField(nameof(Player.unlockedBiomeTorches))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_0);

                cursor.GotoNext(i => i.MatchLdcI4(ItemID.TorchGodsFavor));
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_0);
            };

            // General event clear locations
            IL_NPC.SetEventFlagCleared += il =>
            {
                var cursor = new ILCursor(il);

                var label = cursor.DefineLabel();
                cursor.MarkLabel(label);
                cursor.Emit(OpCodes.Pop);
                cursor.Index--;
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.Emit(OpCodes.Dup);
                cursor.Emit(OpCodes.Ldc_I4_M1);
                cursor.Emit(OpCodes.Beq, label);
                cursor.EmitDelegate((int id) =>
                {
                    var location = id switch
                    {
                        GameEventClearedID.DefeatedSlimeKing => "King Slime",
                        GameEventClearedID.DefeatedEyeOfCthulu => "Eye of Cthulhu",
                        GameEventClearedID.DefeatedEaterOfWorldsOrBrainOfChtulu => "Evil Boss",
                        GameEventClearedID.DefeatedGoblinArmy => "Goblin Army",
                        GameEventClearedID.DefeatedQueenBee => "Queen Bee",
                        GameEventClearedID.DefeatedSkeletron => "Skeletron",
                        GameEventClearedID.DefeatedDeerclops => "Deerclops",
                        GameEventClearedID.DefeatedWallOfFleshAndStartedHardmode => "Wall of Flesh",
                        GameEventClearedID.DefeatedPirates => "Pirate Invasion",
                        GameEventClearedID.DefeatedQueenSlime => "Queen Slime",
                        GameEventClearedID.DefeatedTheTwins => "The Twins",
                        GameEventClearedID.DefeatedDestroyer => "The Destroyer",
                        GameEventClearedID.DefeatedSkeletronPrime => "Skeletron Prime",
                        GameEventClearedID.DefeatedPlantera => "Plantera",
                        GameEventClearedID.DefeatedGolem => "Golem",
                        GameEventClearedID.DefeatedMartians => "Martian Madness",
                        GameEventClearedID.DefeatedFishron => "Duke Fishron",
                        GameEventClearedID.DefeatedHalloweenTree => "Mourning Wood",
                        GameEventClearedID.DefeatedHalloweenKing => "Pumpking",
                        GameEventClearedID.DefeatedChristmassTree => "Everscream",
                        GameEventClearedID.DefeatedSantank => "Santa-NK1",
                        GameEventClearedID.DefeatedIceQueen => "Ice Queen",
                        GameEventClearedID.DefeatedFrostArmy => "Frost Legion",
                        GameEventClearedID.DefeatedEmpressOfLight => "Empress of Light",
                        GameEventClearedID.DefeatedAncientCultist => "Lunatic Cultist",
                        GameEventClearedID.DefeatedMoonlord => "Moon Lord",
                        _ => null,
                    };

                    if (location != null) archipelagoSystem.QueueLocation(location);
                });
                cursor.Emit(OpCodes.Ret);
            };

            // Old One's Army locations
            IL_DD2Event.WinInvasionInternal += il =>
            {
                var cursor = new ILCursor(il);

                foreach (var (flagName, tier) in new Tuple<string, int>[] {
                    Tuple.Create(nameof(DD2Event.DownedInvasionT1), 1),
                    Tuple.Create(nameof(DD2Event.DownedInvasionT2), 2),
                    Tuple.Create(nameof(DD2Event.DownedInvasionT3), 3),
                })
                {
                    var flag = typeof(DD2Event).GetField(flagName);
                    cursor.GotoNext(i => i.MatchStsfld(flag));
                    cursor.EmitDelegate<Action>(() => Temp = (bool)flag.GetValue(null));
                    cursor.Index++;
                    cursor.EmitDelegate(() =>
                    {
                        flag.SetValue(null, Temp);
                        archipelagoSystem.QueueLocation($"Old One's Army Tier {tier}");
                        Temp = false;
                    });
                }
            };

            IL_NPC.DoDeathEvents += il =>
            {
                var cursor = new ILCursor(il);

                // Prevent NPC.downedTower* from being set
                foreach (var flag in new string[] { nameof(NPC.downedTowerSolar), nameof(NPC.downedTowerVortex), nameof(NPC.downedTowerNebula), nameof(NPC.downedTowerStardust) })
                {
                    var field = typeof(NPC).GetField(flag, BindingFlags.Static | BindingFlags.Public);
                    cursor.GotoNext(i => i.MatchStsfld(field));
                    // Crimes
                    cursor.EmitDelegate<Action>(() => Temp = (bool)field.GetValue(null));
                    cursor.Index++;
                    cursor.EmitDelegate(() =>
                    {
                        field.SetValue(null, Temp);
                        Temp = false;
                    });
                }

                // Prevent NPC.downedMechBossAny from being set
                while (cursor.TryGotoNext(i => i.MatchStsfld(typeof(NPC).GetField(nameof(NPC.downedMechBossAny)))))
                {
                    cursor.EmitDelegate<Action>(() => Temp = NPC.downedMechBossAny);
                    cursor.Index++;
                    cursor.EmitDelegate<Action>(() =>
                    {
                        NPC.downedMechBossAny = Temp;
                        Temp = false;
                    });
                }

                // Prevent Hardmode generation Terraria.NPC:69104
                cursor.GotoNext(i => i.MatchCall(typeof(WorldGen).GetMethod(nameof(WorldGen.StartHardmode))));
                cursor.EmitDelegate(() =>
                {
                    Temp = Main.hardMode;
                    Main.hardMode = true;
                });
                cursor.Index++;
                cursor.EmitDelegate<Action>(() =>
                {
                    Main.hardMode = Temp;
                    Temp = false;
                });
            };

            IL_WorldGen.UpdateLunarApocalypse += il =>
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(i => i.MatchCall(typeof(WorldGen).GetMethod(nameof(WorldGen.StartImpendingDoom))));
                cursor.Index--;
                cursor.EmitDelegate(() => archipelagoSystem.QueueLocation("Lunar Events"));
            };

            // Stop loading achievements from disk
            IL_AchievementManager.Load += il =>
            {
                var cursor = new ILCursor(il);
                cursor.Emit(OpCodes.Ret);
            };

            if (Main.netMode != NetmodeID.Server) Main.Achievements.OnAchievementCompleted += OnAchievementCompleted;
            #endregion
            // Unmaintainable reflection
            LoadCalamityReflection();
            LoadFargoReflection();
        }

        #region  Calamity Reflection
        void LoadCalamityReflection()
        {
            var calamity = CheckMod("CalamityMod", calVersionRange);
            if (calamity is null) return;

            Dictionary<string, string> defaultCalamityOnKillChecks = new()
            {
                {"DesertScourgeHead", "Desert Scourge"},
                {"CragmawMire", "Cragmaw Mire"},
                {"Crabulon", "Crabulon"},
                {"HiveMind", "The Hive Mind"},
                {"PerforatorHive", "The Perforators"},
                {"SlimeGodCore", "The Slime God"},
                {"AquaticScourgeHead", "Aquatic Scourge"},
                {"Mauler", "Mauler"},
                {"BrimstoneElemental", "Brimstone Elemental"},
                {"Cryogen", "Cryogen"},
                {"CalamitasClone", "Calamitas Clone"},
                {"GreatSandShark", "Great Sand Shark"},
                {"AstrumAureus", "Astrum Aureus"},
                {"PlaguebringerGoliath", "The Plaguebringer Goliath"},
                {"RavagerBody", "Ravager"},
                {"ProfanedGuardianCommander", "Profaned Guardians"},
                {"Dragonfolly", "The Dragonfolly"},
                {"Providence", "Providence, the Profaned Goddess"},
                {"StormWeaverHead", "Storm Weaver"},
                {"CeaselessVoid", "Ceaseless Void"},
                {"Signus", "Signus, Envoy of the Devourer"},
                {"Polterghast", "Polterghast"},
                {"NuclearTerror", "Nuclear Terror"},
                {"OldDuke", "The Old Duke"},
                {"DevourerofGodsHead", "The Devourer of Gods"},
                {"Yharon", "Yharon, Dragon of Rebirth"},
                {"SupremeCalamitas", "Supreme Witch, Calamitas"},
            };
            var calamityAssembly = calamity.GetType().Assembly;
            foreach (var type in calamityAssembly.GetTypes())
            {
                MethodInfo GetOnKill() => type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public);
                if (defaultCalamityOnKillChecks.TryGetValue(type.Name, out string loc))
                {
                    MethodInfo onKill = GetOnKill();
                    if (onKill is null) continue;
                    MonoModHooks.Add(GetOnKill(), DefaultSendLocOnKill(loc));
                }
                else
                {
                    switch (type.Name)
                    {
                        case "GiantClam":
                            {
                                void hook(OnKill orig, ModNPC self)
                                {
                                    Logger.Info($"Giant Clam hook triggered. Temp is {Temp}.");
                                    if (Temp) orig(self);
                                    else
                                    {
                                        ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Giant Clam");
                                        if (Main.hardMode) ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Hardmode Giant Clam");
                                    }
                                }
                                MonoModHooks.Add(GetOnKill(), hook);
                                break;
                            }
                        case "AcidRainEvent":
                            {
                                MethodInfo invasionUpdate = type.GetMethod("UpdateInvasion", BindingFlags.Static | BindingFlags.Public);
                                void ilHook(ILContext il)
                                {
                                    var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();
                                    var cursor = new ILCursor(il);

                                    cursor.GotoNext(i => i.MatchLdarg(0));
                                    cursor.Index++;
                                    cursor.EmitDelegate<Action<bool>>(won =>
                                    {
                                        Logger.Info($"Acid Rain IL edit reached. Won is {won}.");
                                        if (won)
                                        {
                                            archipelagoSystem.QueueLocation("Acid Rain Tier 1");
                                            if (CalamitySystem.DownedAquaticScourge()) archipelagoSystem.QueueLocation("Acid Rain Tier 2");
                                        }
                                    });
                                    cursor.Emit(OpCodes.Ldc_I4_0);
                                }
                                MonoModHooks.Modify(invasionUpdate, ilHook);
                                break;
                            }
                        case "CalamityGlobalNPC":
                            {
                                MethodInfo globalOnKill = GetOnKill();
                                void hook(CalamityGlobalNpcOnKill orig, object self, NPC npc)
                                {
                                    if (Temp || !vanillaBosses.Contains(npc.type)) orig(self, npc);
                                    else CalamitySystem.HandleBossRush(npc);
                                }
                                MonoModHooks.Add(globalOnKill, hook);
                                break;
                            }
                        case "Leviathan":
                            {
                                MethodInfo realOnKill = type.GetMethod("RealOnKill", BindingFlags.Static | BindingFlags.Public);
                                leviathanRealOnKill = realOnKill;
                                void hook(RealOnKill orig, NPC npc)
                                {
                                    Logger.Info($"Leviathan RealOnKill hook triggered. Temp is {Temp}.");
                                    if (Temp) orig(npc);
                                    else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Leviathan and Anahita");
                                }
                                MonoModHooks.Add(realOnKill, hook);
                                break;
                            }
                        case "AstrumDeusHead":
                            {
                                MethodInfo info = GetOnKill();
                                void ilHook(ILContext il)
                                {
                                    var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();
                                    var cursor = new ILCursor(il);

                                    cursor.GotoNext(i => i.MatchLdsfld(calamityAssembly.GetTypes().First(i => i.Name == "BossRushEvent").GetField("BossRushActive")));  // yuck
                                    cursor.Index++;
                                    cursor.EmitDelegate<Func<bool, bool>>(skip =>
                                    {
                                        Logger.Info($"Astrum Deus IL edit reached. Boss rush is {skip} and Temp is {Temp}.");
                                        bool locTrigger = !Temp;
                                        if (locTrigger) ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Astrum Deus");
                                        return skip || locTrigger;
                                    });
                                }
                                MonoModHooks.Modify(info, ilHook);
                                break;
                            }
                        case "AresBody":
                            {
                                void hook(OnKill orig, ModNPC self)
                                {
                                    Logger.Info($"Ares hook triggered. Temp is {Temp}.");
                                    if (Temp) orig(self);
                                    else if (CalamitySystem.AreExosDead(0)) ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Exo Mechs");
                                }
                                MonoModHooks.Add(GetOnKill(), hook);
                                break;
                            }
                        case "Apollo":
                            {
                                void hook(OnKill orig, ModNPC self)
                                {
                                    Logger.Info($"Apollo hook triggered. Temp is {Temp}.");
                                    if (Temp) orig(self);
                                    else if (CalamitySystem.AreExosDead(1)) ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Exo Mechs");
                                }
                                MonoModHooks.Add(GetOnKill(), hook);
                                break;
                            }
                        case "ThanatosHead":
                            {
                                void hook(OnKill orig, ModNPC self)
                                {
                                    Logger.Info($"Thanatos hook triggered. Temp is {Temp}.");
                                    if (Temp) orig(self);
                                    else if (CalamitySystem.AreExosDead(2)) ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Exo Mechs");
                                }
                                MonoModHooks.Add(GetOnKill(), hook);
                                break;
                            }
                        case "Trasher":
                            {
                                void hook(OnKill orig, ModNPC self)
                                {
                                    Logger.Info($"Trasher hook triggered. Temp is {Temp}.");
                                    var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();
                                    if (archipelagoSystem.world.NPCRandoActive())
                                    {
                                        if (!NPC.savedAngler && Main.netMode != NetmodeID.MultiplayerClient && !NPC.AnyNPCs(NPCID.SleepingAngler))
                                        {
                                            NPC trasher = self.NPC;
                                            NPC.NewNPC(trasher.GetSource_Death(), (int)trasher.Center.X, (int)trasher.Center.Y, NPCID.SleepingAngler);
                                        }
                                    }
                                    else orig(self);
                                }
                                MonoModHooks.Add(GetOnKill(), hook);
                                break;
                            }
                    }
                }
            }
        }
        #endregion

        #region Fargo Souls Reflection
        void LoadFargoReflection()
        {
            var fargo = CheckMod("FargowiltasSouls", fargoVersionRange);
            if (fargo is null) return;

            if (ModLoader.HasMod("CalamityMod"))
            {
                var fargoCal = CheckMod("FargowiltasCrossmod", fargoCalVersionRange);
                if (fargoCal is null) return;
            }

            Dictionary<string, string> defaultFargoOnKillChecks = new()
            {
                {"TrojanSquirrel", "Trojan Squirrel"},
                {"CursedCoffin", "Cursed Coffin"},
                {"DeviBoss", "Deviantt"},
                {"BanishedBaron", "Banished Baron"},
                {"LifeChallenger", "Lifelight"},
                {"TimberChampion", "Champion of Timber"},
                {"TerraChampion", "Champion of Terra"},
                {"EarthChampion", "Champion of Earth"},
                {"NatureChampion", "Champion of Nature"},
                {"LifeChampion", "Champion of Life"},
                {"ShadowChampion", "Champion of Death"},
                {"SpiritChampion", "Champion of Spirit"},
                {"WillChampion", "Champion of Will"},
                {"CosmosChampion", "Eridanus, Champion of Cosmos"},
                {"AbomBoss", "Abominationn"},
                {"MutantBoss", "Mutant"},
            };
            var fargoAssembly = fargo.GetType().Assembly;
            foreach (var type in fargoAssembly.GetTypes())
            {
                MethodInfo GetOnKill() => type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public);

                if (!defaultFargoOnKillChecks.TryGetValue(type.Name, out string loc))
                    continue;

                MethodInfo onKill = GetOnKill();

                if (onKill is null)
                {
                    Logger.Warn($"Could not find OnKill for Fargo type {type.FullName}");
                    continue;
                }

                MonoModHooks.Add(onKill, DefaultSendLocOnKill(loc));
            }
        }
        #endregion

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            var message = reader.ReadString();
            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();

            // The way we handle packets kind of sucks. It's using string IDs with some special
            // cases.
            if (message == "") archipelagoSystem.Chat(archipelagoSystem.Status(), whoAmI);
            else if (message.StartsWith("deathlink")) archipelagoSystem.TriggerDeathlink(message.Substring(9), whoAmI);
            else if (message.StartsWith("[DeathLink]"))
            {
                var player = Main.player[Main.myPlayer];
                if (player.active && !player.dead) player.Hurt(PlayerDeathReason.ByCustomReason(message), 999999, 1);
            }
            else if (message == "YouGotAnItem") Main.LocalPlayer.GetModPlayer<ArchipelagoPlayer>().ReceivedReward(reader.ReadInt32());
            else if (message == "RecievedRewardsForSetupShop")
            {
                var rewards = archipelagoSystem.ReceivedRewards();

                var packet = GetPacket();
                packet.Write("SetupShop");
                foreach (var reward in rewards) packet.Write(reward);
                packet.Write(-1);
                var player = Main.player[whoAmI];
                var position = player.position;
                var npc = NPC.NewNPC(new EntitySource_Misc("Open collection"), (int)position.X, (int)position.Y, ModContent.NPCType<CollectionNPC>(), 0, whoAmI, reader.ReadInt32());
                player.SetTalkNPC(npc);
                packet.Write(npc);
                packet.Send(whoAmI);
            }
            else if (message == "SetupShop")
            {
                var items = new List<int>();

                while (true)
                {
                    var item = reader.ReadInt32();
                    if (item == -1) break;
                    items.Add(item);
                }

                CollectionButton.SetupShop(items, reader.ReadInt32());
            }
            else if (message == "RedeemGhost")
            {
                int index = reader.ReadInt32();
                GhostNPC.RedeemGhost(index);
            }
            else archipelagoSystem.QueueLocation(message);
        }

        public override void Unload()
        {
            if (Main.netMode != NetmodeID.Server) Main.Achievements.OnAchievementCompleted -= OnAchievementCompleted;
        }

        void OnAchievementCompleted(Achievement achievement)
        {
            var name = achievement.Name switch
            {
                "TIMBER" => "Timber!!",
                "BENCHED" => "Benched",
                "OBTAIN_HAMMER" => "Stop! Hammer Time!",
                "MATCHING_ATTIRE" => "Matching Attire",
                "FASHION_STATEMENT" => "Fashion Statement",
                "OOO_SHINY" => "Ooo! Shiny!",
                "NO_HOBO" => "No Hobo",
                "HEAVY_METAL" => "Heavy Metal",
                "FREQUENT_FLYER" => "The Frequent Flyer",
                "GET_GOLDEN_DELIGHT" => "Feast of Midas",
                "DYE_HARD" => "Dye Hard",
                "LUCKY_BREAK" => "Lucky Break",
                "STAR_POWER" => "Star Power",
                "YOU_CAN_DO_IT" => "You Can Do It!",
                "DRINK_BOTTLED_WATER_WHILE_DROWNING" => "Unusual Survival Strategies",
                "TURN_GNOME_TO_STATUE" => "Heliophobia",
                "ARCHAEOLOGIST" => "Archaeologist",
                "PET_THE_PET" => "Feeling Petty",
                "FLY_A_KITE_ON_A_WINDY_DAY" => "A Rather Blustery Day",
                "PRETTY_IN_PINK" => "Pretty in Pink",
                "MARATHON_MEDALIST" => "Marathon Medalist",
                "SERVANT_IN_TRAINING" => "Servant-in-Training",
                "GOOD_LITTLE_SLAVE" => "10 Fishing Quests",
                "TROUT_MONKEY" => "Trout Monkey",
                "GLORIOUS_GOLDEN_POLE" => "Glorious Golden Pole",
                "FAST_AND_FISHIOUS" => "Fast and Fishious",
                "SUPREME_HELPER_MINION" => "Supreme Helper Minion!",
                "INTO_ORBIT" => "Into Orbit",
                "WATCH_YOUR_STEP" => "Watch Your Step!",
                "THROWING_LINES" => "Throwing Lines",
                "VEHICULAR_MANSLAUGHTER" => "Vehicular Manslaughter",
                "FIND_A_FAIRY" => "Hey! Listen!",
                "I_AM_LOOT" => "I Am Loot!",
                "HEART_BREAKER" => "Heart Breaker",
                "HOLD_ON_TIGHT" => "Hold on Tight!",
                "LIKE_A_BOSS" => "Like a Boss",
                "JEEPERS_CREEPERS" => "Jeepers Creepers",
                "FUNKYTOWN" => "Funkytown",
                "DECEIVER_OF_FOOLS" => "Deceiver of Fools",
                "DIE_TO_DEAD_MANS_CHEST" => "Dead Men Tell No Tales",
                "BULLDOZER" => "Bulldozer",
                "THERE_ARE_SOME_WHO_CALL_HIM" => "There are Some Who Call Him...",
                "THROW_A_PARTY" => "Jolly Jamboree",
                "TRANSMUTE_ITEM" => "A Shimmer In The Dark",
                "ITS_GETTING_HOT_IN_HERE" => "It's Getting Hot in Here",
                "ROCK_BOTTOM" => "Rock Bottom",
                "SMASHING_POPPET" => "Smashing, Poppet!",
                "TALK_TO_NPC_AT_MAX_HAPPINESS" => "Leading Landlord",
                "COMPLETELY_AWESOME" => "Completely Awesome",
                "STICKY_SITUATION" => "Sticky Situation",
                "THE_CAVALRY" => "The Cavalry",
                "BLOODBATH" => "Bloodbath",
                "TIL_DEATH" => "Til Death...",
                "FOUND_GRAVEYARD" => "Quiet Neighborhood",
                "PURIFY_ENTIRE_WORLD" => "And Good Riddance!",
                "MINER_FOR_FIRE" => "Miner for Fire",
                "GO_LAVA_FISHING" => "Hot Reels!",
                "GET_TERRASPARK_BOOTS" => "Boots of the Hero",
                "WHERES_MY_HONEY" => "Where's My Honey?",
                "NOT_THE_BEES" => "Not the Bees!",
                "DUNGEON_HEIST" => "Dungeon Heist",
                "GET_CELL_PHONE" => "Black Mirror",
                "BEGONE_EVIL" => "Begone, Evil!",
                "EXTRA_SHINY" => "Extra Shiny!",
                "GET_ANKH_SHIELD" => "Ankhumulation Complete",
                "GELATIN_WORLD_TOUR" => "Gelatin World Tour",
                "HEAD_IN_THE_CLOUDS" => "Head in the Clouds",
                "DEFEAT_DREADNAUTILUS" => "Don't Dread on Me",
                "IT_CAN_TALK" => "It Can Talk?!",
                "ALL_TOWN_SLIMES" => "The Great Slime Mitosis",
                "PRISMANCER" => "Prismancer",
                "GET_A_LIFE" => "Get a Life",
                "TOPPED_OFF" => "Topped Off",
                "BUCKETS_OF_BOLTS" => "Buckets of Bolts",
                "MECHA_MAYHEM" => "Mecha Mayhem",
                "DRAX_ATTAX" => "Drax Attax",
                "PHOTOSYNTHESIS" => "Photosynthesis",
                "YOU_AND_WHAT_ARMY" => "You and What Army?",
                "TO_INFINITY_AND_BEYOND" => "To Infinity... and Beyond!",
                "REAL_ESTATE_AGENT" => "Real Estate Agent",
                "ROBBING_THE_GRAVE" => "Robbing the Grave",
                "BIG_BOOTY" => "Big Booty",
                "RAINBOWS_AND_UNICORNS" => "Rainbows and Unicorns",
                "TEMPLE_RAIDER" => "Temple Raider",
                "SWORD_OF_THE_HERO" => "Sword of the Hero",
                "KILL_THE_SUN" => "Kill the Sun",
                "BALEFUL_HARVEST" => "Baleful Harvest",
                "ICE_SCREAM" => "Ice Scream",
                "SLAYER_OF_WORLDS" => "Slayer of Worlds",
                "SICK_THROW" => "Sick Throw",
                "GET_ZENITH" => "Infinity +1 Sword",
                _ => null,
            };

            if (name == "Begone, Evil!" && Main.gameMenu) return;  // No need to overthink this one, right?
            if (name != null) ModContent.GetInstance<ArchipelagoSystem>().QueueLocationClient(name);
            ModContent.GetInstance<ArchipelagoSystem>().Achieved(achievement.Name);
        }

        delegate void OnKill(ModNPC self);
        Action<OnKill, ModNPC> DefaultSendLocOnKill(string location) => (orig, self) =>
        {
            Logger.Info($"{location} hook triggered. Temp is {Temp}.");
            if (Temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation(location);
        };
        int[] vanillaBosses = { NPCID.KingSlime, NPCID.EyeofCthulhu, NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail, NPCID.BrainofCthulhu, NPCID.QueenBee, NPCID.SkeletronHead, NPCID.Deerclops, NPCID.WallofFlesh, NPCID.BloodNautilus, NPCID.QueenSlimeBoss, NPCID.Retinazer, NPCID.Spazmatism, NPCID.TheDestroyer, NPCID.SkeletronPrime, NPCID.Plantera, NPCID.Golem, NPCID.DukeFishron, NPCID.MourningWood, NPCID.Pumpking, NPCID.Everscream, NPCID.SantaNK1, NPCID.IceQueen, NPCID.HallowBoss, NPCID.CultistBoss, NPCID.MoonLordCore };

        delegate void CalamityGlobalNpcOnKill(object self, NPC npc);

        delegate void RealOnKill(NPC npc);
    }
}
