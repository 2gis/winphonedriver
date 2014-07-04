using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuterDriver.EmulatorHelpers
{
    interface IDeployer
    {

        string DeviceName { get; }

        void Deploy(string appPath);

        String ReceiveIpAddress();

        void Disconnect();
    }
}
