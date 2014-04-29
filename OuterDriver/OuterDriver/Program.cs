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
            int listeningPort = 9999;
            var listener = new Listener(listeningPort);
            Console.WriteLine("Starting listener on " + OuterServer.FindIPAddress() + ":" + listeningPort);
            listener.StartListening();

            //String command = String.Empty;
            //while (!command.Equals("exit"))
            //{
            //    command = Console.ReadLine();
            //    Console.WriteLine(String.Empty);
            //    String[] tokens = command.Split(' ');
            //    Deployer deployer = new Deployer("{846135ee-2c7a-453c-9a72-e57c607c26c8}");

            //    switch (tokens[0])
            //    {

            //        case "ip":
            //            Console.WriteLine(OuterServer.FindIPAddress());
            //            break;

            //        case "install":

            //            deployer.Deploy();
            //            String ip = deployer.ReceiveIpAddress();
            //            Console.WriteLine("Ip: " + ip);
            //            break;

            //        case "file":
            //            deployer.ReceiveIpAddress();
            //            break;

            //        case "listener":
            //            Console.WriteLine("Starting listener on port " + listeningPort);
            //            listener = new Listener(listeningPort);
            //            listener.StartListening();
            //            break;

            //        default:
            //            Console.WriteLine("Unknown command");
            //            break;
            //    }

            //}
        }
        
    }
}
