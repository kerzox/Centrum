using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CentrumConsole.server {

    internal class SocketServer(string url) : CentrumWebSocket {

        private string url = url;

        public HttpListener? instance { get; private set; }

        public async Task Start() {
            try {

                this.instance = new HttpListener();
                instance.Prefixes.Add($"{this.url}");
                instance.Start();

                //this.eventSys.Emit("server_start", new { url = this.url });

                Console.WriteLine("Centrum server is starting\nWaiting for connections");

                while (true) {
                    var context = await instance.GetContextAsync();

                    if (context.Request.IsWebSocketRequest) {
                        var socket = await context.AcceptWebSocketAsync(subProtocol: null);
                        var socketId = Guid.NewGuid();

                        // add the client to the list

                        Client client = new Client(this, socket, socketId);
                        clients.Add(client);
                        Console.WriteLine("client connected");
                        _ = Task.Run(() => HandleSocketAsync(client, socketId));

                    }
                    else {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

        }

        public void Stop() {
            if (instance != null && instance.IsListening) {
                instance.Stop();
                instance.Close();
            }
        }


        private async Task HandleSocketAsync(Client client, Guid socketId) {

            WebSocket socket = client.context.WebSocket;

            while (socket.State == WebSocketState.Open) {
                List<ArraySegment<byte>> buffer = new List<ArraySegment<byte>>();
                buffer.Add(new ArraySegment<byte>(new byte[1024]));
                WebSocketReceiveResult result = await socket.ReceiveAsync(buffer[0], CancellationToken.None);
                int messageLength = result.Count;
                while (!result.EndOfMessage) {
                    buffer.Add(new ArraySegment<byte>(new byte[1024]));
                    result = await socket.ReceiveAsync(buffer[buffer.Count - 1], CancellationToken.None);
                    messageLength += result.Count;
                }

                // create a new buffer based on total size of all segments
                int totalLength = buffer.Sum(seg => seg.Count);
                byte[] resultBuffer = new byte[totalLength];

                int index = 0;

                for (int i = 0; i < buffer.Count; i++) {

                    ArraySegment<byte> buf = buffer[i];

                    foreach (var b in buf) {
                        resultBuffer[index++] = b;
                    }
                }

                if (result.MessageType == WebSocketMessageType.Text) {
                    string receivedMessage = Encoding.UTF8.GetString(resultBuffer, 0, messageLength);
                    try {
                        SocketPacket packet = JsonSerializer.Deserialize<SocketPacket>(receivedMessage);
                        eventSys.Emit(packet.eventKey, packet.data, client);
                    }
                    catch (Exception ex) {
                        Console.WriteLine(ex.Message);
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close) {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
            }
            Console.WriteLine("client disconnected");
            client.clearRooms();
            this.clients.Remove(client);
        }

    }
}
