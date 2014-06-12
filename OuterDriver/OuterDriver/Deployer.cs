using System;
using System.Linq;
using System.Threading;
using System.IO;

// Libraries needed to connect to the Windows Phone X Emulator
using Microsoft.SmartDevice.Connectivity.Interface;
using Microsoft.SmartDevice.MultiTargeting.Connectivity;
using System.Globalization;


namespace OuterDriver {
    class Deployer {

        private readonly IDevice _iDevice;
        private readonly String _appIdString;
        private readonly String _deviceId = "5E7661DF-D928-40ff-B747-A4B1957194F9";

        public Deployer(String appIdString) {
            this._appIdString = appIdString;
            var connectivity = new MultiTargetingConnectivity(CultureInfo.CurrentUICulture.LCID);
            var devices = connectivity.GetConnectableDevices(false);
            // TODO: Replace with searching based on desired capabilities
            var device = devices.FirstOrDefault(x => x.IsEmulator() && x.Name.Equals("Emulator WVGA 512MB"));
            if (device != null)
            {
                _deviceId = device.Id;
                Console.WriteLine("Deploy target: " + device.Name + " id: " + device.Id);
            }

            var connectableDevice = connectivity.GetConnectableDevice(_deviceId);
            this._iDevice = connectableDevice.Connect();
        }

        public void Deploy(string appPath ) {

            // Check if the application is already install, if it is remove it (From WMAppManifect.xml)
            Guid appID = new Guid(_appIdString);

            if (_iDevice.IsApplicationInstalled(appID)) {
                Console.WriteLine("Uninstalling application...");
                _iDevice.GetApplication(appID).Uninstall();
                Console.WriteLine("Done!");
            }

            Guid productId = appID;
            Guid instanceId = appID;
            String applicationGenre = "NormalApp";
            String iconPath = @"C:\test\ApplicationIcon.png";
            var xapPackage = appPath;
            if (String.IsNullOrEmpty(xapPackage))
            {
                // TODO: Come up with a reasonable default or throw exception instead 
                xapPackage = @"C:\test\DoubleGis.WinPhone.App_Release_x86.xap";
            }

            // Install the application 
            Console.WriteLine("Installing the application...");
            IRemoteApplication remoteApplication = _iDevice.InstallApplication(appID, appID, applicationGenre, iconPath, xapPackage);

            Console.WriteLine("Done!");

            // Launch the application
            Console.WriteLine("Starting the application...");
            remoteApplication.Launch();

            int startStopWaitTime = 3000;   // msec
            // int executionWaitTime = 5000; // msec

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
            IRemoteApplication remoteApplication = _iDevice.GetApplication(new Guid(_appIdString));
            // TODO: Chekc if winphone 8.1 and switch ip to host, otherwise request ip address iDevice.GetSystemInfo()
            // See social.msdn.microsoft.com/Forums/sqlserver/en-US/8902939b-233f-4075-99c3-5856f7e6ca6e/windows-phone-81-emulator-no-longer-uses-dhcp?forum=wpdevelop

            IRemoteIsolatedStorageFile remoteIsolatedStorageFile = remoteApplication.GetIsolatedStore("Local");
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
