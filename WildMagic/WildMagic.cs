using BepInEx;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace WildMagic
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.NoSym.wildmagic", "WildMagic", "2.0.0")]
    public class WildMagic : BaseUnityPlugin
    {
        // Preferences - Removed default values since they are set by Bind
        private bool messagesEnabled;
        private bool fun;
        private string rollChance;

        private List<MagicHandler> magicHandlers = new List<MagicHandler>();

        // Woke
        public void Awake()
        {
            // Config Info
            messagesEnabled = Config.Bind("Settings", "MessagesEnabled", true, "If true each wild magic effect will display a chat message when it occurs.").Value;
            rollChance = Config.Bind("Settings", "RollChance", "medium", "Roll chance for a wild magic effect (low, medium, high).").Value;
            fun = Config.Bind("Settings", "SpiteEffect", true, "Whether or not the spite (Funballs, Operation FUN, etc.) effect will roll").Value;

            // Begin Run and initialize junk
            On.RoR2.Run.Start += (orig, self) =>
            {
                orig(self);

                int playerCount = PlayerCharacterMasterController.instances.Count;
                for (int i = 0; i < playerCount; i++)
                {
                    if (i >= magicHandlers.Count)
                    {
                        MagicHandler newHandler = new MagicHandler(PlayerCharacterMasterController.instances[i].master);
                        newHandler.EnableMessages(messagesEnabled);
                        newHandler.SetChance(rollChance);
                        newHandler.SetFun(fun);
                        switch (i)
                        {
                            case 0:
                                newHandler.SetColor("red");
                                break;
                            case 1:
                                newHandler.SetColor("blue");
                                break;
                            case 2:
                                newHandler.SetColor("yellow");
                                break;
                            case 3:
                                newHandler.SetColor("green");
                                break;
                        } // switch
                        magicHandlers.Add(newHandler);
                    } // if
                    else
                    {
                        magicHandlers[i].SetMaster(PlayerCharacterMasterController.instances[i].master);
                    } // else
                } // for
            }; // Run.Start
        } // Awake

        // Note that this keeps the original ai, which is kinda funny since it's literally the previous enemy's brain in a new body
        // THIS DOES NOT WORK RIGHT NOW 
        private void Polymorph(CharacterMaster master)
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
                // Debugging Key
                if (Input.GetKeyDown(KeyCode.F2))
                {

                } // if
            } // if
        } // Update
    } // Chaos
} // Chaos
