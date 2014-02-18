using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterDriver
{
    class Proxy
    {
        private Requester requester;

        public Proxy(String ip, String port)
        {
            requester = new Requester(ip, port)
        }


    }
}
