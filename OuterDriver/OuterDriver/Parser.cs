using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterDriver
{
    class Parser
    {
        static String GetRequestMethod(String requestHeaders)
        {
            String[] headers = requestHeaders.Split('\n');
            String[] firstHeaderTokens = headers[0].Split(' ');
            String method = firstHeaderTokens[0];
            if (!method.Equals("POST") || !(method.Equals("GET")))
                return "";
            return method;
        }

        static String GetRequestUri(String requestHeaders)
        {
            String[] headers = requestHeaders.Split('\n');
            String[] firstHeaderTokens = headers[0].Split(' ');
            return firstHeaderTokens[1];
        }
    }
}
