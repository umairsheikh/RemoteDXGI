using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.LiveControl
{
    public class MouseKeyboardState:ISerializable
    {
       // Define class for mousekeyboard states to generate clicks and curson position and key strokes recvd
        string data { get; set; }

        //public Screenshot() { }

      
        /// <summary>
        /// Writes the contents of this type to the transport.
        /// </summary>
        public void WritePayload(NetOutgoingMessage message)
        {
            message.Write(data);
            //message.Write(Region.X);
            //message.Write(Region.Y);
            //message.Write(Region.Width);
            //message.Write(Region.Height);
            //message.Write(Number);
        }

        /// <summary>
        /// Reads the transport into the contents of this type.
        /// </summary>
        public void ReadPayload(NetIncomingMessage message)
        {
            data = message.ReadString();
            //Region = new Rectangle(message.ReadInt32(), message.ReadInt32(), message.ReadInt32(), message.ReadInt32());
            //Number = message.ReadUInt32();
        }
    }
}
