/*
 * Copyright 2014 Bryan Davis
 * Licensed under GPLv2
 * Refer to the license.txt file included
 */

using FusionBoy.Cartridge;
using System;

namespace FusionBoy.Core
{
    public class Display
    {
        private const int Width = 160;
        private const int Height = 144;
        private const int Mode0Bounds = 204;
        private const int Mode2Bounds = 80;
        private const int Mode3Bounds = 172;
        private const int TileSize = 16;    // 16 bytes per tile
        private const int MaxScrollAmount = 256;

        private struct Sprite
        {
            public int x;
            public int y;
            public int tileIndex;
            public byte attributes;
        }

        // Black, Dark Grey, Light Grey, White
        private readonly byte[] Palette = { 255, 192, 96, 0 };

        public MemoryBankController Memory { get; private set; }
        public byte[] ScreenData { get; private set; }

        public Display(MemoryBankController memory)
        {
            Memory = memory;
            ScreenData = new byte[Width * Height];
        }

        public void RenderScanline()
        {
            byte currentLine = Memory[Util.ScanlineAddress];

            if (currentLine < Height)
            {
                RenderBackground(currentLine);
                RenderWindow(currentLine);
                RenderSprites(currentLine);
            }
            else if (currentLine == Height)
            {
                Util.SetBits(Memory, Util.InterruptFlagAddress, (byte)Interrupts.vBlank);
            }
            else if (currentLine > 153)
            {
                Memory[Util.ScanlineAddress] = 0;
                return;
            }

            Memory.IncrementLcdScanline();
        }

        // http://problemkaputt.de/pandocs.htm#lcdstatusregister
        public void UpdateLcdStatus(int scanlineCycleCounter)
        {
            bool interruptRequested = false;
            int currentLine = Memory[Util.ScanlineAddress];

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

            if (interruptRequested)
            {
                // Request LCD Stat interrupt
                Util.SetBits(Memory, Util.InterruptFlagAddress, (byte)Interrupts.lcdStat);
            }

            UpdateCoincidence();
        }

        private void UpdateCoincidence()
        {
            byte lyRegister = Memory[Util.ScanlineAddress];
            byte lycRegister = Memory[0xFF45];
            if (lyRegister == lycRegister)
            {
                Util.SetBits(Memory, Util.LcdStatAddress, 2);
                if (Util.IsBitSet(Memory, Util.LcdStatAddress, 6))
                {
                    // Request LCD Stat interrupt
                    Util.SetBits(Memory, Util.InterruptFlagAddress, (byte)Interrupts.lcdStat);
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
                int y = (scrollY + line) % MaxScrollAmount;

                RenderComponent(line, scrollX, y, 3, false);
            }
        }

        private void RenderWindow(int line)
        {
            // Is the window display enabled?
            if (Util.IsBitSet(Memory, Util.LcdControlAddress, 5))
            {
                byte windowX = (byte)(Memory[Util.WindowXAddress] - 7);
                byte windowY = (byte)(Memory[Util.WindowYAddress]);
                int y = line - windowY;

                // Window rendering may be further down the screen than the
                // scanline we're rendering.
                if (windowY > line)
                    return;

                RenderComponent(line, windowX, y, 6, true);
            }
        }

        private void RenderComponent(int line, byte xPosition, int yPosition, byte bit, bool renderingWindow)
        {
            ushort tileDataStart =
                (Util.IsBitSet(Memory, Util.LcdControlAddress, 4)) ? (ushort)0x8000 : (ushort)0x8800;
            ushort tileMapStart =
                (Util.IsBitSet(Memory, Util.LcdControlAddress, bit)) ? (ushort)0x9C00 : (ushort)0x9800;

            // Tiles are mapped with signed bytes (-128, 127), when the data
            // address is 0x8800, so they need to be offset by 128.
            int mapOffset = (tileDataStart == 0x8800) ? 128 : 0;

            for (int i = 0; i < Width; i++)
            {
                int x;
                if (renderingWindow)
                    x = (i > xPosition) ? i - xPosition : xPosition + i;
                else
                    x = (xPosition + i) % MaxScrollAmount;

                // 8 x 8 pixels in a tile, 32 x 32 tiles on the screen.
                // Calculates which tile map we should retrieve.
                int tileNumber = Util.Convert2dTo1d(x / 8, yPosition / 8, 32);

                // Calculate the index which contains yet another index 
                // that points to where the tile data is.
                short tileDisplayIndex = Memory[tileMapStart + tileNumber];
                // If the map offset is 128, then the index is a signed number.
                if (mapOffset == 128)
                    tileDisplayIndex = (sbyte)tileDisplayIndex;

                // Calculate where the start of the tile data is in memory.
                int tileStartAddress = tileDataStart + ((tileDisplayIndex + mapOffset) * TileSize);

                // Which column of the tile do we need to address in memory?
                int tilePixelColumn = x % 8;
                // Which row of the tile do we need to address in memory?
                int tilePixelRow = yPosition % 8;

                int tileLineOffset = tilePixelRow * 2;
                byte byte1 = Memory[tileStartAddress + tileLineOffset];
                byte byte2 = Memory[tileStartAddress + tileLineOffset + 1];

                int graphicsIndex = Util.Convert2dTo1d(i, line, Width);
                byte colorValue = GetColorValue(byte1, byte2, tilePixelColumn);

                // Least significant bits are swapped compared to the array containing the screen data
                // e.g. graphicsIndex 0 is equal to bit 7
                ScreenData[graphicsIndex] = GetColor(colorValue, 0xFF47);
            }
        }

        // http://problemkaputt.de/pandocs.htm#vramspriteattributetableoam
        private void RenderSprites(int line)
        {
            // Is the sprite display enabled?
            if (Util.IsBitSet(Memory, Util.LcdControlAddress, 1))
            {
                bool mode8x16 = Util.IsBitSet(Memory, Util.LcdControlAddress, 2);
                int spriteHeight = mode8x16 ? 16 : 8;

                Sprite sprite;
                // Run through all the sprites in the Sprite Attribute Table backwards,
                // since the first sprites have higher priority for display.
                for (int i = 156; i >= 0; i -= 4)
                {
                    sprite.y = Memory[Util.OamAddress + i] - 16;
                    sprite.x = Memory[Util.OamAddress + i + 1] - 8;

                    if (SpriteInScanlineRange(line, sprite.y, spriteHeight))
                    {
                        sprite.tileIndex = Memory[Util.OamAddress + i + 2];
                        if (mode8x16)
                            sprite.tileIndex &= 0xFE;

                        sprite.attributes = Memory[Util.OamAddress + i + 3];
                        RenderSprite(line, spriteHeight, sprite);
                    }
                }
            }
        }

        private void RenderSprite(int currentLine, int spriteHeight, Sprite sprite)
        {
            int tilePixelRow = currentLine - sprite.y;
            if (Util.IsBitSet(sprite.attributes, 6))   // Y is flipped
            {
                // If we were originally going to draw row 3 of the tile and the tile
                // is 8 pixels high, then we'd be drawing row 5 (0-based indexing) 
                // (Math.Abs(2 - 7) if the tile is supposed to be flipped. 
                tilePixelRow = Math.Abs(tilePixelRow - (spriteHeight - 1));
            }

            int tileLineOffset = tilePixelRow * 2;
            int tileAddress = 0x8000 + (sprite.tileIndex * TileSize);
            byte byte1 = Memory[tileAddress + tileLineOffset];
            byte byte2 = Memory[tileAddress + tileLineOffset + 1];
            int paletteAddress = Util.IsBitSet(sprite.attributes, 4) ? 0xFF49 : 0xFF48;

            for (int column = 0; column < 8; column++)
            {
                if (SpritePixelInScreenBounds(sprite.x + column, currentLine))
                {
                    int tilePixelColumn = column;
                    if (Util.IsBitSet(sprite.attributes, 5))   // X is flipped
                        tilePixelColumn = Math.Abs(tilePixelColumn - 7);

                    byte colorValue = GetColorValue(byte1, byte2, tilePixelColumn);
                    // Sprite data 0 is transparent.
                    if (colorValue == 0)
                        continue;
                    byte color = GetColor(colorValue, paletteAddress);

                    int graphicsIndex = Util.Convert2dTo1d(sprite.x + column, currentLine, Width);
                    // Sprite is behind the background, unless the background pixel is white.
                    if (Util.IsBitSet(sprite.attributes, 7) && ScreenData[graphicsIndex] != Palette[0])
                        continue;

                    ScreenData[graphicsIndex] = color;
                }
            }
        }

        // Checks to see if a sprite is in range of the current scanline.
        private bool SpriteInScanlineRange(int line, int spriteYPosition, int spriteHeight)
        {
            return (spriteYPosition <= line && line < spriteYPosition + spriteHeight);
        }

        private bool SpritePixelInScreenBounds(int x, int y)
        {
            return ((0 <= x && x < Width) && (0 <= y && y < Height));
        }

        // Calculates which color index to use in the color palette for a 
        // particular pixel.
        private byte GetColorValue(byte byte1, byte byte2, int tilePixelColumn)
        {
            byte colorValue = 0;
            // Calculate the low bit
            colorValue |= (byte)((byte1 >> (7 - tilePixelColumn)) & 1);
            // Calculate the high bit
            colorValue |= (byte)(((byte2 >> (7 - tilePixelColumn)) & 1) << 1);

            return colorValue;
        }

        // Palettes can be altered by a game
        // http://problemkaputt.de/pandocs.htm#lcdmonochromepalettes
        private byte GetColor(byte colorValue, int address)
        {
            byte palette = Memory[address];
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
