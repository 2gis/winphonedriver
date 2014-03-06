using System;
using System.Collections.Generic;
using System.Linq;

namespace OuterDriver
{
    class Parser
    {

        private static List<String> commandsToProxy = new List<String>{ "element", "click", "text", "displayed", "location" };
        private static List<String> commandsWithGET = new List<String> { "text", "displayed", "location" };

        public static String GetRequestUrn(String request)
        {
            String[] firstHeaderTokens = request.Split(' ');
            return firstHeaderTokens[1];
        }

        public static String GetLastToken(String urn)
        {
            String[] urnTokens = urn.Split(new String[] {"/"}, StringSplitOptions.RemoveEmptyEntries);
            String command = urnTokens[urnTokens.Length - 1];
            return command;
        }

        public static String GetRequestCommand(String request)
        {
            String[] requestTokens = request.Split(' ');
            String urn = requestTokens[1];
            //take care of the slash in the end of the string
            String[] urnTokens = urn.Split(new String[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            String command = urnTokens[urnTokens.Length - 1];
            return command;
        }

        //decides if the request should be simply proxied by looking at the last command token
        public static bool ShouldProxy(String request)
        {
            Console.WriteLine("Checking if need to proxy " + request);
            String urn = GetRequestUrn(request);
            return commandsToProxy.Contains(GetLastToken(urn));
        }

        //chooses the request method by looking at the last command token
        public static String ChooseRequestMethod(String uri)
        {
            Console.WriteLine("choosing request method for " + uri);
            if (commandsWithGET.Contains(GetLastToken(uri)))
                return "GET";
            return "POST";
        }
    }
}
