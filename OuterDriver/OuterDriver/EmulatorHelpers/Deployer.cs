// Libraries needed to connect to the Windows Phone X Emulator
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.SmartDevice.Connectivity.Interface;
using Microsoft.SmartDevice.MultiTargeting.Connectivity;
using OuterDriver.AutomationExceptions;


namespace OuterDriver.EmulatorHelpers {
    public class Deployer {

        private readonly IDevice _iDevice;
        private String _appIdString;
        private IRemoteApplication _remoteApplication;

        public string DeviceName {
            get { return _iDevice != null? _iDevice.Name : string.Empty; }
        }

        public Deployer(string desiredDevice)
        {
            // TODO: Replace fixed current LCID with locale from desired capabilities if possible
            var connectivity = new MultiTargetingConnectivity(CultureInfo.CurrentUICulture.LCID);
            var devices = connectivity.GetConnectableDevices(false);
            var defaultDevice = connectivity.GetConnectableDevice(connectivity.GetDefaultDeviceId());
            var device = defaultDevice;

            if (!String.IsNullOrEmpty(desiredDevice))
            {
                device = devices.FirstOrDefault(x => x.IsEmulator() && x.Name.StartsWith(desiredDevice)) ?? defaultDevice;
            }
            Console.WriteLine("Deploy target: " + device.Name + " id: " + device.Id);

            _iDevice = device.Connect();
        }

        public void Deploy(string appPath, String appIdString, int launchDelay = 3500)
        {
            _appIdString = appIdString;
            // Check if the application is already installed, if it is remove it (From WMAppManifect.xml)
            var appGuid = new Guid(_appIdString);

            if (_iDevice.IsApplicationInstalled(appGuid)) {
                Console.WriteLine("Uninstalling application...");
                _iDevice.GetApplication(appGuid).Uninstall();
                Console.WriteLine("Done!");
            }

            const string applicationGenre = "NormalApp";
            const string iconPath = @"C:\test\ApplicationIcon.png";
            var xapPackage = appPath;
            if (String.IsNullOrEmpty(xapPackage))
            {
                throw new AutomationException("Empty \"app\" capability. No XAP package provided to run the app");
            }

            // Install the application 
            Console.WriteLine("Installing the application...");
            _remoteApplication = _iDevice.InstallApplication(appGuid, appGuid, applicationGenre, iconPath, xapPackage);

            Console.WriteLine("Done!");

            // Launch the application
            Console.WriteLine("Starting the application...");
            _remoteApplication.Launch();

            var startStopWaitTime = launchDelay;   // msec
            
            // Note that IRemoteApplication has a 'IsRunning' method but it is not implemented.
            // So, for the moment we sleep few msec.
            Thread.Sleep(startStopWaitTime);
            Console.WriteLine("Done!");
        }

        public String ReceiveIpAddress() {
            // Windows Phone 8.1 emulators use same ip as host http://social.msdn.microsoft.com/Forums/sqlserver/en-US/8902939b-233f-4075-99c3-5856f7e6ca6e/windows-phone-81-emulator-no-longer-uses-dhcp?forum=wpdevelop
            if (_iDevice.GetSystemInfo().OSMajor == 8 && _iDevice.GetSystemInfo().OSMinor > 0)
            {
                return string.Empty;
            }

            /* TODO: Find better solution. Use something like RemoteAgent to exchange data or something like:
            string pSourceIp, pDestinationIp; int destinationPort;
            _iDevice.GetEndPoints(9998, out pSourceIp, out pDestinationIp, out destinationPort); // looks like port value can be replaced with any value
             // chances are it does not work correctly in some cases (check through RDP) */

            var ip = String.Empty;
            var remoteIsolatedStorageFile = _remoteApplication.GetIsolatedStore("Local");
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

        public void Disconnect()
        {
            // Terminate application
            Console.WriteLine("Terminating the application...");
            _remoteApplication.TerminateRunningInstances();

            Thread.Sleep(3000);
            Console.WriteLine("\nDone!");

            // Disconnect from the emulator
            Console.WriteLine("Disconnecting Device...");
            _iDevice.Disconnect();
            Console.WriteLine("\nDone!");
        }

    }
}
