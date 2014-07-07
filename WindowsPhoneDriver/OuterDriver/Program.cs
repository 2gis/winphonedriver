namespace OuterDriver
{
    using System;

    internal class Program
    {
        #region Methods
        [STAThreadAttribute]
        private static void Main(string[] args)
        {
            const int ListeningPort = 9999;
            var listener = new Listener(ListeningPort);

            listener.StartListening();
        }

        #endregion
    }
}
