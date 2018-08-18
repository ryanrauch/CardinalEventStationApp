using CardinalEventStationApp.Services.Interfaces;
using CardinalEventStationApp.UWP.DependencyServices.RC522;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using Xamarin.Forms;

[assembly: Dependency(typeof(RC522))]
namespace CardinalEventStationApp.UWP.DependencyServices.RC522
{
    public class RC522 : INFCReader
    {
        public SpiDevice _spi { get; private set; }
        public GpioController IoController { get; private set; }
        public GpioPin _resetPowerDown { get; private set; }

        //https--://stackoverflow.com/questions/34284498/rfid-rc522-raspberry-pi-2-windows-iot
        /* Uncomment for Raspberry Pi 2 */
        private const string SPI_CONTROLLER_NAME = "SPI0";
        private const Int32 SPI_CHIP_SELECT_LINE = 0;
        private const Int32 RESET_PIN = 25;

        public async Task InitIO()
        {

            try
            {
                IoController = GpioController.GetDefault();

                _resetPowerDown = IoController.OpenPin(RESET_PIN);
                _resetPowerDown.Write(GpioPinValue.High);
                _resetPowerDown.SetDriveMode(GpioPinDriveMode.Output);
            }
            /* If initialization fails, throw an exception */
            catch (Exception ex)
            {
                throw new Exception("GPIO initialization failed", ex);
            }

            try
            {
                var settings = new SpiConnectionSettings(SPI_CHIP_SELECT_LINE);
                settings.ClockFrequency = 1000000;
                settings.Mode = SpiMode.Mode0;

                String spiDeviceSelector = SpiDevice.GetDeviceSelector();
                IReadOnlyList<DeviceInformation> devices = await DeviceInformation.FindAllAsync(spiDeviceSelector);

                _spi = await SpiDevice.FromIdAsync(devices[0].Id, settings);

            }
            /* If initialization fails, display the exception and stop running */
            catch (Exception ex)
            {
                throw new Exception("SPI Initialization Failed", ex);
            }


            Reset();
        }

        public void Reset()
        {
            _resetPowerDown.Write(GpioPinValue.Low);
            System.Threading.Tasks.Task.Delay(50).Wait();
            _resetPowerDown.Write(GpioPinValue.High);
            System.Threading.Tasks.Task.Delay(50).Wait();

            // Force 100% ASK modulation
            WriteRegister(Registers.TxAsk, 0x40);

            // Set CRC to 0x6363
            WriteRegister(Registers.Mode, 0x3D);

            // Enable antenna
            SetRegisterBits(Registers.TxControl, 0x03);
        }


        public bool IsTagPresent()
        {
            // Enable short frames
            WriteRegister(Registers.BitFraming, 0x07);

            // Transceive the Request command to the tag
            Transceive(false, PiccCommands.Request);

            // Disable short frames
            WriteRegister(Registers.BitFraming, 0x00);

            // Check if we found a card
            return GetFifoLevel() == 2 && ReadFromFifoShort() == PiccResponses.AnswerToRequest;
        }

        public Uid ReadUid()
        {
            // Run the anti-collision loop on the card
            Transceive(false, PiccCommands.Anticollision_1, PiccCommands.Anticollision_2);

            // Return tag UID from FIFO
            return new Uid(ReadFromFifo(5));
        }

        public void HaltTag()
        {
            // Transceive the Halt command to the tag
            Transceive(false, PiccCommands.Halt_1, PiccCommands.Halt_2);
        }

        public bool SelectTag(Uid uid)
        {
            // Send Select command to tag
            var data = new byte[7];
            data[0] = PiccCommands.Select_1;
            data[1] = PiccCommands.Select_2;
            uid.FullUid.CopyTo(data, 2);

            Transceive(true, data);

            return GetFifoLevel() == 1 && ReadFromFifo() == PiccResponses.SelectAcknowledge;
        }

        internal byte[] ReadBlock(byte blockNumber, Uid uid, byte[] keyA = null, byte[] keyB = null)
        {
            if (keyA != null)
                MifareAuthenticate(PiccCommands.AuthenticateKeyA, blockNumber, uid, keyA);
            else if (keyB != null)
                MifareAuthenticate(PiccCommands.AuthenticateKeyB, blockNumber, uid, keyB);
            else
                return null;

            // Read block
            Transceive(true, PiccCommands.Read, blockNumber);

            return ReadFromFifo(16);
        }

        internal bool WriteBlock(byte blockNumber, Uid uid, byte[] data, byte[] keyA = null, byte[] keyB = null)
        {
            if (keyA != null)
                MifareAuthenticate(PiccCommands.AuthenticateKeyA, blockNumber, uid, keyA);
            else if (keyB != null)
                MifareAuthenticate(PiccCommands.AuthenticateKeyB, blockNumber, uid, keyB);
            else
                return false;

            // Write block
            Transceive(true, PiccCommands.Write, blockNumber);

            if (ReadFromFifo() != PiccResponses.Acknowledge)
                return false;

            // Make sure we write only 16 bytes
            var buffer = new byte[16];
            data.CopyTo(buffer, 0);

            Transceive(true, buffer);

            return ReadFromFifo() == PiccResponses.Acknowledge;
        }


        protected void MifareAuthenticate(byte command, byte blockNumber, Uid uid, byte[] key)
        {
            // Put reader in Idle mode
            WriteRegister(Registers.Command, PcdCommands.Idle);

            // Clear the FIFO
            SetRegisterBits(Registers.FifoLevel, 0x80);

            // Create Authentication packet
            var data = new byte[12];
            data[0] = command;
            data[1] = (byte)(blockNumber & 0xFF);
            key.CopyTo(data, 2);
            uid.Bytes.CopyTo(data, 8);

            WriteToFifo(data);

            // Put reader in MfAuthent mode
            WriteRegister(Registers.Command, PcdCommands.MifareAuthenticate);

            // Wait for (a generous) 25 ms
            System.Threading.Tasks.Task.Delay(25).Wait();
        }

        protected void Transceive(bool enableCrc, params byte[] data)
        {
            if (enableCrc)
            {
                // Enable CRC
                SetRegisterBits(Registers.TxMode, 0x80);
                SetRegisterBits(Registers.RxMode, 0x80);
            }

            // Put reader in Idle mode
            WriteRegister(Registers.Command, PcdCommands.Idle);

            // Clear the FIFO
            SetRegisterBits(Registers.FifoLevel, 0x80);

            // Write the data to the FIFO
            WriteToFifo(data);

            // Put reader in Transceive mode and start sending
            WriteRegister(Registers.Command, PcdCommands.Transceive);
            SetRegisterBits(Registers.BitFraming, 0x80);

            // Wait for (a generous) 25 ms
            System.Threading.Tasks.Task.Delay(25).Wait();

            // Stop sending
            ClearRegisterBits(Registers.BitFraming, 0x80);

            if (enableCrc)
            {
                // Disable CRC
                ClearRegisterBits(Registers.TxMode, 0x80);
                ClearRegisterBits(Registers.RxMode, 0x80);
            }
        }


        protected byte[] ReadFromFifo(int length)
        {
            var buffer = new byte[length];

            for (int i = 0; i < length; i++)
                buffer[i] = ReadRegister(Registers.FifoData);

            return buffer;
        }

        protected byte ReadFromFifo()
        {
            return ReadFromFifo(1)[0];
        }

        protected void WriteToFifo(params byte[] values)
        {
            foreach (var b in values)
                WriteRegister(Registers.FifoData, b);
        }

        protected int GetFifoLevel()
        {
            return ReadRegister(Registers.FifoLevel);
        }


        protected byte ReadRegister(byte register)
        {
            register <<= 1;
            register |= 0x80;

            var writeBuffer = new byte[] { register, 0x00 };

            return TransferSpi(writeBuffer)[1];
        }

        protected ushort ReadFromFifoShort()
        {
            var low = ReadRegister(Registers.FifoData);
            var high = (ushort)(ReadRegister(Registers.FifoData) << 8);

            return (ushort)(high | low);
        }

        protected void WriteRegister(byte register, byte value)
        {
            register <<= 1;

            var writeBuffer = new byte[] { register, value };

            TransferSpi(writeBuffer);
        }

        protected void SetRegisterBits(byte register, byte bits)
        {
            var currentValue = ReadRegister(register);
            WriteRegister(register, (byte)(currentValue | bits));
        }

        protected void ClearRegisterBits(byte register, byte bits)
        {
            var currentValue = ReadRegister(register);
            WriteRegister(register, (byte)(currentValue & ~bits));
        }


        private byte[] TransferSpi(byte[] writeBuffer)
        {
            var readBuffer = new byte[writeBuffer.Length];

            _spi.TransferFullDuplex(writeBuffer, readBuffer);

            return readBuffer;
        }
    }
}
