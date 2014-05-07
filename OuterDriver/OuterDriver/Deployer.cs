using System;
using System.Threading;
using System.IO;

// Libraries needed to connect to the Windows Phone X Emulator
using Microsoft.SmartDevice.Connectivity.Interface;
using Microsoft.SmartDevice.MultiTargeting.Connectivity;
using System.Globalization;


namespace OuterDriver {
    class Deployer {

        private IDevice iDevice;
        private String appIdString;
        private String Wvga512DeviceId = "5E7661DF-D928-40ff-B747-A4B1957194F9";

        public Deployer(String appIdString) {
            this.appIdString = appIdString;
            MultiTargetingConnectivity connectivity = new MultiTargetingConnectivity(CultureInfo.CurrentUICulture.LCID);
            ConnectableDevice connectableDevice = connectivity.GetConnectableDevice(Wvga512DeviceId);
            this.iDevice = connectableDevice.Connect();
        }

        public void Deploy() {

            // Check if the application is already install, if it is remove it (From WMAppManifect.xml)
            Guid appID = new Guid(appIdString);

            if (iDevice.IsApplicationInstalled(appID)) {
                Console.WriteLine("Uninstalling application...");
                iDevice.GetApplication(appID).Uninstall();
                Console.WriteLine("Done!");
            }

            Guid productId = appID;
            Guid instanceId = appID;
            String applicationGenre = "NormalApp";
            String iconPath = @"C:\test\ApplicationIcon.png";
            String xapPackage = @"C:\Users\Automator\programming\testApp\testApp\testApp\Bin\Debug\testApp_Debug_AnyCPU.xap";

            // Install the application 
            Console.WriteLine("Installing the application...");
            IRemoteApplication remoteApplication = iDevice.InstallApplication(appID, appID, applicationGenre, iconPath, xapPackage);

            Console.WriteLine("Done!");

            // Launch the application
            Console.WriteLine("Starting the application...");
            remoteApplication.Launch();

            int startStopWaitTime = 3000;   // msec
            int executionWaitTime = 5000; // msec

            // Note that IRemoteApplication has a 'IsRunning' method but it is not implemented.
            // So, for the moment we sleep few msec.
            Thread.Sleep(startStopWaitTime);
            Console.WriteLine("Done!");

            //// Allow application to complete 
            //Console.WriteLine("Application is running! Waiting few seconds...");
            //Thread.Sleep(executionWaitTime);

            //// Terminate application
            //Console.WriteLine("Terminating the application...");
            //remoteApplication.TerminateRunningInstances();

            //Thread.Sleep(startStopWaitTime);
            //Console.WriteLine("\nDone!");

            //// Disconnect from the emulator
            //Console.WriteLine("Disconnecting Device...");
            //iDevice.Disconnect();
            //Console.WriteLine("\nDone!");
        }

        public String ReceiveIpAddress() {
            String ip = String.Empty;
            IRemoteApplication remoteApplication = iDevice.GetApplication(new Guid(appIdString));
            IRemoteIsolatedStorageFile remoteIsolatedStorageFile = remoteApplication.GetIsolatedStore();
            String sourceDeviceFilePath = (object)Path.DirectorySeparatorChar + "ip.txt";
            const String targetDesktopFilePath = @"C:\test\" + "test.txt";
            if (remoteIsolatedStorageFile.FileExists(sourceDeviceFilePath)) {
                remoteIsolatedStorageFile.ReceiveFile(sourceDeviceFilePath, targetDesktopFilePath, true);
                using (var sr = new StreamReader(targetDesktopFilePath)) {
                    ip = sr.ReadLine();
                }
            }
            return ip;
        }


    }
}
