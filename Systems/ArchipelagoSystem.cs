using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Terraria;
using Terraria.Chat;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Social;

namespace SeldomArchipelago.Systems
{
    public class ArchipelagoSystem : ModSystem
    {
        List<string> locationBacklog = new List<string>();
        List<Task<LocationInfoPacket>> locationQueue;
        ArchipelagoSession session;
        bool enabled;
        int collectedItems;
        int currentItem;
        List<string> collectedLocations = new List<string>();

        public override void LoadWorldData(TagCompound tag)
        {
            collectedItems = tag.ContainsKey("ApCollectedItems") ? tag.Get<int>("ApCollectedItems") : 0;
        }

        public override void OnWorldLoad()
        {
            typeof(SocialAPI).GetField("_mode", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, SocialMode.None);

            locationQueue = new List<Task<LocationInfoPacket>>();

            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            var config = ModContent.GetInstance<Config.Config>();
            session = ArchipelagoSessionFactory.CreateSession(config.address, config.port);

            try
            {
                if (session.TryConnectAndLogin("Terraria", config.name, ItemsHandlingFlags.AllItems) is LoginFailure)
                {
                    session = null;
                    return;
                }
            }
            catch
            {
                session = null;
                return;
            }

            var locations = session.DataStorage[Scope.Slot, "CollectedLocations"].To<String[]>();
            if (locations != null)
            {
                collectedLocations = new List<string>(locations);
            }
        }

        public override void PostUpdateWorld()
        {
            if (session == null) return;

            if (!session.Socket.Connected)
            {
                Chat("Disconnected from Archipelago. Reload the world to reconnect.");
                session = null;
                enabled = false;
                return;
            }

            if (!enabled) return;

            var unqueue = new List<int>();
            for (var i = 0; i < locationQueue.Count; i++)
            {
                var status = locationQueue[i].Status;

                if (status switch
                {
                    TaskStatus.RanToCompletion or TaskStatus.Canceled or TaskStatus.Faulted => true,
                    _ => false,
                })
                {
                    if (status == TaskStatus.RanToCompletion)
                    {
                        foreach (var item in locationQueue[i].Result.Locations)
                        {
                            Chat($"Sent {session.Items.GetItemName(item.Item)} to {session.Players.GetPlayerAlias(item.Player)}!");
                        }
                    }
                    else
                    {
                        Chat($"Sent an item to a player...but failed to get info about it!");
                    }

                    unqueue.Add(i);
                }
            }

            unqueue.Reverse();
            foreach (var i in unqueue)
            {
                locationQueue.RemoveAt(i);
            }

            while (session.Items.Any())
            {
                var item = session.Items.DequeueItem();
                var itemName = session.Items.GetItemName(item.Item);

                if (currentItem++ < collectedItems) continue;

                switch (itemName)
                {
                    case "Torch God's Favor": GiveItem(ItemID.TorchGodsFavor); break;
                    case "Post-Goblin Army": NPC.downedGoblins = true; break;
                    case "Post-King Slime": NPC.downedSlimeKing = true; break;
                    case "Post-Eye of Cthulhu": NPC.downedBoss1 = true; break;
                    case "Post-Eater of Worlds or Brain of Cthulhu": NPC.downedBoss2 = true; break;
                    case "Post-Old One's Army Tier 1": DD2Event.DownedInvasionT1 = true; break;
                    case "Post-Queen Bee": NPC.downedQueenBee = true; break;
                    case "Post-Skeletron": NPC.downedBoss3 = true; break;
                    case "Post-Deerclops": NPC.downedDeerclops = true; break;
                    case "Hardmode": WorldGen.StartHardmode(); break;
                    case "Post-Pirate Invasion": NPC.downedPirates = true; break;
                    case "Post-Frost Legion": NPC.downedFrost = true; break;
                    case "Post-Queen Slime": NPC.downedQueenSlime = true; break;
                    case "Post-The Twins": NPC.downedMechBoss1 = NPC.downedMechBossAny = true; break;
                    case "Post-The Destroyer": NPC.downedMechBoss2 = NPC.downedMechBossAny = true; break;
                    case "Post-Skeletron Prime": NPC.downedMechBoss3 = NPC.downedMechBossAny = true; break;
                    case "Post-Old One's Army Tier 2": DD2Event.DownedInvasionT2 = true; break;
                    case "Post-Plantera": NPC.downedPlantBoss = true; break;
                    case "Post-Golem": NPC.downedGolemBoss = true; break;
                    case "Post-Old One's Army Tier 3": DD2Event.DownedInvasionT3 = true; break;
                    case "Post-Martian Madness": NPC.downedMartians = true; break;
                    case "Post-Duke Fishron": NPC.downedFishron = true; break;
                    case "Post-Mourning Wood": NPC.downedHalloweenTree = true; break;
                    case "Post-Pumpking": NPC.downedHalloweenKing = true; break;
                    case "Post-Everscream": NPC.downedChristmasTree = true; break;
                    case "Post-Santa-NK1": NPC.downedChristmasSantank = true; break;
                    case "Post-Ice Queen": NPC.downedChristmasIceQueen = true; break;
                    case "Post-Empress of Light": NPC.downedEmpressOfLight = true; break;
                    case "Post-Lunatic Cultist": NPC.downedAncientCultist = true; break;
                    case "Post-Lunar Events": NPC.downedTowerNebula = NPC.downedTowerSolar = NPC.downedTowerStardust = NPC.downedTowerVortex = true; break;
                    case "Post-Moon Lord": NPC.downedMoonlord = true; break;
                    case "Hermes Boots": GiveItem(ItemID.HermesBoots); break;
                    case "Magic Mirror": GiveItem(ItemID.MagicMirror); break;
                    case "Cloud in a Bottle": GiveItem(ItemID.CloudinaBottle); break;
                    case "Grappling Hook": GiveItem(ItemID.GrapplingHook); break;
                    case "Climbing Claws": GiveItem(ItemID.ClimbingClaws); break;
                    case "Fledgling Wings": GiveItem(ItemID.CreativeWings); break;
                    case "Demon Conch": GiveItem(ItemID.DemonConch); break;
                    case "Magic Conch": GiveItem(ItemID.MagicConch); break;
                    case "Anklet of the Wind": GiveItem(ItemID.AnkletoftheWind); break;
                    case "Aglet": GiveItem(ItemID.Aglet); break;
                    case "Ice Skates": GiveItem(ItemID.IceSkates); break;
                    case "Lava Charm": GiveItem(ItemID.LavaCharm); break;
                    case "Obsidian Rose": GiveItem(ItemID.ObsidianRose); break;
                    case "Nature's Gift": GiveItem(ItemID.NaturesGift); break;
                    case "Feral Claws": GiveItem(ItemID.FeralClaws); break;
                    case "Magma Stone": GiveItem(ItemID.MagmaStone); break;
                    case "Shark Tooth Necklace": GiveItem(ItemID.SharkToothNecklace); break;
                    case "Cobalt Shield": GiveItem(ItemID.CobaltShield); break;
                    case "Band of Regeneration": GiveItem(ItemID.BandofRegeneration); break;
                    case "Philosopher's Stone": GiveItem(ItemID.PhilosophersStone); break;
                    case "Cross Necklace": GiveItem(ItemID.CrossNecklace); break;
                    case "Magic Quiver": GiveItem(ItemID.MagicQuiver); break;
                    case "Rifle Scope": GiveItem(ItemID.RifleScope); break;
                    case "Celestial Magnet": GiveItem(ItemID.CelestialMagnet); break;
                    case "Rod of Discord": GiveItem(ItemID.RodofDiscord); break;
                    case "Flying Carpet": GiveItem(ItemID.FlyingCarpet); break;
                    case "Lifeform Analyzer": GiveItem(ItemID.LifeformAnalyzer); break;
                    case "Ancient Chisel": GiveItem(ItemID.AncientChisel); break;
                    case "Moon Charm": GiveItem(ItemID.MoonCharm); break;
                    case "Neptune's Shell": GiveItem(ItemID.NeptunesShell); break;
                    case "Shoe Spikes": GiveItem(ItemID.ShoeSpikes); break;
                    case "Tabi": GiveItem(ItemID.Tabi); break;
                    case "Black Belt": GiveItem(ItemID.BlackBelt); break;
                    case "Flesh Knuckles": GiveItem(ItemID.FleshKnuckles); break;
                    case "Putrid Scent": GiveItem(ItemID.PutridScent); break;
                    case "Paladin's Shield": GiveItem(ItemID.PaladinsShield); break;
                    case "Frozen Turtle Shell": GiveItem(ItemID.FrozenTurtleShell); break;
                    case "Star Cloak": GiveItem(ItemID.StarCloak); break;
                    case "Discount Card": GiveItem(ItemID.DiscountCard); break;
                    case "Red Counterweight": GiveItem(ItemID.RedCounterweight); break;
                    case "Yoyo Glove": GiveItem(ItemID.YoYoGlove); break;
                    case "Depth Meter": GiveItem(ItemID.DepthMeter); break;
                    case "Compass": GiveItem(ItemID.Compass); break;
                    case "Radar": GiveItem(ItemID.Radar); break;
                    case "DPS Meter": GiveItem(ItemID.DPSMeter); break;
                    case "Metal Detector": GiveItem(ItemID.MetalDetector); break;
                    case "Sextant": GiveItem(ItemID.Sextant); break;
                    case "Stopwatch": GiveItem(ItemID.Stopwatch); break;
                    case "Tally Counter": GiveItem(ItemID.TallyCounter); break;
                    case "Fisherman's Pocket Guide": GiveItem(ItemID.FishermansGuide); break;
                    case "High Test Fishing Line": GiveItem(ItemID.HighTestFishingLine); break;
                    case "Angler Earring": GiveItem(ItemID.AnglerEarring); break;
                    case "Tackle Box": GiveItem(ItemID.TackleBox); break;
                    case "Lavaproof Fishing Hook": GiveItem(ItemID.LavaFishingHook); break;
                    case "Weather Radio": GiveItem(ItemID.WeatherRadio); break;
                    case "Blindfold": GiveItem(ItemID.Blindfold); break;
                    case "Pocket Mirror": GiveItem(ItemID.PocketMirror); break;
                    case "Vitamins": GiveItem(ItemID.Vitamins); break;
                    case "Armor Polish": GiveItem(ItemID.ArmorPolish); break;
                    case "Adhesive Bandage": GiveItem(ItemID.AdhesiveBandage); break;
                    case "Bezoar": GiveItem(ItemID.Bezoar); break;
                    case "Nazar": GiveItem(ItemID.Nazar); break;
                    case "Megaphone": GiveItem(ItemID.Megaphone); break;
                    case "Trifold Map": GiveItem(ItemID.TrifoldMap); break;
                    case "Fast Clock": GiveItem(ItemID.FastClock); break;
                    case "Brick Layer": GiveItem(ItemID.BrickLayer); break;
                    case "Extendo Grip": GiveItem(ItemID.ExtendoGrip); break;
                    case "Paint Sprayer": GiveItem(ItemID.PaintSprayer); break;
                    case "Portable Cement Mixer": GiveItem(ItemID.PortableCementMixer); break;
                    case "Treasure Magnet": GiveItem(ItemID.TreasureMagnet); break;
                    case "Step Stool": GiveItem(ItemID.PortableStool); break;
                    case "Gold Ring": GiveItem(ItemID.GoldRing); break;
                    case "Lucky Coin": GiveItem(ItemID.LuckyCoin); break;
                    case "50 Silver": GiveItem(ItemID.SilverCoin, 50); break;
                    case "Apprentice's Scarf": GiveItem:(ItemID.ApprenticeScarf); break;
                    case "Balloon Pufferfish": GiveItem:(ItemID.BalloonPufferfish); break;
                    case "Band of Starpower": GiveItem:(ItemID.BandofStarpower); break;
                    case "Blizzard in a Bottle": GiveItem:(ItemID.BlizzardinaBottle); break;
                    case "Dunerider Boots": GiveItem:(ItemID.SandBoots); break;
                    case "Eye of the Golem": GiveItem:(ItemID.EyeoftheGolem); break;
                    case "Flipper": GiveItem:(ItemID.Flipper); break;
                    case "Flower Boots": GiveItem:(ItemID.FlowerBoots); break;
                    case "Flurry Boots": GiveItem:(ItemID.FlurryBoots); break;
                    case "Frog Leg": GiveItem:(ItemID.FrogLeg); break;
                    case "Hand Warmer": GiveItem:(ItemID.HandWarmer); break;
                    case "Hercules Beetle": GiveItem:(ItemID.HerculesBeetle); break;
                    case "Honey Comb": GiveItem:(ItemID.HoneyComb); break;
                    case "Huntress's Buckler": GiveItem:(ItemID.HuntressBuckler); break;
                    case "Inner Tube": GiveItem:(ItemID.FloatingTube); break;
                    case "Jellyfish Necklace": GiveItem:(ItemID.JellyfishNecklace); break;
                    case "Lucky Horseshoe": GiveItem:(ItemID.LuckyHorseshoe); break;
                    case "Magiluminescence": GiveItem:(ItemID.Magiluminescence); break;
                    case "Moon Stone": GiveItem:(ItemID.MoonStone); break;
                    case "Monk's Belt": GiveItem:(ItemID.MonkBelt); break;
                    case "Necromantic Scroll": GiveItem:(ItemID.NecromanticScroll); break;
                    case "Obsidian Skull": GiveItem:(ItemID.ObsidianSkull); break;
                    case "Panic Necklace": GiveItem:(ItemID.PanicNecklace); break;
                    case "Pocket Mirror": GiveItem:(ItemID.PocketMirror); break;
                    case "Pygmy Necklace": GiveItem:(ItemID.PygmyNecklace); break;
                    case "Ranger Emblem": GiveItem:(ItemID.RangerEmblem); break;
                    case "Rocket Boots": GiveItem:(ItemID.RocketBoots); break;
                    case "Sailfish Boots": GiveItem:(ItemID.SailfishBoots); break;
                    case "Sandstorm in a Bottle": GiveItem:(ItemID.SandstorminaBottle); break;
                    case "Shackle": GiveItem:(ItemID.Shackle); break;
                    case "Shiny Red Balloon": GiveItem:(ItemID.ShinyRedBalloon); break;
                    case "Sorcerer Emblem": GiveItem:(ItemID.SorcererEmblem); break;
                    case "Squire's Shield": GiveItem:(ItemID.SquireShield); break;
                    case "Summoner Emblem": GiveItem:(ItemID.SummonerEmblem); break;
                    case "Sun Stone": GiveItem:(ItemID.SunStone); break;
                    case "Titan Glove": GiveItem:(ItemID.TitanGlove); break;
                    case "Tsunami in a Bottle": GiveItem:(ItemID.TsunamiInABottle); break;
                    case "Warrior Emblem": GiveItem:(ItemID.WarriorEmblem); break;
                    case "Water Walking Boots": GiveItem:(ItemID.WaterWalkingBoots); break;
                    case "White String": GiveItem:(ItemID.WhiteString); break;
                    case "Ankh Charm": GiveItem:(ItemID.AnkhCharm); break;
                    case "Ankh Shield": GiveItem:(ItemID.AnkhShield); break;
                    case "Amber Horseshoe Balloon": GiveItem:(ItemID.BalloonHorseshoeHoney); break;
                    case "Amphibian Boots": GiveItem:(ItemID.AmphibianBoots); break;
                    case "Arcane Flower": GiveItem:(ItemID.ArcaneFlower); break;
                    case "Arctic Diving Gear": GiveItem:(ItemID.ArcticDivingGear); break;
                    case "Armor Bracing": GiveItem:(ItemID.ArmorBracing); break;
                    case "Avenger Emblem": GiveItem:(ItemID.AvengerEmblem); break;
                    case "Bee Cloak": GiveItem:(ItemID.BeeCloak); break;
                    case "Berserker's Glove": GiveItem:(ItemID.BerserkerGlove); break;
                    case "Blizzard in a Balloon": GiveItem:(ItemID.BlizzardinaBalloon); break;
                    case "Blue Horseshoe Balloon": GiveItem:(ItemID.BlueHorseshoeBalloon); break;
                    case "Celestial Cuffs": GiveItem:(ItemID.CelestialCuffs); break;
                    case "Celestial Emblem": GiveItem:(ItemID.CelestialEmblem); break;
                    case "Celestial Stone": GiveItem:(ItemID.CelestialStone); break;
                    case "Celestial Shell": GiveItem:(ItemID.CelestialShell); break;
                    case "Charm of Myths": GiveItem:(ItemID.CharmofMyths); break;
                    case "Cloud in a Balloon": GiveItem:(ItemID.CloudinaBalloon); break;
                    case "Coin Ring": GiveItem:(ItemID.CoinRing); break;
                    case "Countercurse Mantra": GiveItem:(ItemID.CountercurseMantra); break;
                    case "Destroyer Emblem": GiveItem:(ItemID.DestroyerEmblem); break;
                    case "Diving Gear": GiveItem:(ItemID.DivingGear); break;
                    case "Fairy Boots": GiveItem:(ItemID.FairyBoots); break;
                    case "Fart in a Balloon": GiveItem:(ItemID.FartInABalloon); break;
                    case "Fart in a Jar": GiveItem:(ItemID.FartinaJar); break;
                    case "Fire Gauntlet": GiveItem:(ItemID.FireGauntlet); break;
                    case "Frog Flipper": GiveItem:(ItemID.FrogFlipper); break;
                    case "Frog Gear": GiveItem:(ItemID.FrogGear); break;
                    case "Frog Webbing": GiveItem:(ItemID.FrogWebbing); break;
                    case "Frostspark Boots": GiveItem:(ItemID.FrostsparkBoots); break;
                    case "Greedy Ring": GiveItem:(ItemID.GreedyRing); break;
                    case "Green Horseshoe Balloon": GiveItem:(ItemID.BalloonHorseshoeFart); break;
                    case "Hellfire Treads": GiveItem:(ItemID.HellfireTreads); break;
                    case "Hero Shield": GiveItem:(ItemID.HeroShield); break;
                    case "Honey Balloon": GiveItem:(ItemID.HoneyBalloon); break;
                    case "Jellyfish Diving Gear": GiveItem:(ItemID.JellyfishDivingGear); break;
                    case "Lava Waders": GiveItem:(ItemID.LavaWaders); break;
                    case "Lightning Boots": GiveItem:(ItemID.LightningBoots); break;
                    case "Magic Cuffs": GiveItem:(ItemID.MagicCuffs); break;
                    case "Magnet Flower": GiveItem:(ItemID.MagnetFlower); break;
                    case "Magma Skull": GiveItem:(ItemID.LavaSkull); break;
                    case "Mana Cloak": GiveItem:(ItemID.ManaCloak); break;
                    case "Mana Flower": GiveItem:(ItemID.ManaFlower); break;
                    case "Mana Regeneration Band": GiveItem:(ItemID.ManaRegenerationBand); break;
                    case "Master Ninja Gear": GiveItem:(ItemID.MasterNinjaGear); break;
                    case "Mechanical Glove": GiveItem:(ItemID.MechanicalGlove); break;
                    case "Medicated Bandage": GiveItem:(ItemID.MedicatedBandage); break;
                    case "Molten Charm": GiveItem:(ItemID.MoltenCharm); break;
                    case "Molten Quiver": GiveItem:(ItemID.MoltenQuiver); break;
                    case "Molten Skull Rose": GiveItem:(ItemID.MoltenSkullRose); break;
                    case "Moon Shell": GiveItem:(ItemID.MoonShell); break;
                    case "Neptune's Shell": GiveItem:(ItemID.NeptunesShell); break;
                    case "Obsidian Horseshoe": GiveItem:(ItemID.ObsidianHorseshoe); break;
                    case "Obsidian Shield": GiveItem:(ItemID.ObsidianShield); break;
                    case "Obsidian Skull Rose": GiveItem:(ItemID.ObsidianSkullRose); break;
                    case "Obsidian Water Walking Boots": GiveItem:(ItemID.ObsidianWaterWalkingBoots); break;
                    case "Papyrus Scarab": GiveItem:(ItemID.PapyrusScarab); break;
                    case "Pink Horseshoe Balloon": GiveItem:(ItemID.BalloonHorseshoeSharkron); break;
                    case "Power Glove": GiveItem:(ItemID.PowerGlove); break;
                    case "Recon Scope": GiveItem:(ItemID.ReconScope); break;
                    case "Sandstorm in a Balloon": GiveItem:(ItemID.SandstorminaBalloon); break;
                    case "Sharkron Balloon": GiveItem:(ItemID.SharkronBalloon); break;
                    case "Sniper Scope": GiveItem:(ItemID.SniperScope); break;
                    case "Spectre Boots": GiveItem:(ItemID.SpectreBoots); break;
                    case "Stalker's Quiver": GiveItem:(ItemID.StalkersQuiver); break;
                    case "Star Veil": GiveItem:(ItemID.StarVeil); break;
                    case "Stinger Necklace": GiveItem:(ItemID.StingerNecklace); break;
                    case "Sweetheart Necklace": GiveItem:(ItemID.SweetheartNecklace); break;
                    case "Terraspark Boots": GiveItem:(ItemID.TerrasparkBoots); break;
                    case "The Plan": GiveItem:(ItemID.ThePlan); break;
                    case "Tiger Climbing Gear": GiveItem:(ItemID.TigerClimbingGear); break;
                    case "White Horseshoe Balloon": GiveItem:(ItemID.WhiteHorseshoeBalloon); break;
                    case "Yellow Horseshoe Balloon": GiveItem:(ItemID.YellowHorseshoeBalloon); break;
                    case "Yoyo Bag": GiveItem:(ItemID.YoyoBag); break;


                }

                collectedItems++;
                Chat($"Received {itemName} from {session.Players.GetPlayerAlias(item.Player)}!");
            }
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["ApCollectedItems"] = collectedItems;
            if (enabled) session.DataStorage[Scope.Slot, "CollectedLocations"] = collectedLocations.ToArray();
        }

        public override void OnWorldUnload()
        {
            typeof(SocialAPI).GetField("_mode", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, SocialMode.Steam);

            locationBacklog.Clear();
            locationQueue = null;
            enabled = false;
            collectedItems = 0;
            currentItem = 0;
            collectedLocations = new List<string>();

            Main.Achievements?.ClearAll();

            if (session != null) session.Socket.Disconnect();
            session = null;
        }

        public string[] Status() => Tuple.Create(session != null, enabled) switch
        {
            (false, _) => new string[] {
                "The world is not connected to Archipelago and will need to reload.",
                "If you are the host, check your config in Workshop > Manage Mods > Config."
            },
            (true, false) => new string[] { @"Archipelago is connected but not enabled. Once everyone's joined, run ""/apstart"" to start it." },
            (true, true) => new string[] { "Archipelago is active!" },
        };

        public bool Enable()
        {
            if (session == null)
            {
                return false;
            }

            enabled = true;

            foreach (var location in locationBacklog)
            {
                QueueLocation(location);
            }
            locationBacklog.Clear();

            return true;
        }

        public void Chat(string message, int player = -1)
        {
            if (player == -1)
            {
                if (Main.netMode == NetmodeID.Server)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(message), Color.White);
                    Console.WriteLine(message);
                }
                else Main.NewText(message);
            }
            else ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral(message), Color.White, player);
        }

        public void Chat(string[] messages, int player = -1)
        {
            foreach (var message in messages)
            {
                Chat(message, player);
            }
        }

        public void QueueLocation(string locationName)
        {
            if (!enabled)
            {
                locationBacklog.Add(locationName);
                return;
            }

            var location = session.Locations.GetLocationIdFromName("Terraria", locationName);
            if (location == -1 || !session.Locations.AllMissingLocations.Contains(location)) return;

            if (!collectedLocations.Contains(locationName))
            {
                locationQueue.Add(session.Locations.ScoutLocationsAsync(new long[] { location }));
                collectedLocations.Add(locationName);
            }

            session.Locations.CompleteLocationChecks(new long[] { location });
        }

        public void QueueLocationClient(string locationName)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                QueueLocation(locationName);
                return;
            }

            var packet = ModContent.GetInstance<SeldomArchipelago>().GetPacket();
            packet.Write(locationName);
            packet.Send();
        }

        public void GiveItem(int item, int count = 1)
        {
            for (var i = 0; i < Main.maxPlayers; i++)
            {
                var player = Main.player[i];
                if (player.active) player.QuickSpawnItem(player.GetSource_GiftOrReward(), item, count);
            }
        }
    }
}
