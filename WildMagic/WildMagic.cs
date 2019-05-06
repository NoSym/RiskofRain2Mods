﻿using System;
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
        private string goofName = "";
       
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

            // They took my naaaame Juuustin
            On.RoR2.PlayerCharacterMasterController.GetDisplayName += (orig, self) =>
            {
                if (self.Equals(PlayerCharacterMasterController.instances[0]) && goofName != "")
                    return goofName;
                else
                    return orig(self);
            };

        } // Awake

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

        // Give em a new nick
        private void ruinName()
        {
            string[] goofNames = { "Jerry", "Sarah", "Commando", "Acrid", "Hopoo", "Ghor", "Paul", "Chris", "Providence",
                "Jack-o-Lantern Dwyer", "Dwight", "Lucille Bluth", "John Mulaney", "Stone Titan",
                "Leslie Knope", "Andy Dwyer", "April Ludgate", "Ben Wyatt", "Ann Perkins", "Chris Traeger", "Ron Swanson"};

            goofName = goofNames[UnityEngine.Random.Range(0, goofNames.Length)];
        } // void

        public void Update()
        {
            // Run actually started
            if (Run.instance.fixedTime > 0)
            {
                // Debugging Key
                if (Input.GetKeyDown(KeyCode.F2))
                {
                    CharacterMaster master = PlayerCharacterMasterController.instances[0].master;

                    MagicHandler testHandler = new MagicHandler(master);

                    testHandler.Roll();
                } // if
            } // if
        } // Update
    } // Chaos
} // Chaos
