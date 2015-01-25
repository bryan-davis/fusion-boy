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
        private const int TileSize = 16;    // 16 bytes per tile
        private const int ScrollMaxSize = 256;

        // Black, Dark Grey, Light Grey, White
        private readonly byte[] Palette = { 255, 192, 96, 0 };

        public MemoryBankController Memory { get; private set; }
        public byte[] ScreenData { get; private set; } 

        public Display(MemoryBankController memory)
        {
            Memory = memory;            
            ScreenData = new byte[Width * Height];
        }

        public void Render()
        {
            byte currentLine = Memory[Util.ScanlineAddress];

            if (currentLine == Height)
            {
                Util.SetBits(Memory, Util.InterruptFlagAddress, (byte)Interrupts.vBlank);
            }

            if (currentLine > 153)
            {
                Memory[Util.ScanlineAddress] = 0;
            }

            if (currentLine < Height)
            {
                RenderBackground(currentLine);
                RenderWindow(currentLine);
                RenderSprites(currentLine);
            }
            
            Memory.IncrementLCDScanline();
        }

        // http://problemkaputt.de/pandocs.htm#lcdstatusregister
        public void UpdateLCDStatus(int scanlineCycleCounter)
        {
            int previousMode = Memory[Util.LcdStatAddress] & 3;
            bool interruptRequested = false;
            int currentLine = Memory[0xFF44];

            // Mode 1 - V-Blank
            if (currentLine >= Height)
            {
                Util.SetBits(Memory, Util.LcdStatAddress, 0);
                Util.ClearBits(Memory, Util.LcdStatAddress, 1);
                interruptRequested = Util.IsBitSet(Memory, Util.LcdStatAddress, 4);                
            }
            else
            {
                // Mode 0 - H-Blank
                if (scanlineCycleCounter <= Mode0Bounds)
                {
                    Util.ClearBits(Memory, Util.LcdStatAddress, 0, 1);
                    interruptRequested = Util.IsBitSet(Memory, Util.LcdStatAddress, 3);
                }
                // Mode 2 - Searching OAM RAM
                else if (scanlineCycleCounter <= (Mode0Bounds + Mode2Bounds))
                {
                    Util.SetBits(Memory, Util.LcdStatAddress, 1);
                    Util.ClearBits(Memory, Util.LcdStatAddress, 0);
                    interruptRequested = Util.IsBitSet(Memory, Util.LcdStatAddress, 5);
                }
                // Mode 3 - Transfering Data to LCD Driver
                else
                {
                    Util.SetBits(Memory, Util.LcdStatAddress, 0, 1);             
                }
            }

            int currentMode = Memory[Util.LcdStatAddress] & 3;
            if (currentMode != previousMode && interruptRequested)
            {
                // Request LCD Stat interrupt
                Util.SetBits(Memory, Util.InterruptFlagAddress, (byte)Interrupts.lcdStat);
            }

            UpdateCoincidence();
        }

        private void UpdateCoincidence()
        {
            byte lyRegister = Memory[0xFF44];
            byte lycRegister = Memory[0xFF45];
            if (lyRegister == lycRegister)
            {
                Util.SetBits(Memory, Util.LcdStatAddress, 2);
                if (Util.IsBitSet(Memory, Util.LcdStatAddress, 6))
                {
                    // Request LCD Stat interrupt
                    Util.SetBits(Memory, Util.InterruptFlagAddress, (byte)Interrupts.lcdStat);
                    Util.SetBits(Memory, 0xFF0F, (byte)Interrupts.lcdStat);
                }
            }
            else
            {
                Util.ClearBits(Memory, Util.LcdStatAddress, 2);
            }
        }        

        private void ClearScreen()
        {
            for (int i = 0; i < ScreenData.Length; i++)
            {
                ScreenData[i] = 0;
            }
        }

        private void RenderBackground(int line)
        {
            // Is the background display enabled?
            if (Util.IsBitSet(Memory, Util.LcdControlAddress, 0))
            {
                // A nice visualization of the scrollX and scrollY values
                // http://imrannazar.com/content/img/jsgb-gpu-bg-scrl.png
                byte scrollX = (byte)(Memory[Util.ScrollXAddress]);
                byte scrollY = (byte)(Memory[Util.ScrollYAddress]);
                // Tile data start location
                ushort tileDataAddress = 
                    (Util.IsBitSet(Memory, Util.LcdControlAddress, 4)) ? (ushort)0x8000 : (ushort)0x8800;
                // Tile map start location
                ushort tileMapAddress = 
                    (Util.IsBitSet(Memory, Util.LcdControlAddress, 3)) ? (ushort)0x9C00 : (ushort)0x9800;                
                
                // Tiles are mapped with signed bytes (-128, 127), when the data
                // address is 0x8800, so they need to be offset by 128.
                int mapOffset = (tileDataAddress == 0x8800) ? 128 : 0;

                int y = (scrollY + line) % ScrollMaxSize;
                for (int i = 0; i < Width; i++)
                {
                    int x = (scrollX + i) % ScrollMaxSize;
                    // 8 x 8 pixels in a tile, 32 x 32 tiles on the screen
                    // Calculates which tile map we should retrieve
                    ushort tileNumber = (ushort)Util.Convert2dTo1d(x / 8, y / 8, 32);

                    // Calculate the index which contains yet another index that points to where the tile data is
                    short tileDisplayIndex = Memory[tileMapAddress + tileNumber];
                    // If the map offset is 128, then the index is a signed number
                    if (mapOffset == 128)
                        tileDisplayIndex = (sbyte)tileDisplayIndex;

                    // Calculate where the start of the tile data is in memory
                    int tileStartIndex = tileDataAddress + ((tileDisplayIndex + mapOffset) * TileSize);

                    // Which column of the tile do we need to address in memory?
                    int tileX = (scrollX + i) % 8;
                    // Which line of the tile do we need to address in memory?
                    int tileY = (scrollY + line) % 8;

                    //int tilePixelOffset = Util.Convert2dTo1d(tileX, tileY, 8);
                    int tileLineOffset = tileY * 2;
                    byte byte1 = Memory[tileStartIndex + tileLineOffset];
                    byte byte2 = Memory[tileStartIndex + tileLineOffset + 1];

                    ushort graphicsIndex = (ushort)Util.Convert2dTo1d(i, line, Width);

                    // Calculate the low bit
                    byte colorValue = (byte)((byte1 >> (7 - tileX)) & 1);
                    // Calculate the high bit
                    colorValue |= (byte)(((byte2 >> (7 - tileX)) & 1) << 1);

                    // Least significant bits are swapped compared to the array containing the screen data
                    // e.g. graphicsIndex 0 is equal to bit 7
                    ScreenData[graphicsIndex] = GetColor(colorValue);
                }
            }
        }

        private void RenderWindow(int line)
        {
            // Is the window display enabled?
            if (Util.IsBitSet(Memory, Util.LcdControlAddress, 5))
            {                
                byte windowX = (byte)(Memory[Util.WindowXAddress] - 7);
                byte windowY = (byte)(Memory[Util.WindowYAddress]);

                if (windowY > line)
                    return;

                int y = line - windowY;

                // Tile data start location
                ushort tileDataAddress =
                    (Util.IsBitSet(Memory, Util.LcdControlAddress, 4)) ? (ushort)0x8000 : (ushort)0x8800;
                // Tile map start location
                ushort tileMapAddress =
                    (Util.IsBitSet(Memory, Util.LcdControlAddress, 6)) ? (ushort)0x9C00 : (ushort)0x9800;
                // 16 bytes per tile
                const int TileSize = 16;

                int mapOffset = 0;
                // Tiles are mapped with signed bytes (-128, 127), so they need to be offset by 128
                if (tileDataAddress == 0x8800)
                    mapOffset = 128;

                for (int i = 0; i < Width; i++)
                {
                    int x = windowX + i;
                    if (i > windowX)
                    {
                        x = i - windowX;
                    }

                    // 8 x 8 pixels in a tile, 32 x 32 tiles on the screen
                    // Calculates which tile map we should retrieve
                    // (((windowY / 8) * 32) + (windowX / 8));
                    ushort tileNumber = (ushort)Util.Convert2dTo1d((x) / 8, (y) / 8, 32);

                    // Calculate the index which contains yet another index that points to where the tile data is
                    short tileDisplayIndex = Memory[tileMapAddress + tileNumber];
                    // If the map offset is 128, then the index is a signed number
                    if (mapOffset == 128)
                        tileDisplayIndex = (sbyte)tileDisplayIndex;

                    // Calculate where the start of the tile data is in memory
                    int tileStartIndex = tileDataAddress + ((tileDisplayIndex + mapOffset) * TileSize);

                    // Which column of the tile do we need to address in memory?
                    int tileX = (x) % 8;
                    // Which line of the tile do we need to address in memory?
                    int tileY = (y) % 8;

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
                }
            }
        }

        // http://problemkaputt.de/pandocs.htm#vramspriteattributetableoam
        private void RenderSprites(int line)
        {
            // Is the sprite display enabled?
            if (Util.IsBitSet(Memory, Util.LcdControlAddress, 1))
            {
                bool mode8x16 = Util.IsBitSet(Memory, Util.LcdControlAddress, 2);
                const int oamAddress = 0xFE00;

                for (int i = 0; i < 160; i += 4)
                {
                    byte yPosition = Memory[oamAddress + i];
                    byte xPosition = Memory[oamAddress + i + 1];
                }
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
