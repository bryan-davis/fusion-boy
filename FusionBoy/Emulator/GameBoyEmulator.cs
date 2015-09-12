/*
 * Copyright 2014 Bryan Davis
 * Licensed under GPLv2
 * Refer to the license.txt file included
 */

using FusionBoy.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace FusionBoy.Emulator
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
                int index = value.IndexOf('(');
                if (index != -1)
                    currentGame = value.Substring(0, index - 1);
                else
                    currentGame = value;
                RaisePropertyChanged(nameof(CurrentGame));
            }
        }

        public int FrameRate
        {
            get { return frameRate; }
            private set
            {
                frameRate = value;
                RaisePropertyChanged(nameof(FrameRate));
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
                Sleep(frameRateLimiter, microsecondsPerTick);
            }
        }

        private void Sleep(Stopwatch frameRateLimiter, double microsecondsPerTick)
        {
            double elapsedTime;
            do
            {
                elapsedTime = frameRateLimiter.ElapsedTicks * microsecondsPerTick;
            } while (elapsedTime < microsecondsPerFrame);
        }

        public void LoadRom(string filename)
        {
            CurrentGame = Path.GetFileNameWithoutExtension(filename);
            cpu.LoadRom(filename);
        }

        public void KeyUp(System.Windows.Input.Key key)
        {
            // TODO: Make input user-configurable
            switch (key)
            {
                case System.Windows.Input.Key.A:
                    cpu.KeyUp(Joypad.Left);
                    break;
                case System.Windows.Input.Key.D:
                    cpu.KeyUp(Joypad.Right);
                    break;
                case System.Windows.Input.Key.Down:
                    cpu.KeyUp(Joypad.Start);
                    break;
                case System.Windows.Input.Key.Left:
                    cpu.KeyUp(Joypad.B);
                    break;
                case System.Windows.Input.Key.Right:
                    cpu.KeyUp(Joypad.A);
                    break;
                case System.Windows.Input.Key.S:
                    cpu.KeyUp(Joypad.Down);
                    break;
                case System.Windows.Input.Key.Up:
                    cpu.KeyUp(Joypad.Select);
                    break;
                case System.Windows.Input.Key.W:
                    cpu.KeyUp(Joypad.Up);
                    break;
                default:
                    break;
            }
        }

        public void KeyDown(System.Windows.Input.Key key)
        {
            // TODO: Make input user-configurable
            switch (key)
            {
                case System.Windows.Input.Key.A:
                    cpu.KeyDown(Joypad.Left);
                    break;
                case System.Windows.Input.Key.D:
                    cpu.KeyDown(Joypad.Right);
                    break;
                case System.Windows.Input.Key.Down:
                    cpu.KeyDown(Joypad.Start);
                    break;
                case System.Windows.Input.Key.Left:
                    cpu.KeyDown(Joypad.B);
                    break;
                case System.Windows.Input.Key.Right:
                    cpu.KeyDown(Joypad.A);
                    break;
                case System.Windows.Input.Key.S:
                    cpu.KeyDown(Joypad.Down);
                    break;
                case System.Windows.Input.Key.Up:
                    cpu.KeyDown(Joypad.Select);
                    break;
                case System.Windows.Input.Key.W:
                    cpu.KeyDown(Joypad.Up);
                    break;
                default:
                    break;
            }
        }

        public void LoadState(string saveStateFile)
        {
            
        }

        public void SaveState()
        {
            
        }

        private void UpdateFrame()
        {
            cpu.CyclesExecuted = Math.Max(0, cpu.CyclesExecuted - cyclesPerFrame);
            int currentCycles = cpu.CyclesExecuted;
            while (cpu.CyclesExecuted < cyclesPerFrame)
            {
                cpu.ExecuteOpCode();
                cpu.ProcessInterrupts();
            }            
        }

        private void Render()
        {
            RenderHandler?.Invoke(cpu.Display.ScreenData);

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
