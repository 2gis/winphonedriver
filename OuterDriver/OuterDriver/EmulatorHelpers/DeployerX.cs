// Libraries needed to connect to the Windows Phone X Emulator
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Phone.Tools.Deploy;
using Microsoft.SmartDevice.Connectivity.Interface;
using Microsoft.SmartDevice.MultiTargeting.Connectivity;
using OuterDriver.AutomationExceptions;


namespace OuterDriver.EmulatorHelpers
{
    public class DeployerX : IDeployer
    {

        private readonly IDevice _iDevice;
        private IRemoteApplication _remoteApplication;

        public string DeviceName
        {
            get { return _iDevice != null ? _iDevice.Name : string.Empty; }
        }

        public DeployerX(string desiredDevice)
        {
            Console.WriteLine("Deploy using Microsoft.Phone.Tools.Deploy");
            // TODO: Replace fixed current LCID with locale from desired capabilities if possible
            var connectivity = new MultiTargetingConnectivity(CultureInfo.CurrentUICulture.LCID);
            var devices = connectivity.GetConnectableDevices(true);
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
            var devInfo = Utils.GetDevices().FirstOrDefault(x => x.ToString().StartsWith(_iDevice.Name));
            var appManifestInfo = Utils.ReadAppManifestInfoFromPackage(appPath);

            GlobalOptions.LaunchAfterInstall = true;
            Utils.InstallApplication(devInfo, appManifestInfo, DeploymentOptions.None, appPath);
            _remoteApplication = _iDevice.GetApplication(appManifestInfo.ProductId);

            Thread.Sleep(launchDelay);
            Console.WriteLine("Successfully deployed using Microsoft.Phone.Tools.Deploy");
        }

        public String ReceiveIpAddress()
        {
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
            if (remoteIsolatedStorageFile.FileExists(sourceDeviceFilePath))
            {
                remoteIsolatedStorageFile.ReceiveFile(sourceDeviceFilePath, targetDesktopFilePath, true);
                using (var sr = new StreamReader(targetDesktopFilePath))
                {
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
