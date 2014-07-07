using System;
using System.Windows.Input;

namespace SharpBoy.GameBoySystem
{
    public delegate void RenderEventHandler(byte[] data);

    interface IEmulator
    {        
        event RenderEventHandler RenderHandler;

        string CurrentGame { get; }
        int RenderHeight { get; }
        int RenderWidth { get; }
        bool Stop { get; set; }

        void Run();
        void LoadRom(string filename);
        void KeyUp(Key key);
        void KeyDown(Key key);
        void LoadState(string saveStateFile);
        void SaveState();
    }
}
