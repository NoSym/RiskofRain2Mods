using System;
using BepInEx;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.Networking;

namespace WildMagic
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.NoSym.wildmagic", "WildMagic", "1.0.0")]
    public class WildMagic : BaseUnityPlugin
    {
        private bool haunted = false;
        private string goofName = "";
        private float dmgBuff = 0;
        private float dmgBuffTimer = -1;
        private float dmgDebuff = 0;
        private float dmgDebuffTimer = -1;
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

        private DamageTrail[] trailArray = new DamageTrail[10];

        // Woke
        public void Awake()
        {
            // Not important Really
            On.RoR2.CharacterBody.OnDamageDealt += (orig, self, report) =>
            {
                if (self.Equals(PlayerCharacterMasterController.instances[0].master.GetBody()))
                    ;// instakill(report.victimMaster);// polymorph(report.victimMaster);

                orig(self, report);
            };

            // Spooky
            On.RoR2.GlobalEventManager.OnCharacterDeath += (orig, self, report) =>
            {
                // Ghosts
                if (haunted && report.damageInfo.attacker.Equals(PlayerCharacterMasterController.instances[0].master.GetBody().gameObject))
                {
                    CharacterBody ghost = Util.TryToCreateGhost(report.victimBody, report.victimBody, 30);
                    ghost.baseDamage /= 6.0f; // I mean seriously, 500%?
                } // if

                orig(self, report);
            };

            // They took my naaaame Juuustin
            On.RoR2.PlayerCharacterMasterController.GetDisplayName += (orig, self) =>
            {
                if (self.Equals(PlayerCharacterMasterController.instances[0]) && goofName != "")
                    return goofName;
                else
                    return orig(self);
            };

        } // Awake

        // Spooky
        private void beginHaunt()
        {
            haunted = true;
            hauntedTimer = 1800; // 30 seconds
        } // evilGhost

        // Yeehaw
        private void buffDamage()
        {
            if (dmgBuffTimer == -1)
            {
                PlayerCharacterMasterController p = PlayerCharacterMasterController.instances[0];
                dmgBuff = p.master.GetBody().baseDamage;
                p.master.GetBody().baseDamage += dmgBuff;
                dmgBuffTimer = 900; // 15 seconds
            } // if
        } // buffDamage

        // Go fast
        private void buffMove()
        {
            if(moveBuffTimer == -1)
            {
                PlayerCharacterMasterController p = PlayerCharacterMasterController.instances[0];
                moveBuff = p.master.GetBody().baseMoveSpeed;
                p.master.GetBody().baseMoveSpeed += moveBuff;
                moveBuffTimer = 900; // 15 seconds
            } // if
        } // buffMove

        // 'Pardner
        private void debuffDamage()
        {
            if (dmgDebuffTimer == -1)
            {
                PlayerCharacterMasterController p = PlayerCharacterMasterController.instances[0];
                dmgDebuff = p.master.GetBody().baseDamage / 2;
                p.master.GetBody().baseDamage -= dmgDebuff;
                dmgDebuffTimer = 900; // 15 seconds
            } // if
        } // debuffDamage

        // Let it burn
        private void fireTrail(CharacterMaster master)
        {
            if (trailTimer == -1)
            {
                for (int i = 0; i < trailArray.Length; i++)
                {
                    DamageTrail wildFire;
                    wildFire = Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/FireTrail"), master.GetBody().transform).GetComponent<DamageTrail>();
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
        private void funballs()
        {
            if (!Run.instance.enabledArtifacts.HasArtifact(ArtifactIndex.Bomb))
            {
                Run.instance.enabledArtifacts.AddArtifact(ArtifactIndex.Bomb);
                funballTimer = 3600; // 1 minute
            } // if
        } // funballs

        // Mildly inconvenient
        private void hideCrosshair()
        {
            PlayerCharacterMasterController.instances[0].master.GetBody().hideCrosshair = true;
            hideTimer = 3600; // 1 minute
        } // hideCrosshair

        // Boom
        private void instakill(CharacterMaster victim)
        {
            victim.GetBody().healthComponent.Suicide();
        } // instakill

        // Note that this keeps the original ai, which is kinda funny since it's literally the previous enemy's brain in a new body
        // THIS DOES NOT WORK RIGHT NOW
        private void polymorph(CharacterMaster master)
        {
            string[] names = { "Beetle", "Bell", "Bison", "Golem", "GreaterWisp", "HermitCrab", "Imp", "Jellyfish", "LemurianBruiser", "Lemurian", "Wisp" };
            string name = names[(int)UnityEngine.Random.Range(0, names.Length)];
            master.bodyPrefab = BodyCatalog.FindBodyPrefab(name + "Body");
            master.Respawn(master.GetBody().footPosition, master.GetBody().transform.rotation, true);
            // Atm the guy will spawn and just stand there
        } // polymorph

        // Possibly a terrible situation
        private void tankMode()
        {
            if (tankTimer == -1)
            {
                CharacterBody p = PlayerCharacterMasterController.instances[0].master.GetBody();
                tankDamageBuff = p.baseDamage * 2;
                tankMoveDebuff = p.baseMoveSpeed / 4;
                tankArmorBuff = p.baseArmor * 2;
                p.baseDamage += tankDamageBuff;
                p.baseMoveSpeed -= tankMoveDebuff;
                p.baseArmor += tankArmorBuff;
                tankTimer = 600; // 10 seconds
            } // if
        } // tankMode

        // Give em a new nick
        private void ruinName()
        {
            string[] goofNames = { "Jerry", "Sarah", "Commando", "Acrid", "Hopoo", "Ghor", "Paul", "Chris", "Providence",
                "Jack-o-Lantern Dwyer", "Dwight", "Lucille Bluth", "John Mulaney", "Stone Titan",
                "Leslie Knope", "Andy Dwyer", "April Ludgate", "Ben Wyatt", "Ann Perkins", "Chris Traeger", "Ron Swanson"};

            goofName = goofNames[UnityEngine.Random.Range(0, goofNames.Length)];
        } // void

        // Not great to look at but seems optimized. Might loop this later for the eyes.
        private void updateTimers()
        {
            if(dmgBuffTimer > 0)
                dmgBuffTimer--;
            if (dmgDebuffTimer > 0)
                dmgDebuffTimer--;
            if (moveBuffTimer > 0)
                moveBuffTimer--;
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
        } // timerHandler

        // Hardcoded Finality
        private void resolveTimers()
        {
            CharacterMaster master = PlayerCharacterMasterController.instances[0].master;

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

            if (hideTimer == 0)
            {
                master.GetBody().hideCrosshair = false;
                hideTimer = -1;
            } // hideTimer

            if (trailTimer == 0)
            {
                for (int i = 0; i < trailArray.Length; i++)
                {
                    Destroy(trailArray[i].gameObject);
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
        } // resolveTimers

        public void Update()
        {
            // Run actually started
            if (Run.instance.fixedTime > 0)
            {
                // Debugging Key
                if (Input.GetKeyDown(KeyCode.F2))
                {
                    CharacterMaster master = PlayerCharacterMasterController.instances[0].master;
                    fireTrail(master);

                    MagicHandler testHandler = new MagicHandler(master);

                    testHandler.Roll();

                    // Testing
                    //buffMove();
                    //tankMode();
                } // if

                //updateTimers();
                //resolveTimers();
            } // if
        } // Update
    } // Chaos
} // Chaos
