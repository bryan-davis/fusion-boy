using SharpBoy.Core;
using System.Linq;
using System.Text;

namespace SharpBoy.Cartridge
{
    // Based on http://problemkaputt.de/pandocs.htm#thecartridgeheader
    public class CartridgeInfo
    {
        private readonly byte[] header;

        public string Title { get; private set; }
        public string ManufacturerCode { get; private set; }
        public byte CGBFlag { get; private set; }
        public string NewLicenseeCode { get; private set; }
        public byte SGBFlag { get; private set; }
        public CartType CartType { get; private set; }
        public int ROMSize { get; private set; }
        public int ROMBankCount { get; private set; }
        public int RAMSize { get; private set; }
        public int RAMBankCount { get; private set; }
        public bool HasBattery { get; private set; }
        public byte DestinationCode { get; private set; }
        public byte OldLicenseeCode { get; private set; }
        public byte MaskROMVersion { get; private set; }
        public byte HeaderChecksum { get; private set; }
        public ushort GlobalChecksum { get; private set; }

        public CartridgeInfo(byte[] header)
        {
            this.header = header;
            Initialize();
        }

        private void Initialize()
        {
            Title = ReadTitle();
            ManufacturerCode = ReadManufacturerCode();
            CGBFlag = header[0x143];
            NewLicenseeCode = ReadNewLicenseeCode();
            SGBFlag = header[0x146];
            CartType = ReadCartType();
            ROMSize = ReadRomSize();
            RAMSize = ReadRamSize();
            HasBattery = UsesBattery();
            DestinationCode = header[0x14A];
            OldLicenseeCode = header[0x14B];
            MaskROMVersion = header[0x14C];
            HeaderChecksum = header[0x14D];
            GlobalChecksum = (ushort)((header[0x14E] << 8) | header[0x14F]);
        }

        private string ReadTitle()
        {
            var title = header.Skip(0x134).Take(16).ToArray();
            return Encoding.UTF8.GetString(title);
        }

        private string ReadManufacturerCode()
        {
            var manufacturer = header.Skip(0x13F).Take(2).ToArray();
            return Encoding.UTF8.GetString(manufacturer);
        }

        private string ReadNewLicenseeCode()
        {
            var licenseCode = header.Skip(0x144).Take(2).ToArray();
            return Encoding.UTF8.GetString(licenseCode);
        }

        private CartType ReadCartType()
        {
            CartType type;
            byte value = header[0x147];
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

        private bool UsesBattery()
        {
            bool usesBattery;
            byte value = header[0x147];
            switch (value)
            {
                case 0x03:                    
                case 0x06:
                case 0x09:
                case 0x13:
                case 0x17:
                case 0x1B:
                case 0x1E:
                case 0xFF:
                    usesBattery = true; break;
                default:
                    usesBattery = false; break;
            }

            return usesBattery;
        }

        private int ReadRomSize()
        {
            const int BankSize = 16384;   // 16K

            byte value = header[0x148];
            switch (value)
            {
                case 0x00:
                    ROMBankCount = 2; break;   // 32K
                case 0x01:
                    ROMBankCount = 4; break;   // 64K
                case 0x02:
                    ROMBankCount = 8; break;   // 128K
                case 0x03:
                    ROMBankCount = 16; break;  // 256K
                case 0x04:
                    ROMBankCount = 32; break;  // 512K
                case 0x05:
                    ROMBankCount = 64; break;  // 1M
                case 0x06:
                    ROMBankCount = 128; break; // 2M
                case 0x52:
                    ROMBankCount = 72 ; break;  // 1.1M
                case 0x53:
                    ROMBankCount = 80 ; break;  // 1.2M
                case 0x54:
                    ROMBankCount = 96 ; break;  // 1.5M
                default:
                    ROMBankCount = 0; break;
            }

            return BankSize * ROMBankCount;
        }

        private int ReadRamSize()
        {
            int ramSize = 0;
            const int ByteSize = 1024;   // 1K

            byte value = header[0x149];
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
