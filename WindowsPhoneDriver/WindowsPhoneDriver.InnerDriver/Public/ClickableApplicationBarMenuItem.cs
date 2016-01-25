namespace WindowsPhoneDriver.InnerDriver.Public
{
    using System;
    using Microsoft.Phone.Shell;

    public class ClickableApplicationBarMenuItem : ApplicationBarMenuItem
    {
        public new event EventHandler Click;

        public void RaiseClick()
        {
            if (this.Click != null)
            {
                this.Click(this, EventArgs.Empty);
            }
        }
    }
}
