/*
 * Copyright 2014 Bryan Davis
 * Licensed under GPLv2
 * Refer to the license.txt file included
 */

using System;
using System.Windows.Input;

namespace SharpBoy.Emulator
{
    public delegate void RenderEventHandler(byte[] data);

    interface IEmulator
    {        
        event RenderEventHandler RenderHandler;

        string CurrentGame { get; }
        int RenderHeight { get; }
        int RenderWidth { get; }
        int FrameRate { get; }
        bool Stop { get; set; }

        void Run();
        void LoadRom(string filename);
        void KeyUp(Key key);
        void KeyDown(Key key);
        void LoadState(string saveStateFile);
        void SaveState();
    }
}
