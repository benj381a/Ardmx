using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace Ardmx.WebSocket
{
    public class WS
    {
        public event EventHandler<DataRecivedEventArgs> DataRecived;
        public event EventHandler ClientConnected;
        private NetworkStream stream = null;

        public WS(IPAddress ip, int port)
        {
            TcpListener listener = new TcpListener(ip, port);
            listener.Start();

            new Thread(() => {
                TcpClient client = listener.AcceptTcpClient();
                stream = client.GetStream();

                while (true)
                {
                    while (!stream.DataAvailable) ;
                    while (client.Available < 3) ; // match against "get"

                    (string str, byte[] bytes) = ReadFromStream(client);

                    if (IsHandshake(str))
                    {
                        ShakeHand(str);
                        ClientConnected.Invoke(this, null);
                    }
                    else
                    {
                        if (GetText(bytes, out string text))
                        {
                            DataRecived.Invoke(this, new DataRecivedEventArgs(text));
                        }
                    }
                }
            }).Start();
        }
        public void Send(WebsocketExchange exchange)
        {
            SendData(JsonSerializer.Serialize(exchange));
        }
        public void SendData(string str)
        {
            if (stream == null)
                return;

            Queue<string> que = new Queue<string>(str.SplitInGroups(125));
            int len = que.Count;

            while (que.Count > 0)
            {
                int header = GetHeader(que, len);

                byte[] list = Encoding.UTF8.GetBytes(que.Dequeue());
                header = header.AppendLength(list.Length);
                WriteHeader(header);
                Write(list);
            }
        }

        private (string, byte[]) ReadFromStream(TcpClient client)
        {
            byte[] bytes = new byte[client.Available];
            stream.Read(bytes, 0, bytes.Length);
            
            return (Encoding.UTF8.GetString(bytes), bytes);
        }
        private bool IsHandshake(string str) => Regex.IsMatch(str, "^GET", RegexOptions.IgnoreCase);
        private void ShakeHand(string str)
        {
            // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
            string swk = Regex.Match(str, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
            // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
            string swkAndSalt = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            // 3. Compute SHA-1 and Base64 hash of the new value
            byte[] swkAndSaltSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swkAndSalt));
            string swkAndSaltSha1Base64 = Convert.ToBase64String(swkAndSaltSha1);

            // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
            byte[] response = Encoding.UTF8.GetBytes(
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Connection: Upgrade\r\n" +
                "Upgrade: websocket\r\n" +
                "Sec-WebSocket-Accept: " + swkAndSaltSha1Base64 + "\r\n\r\n");

           Write(response);
        }
        private bool GetText(byte[] bytes, out string text)
        {
            bool fin = (bytes[0] & 0b10000000) != 0,
                            mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"
            int opcode = bytes[0] & 0b00001111; // expecting 1 - text message
            ulong offset = 2,
                  msgLen = bytes[1] & (ulong)0b01111111;

            if (msgLen == 126)
            {
                // bytes are reversed because websocket will print them in Big-Endian, whereas
                // BitConverter will want them arranged in little-endian on windows
                msgLen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                offset = 4;
            }
            else if (msgLen == 127)
            {
                // To test the below code, we need to manually buffer larger messages — since the NIC's autobuffering
                // may be too latency-friendly for this code to run (that is, we may have only some of the bytes in this
                // websocket frame available through client.Available).
                msgLen = BitConverter.ToUInt64(new byte[] { bytes[9], bytes[8], bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2] }, 0);
                offset = 10;
            }

            if (mask)
            {
                byte[] decoded = new byte[msgLen];
                byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                offset += 4;

                for (ulong i = 0; i < msgLen; ++i)
                    decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);

                text = Encoding.UTF8.GetString(decoded);
                return true;
            }
            text = "";
            return false;
        }
        private int GetHeader(Queue<string> que, int len)
        {
            bool finalFrame = que.Count <= 1;
            bool contFrame = que.Count != len;

            int header = finalFrame ? 1 : 0;//fin: 0 = more frames, 1 = final frame
            header = (header << 1) + 0;//rsv1
            header = (header << 1) + 0;//rsv2
            header = (header << 1) + 0;//rsv3
            header = (header << 4) + (contFrame ? 0 : 1);//opcode : 0 = continuation frame, 1 = text
            header = (header << 1) + 0;//mask: server -> client = no mask

            return header;
        }
        private byte[] IntToByteArray(ushort value)
        {
            var ary = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(ary);
            }

            return ary;
        }
        private void Write(byte[] data) => stream.Write(data, 0, data.Length);
        private void WriteHeader(int header) => stream.Write(IntToByteArray((ushort)header), 0, 2);
    }

    public class WebsocketExchange
    {
        public string type { get; set; }
        public Dictionary<string, string> data { get; set; }

        public WebsocketExchange(string nodeText, int nodeStatus, int nodeRx, int nodeTx, string deviceText, int deviceStatus, int deviceRx, int deviceTx)
        {
            this.type = "LED_Update";
            this.data = new Dictionary<string, string>()
            {
                { "node.status.text", nodeText},
                { "node.status.led", $"#{nodeStatus:X}" },
                { "node.rx", $"#{nodeRx:X}" },
                { "node.tx", $"#{nodeTx:X}" },

                { "device.status.text", deviceText},
                { "device.status.led", $"#{deviceStatus:X}" },
                { "device.rx", $"#{deviceRx:X}" },
                { "device.tx", $"#{deviceTx:X}" },
            };
        }
    }

    public class DataRecivedEventArgs : EventArgs
    {
        public string text;

        public DataRecivedEventArgs(string text)
        {
            this.text = text;
        }
    }

    public static class WSExstentions
    {
        public static IEnumerable<string> SplitInGroups(this string original, int size)
        {
            var p = 0;
            var l = original.Length;
            while (l - p > size)
            {
                yield return original.Substring(p, size);
                p += size;
            }
            yield return original.Substring(p);
        }

        public static int AppendLength(this int original, int length) => (original << 7) + length;
    }

}
