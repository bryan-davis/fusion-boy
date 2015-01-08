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
        private const int mode0Bounds = 204;
        private const int mode2Bounds = 80;
        private const int mode3Bounds = 172;
        private const ushort statusAddress = 0xFF41;

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

        // http://problemkaputt.de/pandocs.htm#lcdstatusregister
        public void UpdateLCDStatus(int scanlineCycleCounter)
        {
            int previousMode = Memory[statusAddress] & 3;
            bool interruptRequested = false;
            int currentLine = Memory[0xFF44];

            // Mode 1 - V-Blank
            if (currentLine >= height)
            {
                Util.SetBits(Memory, statusAddress, 0);
                Util.ClearBits(Memory, statusAddress, 1);
                interruptRequested = Util.IsBitSet(Memory, statusAddress, 4);
            }
            else
            {
                // Mode 0 - H-Blank
                if (scanlineCycleCounter <= mode0Bounds)
                {
                    Util.ClearBits(Memory, statusAddress, 0, 1);
                    interruptRequested = Util.IsBitSet(Memory, statusAddress, 3);
                }
                // Mode 2 - Searching OAM RAM
                else if (scanlineCycleCounter <= (mode0Bounds + mode2Bounds))
                {
                    Util.SetBits(Memory, statusAddress, 1);
                    Util.ClearBits(Memory, statusAddress, 0);
                    interruptRequested = Util.IsBitSet(Memory, statusAddress, 5);
                }
                // Mode 3 - Transfering Data to LCD Driver
                else
                {
                    Util.SetBits(Memory, statusAddress, 0, 1);
                }
            }

            int currentMode = Memory[statusAddress] & 3;
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
                Util.SetBits(Memory, statusAddress, 2);
                if (Util.IsBitSet(Memory, statusAddress, 6))
                {
                    Interrupts.LCDStatusRequested = true;
                }
            }
            else
            {
                Util.ClearBits(Memory, statusAddress, 2);
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

        }

        private void RenderTiles()
        {

        }

        private void RenderSprites()
        {

        }
    }
}
