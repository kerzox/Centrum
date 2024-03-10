using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static CentrumConsole.minecraft.MinecraftServer;

namespace CentrumConsole.server {
    internal class CentrumWebSocket {

        internal class SocketPacket {
            public string eventKey { get; set; }
            public object data { get; set; }

        }

        internal class Client {

            private CentrumWebSocket host;
            public User User { get; set; }

            public HttpListenerWebSocketContext context { get; private set; }
            public Guid id { get; private set; }

            public Client(SocketServer host, HttpListenerWebSocketContext context, Guid id) {
                this.host = host;
                this.context = context;
                this.id = id;
            }

            public void To(string eventKey, object data) {
                try {
                    host.sendData(context.WebSocket, eventKey, data);
                }
                catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }
            }

            public void addToRoom(string roomName) {
                if (!this.host.roomMap.ContainsKey(roomName)) { return; }
                this.host.roomMap[roomName].Add(this);
            }

            public void removeFromRoom(string roomName) {
                if (!this.host.roomMap.ContainsKey(roomName)) { return; }
                this.host.roomMap[roomName].Remove(this);
            }

            public void clearRooms() {
                foreach (var key in host.roomMap.Keys) {
                    this.removeFromRoom(key);
                }
            }

        }

        internal class SocketEvent : EventArgs {
            public Client? client { get; set; }
            public string EventKey { get; }
            public object data { get; }

            public SocketEvent(string eventKey, object data, Client? client) {
                EventKey = eventKey;
                this.data = data;
                this.client = client;
            }
        }
        internal class EventSystem {

            public event EventHandler<SocketEvent> OnEvent;

            public void Emit(string eventKey, Object args, Client? client) {
                // Console.WriteLine($"Event emitted: {eventKey}");

                // Check if there are subscribers to the event
                OnEvent?.Invoke(this, new SocketEvent(eventKey, args, client));
            }

            public void Emit(string eventKey, Client? client) {
                // Console.WriteLine($"Event emitted: {eventKey}");

                // Check if there are subscribers to the event
                OnEvent?.Invoke(this, new SocketEvent(eventKey, "", client));
            }

            public void Listen(string eventKey, EventHandler<SocketEvent> handler) {
                // Subscribe the handler to the event
                this.OnEvent += (sender, args) => {
                    if (args.EventKey == eventKey) {
                        handler?.Invoke(sender, args);
                    }
                };
            }


        }

        public Dictionary<string, HashSet<Client>> roomMap { get; private set; } = new Dictionary<string, HashSet<Client>>();
        public List<Client> clients { get; private set; } = new List<Client>();

        public Client GetClient(string uuid) {
            return clients.FirstOrDefault(client => client.id.ToString().Equals(uuid));
        }

        public EventSystem eventSys { get; private set; } = new EventSystem();
        public void Listen(string eventKey, EventHandler<SocketEvent> handler) {
            eventSys.Listen(eventKey, handler);
        }

        public virtual void sendData(WebSocket socket, string eventkey, object data) {
            string json = JsonSerializer.Serialize(new {
                eventKey = eventkey,
                data
            });
            byte[] responseBytes = Encoding.UTF8.GetBytes(json);
            var buffer = new ArraySegment<byte>(responseBytes);
            socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public void createRoom(string roomName) {
            if (roomMap.ContainsKey(roomName)) { return; }
            this.roomMap.Add(roomName, new HashSet<Client>());
        }

        public void Broadcast(string eventKey, object data) {
            clients.ForEach(client => client.To(eventKey, data));
        }

        public void RoomBroadcast(string roomKey, string eventKey, object data) {
            foreach (var client in this.roomMap[roomKey]) {
                client.To(eventKey, data);
            }
        }

    }
}
