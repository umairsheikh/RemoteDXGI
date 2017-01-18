using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Network;
using Network.Messages.LiveControl;
using Providers.LiveControl;
using Providers.LiveControl.Client;

namespace Managers.LiveControl.Client
{
    public class LiveControlManager : Manager<LiveControlProvider>
    {
        public LiveControlManager(NetworkPeer network)
            : base(new LiveControlProvider(network))
        {
            Provider.OnScreenshotReceived += (s, e) => { if (OnScreenshotReceived != null) OnScreenshotReceived(s, e); };
            Provider.OnMouseKeyboardEventReceived += (s, e) => { if (OnMouseKeyboardEventReceived != null) OnMouseKeyboardEventReceived(s, e); };
          
        }

       

        public void RequestScreenshot()
        {
            Network.SendMessage(new RequestScreenshotMessage());
        }

        public event EventHandler<OnMouseKeyboardEventArgs> OnMouseKeyboardEventReceived;
        public event EventHandler<ScreenshotMessageEventArgs> OnScreenshotReceived;
    }
}
