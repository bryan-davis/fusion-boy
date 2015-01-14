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
        private readonly byte[] Palette = { 255, 192, 96, 0 };

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
            RenderWindow();
            RenderSprites();

            // TODO: Remove this random code
            //Random random = new Random();
            //for (int i = 0; i < ScreenData.Length; i++)
            //{
            //    ScreenData[i] = (byte)random.Next(256);
            //}
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
                // A nice visualization of the scrollX and scrollY values
                // http://imrannazar.com/content/img/jsgb-gpu-bg-scrl.png
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

                int line = Memory[0xFF44];
                for (int i = 0; i < Width; i++)
                {
                    // 8 x 8 pixels in a tile, 32 x 32 tiles on the screen
                    // Calculates which tile map we should retrieve
                    // (((scrollY / 8) * 32) + (scrollX / 8));
                    ushort bgTileNumber = (ushort)Util.Convert2dTo1d((scrollX + i) / 8, (scrollY + line) / 8, 32);

                    // Calculate the index which contains yet another index that points to where the tile data is
                    short bgTileDisplayIndex = Memory[bgTileMapAddress + bgTileNumber];                    

                    // Calculate where the start of the tile data is in memory
                    int tileStartIndex = bgTileDataAddress + ((bgTileDisplayIndex + mapOffset) * TileSize);

                    // Which column of the tile do we need to address in memory?
                    int tileX = (scrollX + i) % 8;
                    // Which line of the tile do we need to address in memory?
                    int tileY = (scrollY + line) % 8;

                    //int tilePixelOffset = Util.Convert2dTo1d(tileX, tileY, 8);
                    int tileLineOffset = tileY * 2;
                    byte byte1 = Memory[tileStartIndex + tileLineOffset];
                    byte byte2 = Memory[tileStartIndex + tileLineOffset + 1];

                    ushort graphicsIndex = (ushort)Util.Convert2dTo1d(i, line, Width);

                    byte colorValue = 0;
                    // Calculate the low bit
                    colorValue |= (byte)((byte1 >> (7 - tileX)) & 1);
                    // Calculate the high bit
                    colorValue |= (byte)(((byte2 >> (7 - tileX)) & 1) << 1);

                    // Least significant bits are swapped compared to the array containing the screen data
                    // e.g. graphicsIndex 0 is equal to bit 7
                    ScreenData[graphicsIndex] = GetColor(colorValue);

                    //for (int j = 7 - tileX; j >= 0; j--)
                    //{
                    //    break;
                    //}
                }
            }
        }

        private void RenderWindow()
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

        // Palettes can be altered by a game
        // http://problemkaputt.de/pandocs.htm#lcdmonochromepalettes
        private byte GetColor(byte colorValue)
        {
            byte palette = Memory[0xFF47];
            byte high = 0, low = 0;

            switch (colorValue)
            {
                case 0:
                    high = 1;
                    low = 0;
                    break;
                case 1:
                    high = 3;
                    low = 2;
                    break;
                case 2:
                    high = 5;
                    low = 4;
                    break;
                case 3:
                    high = 7;
                    low = 6;
                    break;
                default:
                    break;
            }

            int colorIndex = 0;
            colorIndex |= Util.GetBitValue(palette, low);
            colorIndex |= Util.GetBitValue(palette, high) << 1;

            return Palette[colorIndex];
        }
    }
}
