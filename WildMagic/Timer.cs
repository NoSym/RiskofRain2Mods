using System;
using System.Collections.Generic;
using BepInEx;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.Networking;

namespace WildMagic
{
    /// <summary>
    /// Basic implementation of timer functionality for WildMagic.
    /// </summary>
    public static class Timer
    {
        private static List<Action> functions = new List<Action>();
        private static List<int> delays = new List<int>();
        private static bool runHook = false;

        /// <summary>
        /// Starts a timer that will execute func after delay seconds.
        /// </summary>
        /// <param name="func">Method to be executed</param>
        /// <param name="delay">Duration, in seconds, to wait before executing func</param>
        public static void SetTimer(Action func, int delay)
        {
            // Check for a paired run instance
            if(!runHook)
            {
                On.RoR2.Run.Update += (orig, self) =>
                {
                    orig(self);
                    TickTimers();
                }; // Update
                runHook = true;
            } // if

            if (delay < 0)
                delay = 0;

            functions.Add(func);
            delays.Add(delay * 60);
        } // SetTimer

        /// <summary>
        /// Call on a new run to reset everything.
        /// </summary>
        public static void HardReset()
        {
            functions = new List<Action>();
            delays = new List<int>();
            runHook = false;
    } // CancelTimers

        // Once per frame
        private static void TickTimers()
        {
            for(int i = 0; i < delays.Count; i++)
            {
                if (delays[i] > 0)
                {
                    delays[i]--;
                } // if
                else
                {
                    functions[i]();
                    delays.RemoveAt(i);
                    functions.RemoveAt(i);
                    i--;
                } // else
            } // for
        } // TickTimers
    } // Timer class
} // WildMagic namespace
