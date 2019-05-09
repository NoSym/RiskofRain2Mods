using System;
using System.Collections.Generic;
using BepInEx;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.Networking;

// Driver for MagicHandler
namespace WildMagic
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.NoSym.wildmagic", "WildMagic", "1.1.0")]
    public class WildMagic : BaseUnityPlugin
    { 
        
        private bool started = false;
        private bool messagesEnabled = false;
        private string rollChance = "medium";
        private List<MagicHandler> magicHandlers = new List<MagicHandler>();
       
        // Woke
        public void Awake()
        {
            messagesEnabled = base.Config.Wrap<bool>("Settings", "MessagesEnabled", "If true each wild magic effect will display a chat message when it occurs.", true).Value;
            rollChance = base.Config.Wrap<string>("Settings", "RollChance", "Roll chance for a wild magic effect (low, medium, high).", "medium").Value;
        } // Awake

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

        public void Update()
        {
            // Run actually started
            if (Run.instance != null && Run.instance.fixedTime >= 0)
            {
                // Shenanigans for multiple runs in a session
                if (Run.instance.fixedTime < 1 && started == true)
                {
                    started = false;
                } // if
                if (Run.instance.fixedTime >= 1 && !started)
                {
                    int playerCount = PlayerCharacterMasterController.instances.Count;
                    for (int i = 0; i < playerCount; i++)
                    {
                        if (i >= magicHandlers.Count)
                        {
                            MagicHandler newHandler = new MagicHandler(PlayerCharacterMasterController.instances[i].master);
                            newHandler.EnableMessages(messagesEnabled);
                            newHandler.SetChance(rollChance);
                            magicHandlers.Add(newHandler);
                        } // if
                        else
                        {
                            magicHandlers[i].SetMaster(PlayerCharacterMasterController.instances[i].master);
                        } // else
                    }
                    started = true;
                } // if
                // Debugging Key
                if (Input.GetKeyDown(KeyCode.F2))
                {
                } // if
            } // if
        } // Update
    } // Chaos
} // Chaos
