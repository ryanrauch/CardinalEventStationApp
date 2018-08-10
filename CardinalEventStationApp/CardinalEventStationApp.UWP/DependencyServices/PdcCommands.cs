using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardinalEventStationApp.UWP.DependencyServices
{
    public static class PcdCommands
    {
        private const byte idle = 0x00;
        private const byte mifareAuthenticate = 0x0E;
        private const byte transceive = 0x0C;

        public static byte Idle
        {
            get
            {
                return idle;
            }
        }

        public static byte MifareAuthenticate
        {
            get
            {
                return mifareAuthenticate;
            }
        }

        public static byte Transceive
        {
            get
            {
                return transceive;
            }
        }
    }
}
