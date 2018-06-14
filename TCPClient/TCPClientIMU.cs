using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TCPClientIMU
{
    public class TCPClientIMU
    {
        private readonly string server;
        private readonly int port;
        private TcpClient client;

        public TCPClientIMU(string server, int port)
        {
            this.server = server;
            this.port = port;

        }

        public async Task ConnectAsync() {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(server, port);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
        }

        public async Task SendMessageToServerTaskAsync(string message) {
            try
            {
                // Translate the passed message into ASCII and store it as a Byte array.
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

                // Get a client stream for reading and writing.
                NetworkStream stream = client.GetStream();

                // Send the message to the connected TcpServer. 
                await stream.WriteAsync(data, 0, data.Length);

                Console.WriteLine("Sent: {0}", message);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
        }

    }
}

