using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using Ardmx.WebSocket;

namespace Ardmx.UI
{
    public static class GUI
    {
        public static void Start()
        {
            // HTTP Server
            HttpListener httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://127.0.0.1:8080/");
            httpListener.Start();
            WS ws = new WS(IPAddress.Parse("127.0.0.1"), 1234);
            ws.DataRecived += (s, d) =>
            {
                Console.WriteLine(d.text);
            };

            
            ws.ClientConnected += (s, d) =>
            {
                Array values = Enum.GetValues(typeof(LEDStatus));
                Random rand = new Random();
                int max = values.Length;

                string c = "qwertyuiopåasdfghjklæøzxcvbnm";
                int cm = c.Length;

                while (true)
                {
                    ws.Send(new WebsocketExchange(
                        $"{c[rand.Next(cm)]}{c[rand.Next(cm)]}{c[rand.Next(cm)]}{c[rand.Next(cm)]}{c[rand.Next(cm)]}{c[rand.Next(cm)]}{c[rand.Next(cm)]}{c[rand.Next(cm)]}",
                        (int)values.GetValue(rand.Next(max)),
                        (int)values.GetValue(rand.Next(max)),
                        (int)values.GetValue(rand.Next(max)),

                        $"{c[rand.Next(cm)]}{c[rand.Next(cm)]}{c[rand.Next(cm)]}{c[rand.Next(cm)]}{c[rand.Next(cm)]}{c[rand.Next(cm)]}{c[rand.Next(cm)]}{c[rand.Next(cm)]}",
                        (int)values.GetValue(rand.Next(max)),
                        (int)values.GetValue(rand.Next(max)),
                        (int)values.GetValue(rand.Next(max))
                    ));
                    Thread.Sleep(100);
                }
                
            };

            while (true)
            {
                HttpListenerContext context = httpListener.GetContext();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
                string responseString = "<HTML><BODY> Hello world!</BODY></HTML>";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            }
        }
        public static void SendMessageToClient(TcpClient client, string msg)
        {
            NetworkStream stream = client.GetStream();
            
        }

        public enum LEDStatus
        {
            Disconnected = 0x646464, // grey
            Connecting = 0xe3b900, // yellow
            Connected = 0x15ad07, // green
            Error = 0xcc0000, // red
            
            DataHigh = 0xeb8900, // orange
            DataLow = 0xaaaaaa, // grey
        }
    }
}
