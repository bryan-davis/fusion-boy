/*
 * Copyright 2014 Bryan Davis
 * Licensed under GPLv2
 * Refer to the license.txt file included
 */

namespace SharpBoy.Cartridge
{
    // Based off http://problemkaputt.de/pandocs.htm#memorybankcontrollers
    // Doesn't include Game Boy Color cartridge types
    public enum CartType
    {
        RomOnly,  // Represents no memory bank controller
        MBC1,
        MBC2,
        MBC3,
        MBC4,
        MBC5,
        PocketCamera,
        BandaiTama5,
        HuC1,
        HuC3,
        Unknown
    }
}
