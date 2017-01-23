using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace Network.Messages.LiveControl
{
    public class RequestScreenshotMessage : NovaMessage
    {
        public int MTU { get; set; } //Message transfer Units
        public int ImageQuality { get; set; } //From 1 to 5. best to worst
        public RequestScreenshotMessage(int mtu, int IQuality)
            : base((ushort) CustomMessageType.RequestScreenshotMessage)
        {
            MTU = mtu;
            ImageQuality = IQuality;
        }

        public RequestScreenshotMessage()
            : base((ushort)CustomMessageType.RequestScreenshotMessage)
        {}
        public override void WritePayload(NetOutgoingMessage message)
        {
            base.WritePayload(message);
            message.Write(MTU);
            message.Write(ImageQuality);

        }

        public override void ReadPayload(NetIncomingMessage message)
        {
            base.ReadPayload(message);
            MTU = message.ReadInt32();
            ImageQuality = message.ReadInt32();
        }
    }
}
