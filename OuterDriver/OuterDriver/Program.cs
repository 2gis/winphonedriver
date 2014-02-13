using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterDriver
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Enter inner driver ip");
            //String innerIp = Console.ReadLine();
            //Console.WriteLine("Enter inner driver port");
            //String innerPort = Console.ReadLine();
            //var outerServer = new OuterServer(innerIp, innerPort);

            Listener listener;
            OuterServer outerServer;

            String command = String.Empty;

            while(!command.Equals("exit"))
            {
                command = Console.ReadLine();
                String[] tokens = command.Split(' ');
                
                switch (tokens[0])
                {

                    case "ip":
                        Console.WriteLine(OuterServer.FindIPAddress());
                        break;

                    case "listener":
                        Console.WriteLine("Enter listening port");
                        int port = Convert.ToInt32(Console.ReadLine());
                        listener = new Listener(port);
                        break;

                    case "send":
                        Console.WriteLine("Enter ip");
                        String innerIp = Console.ReadLine();
                        Console.WriteLine("Enter port");
                        String innerPort = Console.ReadLine();
                        outerServer = new OuterServer(innerIp, innerPort);
                        String response = outerServer.SendRequest("/session/", tokens[1]);
                        Console.WriteLine(response);
                        break;

                    default:
                        Console.WriteLine("Unknown command");
                        break;
                }

                //try
                //{
                //    Console.WriteLine(outerServer.SendRequest("/session/lols", command));
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine(ex.Message);
                //}

            }
        }
    }
}
