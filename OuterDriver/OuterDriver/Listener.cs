using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO;

namespace OuterDriver
{

    public class Listener
    {

        private TcpListener listener;
        private readonly int port;

        public Listener(int port)
        {
            this.port = port;
            this.listener = null;
        }

        public async void StartListening()
        {
            try
            {
                IPAddress localAddr = IPAddress.Parse(OuterServer.FindIPAddress());
                listener = new TcpListener(localAddr, this.port);

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

                    //read HTTP headers
                    var headers = new Dictionary<String, String>();
                    String header;
                    while (!String.IsNullOrEmpty(header = ReadString(stream)))
                    {
                        Console.WriteLine(header);
                        String[] splitHeader;
                        splitHeader = header.Split(':');
                        headers.Add(splitHeader[0], splitHeader[1].Trim(' '));
                    }

                    //try and read request content
                    String contentLengthString;
                    bool hasContentLength = headers.TryGetValue("Content-Length", out contentLengthString);
                    if (hasContentLength)
                    {
                        String content = ReadContent(stream, Convert.ToInt32(contentLengthString));
                        Console.WriteLine(content);
                    }

                    String responseBody = "Success";
                    Responder.WriteResponse(stream, responseBody);

                    // Shutdown and end connection
                    stream.Close();
                    client.Close();

                    Console.WriteLine("Client closed");
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException: {0}", ex);
            }
            finally
            {
                // Stop listening for new clients.
                listener.Stop();
            }
        }

        public void StopListening()
        {
            listener.Stop();
        }

        //reads the content of a request depending on its length
        private String ReadContent(NetworkStream s, int contentLength)
        {
            Byte[] readBuffer = new Byte[contentLength];
            int readBytes = s.Read(readBuffer, 0, readBuffer.Length);
            return System.Text.Encoding.ASCII.GetString(readBuffer, 0, readBytes);
        }

        private String ReadString(NetworkStream stream)
        {
            //StreamReader reader = new StreamReader(stream);
            int nextChar;
            String data = "";
            while (true)
            {
                nextChar = stream.ReadByte();
                if (nextChar == '\n') { break; }
                if (nextChar == '\r') { continue; }
                data += Convert.ToChar(nextChar);
            }
            return data;
        }
    }

}
