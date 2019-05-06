using System;
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

        // Flags
        private bool haunted = false;

        // Arrays
        private DamageTrail[] trailArray = new DamageTrail[10];

        // Timers
        private float dmgBuff = 0;
        private float dmgBuffTimer = -1;
        private float dmgDebuff = 0;
        private float dmgDebuffTimer = -1;
        private float ghostTimer = -1;
        private float moveBuff = 0;
        private float moveBuffTimer = -1;
        private float hideTimer = -1;
        private float trailTimer = -1;
        private float hauntedTimer = -1;
        private float funballTimer = -1;
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
            DoubleMoney();
            GoGhost();
        } // roll

        private void UpdateTimers()
        {
            Chat.AddMessage("It's updating");

            TickTimers();
            ResolveTimers();            
        } // Update

        // Could be cleaner!
        private void ResolveTimers()
        {
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

            if (hideTimer == 0)
            {
                master.GetBody().hideCrosshair = false;
                hideTimer = -1;
            } // hideTimer

            if (trailTimer == 0)
            {
                for (int i = 0; i < trailArray.Length; i++)
                {
                    UnityEngine.Object.Destroy(trailArray[i].gameObject);
                    trailArray[i] = null;
                } // for
                trailTimer = -1;
            } // trailTimer

            if (hauntedTimer == 0)
            {
                haunted = false;
                hauntedTimer = -1;
            } // hauntedTimer

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
            
        } // Resolve Timers

        // DeltaTime?
        private void TickTimers()
        {
            if (dmgBuffTimer > 0)
                dmgBuffTimer--;
            if (dmgDebuffTimer > 0)
                dmgDebuffTimer--;
            if (moveBuffTimer > 0)
                moveBuffTimer--;
            if (ghostTimer > 0)
                ghostTimer--;
            if (hideTimer > 0)
                hideTimer--;
            if (trailTimer > 0)
                trailTimer--;
            if (hauntedTimer > 0)
                hauntedTimer--;
            if (funballTimer > 0)
                funballTimer--;
            if (tankTimer > 0)
                tankTimer--;
        } // TickTimers

        // Spooky
        private void BeginHaunt()
        {
            haunted = true;
            hauntedTimer = 1800; // 30 seconds
        } // evilGhost

        // Yeehaw
        private void BuffDamage()
        {
            if (dmgBuffTimer == -1)
            {
                dmgBuff = master.GetBody().baseDamage;
                master.GetBody().baseDamage += dmgBuff;
                dmgBuffTimer = 900; // 15 seconds
            } // if
        } // buffDamage

        // Go fast
        private void BuffMove()
        {
            if (moveBuffTimer == -1)
            {
                moveBuff = master.GetBody().baseMoveSpeed;
                master.GetBody().baseMoveSpeed += moveBuff;
                moveBuffTimer = 900; // 15 seconds
            } // if
        } // buffMove

        // 'Pardner
        private void DebuffDamage()
        {
            if (dmgDebuffTimer == -1)
            {
                dmgDebuff = master.GetBody().baseDamage / 2;
                master.GetBody().baseDamage -= dmgDebuff;
                dmgDebuffTimer = 900; // 15 seconds
            } // if
        } // debuffDamage

        // Yowch
        private void DestroyEquipment()
        {
            master.inventory.SetEquipmentIndex(EquipmentIndex.None);
        } // destroyEquipment

        // Jackpot
        private void DoubleMoney()
        {
            if (master.money < uint.MaxValue / 2)
                master.money *= 2;
        } // doubleMoney

        // Make a gnome
        private void Effigy()
        {
            NetworkServer.Spawn(UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/NetworkedObjects/CrippleWard"), master.GetBody().corePosition, Quaternion.identity));
        } // effigy

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
        } // fireTrail

        // Just activates the artifact
        private void Funballs()
        {
            if (!Run.instance.enabledArtifacts.HasArtifact(ArtifactIndex.Bomb))
            {
                Run.instance.enabledArtifacts.AddArtifact(ArtifactIndex.Bomb);
                funballTimer = 3600; // 1 minute
            } // if
        } // funballs

        // Catch em all
        private void GoGhost()
        {
            if (ghostTimer == -1)
            {
                master.inventory.GiveItem(ItemIndex.Ghost, 1);
                ghostTimer = 600; // 10 seconds
            } // if
        } // goGhost

        // Mildly inconvenient
        private void HideCrosshair()
        {
            master.GetBody().hideCrosshair = true;
            hideTimer = 3600; // 1 minute
        } // hideCrosshair

        // Sorry
        private void HalveMoney()
        {
            master.money /= 2;
        } // halveMoney

        // Light em up
        private void Hellfire()
        {
            master.GetBody().AddHelfireDuration(6f);
        } // hellfire

        // 1 meteor wave
        private void Meteors()
        {
            MeteorStormController component = UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/NetworkedObjects/MeteorStorm"), master.GetBody().corePosition, Quaternion.identity).GetComponent<MeteorStormController>();
            component.ownerDamage = master.GetBody().damage;
            component.isCrit = Util.CheckRoll(master.GetBody().crit, master);
            NetworkServer.Spawn(component.gameObject);
        } // meteors

        // Because why not
        private void RandomizeSurvivor()
        {
            string[] survivorPrefabs = { "CommandoBody", "ToolbotBody", "HuntressBody", "EngiBody", "MageBody", "MercBody" };
            string newSurvivor = survivorPrefabs[UnityEngine.Random.Range(0, survivorPrefabs.Length)];
            GameObject newBody = BodyCatalog.FindBodyPrefab(newSurvivor);
            master.bodyPrefab = newBody;
            master.Respawn(master.GetBody().footPosition, master.GetBody().transform.rotation, true);
        } // randomizeSurvivor

        // A Friend
        private void SpawnBeetleGuard()
        {
            GameObject prefab = MasterCatalog.FindMasterPrefab("BeetleGuardAlly" + "Master");
            GameObject body = BodyCatalog.FindBodyPrefab("BeetleGuardAlly" + "Body");

            GameObject beetle = UnityEngine.Object.Instantiate<GameObject>(prefab, master.GetBody().transform.position, Quaternion.identity);
            beetle.AddComponent<MasterSuicideOnTimer>().lifeTimer = 300f; // Summons the boy for 5 minutes
            CharacterMaster beetleMaster = beetle.GetComponent<CharacterMaster>();
            beetle.GetComponent<BaseAI>().leader.gameObject = beetleMaster.GetBody().gameObject;
            beetleMaster.teamIndex = TeamIndex.Player;

            NetworkServer.Spawn(beetle);
            beetleMaster.SpawnBody(body, master.GetBody().transform.position, Quaternion.identity);
        } // spawnBeetleGuard

        // Yes I'm writing a function for this
        private void TakeOneDamage()
        {
            DamageInfo damageInfo = new DamageInfo();
            damageInfo.damage = 1.0f;
            damageInfo.damageType = DamageType.NonLethal;
            damageInfo.damageColorIndex = DamageColorIndex.WeakPoint;
            master.GetBody().healthComponent.TakeDamage(damageInfo);
        } // takeOneDamage

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
        } // tankMode
    } // MagicHandler Class
} // Wildmagic Namespace
