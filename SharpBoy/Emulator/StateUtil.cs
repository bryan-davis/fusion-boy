/*
 * Copyright 2014 Bryan Davis
 * Licensed under GPLv2
 * Refer to the license.txt file included
 */

using SharpBoy.Core;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace SharpBoy.Emulator
{
    static class StateUtil
    {
        // Save state names will follow the pattern of ROM.sav#,
        // where # represents the save state number.
        // Examples: BRIX.sav1, BRIX.sav2, etc.
        public static string GenerateSaveStateName(string saveDirectory, string currentGame)
        {
            string saveState = string.Empty;

            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
                saveState = string.Format("{0}.sav1", currentGame);
            }
            else
            {
                string[] existingSaves = Directory.GetFiles(saveDirectory,
                    string.Format("{0}.sav*", currentGame));
                saveState = string.Format("{0}.sav{1}", currentGame, existingSaves.Length + 1);
            }

            return Path.Combine(saveDirectory, saveState);
        }

        public static void SaveState(string saveStateFile, CPU cpu)
        {
            using (Stream stream = File.OpenWrite(saveStateFile))
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, cpu);
            }
        }

        public static CPU LoadState(string saveStateFile)
        {
            CPU cpu;
            using (Stream stream = File.OpenRead(saveStateFile))
            {
                IFormatter formatter = new BinaryFormatter();
                cpu = (CPU)formatter.Deserialize(stream);
            }
            return cpu;
        }
    }
}
