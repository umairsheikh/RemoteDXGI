using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace Network.Messages.LiveControl
{
    /// <summary>
    /// 
    /// </summary>
    public class MouseKeyboardNotification : NovaMessage
    {

        public String data { get; set; }
        public MouseKeyboardNotification()
            : base((ushort)CustomMessageType.MouseKeyboardStateMessage)
        {
        }

        public override void WritePayload(NetOutgoingMessage message)
        {
            base.WritePayload(message);
            message.Write(data);
         
        }

        public override void ReadPayload(NetIncomingMessage message)
        {
            base.ReadPayload(message);
            
            data = message.ReadString();
            //message.ReadBytes(message.LengthBytes, data);
            //message.ReadBytes(message.LengthBytes, data); 
            //message.ReadBytes(message.LengthBytes, data);

        }
    }
}
