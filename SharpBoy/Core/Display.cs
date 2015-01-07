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
        private const int width = 160;
        private const int height = 144;
        private const int cyclesPerScanline = 456;

        // Black, Dark Grey, Light Grey, White
        private readonly int[] palette = { 0, 85, 170, 255 };

        public MemoryBankController Memory { get; private set; }
        public Interrupts Interrupts { get; private set; }
        public byte[] ScreenData { get; private set; } 

        public Display(MemoryBankController memory, Interrupts interrupts)
        {
            Memory = memory;
            Interrupts = interrupts;
            ScreenData = new byte[width * height];                        
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

        public void UpdateLCDStatus(int cycleCount, ref int scanlineCycleCounter)
        {
            byte status = Memory[0xFF41];

        }

        private void RenderBackground()
        {

        }

        private void RenderTiles()
        {

        }

        private void RenderSprites()
        {

        }
    }
}
