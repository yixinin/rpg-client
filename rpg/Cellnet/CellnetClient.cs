using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using pb = global::Google.Protobuf;

namespace rpg.Cellnet
{
    public class CellnetClient
    {
        private StreamWebSocket _websocket;
        public StreamWebSocket Websocket
        {
            get
            {
                return this._websocket;
            }
            set
            {
                this._websocket = value;
            }
        }

        public string Address { get; set; }

        private bool _isConnected;
        public bool IsConnected { get { return _isConnected; } }



        [ComVisible(true)]
        public delegate void OnClosedEventHandler(IWebSocket sender, WebSocketClosedEventArgs args);
        public event OnClosedEventHandler OnClosed;



        [ComVisible(true)]
        public delegate void OnMessageEventHandler(IWebSocket sender, WebSocketMessageEventArgs args);

        public event OnMessageEventHandler OnMessage;

        public CellnetClient(string address)
        {
            this._websocket = new StreamWebSocket();
            this.Address = address;
        }


        public async Task ConnectAsync()
        {
            if (this._websocket == null)
            {
                this._websocket = new StreamWebSocket();
            }
            this._websocket.Closed += _websocket_Closed;
            if (this.Address == "")
            {
                return;
            }

            try
            {
                Task task = this._websocket.ConnectAsync(new Uri(this.Address)).AsTask();
                task.ContinueWith(_ =>
                {
                    Task.Run(() =>
                    {
                        ReceiveMessage();
                    });
                });
                this._isConnected = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("socket connect fail, ex:", ex);
                this._isConnected = false;
            }

        }

        private void _websocket_Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            this._isConnected = false;
            OnClosed?.Invoke(sender, args);
        }

        public void Close()
        {
            this._websocket.Close(1000, "");
        }

        public void Dispose()
        {
            this._websocket.Dispose();
        }

        public async Task Send<T>(T message) where T : pb::IMessage<T>
        {
            try
            {
                byte[] data = new byte[message.CalculateSize()];
                using (var stream = new CodedOutputStream(data))
                {
                    message.WriteTo(stream);
                    var bs = new byte[data.Length + 2];

                    var hashid = Utils.StringHash(message.GetType().FullName.ToLower());
                    var ids = Utils.IntToBitConverter(hashid);
                    for (var i = 0; i < ids.Length; i++)
                    {
                        bs[i] = ids[i];
                    }

                    for (var i = 2; i < bs.Length; i++)
                    {
                        bs[i] = data[i - 2];
                    }

                    using (var dataWriter = new DataWriter(this._websocket.OutputStream))
                    {
                        dataWriter.WriteBytes(bs);
                        await dataWriter.StoreAsync();
                        dataWriter.DetachStream();
                    }
                }
            }
            catch (Exception ex)
            {
                Windows.Web.WebErrorStatus webErrorStatus = Windows.Networking.Sockets.WebSocketError.GetStatus(ex.GetBaseException().HResult);
                Debug.WriteLine("send message ex: " + webErrorStatus.ToString(), ex);
            }
        }


        private async void ReceiveMessage()
        {
            try
            {
                using (var dataReader = new DataReader(this._websocket.InputStream))
                {
                    dataReader.InputStreamOptions = InputStreamOptions.Partial;
                    while (true)
                    {
                        await dataReader.LoadAsync(256);
                        byte[] message = new byte[dataReader.UnconsumedBufferLength];
                        dataReader.ReadBytes(message);
                        var args = new WebSocketMessageEventArgs();

                        var ids = new byte[2];
                        if (message.Length > 2)
                        {
                            ids[0] = message[0];
                            ids[1] = message[1];
                        }
                        args.MessaeId = BitConverter.ToUInt16(ids, 0);

                        args.Message = new byte[message.Length - 2];
                        for (var i = 2; i < message.Length; i++)
                        {
                            args.Message[i - 2] = message[i];
                        }

                        MainPage.Current.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            OnMessage?.Invoke(_websocket, args);
                        });

                    }

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ReceiveMessage ex:", ex);
            }
            //}

        }
    }

    public class WebSocketMessageEventArgs
    {
        public UInt16 MessaeId { get; set; }
        public byte[] Message { get; set; }
    }
}
