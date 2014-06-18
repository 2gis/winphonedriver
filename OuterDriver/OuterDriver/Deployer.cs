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

        public string DeviceName {
            get { return _iDevice != null? _iDevice.Name : string.Empty; }
        }

        public Deployer(String appIdString) {
            _appIdString = appIdString;
            // TODO: Replace fixed current LCID with locale from desired capabilities if possible
            var connectivity = new MultiTargetingConnectivity(CultureInfo.CurrentUICulture.LCID);
            var devices = connectivity.GetConnectableDevices(false);
            // TODO: Replace with searching based on desired capabilities
            const string desiredDevice = "Emulator WVGA 512MB";
            var defaultDevice = connectivity.GetConnectableDevice(_deviceId); // Temporary solution until WP8.1 worked out
            // var defaultDevice = connectivity.GetConnectableDevice(connectivity.GetDefaultDeviceId());
            // Probably will have to replace with x.Name.StartsWith() due to device name ending with locale, e.g. ...(RU)
            var device = devices.FirstOrDefault(x => x.IsEmulator() && x.Name.StartsWith(desiredDevice)) ?? defaultDevice;
            if (device != null)
            {
                _deviceId = device.Id;
                Console.WriteLine("Deploy target: " + device.Name + " id: " + device.Id);
            }

            var connectableDevice = connectivity.GetConnectableDevice(_deviceId);
            _iDevice = connectableDevice.Connect();
        }

        public void Deploy(string appPath ) {

            // Check if the application is already install, if it is remove it (From WMAppManifect.xml)
            var appID = new Guid(_appIdString);

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
            /* TODO: Find better solution. Use something like RemoteAgent to exchange data or something like:
            string pSourceIp, pDestinationIp;
            int destinationPort;
            _iDevice.GetEndPoints(9998, out pSourceIp, out pDestinationIp, out destinationPort); // looks like port value can be replaced with any value
             // chances are it does not work correctly in some cases (check through RDP)
            */

            IRemoteIsolatedStorageFile remoteIsolatedStorageFile = remoteApplication.GetIsolatedStore("Local");
            var sourceDeviceFilePath = (object)Path.DirectorySeparatorChar + "ip.txt";
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
