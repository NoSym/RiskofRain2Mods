using System;
using System.Collections.Generic;
using BepInEx;
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
            BeginHaunt,
            BossMode,
            BuffDamage,
            BuffMove,
            DebuffDamage,
            DestroyEquipment,
            DoubleMoney,
            Effigy,
            FireTrail,
            Funballs,
            Gamble,
            GoGhost,
            HideCrosshair,
            HalveMoney,
            Hellfire,
            Instakill,
            Meteors,
            RandomizeSurvivor,
            RerollItems,
            SpawnBeetleGuard,
            TakeOneDamage,
            TankMode,
            Count
        };

        // For temporary transformations
        private GameObject oldBody;

        // Probably a bad idea
        private CharacterMaster victim;

        // 0.5% per rollChance, starting at 0
        private int rollChance = 0;

        // Flags
        private bool canRoll = true;
        private bool haunted = false;
        private bool messagesEnabled = false;

        // Arrays
        private DamageTrail[] trailArray = new DamageTrail[10];

        // Timers
        private float bossTimer = -1;
        private float dmgBuff = 0;
        private float dmgBuffTimer = -1;
        private float dmgDebuff = 0;
        private float dmgDebuffTimer = -1;
        private float ghostTimer = -1;
        private float hauntedTimer = -1;
        private float moveBuff = 0;
        private float moveBuffTimer = -1;
        private float hideTimer = -1;
        private float trailTimer = -1;
        private float funballTimer = -1;
        private float rollTimer = 0;
        private float tankDamageBuff = 0;
        private float tankMoveDebuff = 0;
        private float tankArmorBuff = 0;
        private float tankTimer = -1;

        /// <summary>
        /// Creates a new wild magic controller for the specified character master.
        /// </summary>
        /// <param name="magicMaster">Owner of wild magic effects.</param>
        public MagicHandler(CharacterMaster magicMaster)
        {
            master = magicMaster;

            // Magic Proc
            On.RoR2.CharacterBody.OnDamageDealt += (orig, self, report) =>
            {
                if (self.Equals(master.GetBody()) && !report.victimMaster.Equals(master))
                {
                    if (rollTimer == 0 && canRoll)
                    {
                        victim = report.victimMaster;

                        if (UnityEngine.Random.Range(0, 200) <= rollChance) // 0.5% chance to magic
                        {
                            Roll();
                        } // if

                        rollTimer = 60; // 1 second cooldown on rolling
                    } // if
                } // if

                orig(self, report);
            };

            // For the timers
            On.RoR2.Run.Update += (orig, self) =>
            {
                orig(self);
                UpdateTimers();
            };

            // Spooky
            On.RoR2.GlobalEventManager.OnCharacterDeath += (orig, self, report) =>
            {
                // Ghosts
                if (haunted && report.damageInfo.attacker.Equals(master.GetBody().gameObject))
                {
                    CharacterBody ghost = Util.TryToCreateGhost(report.victimBody, report.victimBody, 30);
                    ghost.baseDamage /= 6.0f; // I mean seriously, 500%?
                } // if

                orig(self, report);
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
                case Effects.BeginHaunt:
                    BeginHaunt();
                    message = "Souls of the slain return for vengeance.";
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
                case Effects.DebuffDamage:
                    DebuffDamage();
                    message = "Your strikes soften.";
                    break;
                case Effects.DestroyEquipment:
                    DestroyEquipment();
                    message = "Something in your possession has broken.";
                    break;
                case Effects.DoubleMoney:
                    DoubleMoney();
                    message = "Great wealth finds you.";
                    break;
                case Effects.Effigy:
                    Effigy();
                    message = "A 'friend' arrives to help!";
                    break;
                case Effects.FireTrail:
                    FireTrail();
                    message = "Time for trailblazing.";
                    break;
                case Effects.Funballs:
                    Funballs();
                    message = "Operation Fun activated.";
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
                    HideCrosshair();
                    message = "Have you got something in your eye?";
                    break;
                case Effects.HalveMoney:
                    HalveMoney();
                    message = "Your wallet feels lighter.";
                    break;
                case Effects.Hellfire:
                    Hellfire();
                    message = "HOT!";
                    break;
                case Effects.Instakill:
                    Instakill();
                    message = "Death blow!";
                    break;
                case Effects.Meteors:
                    Meteors();
                    message = "The sky is falling.";
                    break;
                case Effects.RandomizeSurvivor:
                    RandomizeSurvivor();
                    message = "Who are you?";
                    break;
                case Effects.RerollItems:
                    RerollItems();
                    message = "Did you always have those?";
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
            } // switch

            if (messagesEnabled)
                Chat.AddMessage(message);
        } // roll

        /// <summary>
        /// Whether or not to display effect flavor text in chat.
        /// </summary>
        /// <param name="flag">Yea or nay</param>
        public void EnableMessages(bool flag)
        {
            messagesEnabled = flag;
        } // EnableMessages

        public void SetChance(string chance)
        {
            switch(chance)
            {
                case "low":
                    rollChance = 0; // 0.5%
                    break;
                case "medium":
                    rollChance = 9; // 5%
                    break;
                case "high":
                    rollChance = 39; // 20%
                    break;
                default:
                    rollChance = 0;
                    break;
            } // switch
        } // setChance

        private void UpdateTimers()
        {
            TickTimers();
            ResolveTimers();            
        } // Update

        // Could be cleaner!
        private void ResolveTimers()
        {
            if (bossTimer == 0)
            {
                master.bodyPrefab = oldBody;
                master.Respawn(master.GetBody().footPosition, master.GetBody().transform.rotation, true);
                canRoll = true;
                bossTimer = -1;
            } // bossTimer

            if (dmgBuffTimer == 0)
            {
                master.GetBody().baseDamage -= dmgBuff;
                dmgBuffTimer = -1;
            } // dmgBuffTimer

            if (dmgDebuffTimer == 0)
            {
                master.GetBody().baseDamage += dmgDebuff;
                dmgDebuffTimer = -1;
            } // dmgDebuffTimer

            if (ghostTimer == 0)
            {
                master.inventory.RemoveItem(ItemIndex.Ghost, 1);
                ghostTimer = -1;
            } // ghostTimer

            if (hauntedTimer == 0)
            {
                haunted = false;
                hauntedTimer = -1;
            } // hauntedTimer

            if (hideTimer == 0)
            {
                master.GetBody().hideCrosshair = false;
                hideTimer = -1;
            } // hideTimer

            if (funballTimer == 0)
            {
                Run.instance.enabledArtifacts.RemoveArtifact(ArtifactIndex.Bomb);
                funballTimer = -1;
            } // funballTimer

            if (moveBuffTimer == 0)
            {
                master.GetBody().baseMoveSpeed -= moveBuff;
                moveBuffTimer = -1;
            } // moveBuffTimer

            if (tankTimer == 0)
            {
                master.GetBody().baseDamage -= tankDamageBuff;
                master.GetBody().baseMoveSpeed += tankMoveDebuff;
                master.GetBody().baseArmor -= tankArmorBuff;
                tankTimer = -1;
            } // tankTimer

            if (trailTimer == 0)
            {
                for (int i = 0; i < trailArray.Length; i++)
                {
                    UnityEngine.Object.Destroy(trailArray[i].gameObject);
                    trailArray[i] = null;
                } // for
                trailTimer = -1;
            } // trailTimer
        } // Resolve Timers

        // DeltaTime?
        private void TickTimers()
        {
            if (bossTimer > 0)
                bossTimer--;
            if (dmgBuffTimer > 0)
                dmgBuffTimer--;
            if (dmgDebuffTimer > 0)
                dmgDebuffTimer--;
            if (moveBuffTimer > 0)
                moveBuffTimer--;
            if (funballTimer > 0)
                funballTimer--;
            if (ghostTimer > 0)
                ghostTimer--;
            if (hideTimer > 0)
                hideTimer--;
            if (hauntedTimer > 0)
                hauntedTimer--;
            if (rollTimer > 0)
                rollTimer--;
            if (tankTimer > 0)
                tankTimer--;
            if (trailTimer > 0)
                trailTimer--;
        } // TickTimers

        // Spooky
        private void BeginHaunt()
        {
            haunted = true;
            hauntedTimer = 1800; // 30 seconds
        } // BeginHaunt

        // I am become imp
        private void BossMode()
        {
            if (bossTimer == -1)
            {
                string[] bossPrefabs = { "ImpBossBody", "TitanBody", "ClayBossBody" };
                string boss = bossPrefabs[UnityEngine.Random.Range(0, bossPrefabs.Length)];
                GameObject newBody = BodyCatalog.FindBodyPrefab(boss);
                oldBody = master.bodyPrefab;
                master.bodyPrefab = newBody;
                master.Respawn(master.GetBody().footPosition, master.GetBody().transform.rotation, true);
                canRoll = false; // Things could get weird if we use magic while Imp'd
                bossTimer = 1800;// 30 seconds
            } // if
            else
            {
                bossTimer = 1800; // Should never happen but just in case
            } // else
        } // BossMode

        // Yeehaw
        private void BuffDamage()
        {
            if (dmgBuffTimer == -1)
            {
                dmgBuff = master.GetBody().baseDamage;
                master.GetBody().baseDamage += dmgBuff;
                dmgBuffTimer = 900; // 15 seconds
            } // if
        } // BuffDamage

        // Go fast
        private void BuffMove()
        {
            if (moveBuffTimer == -1)
            {
                moveBuff = master.GetBody().baseMoveSpeed;
                master.GetBody().baseMoveSpeed += moveBuff;
                moveBuffTimer = 900; // 15 seconds
            } // if
        } // BuffMove

        // 'Pardner
        private void DebuffDamage()
        {
            if (dmgDebuffTimer == -1)
            {
                dmgDebuff = master.GetBody().baseDamage / 2;
                master.GetBody().baseDamage -= dmgDebuff;
                dmgDebuffTimer = 900; // 15 seconds
            } // if
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

        // Let it burn
        private void FireTrail()
        {
            if (trailTimer == -1)
            {
                for (int i = 0; i < trailArray.Length; i++)
                {
                    DamageTrail wildFire;
                    wildFire = UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/FireTrail"), master.GetBody().transform).GetComponent<DamageTrail>();
                    wildFire.transform.position = master.GetBody().footPosition;
                    wildFire.owner = master.GetBody().gameObject;
                    wildFire.radius *= master.GetBody().radius * 10;
                    wildFire.damagePerSecond *= master.GetBody().damage * 1.5f;
                    trailArray[i] = wildFire;
                } // for
                trailTimer = 3600; // 1 minute lifespan
            } // if
        } // FireTrail

        // Just activates the artifact
        private void Funballs()
        {
            if (!Run.instance.enabledArtifacts.HasArtifact(ArtifactIndex.Bomb))
            {
                Run.instance.enabledArtifacts.AddArtifact(ArtifactIndex.Bomb);
                funballTimer = 3600; // 1 minute
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
                /*
                Action<bool, Interactor> action = ShrineChanceBehavior.onShrineChancePurchaseGlobal;
                if (action != null)
                {
                    action(flag, activator);
                }
                */

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
            if (ghostTimer == -1)
            {
                master.inventory.GiveItem(ItemIndex.Ghost, 1);
                ghostTimer = 600; // 10 seconds
            } // if
        } // GoGhost

        // Mildly inconvenient
        private void HideCrosshair()
        {
            master.GetBody().hideCrosshair = true;
            hideTimer = 3600; // 1 minute
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

        // Because why not
        private void RandomizeSurvivor()
        {
            string[] survivorPrefabs = { "CommandoBody", "ToolbotBody", "HuntressBody", "EngiBody", "MageBody", "MercBody" };
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

        // A Friend
        private void SpawnBeetleGuard()
        {
            GameObject prefab = MasterCatalog.FindMasterPrefab("BeetleGuardAlly" + "Master");
            GameObject body = BodyCatalog.FindBodyPrefab("BeetleGuardAlly" + "Body");

            GameObject beetle = UnityEngine.Object.Instantiate<GameObject>(prefab, master.GetBody().transform.position, Quaternion.identity);
            beetle.AddComponent<MasterSuicideOnTimer>().lifeTimer = 120f; // Summons the boy for 2 minutes
            CharacterMaster beetleMaster = beetle.GetComponent<CharacterMaster>();
            beetle.GetComponent<BaseAI>().leader.gameObject = master.GetBody().gameObject;
            beetleMaster.teamIndex = TeamIndex.Player;

            NetworkServer.Spawn(beetle);
            beetleMaster.SpawnBody(body, master.GetBody().transform.position, Quaternion.identity);
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
            if (tankTimer == -1)
            {
                CharacterBody b = master.GetBody();
                tankDamageBuff = b.baseDamage * 2;
                tankMoveDebuff = b.baseMoveSpeed / 4;
                tankArmorBuff = b.baseArmor * 2;
                b.baseDamage += tankDamageBuff;
                b.baseMoveSpeed -= tankMoveDebuff;
                b.baseArmor += tankArmorBuff;
                tankTimer = 600; // 10 seconds
            } // if
        } // TankMode
    } // MagicHandler Class
} // Wildmagic Namespace
