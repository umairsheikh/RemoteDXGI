using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Network;
using Providers.LiveControl.Server;
using Network.Messages.LiveControl;
using Providers.LiveControl;
using Providers.LiveControl.Client;

namespace Managers.LiveControl.Server
{
    public class LiveControlManager : Manager<LiveControllerProvider8>
    {
        public LiveControlManager(NetworkPeer network)
            : base(new LiveControllerProvider8(network))
        {
            //Provider.OnMouseKeyboardEventReceived += (s, e) => { if (OnMouseKeyboardEventReceived != null) OnMouseKeyboardEventReceived(s, e); };
            Provider.OnMouseKeyboardEventReceived += (s, e) => { if (OnMouseKeyboardEventReceived != null) OnMouseKeyboardEventReceived(s, e); };
          
        }

        public void RequestMouseKeyboardStates()
        {
            Network.SendMessage(new MouseClickMessage());
            Network.SendMessage(new KeyPressMessage());
            
        }
        //public event EventHandler<OnMouseKeyboardEventArgs> OnMouseKeyboardEventReceived;
        public event EventHandler<MouseKeyboardNotification> OnMouseKeyboardEventReceived;
    }
}
