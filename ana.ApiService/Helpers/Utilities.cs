﻿using System;
using System.Threading;

    internal static class Utilities
    {
        internal static int GenerateRandomInt()
        {
            Thread.Sleep(20); // Ensure that the seed changes.
            var rand = new Random();
            return rand.Next(1, int.MaxValue);
        }
    }

