namespace OuterDriver.EmulatorHelpers
{
    using System;

    internal interface IDeployer
    {
        #region Public Properties

        string DeviceName { get; }

        #endregion

        #region Public Methods and Operators

        void Deploy(string appPath);

        void Disconnect();

        string ReceiveIpAddress();

        #endregion
    }
}
