using SharpBoy.Core;
using System.Linq;
using System.Text;

namespace SharpBoy.Cartridge
{
    // Based on http://problemkaputt.de/pandocs.htm#thecartridgeheader
    public class CartridgeInfo
    {
        private Memory memory;

        public string Title { get; private set; }
        public string ManufacturerCode { get; private set; }
        public byte CGBFlag { get; private set; }
        public string NewLicenseeCode { get; private set; }
        public byte SGBFlag { get; private set; }
        public CartType CartType { get; private set; }
        public int ROMSize { get; private set; }
        public int RAMSize { get; private set; }
        public byte DestinationCode { get; private set; }
        public byte OldLicenseeCode { get; private set; }
        public byte MaskROMVersion { get; private set; }
        public byte HeaderChecksum { get; private set; }
        public ushort GlobalChecksum { get; private set; }

        public CartridgeInfo(Memory memory)
        {
            this.memory = memory;
            Initialize();
        }

        private void Initialize()
        {
            Title = ReadTitle();
            ManufacturerCode = ReadManufacturerCode();
            CGBFlag = memory[0x143];
            NewLicenseeCode = ReadNewLicenseeCode();
            SGBFlag = memory[0x146];
            CartType = ReadCartType();
            ROMSize = ReadRomSize();
            RAMSize = ReadRamSize();
            DestinationCode = memory[0x14A];
            OldLicenseeCode = memory[0x14B];
            MaskROMVersion = memory[0x14C];
            HeaderChecksum = memory[0x14D];
            GlobalChecksum = (ushort)((memory[0x14E] << 8) | memory[0x14F]);
        }

        private string ReadTitle()
        {
            var title = memory.Data.Skip(0x134).Take(16).ToArray();
            return Encoding.UTF8.GetString(title);
        }

        private string ReadManufacturerCode()
        {
            var manufacturer = memory.Data.Skip(0x13F).Take(2).ToArray();
            return Encoding.UTF8.GetString(manufacturer);
        }

        private string ReadNewLicenseeCode()
        {
            var licenseCode = memory.Data.Skip(0x144).Take(2).ToArray();
            return Encoding.UTF8.GetString(licenseCode);
        }

        private CartType ReadCartType()
        {
            CartType type;
            byte value = memory[0x147];
            switch (value)
            {
                case 0x00:
                    type = CartType.RomOnly; break;
                case 0x01:
                case 0x02:
                case 0x03:
                    type = CartType.MBC1; break;
                case 0x05:
                case 0x06:
                    type = CartType.MBC2; break;
                case 0x08:
                case 0x09:
                    type = CartType.RomOnly; break;
                case 0x0F:
                case 0x10:
                case 0x11:
                case 0x12:
                case 0x13:
                    type = CartType.MBC3; break;
                case 0x15:
                case 0x16:
                case 0x17:
                    type = CartType.MBC4; break;
                case 0x19:
                case 0x1A:
                case 0x1B:
                case 0x1C:
                case 0x1D:
                case 0x1E:
                    type = CartType.MBC5; break;
                case 0xFC:
                    type = CartType.PocketCamera; break;
                case 0xFD:
                    type = CartType.BandaiTama5; break;
                case 0xFE:
                    type = CartType.HuC3; break;
                case 0xFF:
                    type = CartType.HuC1; break;
                default:
                    type = CartType.Unknown; break;
            }

            return type;
        }

        private int ReadRomSize()
        {
            int romSize = 0;
            const int BankSize = 16384;   // 16K

            byte value = memory[0x148];
            switch (value)
            {
                case 0x00:
                    romSize = 2 * BankSize; break;   // 32K
                case 0x01:
                    romSize = 4 * BankSize; break;   // 64K
                case 0x02:
                    romSize = 8 * BankSize; break;   // 128K
                case 0x03:
                    romSize = 16 * BankSize; break;  // 256K
                case 0x04:
                    romSize = 32 * BankSize; break;  // 512K
                case 0x05:
                    romSize = 64 * BankSize; break;  // 1M
                case 0x06:
                    romSize = 128 * BankSize; break; // 2M
                case 0x52:
                    romSize = 72 * BankSize; break;  // 1.1M
                case 0x53:
                    romSize = 80 * BankSize; break;  // 1.2M
                case 0x54:
                    romSize = 96 * BankSize; break;  // 1.5M
                default:
                    romSize = 0; break;
            }

            return romSize;
        }

        private int ReadRamSize()
        {
            int ramSize = 0;
            const int ByteSize = 1024;   // 1K

            byte value = memory[0x149];
            switch (value)
            {
                case 0x00:
                    ramSize = 0; break;
                case 0x01:
                    ramSize = 2 * ByteSize; break;
                case 0x02:
                    ramSize = 8 * ByteSize; break; 
                case 0x03:
                    ramSize = 32 * ByteSize; break;
                default:
                    ramSize = 0; break;
            }

            return ramSize;
        }
    }
}
