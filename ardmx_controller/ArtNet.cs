using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Ardmx.Art_Net
{
    public static class ArtNet
    {
        public static event EventHandler<RecivedDmxEventArgs> RecivedDmx;
        private static UdpClient client;
        private static IPEndPoint remote;

        private static bool artPollReplyDebounce = true;
        public static void Start()
        {
            byte[] ip = GetLocalIPAddress();
            Console.WriteLine($"{ip[0]}.{ip[1]}.{ip[2]}.{ip[3]}");
            new Thread(() => { while (true) { Console.Read(); artPollReplyDebounce = true; } }).Start();

            new Thread(() => {
                client = new UdpClient(6454); // Art-Net port
                while (true)
                {
                    remote = null;
                    byte[] packet = client.Receive(ref remote);
                    Console.WriteLine($"Packet of type: {(OpCode)GetOpCode(packet)} - Recived");
                    if (ValidateArtNet(packet))
                    {
                        RecivedArtNet(packet);
                    }
                    else
                    {
                        Console.WriteLine("Wrong Format");
                    }

                }
            }).Start();
        }
        private static bool ValidateArtNet(byte[] packet)
        {
            if (packet.Length < 14)
                return false;

            if (Encoding.ASCII.GetString(packet.Take(8).ToArray()) != "Art-Net\0")
                return false;

            return true;
        }
        private static OpCode GetOpCode(byte[] packet) => (OpCode)((packet[9] << 8) | packet[8]);
        
        private static byte[] NewArtNetPacket(OpCode op, int length)
        {
            byte[] opBytes = BitConverter.GetBytes((int)op);

            byte[] packet = new byte[length];
            
            packet[0] = (byte)'A';
            packet[1] = (byte)'r';
            packet[2] = (byte)'t';
            packet[3] = (byte)'-';
            packet[4] = (byte)'N';
            packet[5] = (byte)'e';
            packet[6] = (byte)'t';
            packet[7] = (byte)'\0';

            packet[8] = opBytes[0];
            packet[9] = opBytes[1];

            return packet;
        }
        private static void SendPollReply()
        {
            client.Connect(remote);
            /*
            byte[] macAddress = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(nic => nic.GetPhysicalAddress())
                .FirstOrDefault().GetAddressBytes();
            byte[] ipAddress = GetLocalIPAddress();

            // TODO: add random delay
            byte[] packet = NewArtNetPacket(OpCode.OpPollReply, 207);

            // ip addr
            packet[0x0B] = ipAddress[0];
            packet[0x0B] = ipAddress[1];
            packet[0x0B] = ipAddress[2];
            packet[0x0B] = ipAddress[3];

            // port 6454
            packet[0x0C] = 0x19;
            packet[0x0D] = 0x36;

            packet[0xCE] = 0x40; // port 1: listen

            // mac
            packet[0xC9] = macAddress[0];
            packet[0xCA] = macAddress[1];
            packet[0xCB] = macAddress[2];
            packet[0xCC] = macAddress[3];
            packet[0xCD] = macAddress[4];
            packet[0xCE] = macAddress[5];
            */
            
            
            byte[] responcePacket = {0x41, 0x72, 0x74, 0x2d, 0x4e, 0x65, 0x74, 0x0, 0x0, 0x21, 0xac, 0x12, 0x50, 0x1, 0x36, 0x19, 0x4, 0x34, 0x0, 0x0, 0x22, 0x69, 0x0, 0x0, 0x79, 0x53, 0x41, 0x4d, 0x58, 0x2d, 0x57, 0x6f, 0x72, 0x6b, 0x73, 0x68, 0x6f, 0x70, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x41, 0x4d, 0x58, 0x2d, 0x57, 0x6f, 0x72, 0x6b, 0x73, 0x68, 0x6f, 0x70, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x23, 0x30, 0x30, 0x30, 0x31, 0x20, 0x5b, 0x30, 0x30, 0x30, 0x31, 0x5d, 0x20, 0x50, 0x6f, 0x77, 0x65, 0x72, 0x20, 0x4f, 0x6e, 0x20, 0x54, 0x65, 0x73, 0x74, 0x73, 0x20, 0x50, 0x61, 0x73, 0x73, 0x2e, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x1, 0x40, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x1, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x15, 0x5d, 0x8e, 0xaf, 0xe6, 0xac, 0x12, 0x50, 0x1, 0x1, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0};
            client.Send(responcePacket, responcePacket.Length);
        }
        private static void RecivedArtNet(byte[] packet)
        {
            if (GetOpCode(packet) != OpCode.OpPoll)
            {
                Console.WriteLine("!=");
            }
            switch (GetOpCode(packet))
            {
                case OpCode.OpPoll:
                    if (artPollReplyDebounce)
                    {
                        artPollReplyDebounce = false;
                        SendPollReply();
                    }
                    break;

                case OpCode.OpDmx:
                    Console.WriteLine("Test");
                    break;
                default:
                    Console.WriteLine($"Opcode {Enum.GetName(typeof(OpCode), GetOpCode(packet))} (0x{(int)GetOpCode(packet):X4}) - not implemented");
                    break;
            }
        }

        private enum OpCode
        {
            OpPoll = 0x2000,
            OpPollReply = 0x2100,
            OpDiagData = 0x2300,
            OpCommand = 0x2400,
            OpDataRequest = 0x2700,
            OpDataReply = 0x2800,
            OpDmx = 0x5000, OpOutput = 0x5000,
            OpNzs = 0x5100,
            OpSync = 0x5200,
            OpAddress = 0x6000,
            OpInput = 0x7000,
            OpTodRequest = 0x8000,
            OpTodData = 0x8100,
            OpTodControl = 0x8200,
            OpRdm = 0x8300,
            OpRdmSub = 0x8400,
            OpVideoSetup = 0xa010,
            OpVideoPalette = 0xa020,
            OpVideoData = 0xa040,
            OpFirmwareMaster = 0xf200,
            OpFirmwareReply = 0xf300,
            OpFileTnMaster = 0xf400,
            OpFileFnMaster = 0xf500,
            OpFileFnReply = 0xf600,
            OpIpProg = 0xf800,
            OpIpProgReply = 0xf900,
            OpMedia = 0x9000,
            OpMediaPatch = 0x9100,
            OpMediaControl = 0x9200,
            OpMediaContrlReply = 0x9300,
            OpTimeCode = 0x9700,
            OpTimeSync = 0x9800,
            OpTrigger = 0x9900,
            OpDirectory = 0x9a00,
            OpDirectoryReply = 0x9b00,
        }
        public class RecivedDmxEventArgs : EventArgs
        {
            public DmxUniverse dmx { get; }
            public RecivedDmxEventArgs(DmxUniverse dmxUniverse)
            {
                dmx = dmxUniverse;
            }
        }


        public static byte[] GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.GetAddressBytes();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

    }
    public class DmxUniverse
    {
        public byte[] channels;

        public DmxUniverse()
        {
            channels = new byte[512];
        }

        public byte Get(int channel) => channels[channel];
        public void Set(int channel, byte value) { channels[channel] = value; }
    }
}
