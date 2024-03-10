using CentrumConsole.minecraft;
using CentrumConsole.server;
using WinFormsApp1.minecraft;
using static System.Net.Mime.MediaTypeNames;

namespace CentrumConsole;

internal class Program {


    public static string TITLE = "\r\n\r\n  ____ _____ _   _ _____ ____  _   _ __  __ \r\n / ___| ____| \\ | |_   _|  _ \\| | | |  \\/  |\r\n| |   |  _| |  \\| | | | | |_) | | | | |\\/| |\r\n| |___| |___| |\\  | | | |  _ <| |_| | |  | |\r\n \\____|_____|_| \\_| |_| |_| \\_\\\\___/|_|  |_|\r\n\r\n";
    private static SocketServer server;
    private static ServerManager manager = new ServerManager();

    private static string commands = "Centrum Commands\n- help\n- user\n- start\n- stop\n- refresh\n- quit";

    static void Main(string[] args) {
        Console.WriteLine(TITLE);
        Console.WriteLine("Welcome to Centrum\r\n\r\n");

        bool running = true;
        bool blocked = false;

        Console.WriteLine(commands);

        AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
            Console.WriteLine("Total application crash");
            Console.WriteLine(args.ExceptionObject.ToString());
            foreach (var item in MinecraftHandler.serverLists) {
                item.Value.Shutdown();
            }
            Environment.Exit(1);
        };

        AppDomain.CurrentDomain.ProcessExit += (sender, args) => {
            Console.WriteLine("Centrum shutting down\nServers are shutting down...");
            foreach (var item in MinecraftHandler.serverLists) {
                item.Value.Shutdown();
            }
        };

        // run setup in case this is the first time running
        try {
            Setup();
        } catch (ArgumentException ex) {
            Console.WriteLine(ex.Message);
            blocked = true;
        }

        while (running) {
            var str = Console.ReadLine();
            if (str == null) continue; 
            string[] command = str.Split(" -");

            switch (command[0].Trim()) {
                case "start":

                    if (blocked) {
                        Console.WriteLine("Centrum is blocked from starting\nMake sure to fix any setup errors and call refresh when complete");
                        break;
                    }

                    if (command.Length < 2) {
                        Console.WriteLine(
                        "Missing arguments\n" +
                        "You must provide a port to start the server on: start -port:number");
                        break;
                    }

                    var portArgument = command[1].Split(":");
                    if (portArgument.Length < 2) {
                        Console.WriteLine(
                          "You must provide a port to open at\n" +
                          "Example: -port:number");
                        break;
                    }

                    try {
                        int port = int.Parse(portArgument[1].Trim());

                        server = new SocketServer($"http://localhost:{port}/");
                        // Start the socket
                        Task.Run(() => server.Start());
                        manager.ListenTo(server);

                    }
                    catch (FormatException ex) {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine($"Port paramter is invalid: {portArgument[1]}\nMust be a valid integer");
                    }

                    break;
                case "stop":
                    if (server != null) server.Stop();
                    break;
                case "user":
                    Console.Clear();
                    Console.WriteLine(TITLE);
                    UserHandler.Run();
                    Console.Clear();
                    Console.WriteLine(TITLE);
                    Console.WriteLine(commands);
                    break;
                case "refresh":
                    try {
                        Setup();
                    }
                    catch (ArgumentException ex) {
                        Console.WriteLine(ex.Message);
                        blocked = true;
                    }
                    break;
                case "connected":
                    foreach (var item in server.clients)
                    {
                        Console.WriteLine($"{item.id}\n{item.User?.username}");
                    }
                    break;
                case "help":
                    Console.Clear();
                    Console.WriteLine(TITLE);
                    Console.WriteLine(commands);
                    break;
                case "quit":
                    running = false;
                    break;
                default:
                    Console.WriteLine("unknown command");
                    break;
            }
        }
    }
    private static void Setup() {

        // create the minecraft directory
        MinecraftDirectoryHelper.Setup();

        // create our profile json
        MinecraftDirectoryHelper.CreateFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "users.json"), "{}", false);
        MinecraftHandler.LoadAllInstances();


        // user validity
        UserHandler.RunUserValidity();

    }

}