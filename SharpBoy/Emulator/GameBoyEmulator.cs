/*
 * Copyright 2014 Bryan Davis
 * Licensed under GPLv2
 * Refer to the license.txt file included
 */

using SharpBoy.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SharpBoy.Emulator
{
    public class GameBoyEmulator : ViewModelBase, IEmulator
    {
        private Stopwatch frameRateTimer;
        private int frameCount = 0;
        HashSet<int> missingCodes = new HashSet<int>();
        
        public event RenderEventHandler RenderHandler;

        private int cyclesPerFrame;
        private double microsecondsPerFrame;
        private string saveStateDirectory;
        private string currentGame;
        private CPU cpu;
        private int frameRate;

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

        public int FrameRate
        {
            get { return frameRate; }
            private set
            {
                frameRate = value;
                RaisePropertyChanged("FrameRate");
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

        public void Run()
        {
            Stopwatch frameRateLimiter = Stopwatch.StartNew();
            frameCount = 0;
            double microsecondsPerTick = (1000.0 * 1000.0) / Stopwatch.Frequency;
            frameRateTimer = Stopwatch.StartNew();           

            Stop = false;
            while (!Stop)
            {
                frameRateLimiter.Restart();
                UpdateFrame();
                Render();

                double elapsedTime;
                do
                {
                    elapsedTime = frameRateLimiter.ElapsedTicks * microsecondsPerTick;
                } while (elapsedTime < microsecondsPerFrame);
            }
        }

        public void LoadRom(string filename)
        {
            CurrentGame = Path.GetFileNameWithoutExtension(filename);
            cpu.LoadRom(filename);
        }

        public void KeyUp(System.Windows.Input.Key key)
        {
            
        }

        public void KeyDown(System.Windows.Input.Key key)
        {
            
        }

        public void LoadState(string saveStateFile)
        {
            
        }

        public void SaveState()
        {
            
        }

        private void UpdateFrame()
        {
            int currentCycles = 0;
            int opCodeCount = 0;

            while (currentCycles < cyclesPerFrame)
            {
                opCodeCount++;
                int opCodeLookup = ExecuteOpCode();

                int cycles;
                if (cpu.CycleMap.TryGetValue(opCodeLookup, out cycles))
                {
                    currentCycles += cycles;
                }
                else
                {
                    missingCodes.Add(opCodeLookup);
                }

                cpu.HandleTimers(cycles);
                cpu.UpdateGraphics(cycles);
                cpu.ProcessInterrupts();                 
            }            
        }

        private int ExecuteOpCode()
        {
            if (cpu.Halted)
            {
                // Return the HALT op code
                return 0x76;
            }

            byte opCode = cpu.ReadNextValue();
            int opCodeLookup;

            if (opCode != 0xCB)
            {
                opCodeLookup = opCode;
                cpu.ExecuteOpCode(opCode);
            }
            else
            {
                ushort extendedOpCode = (ushort)(opCode << 8);
                extendedOpCode |= cpu.ReadNextValue();
                cpu.ExecuteExtendedOpCode(extendedOpCode);
                opCodeLookup = extendedOpCode;
            }

            return opCodeLookup;
        }

        private void Render()
        {
            if (RenderHandler != null)
            {
                RenderHandler(cpu.Display.ScreenData);
            }

            frameCount++;
            if (frameRateTimer.ElapsedMilliseconds >= 1000)
            {
                FrameRate = frameCount;
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
            double framesPerSecond = Properties.Settings.Default.targetFrameRate;
            double millsecondsPerFrame = 1000.0 / framesPerSecond;
            microsecondsPerFrame = 1000.0 * millsecondsPerFrame;

            int cyclesPerSecond = Properties.Settings.Default.cyclesPerSecond;
            cyclesPerFrame = (int)(cyclesPerSecond / framesPerSecond);
        }
    }
}
