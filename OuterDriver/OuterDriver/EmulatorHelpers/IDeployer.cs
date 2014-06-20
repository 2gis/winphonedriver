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

        void Deploy(string appPath, String appIdString, int launchDelay = 3500);

        String ReceiveIpAddress();

        void Disconnect();
    }
}
