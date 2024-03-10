using CentrumConsole.minecraft;
using CentrumConsole.server.JsonStructures;
using Microsoft.IdentityModel.JsonWebTokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WinFormsApp1.minecraft;
using InstanceInformation = CentrumConsole.server.JsonStructures.InstanceInformation;

namespace CentrumConsole.server {
    internal class ServerManager {

        private CentrumWebSocket socket { get; set; }
        private UserHandler userHandler { get; set; }

        public void ListenTo(CentrumWebSocket server) {
            this.socket = server;
            this.userHandler = new UserHandler();

            foreach (var item in MinecraftHandler.serverLists) {
                string name = item.Key;

                // create rooms from the instance names
                socket.createRoom(name);

            }

            // login event
            LoginEvent();
            Reauthenticate();

            // instance events

            // data manipulation or IO
            CreateInstance();
            RemoveInstance();
            InstanceState();
            InstanceCommandInput();


            // information grabbing
            GrabInstances();
            ChooseInstance();
            InstanceInformation();
            InstanceSPL();

            // file manager
            FileManager();
            FileManagerListFiles();
            SaveFile();
            GetFile();
            FileInterationEvent();
        }

        private void CheckInstanceValidity(string instanceName, Action? action) {
            if (instanceName == null || !MinecraftDirectoryHelper.hasInstanceByName(instanceName) || !MinecraftHandler.serverLists.ContainsKey(instanceName)) {
                action?.Invoke();
                return;
            }
        }

        private void LoginEvent() {
            socket.Listen("login", (sender, evt) => {
                // call login and get send status to the client
                UserHandler.LoginStructure loginDetails = userHandler.Login(evt.data, evt.client);
                if (loginDetails.token != null) {
                    evt.client.To("login", new { status = 200, token = loginDetails.token, username = loginDetails.user.username });
                }
                else evt.client.To("login", new { status = 401 });
            });
        }

        private void Reauthenticate() {
            socket.Listen("reauthenticate", async (sender, evt) => {
                Authorisation authorisation = JsonSerializer.Deserialize<Authorisation>(evt.data.ToString());
                var status = await userHandler.ValidateClient(authorisation, evt.client);
                evt.client.To("reauthenticate", new { status });
            });
        }

        private void GrabInstances() {
            socket.Listen("grab_instances", async (sender, evt) => {
                Authorisation authorisation = JsonSerializer.Deserialize<Authorisation>(evt.data.ToString());
                int status = await userHandler.ValidateClient(authorisation, evt.client);
                if (status != 200) {
                    evt.client.To("reauthenticate", new { status });
                }
                MinecraftHandler.SendInstancesToAuthorisedClient(evt.client);
            });
        }

        private void ChooseInstance() {
            socket.Listen("choose_instance", async (sender, evt) => {
                Instance instanceObj = JsonSerializer.Deserialize<Instance>(evt.data.ToString());

                int status = await userHandler.ValidateClient(instanceObj, evt.client);
                if (status != 200) {
                    evt.client.To("reauthenticate", new { status });
                    return;
                };

                CheckInstanceValidity(instanceObj.instanceName,
                    () => evt.client.To("choose_instance", new { message = $"{instanceObj.instanceName} couldn't be found" }));

                MinecraftServer server = MinecraftHandler.serverLists[instanceObj.instanceName];

                if (server.initalizing) {
                    evt.client.To("choose_instance", new { message = $"{instanceObj.instanceName} is initalizing\nplease wait for it finish" });
                    return;
                }
                evt.client.clearRooms();
                evt.client.addToRoom(instanceObj.instanceName);
                evt.client.To("choose_instance", new { ok = true });
            });
        }

        private void LeaveInstance() {
            socket.Listen("leave_instance", (sender, evt) => {
                Instance instanceObj = JsonSerializer.Deserialize<Instance>(evt.data.ToString());
                CheckInstanceValidity(instanceObj.instanceName, null);

                MinecraftServer server = MinecraftHandler.serverLists[instanceObj.instanceName];
                // remove client from all other rooms
                evt.client.clearRooms();

            });
        }

        private void CreateInstance() {
            socket.Listen("create_instance", async (sender, evt) => {
                CreateInstance instanceObj = JsonSerializer.Deserialize<CreateInstance>(evt.data.ToString());

                if (MinecraftDirectoryHelper.hasInstanceByName(instanceObj.instanceName)) {
                    evt.client.To("create_instance", new { error = $"Instance by {instanceObj.instanceName} already exists" });
                    return;
                }

                if (MinecraftHandler.serverLists.ContainsKey(instanceObj.instanceName)) {
                    //evt.client.To("create_instance", new { message = $"Instance by {instanceObj.instanceName} already exists" });
                    Console.WriteLine("ERROR: create instance\nServer already exists but instance directory doesn't");
                    return;
                }

                int status = await userHandler.ValidateClient(instanceObj, evt.client);
                if (status != 200) {
                    evt.client.To("reauthenticate", new { status });
                    return;
                };

                string path = MinecraftDirectoryHelper.CreateNewInstance(instanceObj.instanceName);
                MinecraftHandler.LoadAllInstances();
                MinecraftServer server = MinecraftHandler.serverLists[instanceObj.instanceName];

                // set the server to initalizing so clients can't open it
                server.initalizing = true;

                _ = Task.Run(async () => {
                    // run a async task to download server files
                    // then create a socket room and start the server
                    await MinecraftDirectoryHelper.DownloadServerFiles(path);
                    socket.createRoom(instanceObj.instanceName);
                    server.Start();
                });


                server.ListenOnce(MinecraftServer.EventKeys.STOPPED, (sender, arg) => {
                    // turn initalizing off
                    server.initalizing = false;

                    var settingsPath = Path.Combine(server.directory, "settings.txt");
                    var path = Path.Combine(server.directory, "logs/latest.log");

                    try {
                        MinecraftDirectoryHelper.CreateFile(settingsPath, $"instance settings\nmemory={instanceObj.memory}\njarfile={instanceObj.jarfile}", true);
                        MinecraftDirectoryHelper.DeleteFile(path);
                        //MinecraftHelper.EditPropertyOnFile(server.eulaPath, "eula=false", "eula=true");
                        MinecraftDirectoryHelper.EditPropertyOnFile(server.serverProperties, "max-players=20", $"server-port={instanceObj.playerSlots}");
                        MinecraftDirectoryHelper.EditPropertyOnFile(server.serverProperties, "server-port=25565", $"server-port={instanceObj.port}");
                        MinecraftHandler.BroadcastInstancesToAuthorisedClients(socket);
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"ERROR: create instance\n{ex.Message}");
                    }



                });

                socket.Broadcast("create_instance", new { message = "Instance was created successfully" });
                MinecraftHandler.BroadcastInstancesToAuthorisedClients(socket);
            });
        }

        private void RemoveInstance() {
            socket.Listen("remove_instance", async (sender, evt) => {
                Instance instanceObj = JsonSerializer.Deserialize<Instance>(evt.data.ToString());

                MinecraftServer minecraftInstance = MinecraftHandler.serverLists[instanceObj.instanceName];

                int status = await userHandler.ValidateClient(instanceObj, evt.client);
                if (status != 200) {
                    evt.client.To("reauthenticate", new { status });
                    return;
                };

                // check if the instance is running
                if (minecraftInstance.running) {
                    // its still running send a failure message
                    evt.client.To("remove_instance",
                        new { error = $"Unable to delete {instanceObj.instanceName}\nThe instance is running" });
                    return;

                }

                if (minecraftInstance.serverProcess != null && !minecraftInstance.serverProcess.HasExited) {
                    // its still running send a failure message
                    evt.client.To("remove_instance",
                        new { error = $"Unable to delete {instanceObj.instanceName}\nThe instance is running" });
                    return;

                }

                // try to remove it from the server
                _ = Task.Run(() => {
                    try {
                        MinecraftDirectoryHelper.removeInstance(instanceObj.instanceName);
                        MinecraftHandler.serverLists.Remove(instanceObj.instanceName);
                        MinecraftDirectoryHelper.instances.Remove(instanceObj.instanceName);
                        socket.Broadcast("remove_instance", new { });
                        MinecraftHandler.BroadcastInstancesToAuthorisedClients(socket);

                    }
                    catch (Exception ex) {
                        evt.client.To("remove_instance", new { message = $"Unable to delete {instanceObj.instanceName}\n{ex.Message}" });
                        Console.WriteLine($"ERROR: remove_instance\n{ex.Message}");
                    }

                });

            });
        }

        private void InstanceInformation() {
            socket.Listen("instance_information", async (sender, evt) => {

                InstanceInformation instanceObj = JsonSerializer.Deserialize<InstanceInformation>(evt.data.ToString());
                if (instanceObj.instanceName == null) return;

                // instance could not be found so just redirect the client out of the dashboard
                CheckInstanceValidity(instanceObj.instanceName,
                  () => evt.client.To("redirect", new {
                      url = "",
                      message = $"{instanceObj.instanceName} is not available"
                  }));


                int status = await userHandler.ValidateClient(instanceObj, evt.client);
                if (status != 200) {
                    evt.client.To("reauthenticate", new { status });
                    return;
                };

                MinecraftServer server = MinecraftHandler.serverLists[instanceObj.instanceName];
                socket.RoomBroadcast(instanceObj.instanceName, "instance_information", server.GetInstanceInformation(instanceObj.full_information));

            });
        }

        private void InstanceCommandInput() {
            socket.Listen("instance_command", async (sender, evt) => {

                InstanceCommand instanceObj = JsonSerializer.Deserialize<InstanceCommand>(evt.data.ToString());

                CheckInstanceValidity(instanceObj.instanceName,
                  () => evt.client.To("redirect", new {
                      url = "",
                      message = $"{instanceObj.instanceName} is not available"
                  }));

                int status = await userHandler.ValidateClient(instanceObj, evt.client);
                if (status != 200) {
                    evt.client.To("reauthenticate", new { status });
                    return;
                };

                MinecraftServer server = MinecraftHandler.serverLists[instanceObj.instanceName];

                server.SendCommand(instanceObj.cmd);

                if (instanceObj.callback != null) {
                    evt.client.To("instance_command", new { ok = true });
                }

            });
        }

        private void InstanceState() {
            socket.Listen("instance_state", async (sender, evt) => {

                InstanceState instanceObj = JsonSerializer.Deserialize<InstanceState>(evt.data.ToString());

                // instance could not be found so just redirect the client out of the dashboard


                CheckInstanceValidity(instanceObj.instanceName,
                  () => evt.client.To("redirect", new {
                      url = "",
                      message = $"{instanceObj.instanceName} is not available"
                  }));

                int status = await userHandler.ValidateClient(instanceObj, evt.client);
                if (status != 200) {
                    evt.client.To("reauthenticate", new { status });
                    return;
                };


                MinecraftServer server = MinecraftHandler.serverLists[instanceObj.instanceName];

                // if our state is running and the server isn't running then start it
                if (instanceObj.state == "running" && !server.running) {

                    // list to the minecraft server events
                    // start
                    // console output
                    // stop


                    server.Listen(MinecraftServer.EventKeys.START,
                         (s, a) => {
                             socket.RoomBroadcast(instanceObj.instanceName, "console", new { output = $"[CENTRUM]: Starting instance {instanceObj.instanceName}" });
                             // broadcast new state
                             MinecraftHandler.BroadcastInstancesToAuthorisedClients(socket);
                             socket.RoomBroadcast(instanceObj.instanceName, "instance_information", server.GetInstanceInformation(false));

                         });

                    server.Listen(MinecraftServer.EventKeys.CONSOLE, (s, a) => {
                        if (a.Args is MinecraftServerEvent.ConsoleEvent consoleArgs) {
                            socket.RoomBroadcast(instanceObj.instanceName, "console", new { output = consoleArgs.log });
                        }
                    });

                    server.Listen(MinecraftServer.EventKeys.PLAYER_JOIN, (s, a) => {
                        if (a.Args is MinecraftServerEvent.PlayerEvent playerEvent) {
                            new Timer((notUsed) => {
                                socket.RoomBroadcast(instanceObj.instanceName, "get_spl", server.SPLPing());
                            }, null, 2500, Timeout.Infinite);
                        }
                    });

                    server.Listen(MinecraftServer.EventKeys.PLAYER_LEAVE, (s, a) => {
                        if (a.Args is MinecraftServerEvent.PlayerEvent playerEvent) {
                            new Timer((notUsed) => {
                                socket.RoomBroadcast(instanceObj.instanceName, "get_spl", server.SPLPing());
                            }, null, 2500, Timeout.Infinite);
                        }
                    });

                    server.Listen(MinecraftServer.EventKeys.STOPPED, (s, a) => {

                        socket.RoomBroadcast(instanceObj.instanceName,
                        "console",
                        new { output = $"[CENTRUM]: {instanceObj.instanceName} has shutdown" });

                        MinecraftHandler.BroadcastInstancesToAuthorisedClients(socket);

                        socket.RoomBroadcast(instanceObj.instanceName, "instance_information", server.GetInstanceInformation(false));
                    });

                    // start the minecraft instance

                    _ = Task.Run(() => {
                        server.Start();
                    });


                }
                else if (instanceObj.state == "stopped") {
                    server.Stop();
                }
            });

        }

        public void InstanceSPL() {
            socket.Listen("get_spl", async (sender, evt) => {
                Instance instanceObj = JsonSerializer.Deserialize<Instance>(evt.data.ToString());

                CheckInstanceValidity(instanceObj.instanceName, null);

                int status = await userHandler.ValidateClient(instanceObj, evt.client);
                if (status != 200) {
                    evt.client.To("reauthenticate", new { status });
                    return;
                };

                MinecraftServer server = MinecraftHandler.serverLists[instanceObj.instanceName];

                _ = Task.Run(() => {
                    object ret = server.SPLPing();
                    evt.client.To($"get_spl", ret);
                });

            });
        }

        private void FileManager() {

            socket.Listen("file_manager", async (sender, evt) => {

                FileManager instanceObj = JsonSerializer.Deserialize<FileManager>(evt.data.ToString());

                // instance could not be found so just redirect the client out of the dashboard
                CheckInstanceValidity(instanceObj.instanceName,
                  () => evt.client.To("redirect", new {
                      url = "",
                      message = $"{instanceObj.instanceName} is not available"
                  }));

                int status = await userHandler.ValidateClient(instanceObj, evt.client);
                if (status != 200) {
                    evt.client.To("reauthenticate", new { status });
                    return;
                };

                MinecraftServer server = MinecraftHandler.serverLists[instanceObj.instanceName];
                string path = Path.Combine(server.directory, instanceObj.relativePath);
                if (!Directory.Exists(path)) {
                    evt.client.To("redirect", new {
                        url = "/dashboard",
                        message = $"File manager fail"
                    });
                    return;
                }
                DirectoryInfo rootDirectory = new DirectoryInfo(path);

                evt.client.To($"file_manager", new {
                    content = MinecraftDirectoryHelper.GetDirectoryStructure(Path.Combine(server.directory, instanceObj.relativePath))
                });

            });

        }

        private void FileManagerListFiles() {
            socket.Listen("file_manager_files", async (sender, evt) => {

                FileManager instanceObj = JsonSerializer.Deserialize<FileManager>(evt.data.ToString());

                // instance could not be found so just redirect the client out of the dashboard
                CheckInstanceValidity(instanceObj.instanceName,
                  () => evt.client.To("redirect", new {
                      url = "",
                      message = $"{instanceObj.instanceName} is not available"
                  }));

                int status = await userHandler.ValidateClient(instanceObj, evt.client);
                if (status != 200) {
                    evt.client.To("reauthenticate", new { status });
                    return;
                };

                MinecraftServer server = MinecraftHandler.serverLists[instanceObj.instanceName];
                string path = Path.Combine(server.directory, instanceObj.relativePath);
                if (!Directory.Exists(path)) {
                    evt.client.To("redirect", new {
                        url = "/dashboard",
                        message = $"File manager fail"
                    });
                    return;
                }
                DirectoryInfo dir = new DirectoryInfo(path);

                evt.client.To($"file_manager_files", new {
                    content = dir.GetFiles().Select(f => f.Name).ToArray()
                });



            });
        }

        private void GetFile() {

            socket.Listen("get_file", async (sender, evt) => {

                FileManager instanceObj = JsonSerializer.Deserialize<FileManager>(evt.data.ToString());

                // instance could not be found so just redirect the client out of the dashboard
                CheckInstanceValidity(instanceObj.instanceName,
                  () => evt.client.To("redirect", new {
                      url = "",
                      message = $"{instanceObj.instanceName} is not available"
                  }));

                int status = await userHandler.ValidateClient(instanceObj, evt.client);
                if (status != 200) {
                    evt.client.To("reauthenticate", new { status });
                    return;
                };

                MinecraftServer server = MinecraftHandler.serverLists[instanceObj.instanceName];

                string path = Path.Combine(server.directory, instanceObj.relativePath);

                if (!File.Exists(path)) {
                    return;
                }

                string file = File.ReadAllText(path);


                evt.client.To($"get_file", new {
                    content = file
                });



            });

        }
        private void SaveFile() {
            socket.Listen("save_file", async (sender, evt) => {

                SaveFile instanceObj = JsonSerializer.Deserialize<SaveFile>(evt.data.ToString());

                // instance could not be found so just redirect the client out of the dashboard
                CheckInstanceValidity(instanceObj.instanceName,
                  () => evt.client.To("redirect", new {
                      url = "",
                      message = $"{instanceObj.instanceName} is not available"
                  }));

                int status = await userHandler.ValidateClient(instanceObj, evt.client);
                if (status != 200) {
                    evt.client.To("reauthenticate", new { status });
                    return;
                };

                MinecraftServer server = MinecraftHandler.serverLists[instanceObj.instanceName];

                string path = Path.Combine(server.directory, instanceObj.relativePath);

                if (!File.Exists(path)) {
                    evt.client.To($"save_file", new {
                        message = $"File doesn't exist"
                    });
                }

                using (StreamWriter writer = new StreamWriter(path)) {
                    writer.Write(instanceObj.content);
                }

                evt.client.To($"save_file", new {
                    message = "Successfully edited the file"
                });



            });
        }

        private void FileInterationEvent() {
            socket.Listen("file_interaction", async (sender, evt) => {

                FileInteraction instanceObj = JsonSerializer.Deserialize<FileInteraction>(evt.data.ToString());

                // instance could not be found so just redirect the client out of the dashboard
                CheckInstanceValidity(instanceObj.instanceName,
                  () => evt.client.To("redirect", new {
                      url = "",
                      message = $"{instanceObj.instanceName} is not available"
                  }));

                int status = await userHandler.ValidateClient(instanceObj, evt.client);
                if (status != 200) {
                    evt.client.To("reauthenticate", new { status });
                    return;
                };

                MinecraftServer server = MinecraftHandler.serverLists[instanceObj.instanceName];

                string path = Path.Combine(server.directory, instanceObj.relativePath);

                // check the file interation type

                switch (instanceObj.interaction) {
                    case "create":
                        // check type
                        path = Path.Combine(path, instanceObj.content);

                        if (instanceObj.type == "file") {
                            if (File.Exists(path)) {
                                evt.client.To($"file_interaction", new {
                                    error = "File already exists"
                                });
                                return;
                            }
                            try {
                                using (FileStream fs = File.Create(path)) {

                                }

                                evt.client.To($"file_interaction", new {
                                    message = $"Created a new file at {path}"
                                });
                            }
                            catch (Exception e) {
                                evt.client.To($"file_interaction", new {
                                    error = $"Failed to create file\n{e.Message}"
                                });
                            }

                            return;
                        }

                        if (Directory.Exists(path)) {
                            evt.client.To($"file_interaction", new {
                                error = "Directory already exists"
                            });
                            return;
                        }

                        try {
                            Directory.CreateDirectory(path);
                            evt.client.To($"file_interaction", new {
                                message = $"Created new directory at {path}"
                            });
                        }
                        catch (Exception e) {
                            evt.client.To($"file_interaction", new {
                                error = $"Failed to create directory\n{e.Message}"
                            });
                        }
                        break;
                    case "delete":
                        // check type
                        path = Path.Combine(path, instanceObj.content);

                        if (instanceObj.type == "file") {

                            return;
                        }

                        if (!Directory.Exists(path)) {
                            evt.client.To($"file_interaction", new {
                                error = "Directory doesn't exist"
                            });
                            return;
                        }

                        try {
                            Directory.Delete(path, true);
                            evt.client.To($"file_interaction", new {
                                message = $"Deleted directory at {path}"
                            });
                        }
                        catch (Exception e) {
                            evt.client.To($"file_interaction", new {
                                error = $"Failed to delete directory\n{e.Message}"
                            });
                        }
                        break;
                    case "rename":
                        var test = new string[1];
                        path = Path.Combine(server.directory, instanceObj.relativePath);

                        if (instanceObj.type == "file") {
                            if (!File.Exists(path)) {
                                evt.client.To($"file_interaction", new {
                                    error = "File doesn't exist"
                                });
                                return;
                            };

                            test = instanceObj.relativePath.Split("/");

                            try {
                                File.Move(path, Path.Combine(path.Replace(test[test.Length - 1], ""), instanceObj.content));
                                evt.client.To($"file_interaction", new {
                                    message = $"Renamed file"
                                });
                            }
                            catch (Exception e) {
                                evt.client.To($"file_interaction", new {
                                    error = $"Failed to rename file\n{e.Message}"
                                });
                            }
                            return;
                        }

                        if (!Directory.Exists(path)) {
                            evt.client.To($"file_interaction", new {
                                error = "Directory doesn't exist"
                            });
                            return;
                        }

                        if (path == server.directory) {
                            evt.client.To($"file_interaction", new {
                                error = "You can't rename the instance through file manager"
                            });
                            return;
                        }

                        test = instanceObj.relativePath.Split("/");

                        try {
                            Directory.Move(path, Path.Combine(path.Replace(test[test.Length - 1], ""), instanceObj.content));
                            evt.client.To($"file_interaction", new {
                                message = $"Renamed directory"
                            });
                        }
                        catch (Exception e) {
                            evt.client.To($"file_interaction", new {
                                error = $"Failed to rename directory\n{e.Message}"
                            });
                        }
                        break;
                    case "paste":

                        CopyFileContent content = JsonSerializer.Deserialize<CopyFileContent>(instanceObj.content);

                        var fileToCopy = Path.Combine(server.directory, content.path);
                        var destinationPath = Path.Combine(Path.Combine(server.directory, instanceObj.relativePath), content.name);

                        if (instanceObj.type == "file") {
                            return;
                        }

                        if (!Directory.Exists(fileToCopy)) {
                            evt.client.To($"file_interaction", new {
                                error = "Directory doesn't exist"
                            });
                            return;
                        }

                        try {

                            int counter = 1;
                            string destinationDirectory = destinationPath;

                            // loop untill we can create a new directory
                            while (Directory.Exists(destinationDirectory)) {
                                destinationDirectory = $"{destinationPath}_{counter}";
                                counter++;
                            }

                            MinecraftDirectoryHelper.CopyDirectory(fileToCopy, destinationDirectory);
                            evt.client.To($"file_interaction", new {
                                message = $"Copied file {content.name} to {destinationDirectory}"
                            });

                        }
                        catch (Exception e) {
                            evt.client.To($"file_interaction", new {
                                error = $"Failed to delete directory\n{e.Message}"
                            });
                        }
                        break;

                }
            });

        }
    }
               

    namespace JsonStructures {

        public class Authorisation {
            public string authorisation { get; set; }
        }

        public class Instance : Authorisation {

            public string instanceName { get; set; }
            public int? callback { get; set; }

        }

        public class InstanceInformation : Instance {

            public bool full_information { get; set; }

        }

        public class FileManager : Instance {
            public string relativePath { get; set; }
        }

        public class FileInteraction : Instance {
            public string relativePath { get; set; }
            public string interaction { get; set; }
            public string type { get; set; }
            public string content { get; set; }
        }

        public class CopyFileContent {
            public string name { get; set; }
            public string path { get; set; }
        }

        public class SaveFile : Instance {
            public string relativePath { get; set; }
            public string content { get; set; }
        }

        public class InstanceState : Instance {
            public string state { get; set; }
        }

        public class InstanceCommand : Instance {
            public string cmd { get; set; }
        }


        public class CreateInstance : Instance {
            public int playerSlots { get; set; }
            public int port { get; set; }
            public int memory { get; set; }

            public string jarfile { get; set; }

        }

    }

}
