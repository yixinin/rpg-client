using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pb = global::Google.Protobuf;

namespace rpg.Cellnet
{
    public static class Message
    {
        public static Dictionary<UInt16, Type> messages { get; set; }


        public static void InitMessageIds()
        {
            messages = new Dictionary<ushort, Type>();
            {
                var t = typeof(Protocol.LoginAck);
                messages[Utils.StringHash(t.FullName.ToLower())] = t;
            }

            {
                var t = typeof(Protocol.LogoutAck);
                messages[Utils.StringHash(t.FullName.ToLower())] = t;
            }

            {
                var t = typeof(Protocol.GetGameRoomTypeListAck);
                messages[Utils.StringHash(t.FullName.ToLower())] = t;
            }

        }

        public static T Frombytes<T>(byte[] dataBytes) where T : IMessage, new()
        {
            CodedInputStream stream = new CodedInputStream(dataBytes);
            T msg = new T();
            stream.ReadMessage(msg);
            return msg;
        }

    }
}
