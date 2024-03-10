using CentrumConsole.server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WinFormsApp1.minecraft;

namespace CentrumConsole.minecraft {
    internal class MinecraftHandler {

        public static Dictionary<string, MinecraftServer> serverLists = new Dictionary<string, MinecraftServer>();

        public static void LoadAllInstances() {
            // load instances
            MinecraftDirectoryHelper.findAllInstances();
            // create instances
            foreach (var item in MinecraftDirectoryHelper.instances) {
                string key = item.Key;
                DirectoryInfo dir = item.Value;
                if (dir.Exists && !serverLists.ContainsKey(key)) {
                    LoadInstance(dir);
                }
            }
        }

        private static void LoadInstance(DirectoryInfo directory) {
            MinecraftServer server = new MinecraftServer(directory.Name, directory.FullName);
            serverLists.Add(directory.Name, server);
        }

        public static void SendInstancesToAuthorisedClient(CentrumWebSocket.Client client) {
            List<object> list = new List<object>();

            if (client.User == null) return;

            foreach (var kvp in serverLists) {
                string name = kvp.Key;
                MinecraftServer server = kvp.Value;

                if (!client.User.assignedInstances.Contains(name) && !client.User.assignedInstances.Contains("*")) { continue; }

                list.Add(new {
                    instanceName = name,
                    serverStatus = server.initalizing ? "initalizing" : server.running ? "running" : "stopped"
                });
            }

            if (list.Count > 0) {
                client.To("grab_instances", new {
                    instances = JsonSerializer.Serialize(list)
                });
            }
        }

        public static void BroadcastInstancesToAuthorisedClients(CentrumWebSocket socket) {

            foreach (var client in socket.clients)
            {
                SendInstancesToAuthorisedClient(client);
            }
        }

        // TODO fix this to work again
        public static double GetMachineMemory() {
            return 3.2e+10 / (1024.0 * 1024.0);
        }

    }
}
