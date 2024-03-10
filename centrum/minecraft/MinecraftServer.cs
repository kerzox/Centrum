using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using WinFormsApp1.minecraft;
using static CentrumConsole.minecraft.MinecraftServer;
using static CentrumConsole.minecraft.MinecraftServerEvent;

namespace CentrumConsole.minecraft {

    internal class MinecraftServerEvent : EventArgs {

        public class MinecraftServerEventArgument {
            public MinecraftServer instance { get; set; }
        }

        public class ServerCrashEvent : MinecraftServerEventArgument {
 
            public string reason { get; set; }
        }
        public class ConsoleEvent : MinecraftServerEventArgument {
            public string log { get; set; }
        }

        public class PlayerEvent : MinecraftServerEventArgument {
            public string playerName { get; set; }
        }

        public EventKeys EventKey { get; }
        public MinecraftServerEventArgument Args { get; }
        public MinecraftServerEvent(EventKeys eventKey, MinecraftServerEventArgument args) {
            EventKey = eventKey;
            Args = args;
        }

    }

    internal class MinecraftServer {

        public enum EventKeys {
            STOPPED,
            START,
            PLAYER_JOIN,
            PLAYER_LEAVE,
            CRASH,
            CONSOLE
        }

        public struct InstanceInformation {
            public string name { get; set; }
            public string state { get; set; }
            public string? ops { get; set; }
            public string? bannedPlayers { get; set; }
            public string[]? console_log { get; set; }
        }

        public string name { get; }
        public string eulaPath { get; }
        public string serverProperties { get; }
        public string opsJson { get; }
        public string bannedPlayers { get; }
        public string bannedIps { get; }
        public string whitelist { get; }
        public string settings { get; }
        public int memory { get; set; }
        public string directory { get; }
        public Process? serverProcess { get; private set; }
        public bool running { get; private set; }
        public bool initalizing { get; set; }


        public event EventHandler<MinecraftServerEvent>? OnEvent;

        private List<EventHandler<MinecraftServerEvent>> subscribedEvents = new List<EventHandler<MinecraftServerEvent>>();

        internal MinecraftServer(string instanceName, string directoryPath) {
            directory = directoryPath;
            name = instanceName;
            eulaPath = Path.Combine(directoryPath, "eula.txt");
            serverProperties = Path.Combine(directoryPath, "server.properties");
            opsJson = Path.Combine(directoryPath, "ops.json");
            bannedPlayers = Path.Combine(directoryPath, "banned-players.json");
            bannedIps = Path.Combine(directoryPath, "banned-ips.json");
            whitelist = Path.Combine(directoryPath, "whitelist.json");
            settings = Path.Combine(directoryPath, "settings.txt");
            memory = 1024;
        }

        public void Start() {
            string serverPath = Path.Combine(directory, "server.jar");

            if (running) return;
            if (Directory.Exists(settings)) memory = int.Parse(MinecraftDirectoryHelper.GetProperty(settings, "memory"));

            running = true;
            Emit(EventKeys.START, new MinecraftServerEventArgument { instance = this });

            try {
                Directory.SetCurrentDirectory(directory);

                // Create a new process start info
                ProcessStartInfo startInfo = new ProcessStartInfo {
                    FileName = "java",
                    Arguments = $"-Xmx{memory}M -Xms{memory}M -jar \"{serverPath}\" nogui",
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                // Start the process
                serverProcess = new Process { StartInfo = startInfo };
                serverProcess.OutputDataReceived += (sender, e) => {
                    Emit(EventKeys.CONSOLE, new ConsoleEvent { instance = this, log = e.Data });

                    // emit on player join
                    checkForPlayerJoin(e.Data);

                    // emit on player leave
                    checkForPlayerLeave(e.Data);

                };
                serverProcess.Start();
                serverProcess.BeginOutputReadLine();
                serverProcess.WaitForExit();
            }
            catch (Exception ex) {
                Console.WriteLine($"Error starting Minecraft server: {ex.Message}");
                Emit(EventKeys.CRASH, new ServerCrashEvent { instance = this, reason = ex.Message });
            }
            finally {
                running = false;
                Emit(EventKeys.STOPPED, new MinecraftServerEventArgument { instance = this });
                ClearListeners();
            }
        }

        public InstanceInformation GetInstanceInformation(bool all) {
            if (!all) {
                return new InstanceInformation {
                    name = this.name,
                    state = this.initalizing ? "initalizing" : this.running ? "running" : "stopped"
                };
            }


            return new InstanceInformation {
                name = this.name,
                state = this.initalizing ? "initalizing" : this.running ? "running" : "stopped",
                ops = MinecraftDirectoryHelper.ReadFileToEnd(opsJson),
                bannedPlayers = MinecraftDirectoryHelper.ReadFileToEnd(bannedPlayers),
                console_log = MinecraftDirectoryHelper.ReadLatestLog(directory)
            };
        }

        private void Emit(EventKeys eventKey, MinecraftServerEventArgument args) {
            OnEvent?.Invoke(this, new MinecraftServerEvent(eventKey, args));
        }

        public EventHandler<MinecraftServerEvent> Listen(EventKeys eventKey, EventHandler<MinecraftServerEvent> handler) {

            EventHandler<MinecraftServerEvent> listener = null;

            listener += (sender, args) => {
                if (args.EventKey == eventKey) {
                    handler?.Invoke(sender, args);
                }
            };

            this.OnEvent += listener;
            subscribedEvents.Add(listener);
            return listener;
        }

        public void ListenOnce(EventKeys eventKey, EventHandler<MinecraftServerEvent> handler) {
            EventHandler<MinecraftServerEvent> listener = null;

            listener = (sender, args) => {
                if (args.EventKey == eventKey) {
                    handler?.Invoke(sender, args);
                    this.OnEvent -= listener; 
                }
            };

            this.OnEvent += listener;
            subscribedEvents.Add(listener);
        }

        public void RemoveListener(EventHandler<MinecraftServerEvent> handler) {
            this.OnEvent -= handler;
        }

        private void ClearListeners() {
            foreach (var item in subscribedEvents)
            {
                RemoveListener(item);
            }
        }

        public void Stop() {
            try {
                // Send the "stop" command to the server console
                if (serverProcess != null && !serverProcess.HasExited) {
                    serverProcess.StandardInput.WriteLine("stop");
                    running = false;
                }
                else {
                    Console.WriteLine("Minecraft server process is not running or has already exited.");
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Error stopping Minecraft server: {ex.Message}");
            }
        }

        public void SendCommand(string command) {
            try {
                if (serverProcess != null && !serverProcess.HasExited && running) {
                    serverProcess.StandardInput.WriteLine(command);
                }
                else {
                    Console.WriteLine("Minecraft server process is not running");
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Error running {command}, {ex.Message}");
            }
        }

        public void Kill() {

            if (serverProcess != null && !serverProcess.HasExited) {
                serverProcess.Kill();
            }
        }

        public void Shutdown() {
       
            if (serverProcess != null && !serverProcess.HasExited) {
                // try to stop safely
                Stop();

                // wait ten seconds
                serverProcess.WaitForExit(10000);

                if (!serverProcess.HasExited) {
                    Kill();
                }
            }
        }

        private void checkForPlayerLeave(string data) {
            string pattern = @"^\[\d+:\d+:\d+] \[Server thread/INFO]: (\w+) left the game";
            if (data == null) { return; }

            Match match = Regex.Match(data, pattern);

            if (match.Success) {
                string username = match.Groups[1].Value;
                Emit(EventKeys.PLAYER_LEAVE, new PlayerEvent { instance = this, playerName = username });
            }
        }

        private void checkForPlayerJoin(string data) {

            string pattern = @"^\[\d+:\d+:\d+] \[Server thread/INFO]: (\w+) joined the game";
            if (data == null) { return; }

            Match match = Regex.Match(data, pattern);

            if (match.Success) {
                string username = match.Groups[1].Value;
                Emit(EventKeys.PLAYER_JOIN, new PlayerEvent { instance = this, playerName = username });
            }
        }

        public object SPLPing() {

            try {
                int port = 25565;
                port = int.Parse(MinecraftDirectoryHelper.GetProperty(serverProperties, "server-port"));

                // TODO replace the ip with actual supplied ip if we change it
                TcpClient tcpClient = new TcpClient("127.0.0.1", port);

                // Get the network stream for reading and writing
                NetworkStream networkStream = tcpClient.GetStream();

                using (MemoryStream stream = new MemoryStream()) {
                    // packet id
                    byte[] packetId = EncodeVarint(0);
                    int additional = packetId.Length;

                    // handshake
                    stream.Write(EncodeVarint(765));
                    byte[] host = Encoding.UTF8.GetBytes("localhost");
                    stream.Write(EncodeVarint(host.Length));
                    stream.Write(host);
                    stream.Write(BitConverter.GetBytes((ushort)25565));
                    stream.Write(EncodeVarint(1));


                    byte[] handshake = stream.ToArray();
                    byte[] length = EncodeVarint(handshake.Length + additional);
                    // buffer length
                    networkStream.Write(length, 0, length.Length);
                    // packet id
                    networkStream.Write(packetId, 0, packetId.Length);
                    // handshake
                    networkStream.Write(handshake, 0, handshake.Length);
                    // status 
                    byte[] empty = new byte[0];
                    byte[] status = EncodeVarint(0);
                    byte[] statusLength = EncodeVarint(status.Length);
                    networkStream.Write(statusLength, 0, statusLength.Length);
                    networkStream.Write(status, 0, status.Length);
                    networkStream.Write(empty, 0, empty.Length);
                }


                // Read the message from the server
                byte[] responseData = new byte[Int16.MaxValue];
                int bytesRead = networkStream.Read(responseData, 0, responseData.Length);

                int offset = 0;

                var rLength = DecodeVarint(responseData, ref offset);
                var packet = DecodeVarint(responseData, ref offset);
                var jsonLength = DecodeVarint(responseData, ref offset);

                var data = new byte[jsonLength];
                Array.Copy(responseData, offset, data, 0, jsonLength);
                offset += jsonLength;
                string str = Encoding.UTF8.GetString(data);
                networkStream.Close();
                tcpClient.Close();
                return str;
            }
            catch (Exception e) {
                return new { error = true };
            }
        }


        private byte[] EncodeVarint(int num) {
            using (MemoryStream memoryStream = new MemoryStream()) {
                while (num >= 128) {
                    memoryStream.WriteByte((byte)(num & 0x7F | 0x80));
                    num >>= 7;
                }
                memoryStream.WriteByte((byte)num);
                return memoryStream.ToArray();
            }
        }

        private int DecodeVarint(byte[] buf, ref int offset) {
            int result = 0;
            int shift = 0;

            for (int i = offset; i < buf.Length; i++) {
                result |= (buf[i] & 0x7F) << shift;
                shift += 7;
                offset += 1;

                if ((buf[i] & 0x80) == 0) { // msb is 0 so we are finished
                    return result;
                }
            }

            return result;
        }

    }
}
