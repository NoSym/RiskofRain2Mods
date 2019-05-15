using System;
using System.Collections.Generic;
using BepInEx;
using RoR2.UI;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.Networking;

// Wherein I begin using PascalCase method names
namespace WildMagic
{
    /// <summary>
    /// Controller for Wild Magic functionality. Methods are aimed at players but most will work with any CharacterMaster.
    /// </summary>
    public class MagicHandler
    {
        private CharacterMaster master;

        private enum Effects
        {
            BeetleSquad,
            BeginHaunt,
            BossMode,
            BuffDamage,
            BuffMove,
            CommandoBackup,
            Charm,
            DebuffDamage,
            DestroyEquipment,
            DoubleMoney,
            Effigy,
            EnemyMerc,
            FireTrail,
            Funballs,
            Gamble,
            GoGhost,
            HideCrosshair,
            HalveMoney,
            Hellfire,
            IcarianFlight,
            Instakill,
            Meteors,
            MotherVagrant,
            RandomizeSurvivor,
            RerollItems,
            RuinName,
            SpawnBeetleGuard,
            TakeOneDamage,
            TankMode,
            WildSurge,
            Count
        };

        // For temporary transformations
        private GameObject oldBody;

        // Probably a bad idea
        private CharacterMaster victim;

        // :)
        private List<ScoreboardStrip> stripList = new List<ScoreboardStrip>();

        private List<CharacterBody> charmedList = new List<CharacterBody>();

        // proc chance = rollChance * 0.5 + 0.5
        private int rollChance = 0;
        private int rngCap = 200;

        // Clarity
        string color = "red";

        // Janky move speed buff
        int fakeHooves = 0;

        // Casper
        private int hauntCounter = 0;

        // For the horde
        private int beetleWaveMax = 5;

        // Jellies
        private List<CharacterMaster> vagrantList = new List<CharacterMaster>();

        // Flags
        private bool canRoll = true; // For bossmode
        private bool rollReady = true; // For rolls
        private bool debuffed = false;
        private bool messagesEnabled = true;
        private bool spite = true;

        /// <summary>
        /// Creates a new wild magic controller for the specified character master.
        /// </summary>
        /// <param name="magicMaster">Owner of wild magic effects.</param>
        public MagicHandler(CharacterMaster magicMaster)
        {
            master = magicMaster;

            // Incompatibility with mods that want to show you this placeholder icon, I guess
            ItemCatalog.GetItemDef(ItemIndex.BoostDamage).hidden = true;

            // Yeah just lie about how many hooves you have sure
            On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
            {
                if (master != null && master.GetBody() != null && master.GetBody().Equals(self))
                {
                    master.inventory.GiveItem(ItemIndex.Hoof, fakeHooves);
                    orig(self);
                    master.inventory.RemoveItem(ItemIndex.Hoof, fakeHooves);
                } // if
                else
                {
                    orig(self);
                } // else
            }; // RecalculateStats

            // Magic Proc
            On.RoR2.CharacterBody.OnDamageDealt += (orig, self, report) =>
            {
                if (master != null && self.Equals(master.GetBody()) && report.victimMaster != null && !report.victimMaster.Equals(master))
                {
                    if (canRoll && rollReady)
                    {
                        victim = report.victimMaster;
                        HideCrosshair();
                        if (UnityEngine.Random.Range(0, rngCap) <= rollChance) // 0.5% chance to magic by default
                        {
                            Roll();
                        } // if

                        rollReady = false;

                        Timer.SetTimer(() =>
                        {
                            rollReady = true;
                        }, 1);
                    } // if
                } // if

                orig(self, report);
            }; // DamageDealt

            // Why not just do every single thing differently
            On.RoR2.HealthComponent.TakeDamage += (orig, self, damager) =>
            {
                if (debuffed && damager.attacker.Equals(master.GetBody().gameObject))
                {
                    damager.damage /= 2;
                } // if

                orig(self, damager);
            };

            // Spooky
            On.RoR2.GlobalEventManager.OnCharacterDeath += (orig, self, report) =>
            {
                if (master != null)
                {
                    // Ghosts
                    if (hauntCounter > 0 && report.damageInfo.attacker.Equals(master.GetBody().gameObject))
                    {
                        CharacterBody ghost = Util.TryToCreateGhost(report.victimBody, report.victimBody, 20);
                        ghost.baseDamage /= 6.0f; // I mean seriously, 500%?
                        ghost.RecalculateStats();
                        hauntCounter--;
                    } // if

                    // Big jellyfish
                    for (int i = 0; i < vagrantList.Count; i++)
                    {
                        if (report.victimMaster.Equals(vagrantList[i]))
                        {
                            for (int j = 0; j < 20; j++)
                            {
                                GameObject gameObject = DirectorCore.instance.TrySpawnObject((SpawnCard)Resources.Load("SpawnCards/CharacterSpawnCards/cscJellyfish"), new DirectorPlacementRule
                                {
                                    placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                                    minDistance = 3f,
                                    maxDistance = 25f,
                                    spawnOnTarget = report.victimBody.transform
                                }, RoR2Application.rng);
                                if (gameObject)
                                {
                                    CharacterMaster component = gameObject.GetComponent<CharacterMaster>();
                                    if (component)
                                    {
                                        component.teamIndex = TeamIndex.Monster;
                                    } // if
                                } // if
                            } // for

                            vagrantList.RemoveAt(i);
                            i--; // In case multiple vagrants die in the same frame somehow
                        } // if
                    } // for
                } // if

                orig(self, report);
            }; // CharacterDeath

            // Keep track of scoreboard instance
            On.RoR2.UI.ScoreboardStrip.SetMaster += (orig, self, newMaster) =>
            {
                if (!stripList.Contains(self))
                    stripList.Add(self);
                orig(self, newMaster);
            };

            // Make player invisible to charmed enemies to drop aggro
            On.RoR2.CharacterBody.GetVisibilityLevel += (orig, self, body) =>
            {
                // If you need to charm a LOT of enemies remove this loop for efficiency, probably
                for (int i = 0; i < charmedList.Count; i++)
                    if (self.Equals(master.GetBody()) && body.Equals(charmedList[i]))
                        return VisibilityLevel.Invisible;

                return orig(self, body);
            };
        } // MagicHandler Constructor

        /// <summary>
        /// Roll a random available effect.
        /// </summary>
        public void Roll()
        {
            string message = "";

            switch((Effects)UnityEngine.Random.Range(0, (int)Effects.Count))
            {
                case Effects.BeetleSquad:
                    BeetleSquad();
                    message = "Infestation!";
                    break;
                case Effects.BeginHaunt:
                    BeginHaunt();
                    message = "Vengeful spirits rise to haunt you!";
                    break;
                case Effects.BossMode:
                    BossMode();
                    message = "Unstable transformation!";
                    break;
                case Effects.BuffDamage:
                    BuffDamage();
                    message = "You feel powerful!";
                    break;
                case Effects.BuffMove:
                    BuffMove();
                    message = "You've got the wind at your back.";
                    break;
                case Effects.CommandoBackup:
                    CommandoBackup();
                    message = "Hoorah.";
                    break;
                case Effects.Charm:
                    Charm();
                    message = "Someone had a change of heart.";
                    break;
                case Effects.DebuffDamage:
                    DebuffDamage();
                    message = "Your strikes soften.";
                    break;
                case Effects.DestroyEquipment:
                    DestroyEquipment();
                    message = "You feel underequipped for this task.";
                    break;
                case Effects.DoubleMoney:
                    DoubleMoney();
                    message = "Great wealth finds you.";
                    break;
                case Effects.Effigy:
                    Effigy();
                    message = "A 'friend' arrives to help!";
                    break;
                case Effects.EnemyMerc:
                    EnemyMerc();
                    message = "He's found you!";
                    break;
                case Effects.FireTrail:
                    FireTrail();
                    message = "Time to blaze a trail.";
                    break;
                case Effects.Funballs:
                    if (spite)
                    {
                        Funballs();
                        message = "Operation FUN activated.";
                    } // if
                    else
                    {
                        Roll(); // There's a world where funballs rolls infinitely and the program locks, yeah
                        message = "";
                    } // else
                    break;
                case Effects.Gamble:
                    Gamble();
                    message = "The urge to gamble overcomes you.";
                    break;
                case Effects.GoGhost:
                    GoGhost();
                    message = "Welcome to the Astral Plane.";
                    break;
                case Effects.HideCrosshair:
                    if (color == "red")
                    {
                        HideCrosshair(); // This one only works for hosts so just reroll if a client gets it
                        message = "Have you got something in your eye?";
                    } // if
                    else
                    {
                        Roll();
                        message = "";
                    } // else
                    break;
                case Effects.HalveMoney:
                    HalveMoney();
                    message = "Your wallet feels lighter.";
                    break;
                case Effects.Hellfire:
                    Hellfire();
                    message = "HOT!";
                    break;
                case Effects.IcarianFlight:
                    IcarianFlight();
                    message = "Icarian Flight!";
                    break;
                case Effects.Instakill:
                    Instakill();
                    message = "Death blow!";
                    break;
                case Effects.Meteors:
                    Meteors();
                    message = "The sky is falling.";
                    break;
                case Effects.MotherVagrant:
                    MotherVagrant();
                    message = "Mother Vagrant appears.";
                    break;
                case Effects.RandomizeSurvivor:
                    RandomizeSurvivor();
                    message = "Who are you?";
                    break;
                case Effects.RerollItems:
                    RerollItems();
                    message = "Did you always have those?";
                    break;
                case Effects.RuinName:
                    RuinName();
                    message = "Having an identity crisis?";
                    break;
                case Effects.SpawnBeetleGuard:
                    SpawnBeetleGuard();
                    SpawnBeetleGuard();
                    message = "Hello little bug.";
                    break;
                case Effects.TakeOneDamage:
                    TakeOneDamage();
                    message = "You feel a slight pinch.";
                    break;
                case Effects.TankMode:
                    TankMode();
                    message = "Survive.";
                    break;
                case Effects.WildSurge:
                    WildSurge();
                    message = "Magic overflowing!";
                    break;
            } // switch

            if (messagesEnabled && message != "")
            {
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = "<color=" + color + ">" + master.GetComponent<PlayerCharacterMasterController>().GetDisplayName() + "</color> <color=#1E90FF>triggered wild magic!\n" + message + "</color>"
                });
            } // if
        } // roll

        /// <summary>
        /// Whether or not to display effect flavor text in chat.
        /// </summary>
        /// <param name="flag">Yea or nay</param>
        public void EnableMessages(bool flag)
        {
            messagesEnabled = flag;
        } // EnableMessages

        /// <summary>
        /// Returns Scoreboard Strips
        /// </summary>
        /// <returns></returns>
        public List<ScoreboardStrip> GetStrips()
        {
            return stripList;
        } // GetStrips

        /// <summary>
        /// Effect proc chance
        /// </summary>
        /// <param name="chance">Possible values: 'low', 'medium', 'high'</param>
        public void SetChance(string chance)
        {
            switch(chance)
            {
                case "low":
                    rollChance = 0; // 0.5%
                    break;
                case "medium":
                    rollChance = 4; // 2.5%
                    break;
                case "high":
                    rollChance = 29; // 15%
                    break;
                default:
                    rollChance = 0;
                    break;
            } // switch
        } // setChance

        /// <summary>
        /// Sets the color of the player's username for popup messages
        /// </summary>
        /// <param name="color">Name or hex code of color</param>
        public void SetColor(string color)
        {
            this.color = color;
        } // SetColor

        /// <summary>
        /// Enables or disables the spite (Operation FUN) effect
        /// </summary>
        /// <param name="fun">yes fun or no fun</param>
        public void SetFun(bool fun)
        {
            spite = fun;
        } // SetFun

        // Changing of the guard
        public void SetMaster(CharacterMaster newMaster)
        {
            master = newMaster;
            ResetTemps();
        } // SetMaster

        // Called with set master
        private void ResetTemps()
        {
            vagrantList = new List<CharacterMaster>();
            fakeHooves = 0;
            hauntCounter = 0;
            rngCap = 200;
            canRoll = true;
            rollReady = true;
        } // ResetTemps

        // They comin
        private void BeetleSquad()
        {
            for (int i = 0; i < beetleWaveMax; i++)
            {
                Timer.SetTimer(() =>
                {
                    BeetleSquadHelper();
                }, 5 * i);
            } // for
        } // BeetleSquad

        // Does what it says it does
        private void BeetleSquadHelper()
        {
            for (int i = 0; i < 10; i++)
            {
                GameObject gameObject = DirectorCore.instance.TrySpawnObject((SpawnCard)Resources.Load("SpawnCards/CharacterSpawnCards/cscBeetle"), new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                    minDistance = 10f,
                    maxDistance = 30f,
                    spawnOnTarget = master.GetBody().transform
                }, RoR2Application.rng);
                if (gameObject)
                {
                    CharacterMaster component = gameObject.GetComponent<CharacterMaster>();
                    if (component)
                    {
                        component.teamIndex = TeamIndex.Monster;
                        component.inventory.GiveItem(ItemIndex.Hoof, 10);
                        component.inventory.GiveItem(ItemIndex.Syringe, 10);
                        component.money = (uint)Run.instance.GetDifficultyScaledCost(1);
                        if (UnityEngine.Random.Range(0, 100) < 20)
                        {
                            switch (UnityEngine.Random.Range(0, 3))
                            {
                                case 0:
                                    component.inventory.SetEquipmentIndex(EquipmentIndex.AffixRed);
                                    break;
                                case 1:
                                    component.inventory.SetEquipmentIndex(EquipmentIndex.AffixBlue);
                                    break;
                                case 2:
                                    component.inventory.SetEquipmentIndex(EquipmentIndex.AffixWhite);
                                    break;
                            } // switch
                        } // if
                    } // if
                } // if
            } // for
        } // BeetleSquadHelper

        // Spooky
        private void BeginHaunt()
        {
            hauntCounter = 5;
        } // BeginHaunt

        // I am become imp
        private void BossMode()
        {
            if (canRoll)
            {
                master.GetBody().AddTimedBuff(BuffIndex.Immune, 3f);
                string[] bossPrefabs = { "ImpBossBody", "TitanBody", "ClayBossBody" };
                string boss = bossPrefabs[UnityEngine.Random.Range(0, bossPrefabs.Length)];
                GameObject newBody = BodyCatalog.FindBodyPrefab(boss);
                oldBody = master.bodyPrefab;
                master.bodyPrefab = newBody;
                master.Respawn(master.GetBody().footPosition, master.GetBody().transform.rotation, true);
                canRoll = false; // Things could get weird if we use magic while Boss'd
                Timer.SetTimer(() =>
                {
                    master.bodyPrefab = oldBody;
                    master.Respawn(master.GetBody().footPosition, master.GetBody().transform.rotation, true);
                    canRoll = true;
                }, 30);
            } // if
        } // BossMode

        // Yeehaw
        private void BuffDamage()
        {
            master.inventory.GiveItem(ItemIndex.BoostDamage, 10);
            Timer.SetTimer(() =>
            {
                master.inventory.RemoveItem(ItemIndex.BoostDamage, 10);
            }, 15);
        } // BuffDamage

        // Go fast
        private void BuffMove()
        {
            fakeHooves += 7;
            master.GetBody().RecalculateStats();
            Timer.SetTimer(() =>
            {
                fakeHooves -= 7;
                master.GetBody().RecalculateStats();
            }, 15);
        } // BuffMove

        // Hoorah
        private void CommandoBackup()
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject prefab = MasterCatalog.FindMasterPrefab("CommandoMonsterMaster");
                GameObject body = BodyCatalog.FindBodyPrefab("CommandoBody");

                GameObject commando = UnityEngine.Object.Instantiate<GameObject>(prefab, master.GetBody().transform.position, Quaternion.identity);
                commando.AddComponent<MasterSuicideOnTimer>().lifeTimer = UnityEngine.Random.Range(60f, 120f);
                CharacterMaster commandoMaster = commando.GetComponent<CharacterMaster>();
                commandoMaster.teamIndex = TeamIndex.Player;

                NetworkServer.Spawn(commando);
                if (victim != null)
                {
                    commandoMaster.SpawnBody(body, victim.GetBody().transform.position, Quaternion.identity);
                } // if
                else
                {
                    commandoMaster.SpawnBody(body, master.GetBody().transform.position, Quaternion.identity);
                } // else
                
                // Support Builds
                switch (i)
                {
                    case 0:
                        commandoMaster.inventory.GiveItem(ItemIndex.Bandolier, 5);
                        break;
                    case 1:
                        commandoMaster.inventory.GiveItem(ItemIndex.Tooth, 5);
                        break;
                    case 2:
                        commandoMaster.inventory.GiveItem(ItemIndex.SlowOnHit, 1);
                        break;
                } // switch
            } // for
        } // CommandoBackup

        // I'm sure
        private void Charm()
        {
            if (victim != null)
            {
                victim.GetBody().teamComponent.teamIndex = TeamIndex.Player;
                victim.teamIndex = TeamIndex.Player;
                victim.inventory.GiveItem(ItemIndex.BoostDamage, 30);
                victim.inventory.GiveItem(ItemIndex.HealthDecay, 30);
                charmedList.Add(victim.GetBody());
            } // if
        } // Charm

        // 'Pardner
        private void DebuffDamage()
        {
            debuffed = true;
            //master.GetBody().baseDamage -= 3;
            Timer.SetTimer(() =>
            {
                debuffed = false;//master.GetBody().baseDamage += 3;
            }, 15);
        } // DebuffDamage

        // Yowch
        private void DestroyEquipment()
        {
            master.inventory.SetEquipmentIndex(EquipmentIndex.None);
        } // DestroyEquipment

        // Jackpot
        private void DoubleMoney()
        {
            if (master.money < uint.MaxValue / 2)
                master.money *= 2;
        } // DoubleMoney

        // Make a gnome
        private void Effigy()
        {
            NetworkServer.Spawn(UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/NetworkedObjects/CrippleWard"), master.GetBody().corePosition, Quaternion.identity));
        } // Effigy

        // They ARE mercenaries
        private void EnemyMerc()
        {
            GameObject prefab = MasterCatalog.FindMasterPrefab("MercMonsterMaster");
            GameObject body = BodyCatalog.FindBodyPrefab("MercBody");

            GameObject merc = UnityEngine.Object.Instantiate<GameObject>(prefab, master.GetBody().transform.position, Quaternion.identity);
            CharacterMaster mercMaster = merc.GetComponent<CharacterMaster>();
            mercMaster.teamIndex = TeamIndex.Monster;

            NetworkServer.Spawn(merc);
            CharacterBody m = mercMaster.SpawnBody(body, master.GetBody().transform.position, Quaternion.identity);
            m.baseDamage = master.GetBody().baseDamage / 4;

            // More to make them look goofy than get their effects.
            mercMaster.inventory.GiveItem(ItemIndex.SprintArmor, 1);
            mercMaster.inventory.GiveItem(ItemIndex.CritGlasses, 1);
            mercMaster.inventory.GiveItem(ItemIndex.AttackSpeedOnCrit, 1);
            mercMaster.inventory.GiveItem(ItemIndex.PersonalShield, 1);
        } // EnemyMerc

        // Let it burn
        private void FireTrail()
        {
            master.GetBody().AddTimedBuff(BuffIndex.AffixRed, 10);
        } // FireTrail

        // Just activates the artifact
        private void Funballs()
        {
            if (!Run.instance.enabledArtifacts.HasArtifact(ArtifactIndex.Bomb))
            {
                Run.instance.enabledArtifacts.AddArtifact(ArtifactIndex.Bomb);
                Timer.SetTimer(() =>
                {
                    Run.instance.enabledArtifacts.RemoveArtifact(ArtifactIndex.Bomb);
                }, 60);
            } // if
        } // Funballs

        // Pretty much just a copy of gold shrine code
        private void Gamble()
        {
            int cost = Run.instance.GetDifficultyScaledCost(17);
            while (master.money >= cost)
            {
                Xoroshiro128Plus rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
                PickupIndex none = PickupIndex.none;
                PickupIndex value = Run.instance.availableTier1DropList[rng.RangeInt(0, Run.instance.availableTier1DropList.Count - 1)];
                PickupIndex value2 = Run.instance.availableTier2DropList[rng.RangeInt(0, Run.instance.availableTier2DropList.Count - 1)];
                PickupIndex value3 = Run.instance.availableTier3DropList[rng.RangeInt(0, Run.instance.availableTier3DropList.Count - 1)];
                PickupIndex value4 = Run.instance.availableEquipmentDropList[rng.RangeInt(0, Run.instance.availableEquipmentDropList.Count - 1)];
                WeightedSelection<PickupIndex> weightedSelection = new WeightedSelection<PickupIndex>(8);
                weightedSelection.AddChoice(none, 50.0f); // Definitely just made all these weights up
                weightedSelection.AddChoice(value, 25.0f);
                weightedSelection.AddChoice(value2, 14.0f);
                weightedSelection.AddChoice(value3, 1.0f);
                weightedSelection.AddChoice(value4, 10.0f);
                PickupIndex pickupIndex = weightedSelection.Evaluate(rng.nextNormalizedFloat);
                bool flag = pickupIndex == PickupIndex.none;
                if (flag)
                {
                    Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
                    {
                        subjectCharacterBodyGameObject = master.GetBody().gameObject,
                        baseToken = "SHRINE_CHANCE_FAIL_MESSAGE"
                    });
                }
                else
                {
                    PickupDropletController.CreatePickupDroplet(pickupIndex, master.GetBody().transform.position, master.GetBody().transform.forward * 20f);
                    Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
                    {
                        subjectCharacterBodyGameObject = master.GetBody().gameObject,
                        baseToken = "SHRINE_CHANCE_SUCCESS_MESSAGE"
                    });
                }

                EffectManager.instance.SpawnEffect(Resources.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
                {
                    origin = master.GetBody().transform.position,
                    rotation = Quaternion.identity,
                    scale = 1f,
                    color = Color.yellow
                }, true);

                master.money -= (uint)cost;
                cost = (int)(cost * 1.5);
            } // while
        } // Gamble

        // Catch em all
        private void GoGhost()
        {
            ItemCatalog.GetItemDef(ItemIndex.Ghost).hidden = true;
            master.inventory.GiveItem(ItemIndex.Ghost, 1);
            Timer.SetTimer(() =>
            {
                master.inventory.RemoveItem(ItemIndex.Ghost, 1);
            }, 10);
        } // GoGhost

        // Mildly inconvenient
        private void HideCrosshair()
        {
            master.GetBody().hideCrosshair = true;
            Timer.SetTimer(() =>
            {
                master.GetBody().hideCrosshair = false;
            }, 60);
        } // HideCrosshair

        // Sorry
        private void HalveMoney()
        {
            master.money /= 2;
        } // HalveMoney

        // Light em up
        private void Hellfire()
        {
            master.GetBody().AddHelfireDuration(6f);
        } // Hellfire

        // References
        private void IcarianFlight()
        {
            master.GetBody().baseJumpCount += 1000;
            master.GetBody().baseJumpPower += 25;
            master.GetBody().RecalculateStats();
            Timer.SetTimer(() =>
            {
                master.GetBody().baseJumpCount -= 1000;
                master.GetBody().baseJumpPower -= 25;
                master.GetBody().RecalculateStats();
            }, 20);
        } // IcarianFlight

        // Instant death
        private void Instakill()
        {
            if(victim != null)
                victim.GetBody().healthComponent.Suicide();
        } // Instakill

        // 1 meteor wave
        private void Meteors()
        {
            MeteorStormController component = UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/NetworkedObjects/MeteorStorm"), master.GetBody().corePosition, Quaternion.identity).GetComponent<MeteorStormController>();
            component.ownerDamage = master.GetBody().damage;
            component.isCrit = Util.CheckRoll(master.GetBody().crit, master);
            NetworkServer.Spawn(component.gameObject);
        } // Meteors

        // Biggun
        private void MotherVagrant()
        {
            GameObject gameObject = DirectorCore.instance.TrySpawnObject((SpawnCard)Resources.Load("SpawnCards/CharacterSpawnCards/cscVagrant"), new DirectorPlacementRule
            {
                placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                minDistance = 10f,
                maxDistance = 30f,
                spawnOnTarget = master.GetBody().transform
            }, RoR2Application.rng);
            if (gameObject)
            {
                CharacterMaster component = gameObject.GetComponent<CharacterMaster>();
                CharacterBody component2 = gameObject.GetComponent<CharacterBody>();
                if (component)
                {
                    component.teamIndex = TeamIndex.Monster;
                    component.GetBody().baseMaxHealth = component.GetBody().baseMaxHealth / 4;
                    component.money = (uint)Run.instance.GetDifficultyScaledCost(50);
                    vagrantList.Add(component);
                } // if
            } // if
        } // MotherVagrant

        // Because why not
        private void RandomizeSurvivor()
        {
            master.GetBody().AddTimedBuff(BuffIndex.Immune, 3f);
            string[] survivorPrefabs = { "CommandoBody", "ToolbotBody", "HuntressBody", "EngiBody", "MageBody", "MercBody" };
            if (SurvivorCatalog.FindSurvivorDefFromBody(BodyCatalog.FindBodyPrefab("BanditBody")) != null)
                survivorPrefabs = new string[] { "CommandoBody", "ToolbotBody", "HuntressBody", "EngiBody", "MageBody", "MercBody", "BanditBody" };
            string newSurvivor = survivorPrefabs[UnityEngine.Random.Range(0, survivorPrefabs.Length)];
            GameObject newBody = BodyCatalog.FindBodyPrefab(newSurvivor);
            master.bodyPrefab = newBody;
            master.Respawn(master.GetBody().footPosition, master.GetBody().transform.rotation, true);
        } // RandomizeSurvivor

        // Rerolls one item of each rarity in the player's possession
        private void RerollItems()
        {
            List<ItemIndex> whiteItems = new List<ItemIndex>();
            List<ItemIndex> greenItems = new List<ItemIndex>();
            List<ItemIndex> redItems = new List<ItemIndex>();
            List<ItemIndex> blueItems = new List<ItemIndex>();

            // Determine inventory
            for (int i = 0; i < (int)ItemIndex.Count; i++)
            {
                if (master.inventory.GetItemCount((ItemIndex)i) > 0)
                {
                    switch (ItemCatalog.GetItemDef((ItemIndex)i).tier)
                    {
                        case ItemTier.Tier1:
                            whiteItems.Add((ItemIndex)i);
                            break;

                        case ItemTier.Tier2:
                            greenItems.Add((ItemIndex)i);
                            break;

                        case ItemTier.Tier3:
                            redItems.Add((ItemIndex)i);
                            break;

                        case ItemTier.Lunar:
                            blueItems.Add((ItemIndex)i);
                            break;
                    } // switch
                } // if
            } // for

            // Reroll
            if (whiteItems.Count > 0)
            {
                master.inventory.RemoveItem(whiteItems[UnityEngine.Random.Range(0, whiteItems.Count)], 1);
                master.inventory.GiveItem(ItemCatalog.tier1ItemList[UnityEngine.Random.Range(0, ItemCatalog.tier1ItemList.Count)], 1);
            } // if

            if (greenItems.Count > 0)
            {
                master.inventory.RemoveItem(greenItems[UnityEngine.Random.Range(0, greenItems.Count)], 1);
                master.inventory.GiveItem(ItemCatalog.tier2ItemList[UnityEngine.Random.Range(0, ItemCatalog.tier2ItemList.Count)], 1);
            } // if

            if (redItems.Count > 0)
            {
                master.inventory.RemoveItem(redItems[UnityEngine.Random.Range(0, redItems.Count)], 1);
                master.inventory.GiveItem(ItemCatalog.tier3ItemList[UnityEngine.Random.Range(0, ItemCatalog.tier3ItemList.Count)], 1);
            } // if

            if (blueItems.Count > 0)
            {
                master.inventory.RemoveItem(blueItems[UnityEngine.Random.Range(0, blueItems.Count)], 1);
                master.inventory.GiveItem(ItemCatalog.lunarItemList[UnityEngine.Random.Range(0, ItemCatalog.lunarItemList.Count)], 1);
            } // if
        } // ReRollItem

        // Give em a new nick
        private void RuinName()
        {
            string currName = master.GetComponent<PlayerCharacterMasterController>().GetDisplayName();
            string[] goofNames = { "Commando", "Acrid", "Providence", "Jack-o-Lantern Dwyer", "Lucille Bluth", "Stone Titan",
                "Leslie Knope", "Andy Dwyer", "April Ludgate", "Ben Wyatt", "Ann Perkins", "Chris Traeger", "Ron Swanson", "Michael Scott",
                "Jim from The Office", "Rainn Wilson", "Robert 'The Lizard King' California", "Enforcer", "Sniper", "No One", "UES Contact Light",
                "Lemurian", "Bandit", "Miner", "Loader", "Chef", "HAN-D", "Huntress", "Artificer", "MUL-T", "Mercenary", "Imp Overlord",
                "Cremator", currName + ", Ancient Lava Swimmer", "Not " + currName, "Dean Pelton", "Action Jack Barker", "Liz Lemon",
                "Eleanor Shellstrop", "Danny DeVito", "Beetle Guard", "Gunner Drone", "Matt LeBlanc", "Scarlett Johansson", "Ron Dunn",
                currName + ", Lord of Cinder", currName + " the Great", "High King " + currName, currName + " the Expendable", "Engineer"};

            string goofName = goofNames[UnityEngine.Random.Range(0, goofNames.Length)];
            PlayerCharacterMasterController controller = master.GetComponent<PlayerCharacterMasterController>();

            // Set the goof
            if (controller && controller.networkUser)
            {
                controller.networkUser.userName = goofName;
            } // if

            // Update scoreboard names
            for (int i = 0; i < stripList.Count; i++)
            {
                stripList[i].nameLabel.text = PlayerCharacterMasterController.instances[i].networkUser.userName;
            } // for
        } // void

        // A Friend
        private void SpawnBeetleGuard()
        {
            GameObject gameObject = DirectorCore.instance.TrySpawnObject((SpawnCard)Resources.Load("SpawnCards/CharacterSpawnCards/cscBeetleGuardAlly"), new DirectorPlacementRule
            {
                placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                minDistance = 3f,
                maxDistance = 40f,
                spawnOnTarget = master.GetBody().transform
            }, RoR2Application.rng);

            if (gameObject)
            {
                CharacterMaster component = gameObject.GetComponent<CharacterMaster>();
                AIOwnership component2 = gameObject.GetComponent<AIOwnership>();
                BaseAI component3 = gameObject.GetComponent<BaseAI>();
                if (component)
                {
                    component.teamIndex = TeamIndex.Player;
                    component.inventory.GiveItem(ItemIndex.BoostDamage, 30);
                    component.inventory.GiveItem(ItemIndex.BoostHp, 10);
                }
                if (component2)
                {
                    component2.ownerMaster = master;
                }
                if (component3)
                {
                    component3.leader.gameObject = master.GetBody().gameObject;
                }
            }
        } // SpawnBeetleGuard

        // Yes I'm writing a function for this
        private void TakeOneDamage()
        {
            DamageInfo damageInfo = new DamageInfo();
            damageInfo.damage = 1.0f;
            damageInfo.damageType = DamageType.NonLethal;
            damageInfo.damageColorIndex = DamageColorIndex.WeakPoint;
            master.GetBody().healthComponent.TakeDamage(damageInfo);
        } // TakeOneDamage

        // Possibly a terrible situation
        private void TankMode()
        {
            CharacterBody b = master.GetBody();
            master.inventory.GiveItem(ItemIndex.BoostDamage, 5);
            b.AddTimedBuff(BuffIndex.ArmorBoost, 10f);
            b.AddTimedBuff(BuffIndex.ClayGoo, 10f);
            Timer.SetTimer(() =>
            {
                master.inventory.RemoveItem(ItemIndex.BoostDamage, 5);
            }, 10);
        } // TankMode

        // Infinite power
        private void WildSurge()
        {
            if (rngCap == 200)
            {
                Timer.SetTimer(() =>
                {
                    rngCap = 0;
                    Timer.SetTimer(() =>
                    {
                        rngCap = 200;
                    }, 5); // 5 second duration
                }, 3); // 3 second delay
            } // if
        } // WildSurge
    } // MagicHandler Class
} // Wildmagic Namespace
