using SharpBoy.GameBoyCore;
using System;
using System.Diagnostics;
using System.IO;

namespace SharpBoy.GameBoySystem
{
    public class GameBoyEmulator : ViewModelBase, IEmulator
    {
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

        public void Run()
        {
            Stopwatch frameRateLimiter = new Stopwatch();
            frameRateLimiter.Start();
            double microsecondsPerTick = (1000.0 * 1000.0) / Stopwatch.Frequency;

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

        }

        private void Render()
        {

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
