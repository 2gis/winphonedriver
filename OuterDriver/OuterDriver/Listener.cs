using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace OuterDriver
{

    public class Listener
    {

        private readonly int bufferSize = 512;
        Byte[] readBuffer;
        TcpListener listener;

        public Listener(int port)
        {
            this.listener = null;
            this.readBuffer = new Byte[bufferSize];
            try
            {
                IPAddress localAddr = IPAddress.Parse(OuterServer.FindIPAddress());
                listener = new TcpListener(localAddr, port);

                // Start listening for client requests.
                listener.Start();

                // Enter the listening loop
                while (true)
                {
                    Console.Write("Waiting for a connection... ");

                    // Perform a blocking call to accept requests. 
                    // You could also user listener.AcceptSocket() here.
                    TcpClient client = listener.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();

                    String headers = ReadString(stream);
                    //TODO read body only if it's a POST request
                    String body = ReadString(stream);


                    Console.WriteLine("Headers: " + headers);
                    Console.WriteLine("Body: " + body);

                    String responseBody = "Success";
                    Responder.Respond(stream, responseBody);
                    

                    // Shutdown and end connection
                    client.Close();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                listener.Stop();
            }
        }

        private String ReadString(NetworkStream stream)
        {
            Byte[] readBuffer = new Byte[1024];
            int readBytes = stream.Read(readBuffer, 0, readBuffer.Length);
            String result = System.Text.Encoding.ASCII.GetString(readBuffer, 0, readBytes);
            return result;
        }
    }

}
