using System;
using System.Linq;
using System.Threading;
using Microsoft.Phone.Tools.Deploy; // Library needed to connect to the Windows Phone X Emulator


namespace OuterDriver.EmulatorHelpers
{
    /// <summary>
    /// App Deploy for 8.1 or greater (uses  Microsoft.Phone.Tools.Deploy shipped with Microsoft SDKs\Windows Phone\v8.1\Tools\AppDeploy)
    /// </summary>
    /// TODO: do not copy Microsoft.Phone.Tools.Deploy assembly on build. Set Copy Local to false and use specified path to assembly.
    public class Deployer81 : IDeployer
    {
        private readonly DeviceInfo _deviceInfo;

        public string DeviceName
        {
            get { return _deviceInfo != null ? _deviceInfo.ToString() : string.Empty; }
        }

        public Deployer81(string desiredDevice)
        {
            var devices = Utils.GetDevices();

                _deviceInfo = devices.FirstOrDefault(x => x.ToString().StartsWith(desiredDevice) && !x.ToString().Equals("Device")); // Exclude device
            if (_deviceInfo == null)
            {
                Console.WriteLine("Desired target "+desiredDevice+" not found. Using default instead.");

                _deviceInfo = devices.First(x => !x.ToString().Equals("Device"));
            }
            Console.WriteLine("Deploy target: " + _deviceInfo);
        }

        public void Deploy(string appPath)
        {
            
            var appManifestInfo = Utils.ReadAppManifestInfoFromPackage(appPath);

            GlobalOptions.LaunchAfterInstall = true;
            Utils.InstallApplication(_deviceInfo, appManifestInfo, DeploymentOptions.None, appPath);

            Console.WriteLine("Successfully deployed using Microsoft.Phone.Tools.Deploy");
        }

        public String ReceiveIpAddress()
        {
            // Windows Phone 8.1 emulators use same ip as host http://social.msdn.microsoft.com/Forums/sqlserver/en-US/8902939b-233f-4075-99c3-5856f7e6ca6e/windows-phone-81-emulator-no-longer-uses-dhcp?forum=wpdevelop
            // Driver will use host ip if empty string is returned
            return string.Empty;
        }

        public void Disconnect()
        {
            // Utils.InstallApplication automatically disconnects after deployment
        }

    }
}
