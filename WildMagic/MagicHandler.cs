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

        // Timers
        private float ghostTimer = -1;

        /// <summary>
        /// Creates a new wild magic controller for the specified character master.
        /// </summary>
        /// <param name="magicMaster">Owner of wild magic effects.</param>
        public MagicHandler(CharacterMaster magicMaster)
        {
            master = magicMaster;
            On.RoR2.Run.Update += (orig, self) =>
            {
                orig(self);
                UpdateTimers();
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

        private void ResolveTimers()
        {
            if (ghostTimer == 0)
            {
                master.inventory.RemoveItem(ItemIndex.Ghost, 1);
                ghostTimer = -1;
            } // ghostTimer
        } // Resolve Timers

        private void TickTimers()
        {
            if (ghostTimer > 0)
                ghostTimer--;
        } // TickTimers

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

        // Catch em all
        private void GoGhost()
        {
            if (ghostTimer == -1)
            {
                master.inventory.GiveItem(ItemIndex.Ghost, 1);
                ghostTimer = 600; // 10 seconds
            } // if
        } // goGhost

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
    } // MagicHandler Class
} // Wildmagic Namespace
