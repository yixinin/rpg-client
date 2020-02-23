using Google.Protobuf;
using ProtoBuf;
using Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace rpg
{


    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Cellnet.CellnetClient socket;
        private string token = "";

        public static MainPage Current { get; set; }
        public MainPage()
        {
            this.InitializeComponent();
            Current = this;
            Cellnet.Message.InitMessageIds();
        }

        private void connect_Click(object sender, RoutedEventArgs e)
        {


            var url = wsHost.Text;

            this.socket = null;
            switch (wsItem.SelectedIndex)
            {
                case 0:
                    url = (wsItem.ContainerFromIndex(0) as ComboBoxItem).Content.ToString();
                    break;
                case 1:
                    url = (wsItem.ContainerFromIndex(1) as ComboBoxItem).Content.ToString();
                    break;
            }
            this.socket = new Cellnet.CellnetClient(url);





            this.socket.OnClosed += WebSocket_Closed;
            socket.OnMessage += Socket_OnMessage;

            this.socket.ConnectAsync();

        }

        private void Socket_OnMessage(Windows.Networking.Sockets.IWebSocket sender, Cellnet.WebSocketMessageEventArgs args)
        {
            Debug.WriteLine(args.MessaeId);
            var t = Cellnet.Message.messages[args.MessaeId];
            if (t.FullName == typeof(Protocol.LoginAck).FullName)
            {
                var msg = Protocol.LoginAck.Parser.ParseFrom(args.Message);
                Debug.WriteLine(msg.Token);
                this.token = msg.Token;
            }
            else if (t.FullName == typeof(GetGameRoomTypeListAck).FullName)
            {
                var msg = GetGameRoomTypeListAck.Parser.ParseFrom(args.Message);
                if (msg.Items != null)
                {
                    foreach (var item in msg.Items)
                    {
                        Debug.Write(item.GameType);
                        Debug.WriteLine(item.GameInfo);
                    }
                }
            }
        }

        private void WebSocket_Closed(Windows.Networking.Sockets.IWebSocket sender, Windows.Networking.Sockets.WebSocketClosedEventArgs args)
        {
            Debug.WriteLine("WebSocket_Closed; Code: " + args.Code + ", Reason: \"" + args.Reason + "\"");
            if (args.Code == 1000)
            {
                return;
            }
            Task.Run(() =>
            {
                while (!this.socket.IsConnected)
                {

                    Debug.WriteLine("socket try to reconnect after 10 seconds !");
                    Task.Delay(1000 * 10);
                    this.socket.ConnectAsync();
                }
            });
        }


        private async Task Login(string userName, string password)
        {
            var loginReq = new LoginReq();
            loginReq.UserName = userName;
            loginReq.Password = password;
            loginReq.LoginType = 1;
            loginReq.Register = true;
            try
            {

                await this.socket.Send(loginReq);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("login fail, ex:", ex);
            }
        }

        private async Task GetGameTypes(int gameId)
        {
            var req = new GetGameRoomTypeListReq();
            req.GameId = gameId;
            req.Header = new ReqHeader ();
            req.Header.Token = this.token;

            try
            {

                await this.socket.Send(req);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GetGameTypes fail, ex:", ex);
            }
        }





        async private void login_Click(object sender, RoutedEventArgs e)
        {
            await Login("yixin", "asd");
           
        }

        async private void gameTypes_Click(object sender, RoutedEventArgs e)
        {
            await GetGameTypes(10001);
        }
    }
}
