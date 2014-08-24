using SharpBoy.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SharpBoy.Emulator
{
    public class GameBoyEmulator : ViewModelBase, IEmulator
    {
        private Stopwatch frameRateTimer = new Stopwatch();
        private int frameCount = 0;
        HashSet<int> missingCodes = new HashSet<int>();
        
        public event RenderEventHandler RenderHandler;

        private int cyclesPerFrame;
        private double microsecondsPerFrame;
        private string saveStateDirectory;
        private string currentGame;
        private CPU cpu;

        public GameBoyEmulator()
        {
            saveStateDirectory = Path.Combine(Directory.GetCurrentDirectory(), "save");
            CalculateSpeedLimit();
            cpu = new CPU();
        }

        public string CurrentGame
        {
            get { return currentGame; }
            private set
            {
                currentGame = value;
                RaisePropertyChanged("CurrentGame");
            }
        }

        public int RenderHeight 
        { 
            get { return 144; } 
        }

        public int RenderWidth
        {
            get { return 160; }
        }

        public bool Stop { get; set; }

        // TODO: This doesn't limit frames as expected.
        public void Run()
        {
            Stopwatch frameRateLimiter = new Stopwatch();
            frameRateLimiter.Start();
            double microsecondsPerTick = (1000.0 * 1000.0) / Stopwatch.Frequency;

            frameRateTimer.Start();            
            Stop = false;
            while (!Stop)
            {
                double elapsedTime = frameRateLimiter.ElapsedTicks * microsecondsPerTick;
                if (elapsedTime >= microsecondsPerFrame)
                {                    
                    UpdateFrame();
                    Render();
                    frameRateLimiter.Restart();
                }
            }
        }

        public void LoadRom(string filename)
        {
            CurrentGame = Path.GetFileNameWithoutExtension(filename);
            cpu.LoadRom(filename);
        }

        public void KeyUp(System.Windows.Input.Key key)
        {
            throw new NotImplementedException();
        }

        public void KeyDown(System.Windows.Input.Key key)
        {
            throw new NotImplementedException();
        }

        public void LoadState(string saveStateFile)
        {
            throw new NotImplementedException();
        }

        public void SaveState()
        {
            throw new NotImplementedException();
        }

        private void UpdateFrame()
        {
            int currentCycles = 0;

            while (currentCycles < cyclesPerFrame)
            {
                byte opCode = cpu.ReadNextValue();
                int lookup;
                if (opCode != 0xCB)
                {
                    lookup = opCode;
                    cpu.ExecuteOpCode(opCode);
                }
                else
                {
                    ushort extendedOpCode = (ushort)(opCode << 8);
                    extendedOpCode |= cpu.ReadNextValue();
                    cpu.ExecuteExtendedOpCode(extendedOpCode);
                    lookup = extendedOpCode;
                }

                int cycles;
                if (cpu.CycleMap.TryGetValue(lookup, out cycles))
                {
                    currentCycles += cycles;
                }
                else
                {
                    missingCodes.Add(lookup);
                }
            }            
        }

        private void Render()
        {
            frameCount++;
            if (frameRateTimer.Elapsed.Seconds >= 1)
            {
                Debug.WriteLine("{0} FPS", frameCount);
                frameCount = 0;
                frameRateTimer.Restart();
            }
        }

        private void CalculateSpeedLimit()
        {
            /* We need to get a higher precision for time slices. Unfortunately,
             * millisecond precision isn't good enough, as 16 ms slices equate to
             * ~62 updates per second, and 17 ms equate to ~58.
            */
            int framesPerSecond = Properties.Settings.Default.targetFrameRate;
            double millsecondsPerFrame = 1000.0 / framesPerSecond;
            microsecondsPerFrame = 1000.0 * millsecondsPerFrame;

            int cyclesPerSecond = Properties.Settings.Default.cyclesPerSecond;
            cyclesPerFrame = cyclesPerSecond / framesPerSecond;
        }
    }
}
