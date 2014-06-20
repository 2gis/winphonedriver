using System;
using OuterDriver.EmulatorHelpers;

namespace DeployerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var appPath = args[0];

            const string appId = "69b4ce34-a3e0-414a-92d9-1302449f587c";
            var deployer = new Deployer(string.Empty);
            if (!String.IsNullOrEmpty(appPath))
            {
                deployer.Deploy(appPath, appId);
            }
        }
    }
}
