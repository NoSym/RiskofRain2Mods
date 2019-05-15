using System;
using System.Collections.Generic;
using RoR2;

namespace WildMagic
{
    /// <summary>
    /// Basic implementation of timer functionality.
    /// </summary>
    public static class Timer
    {
        private static List<Action> functions = new List<Action>();
        private static List<float> delays = new List<float>();
        private static bool runHook = false;

        /// <summary>
        /// Starts a timer that will execute func after delay in-game seconds.
        /// </summary>
        /// <param name="func">Method to be executed</param>
        /// <param name="delay">Duration, in seconds, to wait before executing func</param>
        public static void SetTimer(Action func, int delay)
        {
            // Hook into run methods
            if (!runHook)
            {
                On.RoR2.Run.Update += (orig, self) =>
                {
                    orig(self);
                    TickTimers();
                }; // Update

                On.RoR2.Run.Start += (orig, self) =>
                {
                    orig(self);
                    HardReset();
                }; // Start

                runHook = true;
            } // if

            if (delay < 0)
                delay = 0;

            functions.Add(func);
            delays.Add(Run.instance.fixedTime + delay);
        } // SetTimer

        // Square 1
        private static void HardReset()
        {
            functions = new List<Action>();
            delays = new List<float>();
        } // CancelTimers

        // Less of a tick more of a check
        private static void TickTimers()
        {
            for (int i = 0; i < delays.Count; i++)
            {
                if (Run.instance.fixedTime >= delays[i])
                {
                    functions[i]();
                    delays.RemoveAt(i);
                    functions.RemoveAt(i);
                    i--;
                } // if
            } // for
        } // TickTimers
    } // Timer class
} // WildMagic namespace
