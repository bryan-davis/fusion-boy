/*
 * Copyright 2014 Bryan Davis
 * Licensed under GPLv2
 * Refer to the license.txt file included
 */

using SharpBoy.Cartridge;
using System;

namespace SharpBoy.Core
{
    public class Display
    {
        private const int Width = 160;
        private const int Height = 144;        
        private const int Mode0Bounds = 204;
        private const int Mode2Bounds = 80;
        private const int Mode3Bounds = 172;
        private const ushort ControlAddress = 0xFF40;
        private const ushort StatusAddress = 0xFF41;

        // Black, Dark Grey, Light Grey, White
        private readonly int[] palette = { 0, 96, 192, 255 };

        public MemoryBankController Memory { get; private set; }
        public Interrupts Interrupts { get; private set; }
        public byte[] ScreenData { get; private set; } 

        public Display(MemoryBankController memory, Interrupts interrupts)
        {
            Memory = memory;
            Interrupts = interrupts;
            ScreenData = new byte[Width * Height];                        
        }

        public void Render()
        {
            RenderBackground();
            RenderTiles();
            RenderSprites();

            // TODO: Remove this random code
            Random random = new Random();
            for (int i = 0; i < ScreenData.Length; i++)
            {
                ScreenData[i] = (byte)random.Next(256);
            }
        }

        // http://problemkaputt.de/pandocs.htm#lcdstatusregister
        public void UpdateLCDStatus(int scanlineCycleCounter)
        {
            int previousMode = Memory[StatusAddress] & 3;
            bool interruptRequested = false;
            int currentLine = Memory[0xFF44];

            // Mode 1 - V-Blank
            if (currentLine >= Height)
            {
                Util.SetBits(Memory, StatusAddress, 0);
                Util.ClearBits(Memory, StatusAddress, 1);
                interruptRequested = Util.IsBitSet(Memory, StatusAddress, 4);
                
                Memory.IncrementLCDScanline();
                if (Memory[0xFF44] > 153)
                {
                    Memory[0xFF44] = 0;
                }
            }
            else
            {
                // Mode 0 - H-Blank
                if (scanlineCycleCounter <= Mode0Bounds)
                {
                    Memory.IncrementLCDScanline();
                    Util.ClearBits(Memory, StatusAddress, 0, 1);
                    interruptRequested = Util.IsBitSet(Memory, StatusAddress, 3);
                }
                // Mode 2 - Searching OAM RAM
                else if (scanlineCycleCounter <= (Mode0Bounds + Mode2Bounds))
                {
                    Util.SetBits(Memory, StatusAddress, 1);
                    Util.ClearBits(Memory, StatusAddress, 0);
                    interruptRequested = Util.IsBitSet(Memory, StatusAddress, 5);
                }
                // Mode 3 - Transfering Data to LCD Driver
                else
                {
                    Util.SetBits(Memory, StatusAddress, 0, 1);
                    // TODO: Kick off scanline rendering from here?
                }
            }

            int currentMode = Memory[StatusAddress] & 3;
            if (currentMode != previousMode && interruptRequested)
            {
                Interrupts.LCDStatusRequested = true;
            }

            UpdateCoincidence();
        }

        private void UpdateCoincidence()
        {
            byte lyRegister = Memory[0xFF44];
            byte lycRegister = Memory[0xFF45];
            if (lyRegister == lycRegister)
            {
                Util.SetBits(Memory, StatusAddress, 2);
                if (Util.IsBitSet(Memory, StatusAddress, 6))
                {
                    Interrupts.LCDStatusRequested = true;
                }
            }
            else
            {
                Util.ClearBits(Memory, StatusAddress, 2);
            }
        }        

        private void ClearScreen()
        {
            for (int i = 0; i < ScreenData.Length; i++)
            {
                ScreenData[i] = 0;
            }
        }

        private void RenderBackground()
        {
            // Is the background display enabled?
            if (Util.IsBitSet(Memory, ControlAddress, 0))
            {
                byte scrollX = (byte)(Memory[0xFF42]);
                byte scrollY = (byte)(Memory[0xFF43]);
                // Tile data start location
                ushort bgTileDataAddress = 
                    (Util.IsBitSet(Memory, ControlAddress, 4)) ? (ushort)0x8000 : (ushort)0x8800;
                // Tile map start location
                ushort bgTileMapAddress = 
                    (Util.IsBitSet(Memory, ControlAddress, 3)) ? (ushort)0x9800 : (ushort)0x9C00;
                // 16 bytes per tile
                const int TileSize = 16;
                
                int mapOffset = 0;
                // Tiles are mapped with signed bytes (-128, 127), so they need to be offset by 128
                if (bgTileDataAddress == 0x8800)
                    mapOffset = 128;

                for (int i = 0; i < Width; i++)
                {
                    // 8 x 8 pixels in a tile, 32 x 32 tiles on the screen
                    // Calculates which tile map we should retrieve
                    ushort bgTileNumber = (ushort)( ((scrollY / 8) * 32) + (scrollX / 8) );
                    short bgTileDisplayIndex = Memory[bgTileMapAddress + bgTileNumber];
                    byte byte1 = Memory[bgTileDataAddress + ((bgTileDisplayIndex + mapOffset) * TileSize)];
                    byte byte2 = Memory[bgTileDataAddress + ((bgTileDisplayIndex + mapOffset) * TileSize) + 1];
                }
            }
        }

        private void RenderTiles()
        {
            // Is the window display enabled?
            if (Util.IsBitSet(Memory, ControlAddress, 5))
            {
                
            }
        }

        private void RenderSprites()
        {
            // Is the sprite display enabled?
            if (Util.IsBitSet(Memory, ControlAddress, 1))
            {

            }
        }

        // Converts a 2d screen coordinate to the 1d index
        // that the screen data is actually stored in
        private int Convert2dTo1d(int x, int y)
        {
            return (y * Width) + x;
        }
    }
}
