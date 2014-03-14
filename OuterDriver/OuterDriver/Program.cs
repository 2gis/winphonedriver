using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace OuterDriver
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(OuterServer.FindIPAddress());
            Console.WriteLine("Enter inner driver ip");
            String innerIp = Console.ReadLine();
            int innerPort = 9998; 
            int listeningPort = 9999;
            var outerServer = new OuterServer(innerIp, innerPort);

            Listener listener;
            Console.WriteLine("Starting listener on port " + listeningPort);
            listener = new Listener(listeningPort, innerPort, innerIp);
            listener.StartListening();

            String command = String.Empty;

            while(!command.Equals("exit"))
            {
                command = Console.ReadLine();
                Console.WriteLine(String.Empty);
                String[] tokens = command.Split(' ');
                
                switch (tokens[0])
                {

                    case "ip":
                        Console.WriteLine(OuterServer.FindIPAddress());
                        break;

                    case "listener":
                        Console.WriteLine("Starting listener on port " + listeningPort);
                        listener = new Listener(listeningPort, innerPort, innerIp);
                        listener.StartListening();
                        break;

                    case "send":
                        String response;
                        if (tokens.Length == 3)
                            response = outerServer.SendRequest(tokens[1], tokens[2]);
                        else
                            response = outerServer.SendRequest(tokens[1]);
                        Console.WriteLine(response);
                        break;

                    default:
                        Console.WriteLine("Unknown command");
                        break;
                }

            }
        }
        
    }
}
