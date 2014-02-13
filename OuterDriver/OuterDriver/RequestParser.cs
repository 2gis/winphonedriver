using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterDriver
{
    class RequestParser
    {
        public static String GetLastToken(String uri)
        {
            String[] tokens = uri.Split('/');
            return tokens[tokens.Length - 1];
        }
    }
}
