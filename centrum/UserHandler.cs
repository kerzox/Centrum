using CentrumConsole.server;
using CentrumConsole.server.JsonStructures;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static CentrumConsole.User;
using static System.Net.WebRequestMethods;

namespace CentrumConsole {
    internal class UserHandler {

        private static JsonWebTokenHandler JsonWebTokenHandler = new JsonWebTokenHandler();
        private static SymmetricSecurityKey secretKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(256));
        private static SigningCredentials signingCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256Signature);
        private static string userPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "users.json");
        private static string userCommands = "Commands for user\n- create\n- delete\n- permissions\n- list\n- back";

        // JWT token and their assigned user
        private Dictionary<string, User> users = new Dictionary<string, User>();
        
        private class UserPacket {
            public string username { get; set; }
            public string password { get; set; }

        }

        public struct LoginStructure {
            public string token { get; set; }
            public User user { get; set; }
        }



        internal static void Run() {
            bool running = true;
            Console.WriteLine(userCommands);

            while (running) {
                var str = Console.ReadLine();
                if (str == null) continue;
                string[] command = str.Split('-');
                switch (command[0].Trim()) {
                    case "create":
                        if (command.Length < 3) {
                            Console.WriteLine(
                            "Missing arguments\n" +
                            "You must provide a username and a password for the account\n create -username:name -password:password");
                            break;
                        }

                        string[] username = command[1].Split(":");
                        if (username.Length < 2) {
                            Console.WriteLine(
                              "You must provide a valid username\n" +
                              "Example: -username:str");
                            break;
                        }

                        string[] password = command[2].Split(":");
                        if (password.Length < 2) {
                            Console.WriteLine(
                              "You must provide a valid password\n" +
                              "Example: -password:str");
                            break;
                        }
                        CreateAccount(username[1].Trim(), password[1].Trim());
                        break;
                    case "delete":
                        if (command.Length < 2) {
                            Console.WriteLine(
                            "Missing arguments\n" +
                            "You must provide the user id\n delete -id:id");
                            break;
                        }

                        string[] id = command[1].Split(":");
                        if (id.Length < 2) {
                            Console.WriteLine(
                              "You must provide a valid id\n" +
                              "Example: -id:id");
                            break;
                        }
                        DeleteAccount(id[1].Trim());
                        break;
                    case "permissions":
                        handlePermission();
                        Console.WriteLine(userCommands);
                        break;
                    case "list":
                        foreach (var item in GetUsersFromFile())
                        {
                            Console.WriteLine($"USER: \nid:{item.guid}\n{item.username}");
                        }
                        break;
                    case "back":
                        running = false;
                        break;
                    default:
                        Console.WriteLine("unknown command");
                        break;
                }
            }
        }


        private static void handlePermission() {
            bool stop = false;
            Console.WriteLine("Commands for user permissions\n- assign\n- back");
            while (!stop) {
                var str = Console.ReadLine();
                if (str == null) continue;
                string[] command = str.Split('-');
                switch (command[0].Trim()) {
                    case "assign":
                        if (command.Length < 3) {
                            Console.WriteLine(
                            "Missing arguments\n" +
                            "You must provide a user id and a list of instancesNames\n assign -id:id -instances:name name name");
                            break;
                        }

                        string[] id = command[1].Split(":");
                        if (id.Length < 2) {
                            Console.WriteLine(
                              "You must provide a valid id\n" +
                              "Example: -id:id");
                            break;
                        }

                        string[] instanceArg = command[2].Split(":");
                        if (instanceArg.Length < 2) {
                            Console.WriteLine(
                              "You must provide valid instances\n" +
                              "Example: -instance:str");
                            break;
                        }

                        string[] instances = instanceArg[1].Split(" ");
                        if (instances.Length <= 0) {
                            Console.WriteLine(
                              "You must provide valid instances\n" +
                              "Example: -instance:str | -instance:name name2 name3");
                            break;
                        }

                        AssignUserToInstance(id[1].Trim(), instances.Select(i => i.Trim()).ToArray());

                        break;
                    case "back":
                        stop = true;
                        break;
                }
            }
        }


        private static string ReadUserFile() {
            string content = System.IO.File.ReadAllText(userPath);
            return content;
        }

        private static List<User> GetUsersFromFile() {
            return JsonSerializer.Deserialize<List<User>>(ReadUserFile());
        }

        public static void RunUserValidity() {
            try {

                List<string> existing = new List<string>();

                foreach (var item in GetUsersFromFile()) {
                    if (existing.Contains(item.username)) {
                        throw new ArgumentException($"Duplicate usernames found: {item.username}\nThis must be fixed before the Centrum will run");
                    }
                    existing.Add(item.username);
                }
            } catch (JsonException ex) {
                
            }
        }

        private static void AssignUserToInstance(string id, string[] instanceNames) {
            try {
                List<User> file = GetUsersFromFile();
                if (file == null) {
                    Console.WriteLine("There are no users");
                    return; ;
                }

                User? user = null;

                foreach (var item in file) {
                    if (item.guid.Equals(id)) {
                        user = item;
                    }

                }

                if (user == null) {
                    Console.WriteLine("Couldn't find the user");
                    return;

                }

                user.assignedInstances.Clear();
                user.assignedInstances.AddRange(instanceNames);
                string json = JsonSerializer.Serialize(file, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(userPath, json);
                Console.WriteLine($"user has been updated: {user.username}");
                return;
            }
            catch (JsonException ex) {

            }
        }

        public static User GetUser(string id) {
            try {
                List<User> file = GetUsersFromFile();
                foreach (var item in file) {
                    if (item.guid.Equals(id)) {
                        return item;
                    }
                }
            } catch (JsonException ex) {
                
            }
            return null;
        }

        public static User GetUserByUsername(string username) {
            try {
                List<User> file = GetUsersFromFile();
                foreach (var item in file) {
                    if (item.username.Equals(username)) {
                        return item;
                    }
                }
            }
            catch (JsonException ex) {

            }
            return null;
        }

        public static void DeleteAccount(string id) {
            string content = System.IO.File.ReadAllText(userPath);
            List<User> file = new List<User>();
            try {
                file = GetUsersFromFile();
                if (file == null) {
                    Console.WriteLine("There are no users");
                    return; ;
                }

                User? user = null;

                foreach (var item in file)
                {
                    if (item.guid.Equals(id)) {
                        user = item;
                    }

                }

                if (user == null) {
                    Console.WriteLine("Couldn't find the user");
                    return;
        
                }
                file.Remove(user);
                string json = JsonSerializer.Serialize(file, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(userPath, json);
                Console.WriteLine($"user has been deleted: {user.username} : {user.guid}");
                return;
            }
            catch (JsonException ex) {

            }

        }
        public static void CreateAccount(string username, string password) {

            User user = new User(username, password);
            List<User> file = new List<User>();

            try {
                file = GetUsersFromFile();
            }
            catch (JsonException ex) {

            }

            if (file.Count != 0) {
                foreach (var item in file) {
                    if (item.username.Equals(username)) {
                        Console.WriteLine($"The username has been taken");
                        return;
                    }
                }
            }

            file.Add(user);
            string json = JsonSerializer.Serialize(file, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(userPath, json);
            Console.WriteLine($"user has been created: {user.username}");

        }
        internal LoginStructure Login(object json, CentrumWebSocket.Client clientSocket) {
            UserPacket user = JsonSerializer.Deserialize<UserPacket>(json.ToString());
            try {
                // get list of users
                List<User> file = GetUsersFromFile();

                // find users by the username and check if 
                for (int i = 0; i < file.Count; i++) {
                    var item = file[i];
                    if (item.username.Equals(user.username)) {
                        if (BCrypt.Net.BCrypt.Verify(user.password, item.hash)) {

                            ClaimsIdentity claims = new ClaimsIdentity();

                            // username
                            claims.AddClaim(new Claim("name", item.username));

                            string token = JsonWebTokenHandler.CreateToken(new SecurityTokenDescriptor {
                                Subject = claims,
                                Issuer = "centrum-daemon",
                                Expires = DateTime.UtcNow.AddHours(2),
                                SigningCredentials = signingCredentials
                            });

                            //item.client = clientSocket;
                            //users.Add(token, item);

                            item.token = token;

                            return new LoginStructure {
                                token = token,
                                user = item
                            };
                        }
                    }
                }

                return new LoginStructure { };

            } catch (JsonException ex) {
                return new LoginStructure { };
            }

        }

        

        internal Task<TokenValidationResult> ValidateToken(Authorisation authorisation, CentrumWebSocket.Client client) {
            return JsonWebTokenHandler.ValidateTokenAsync(authorisation.authorisation, new TokenValidationParameters {
                ValidateAudience = false,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = secretKey,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidIssuer = "centrum-daemon"
            });
        }

        internal async Task<int> ValidateClient(Authorisation authorisation, CentrumWebSocket.Client client) {
            if (JsonWebTokenHandler.CanReadToken(authorisation.authorisation)) {
                var result = await ValidateToken(authorisation, client);

                if (result.IsValid) {

                    // check if the resulting token has a claim of the clients signed in username
                    if (!result.Claims.ContainsKey("name")) {
                        // because we dont have the name of the client we fail our validation
                        return 403;
                    }

                    // get the name
                    var nameClaim = result.Claims["name"];
                    // assign this client socket with the user
                    client.User = GetUserByUsername((string)nameClaim);
                    client.User.token = authorisation.authorisation;

                    return 200;

                }
            }
            return 401;
        }

        internal Dictionary<string, User> GetLoggedInUsers() {
            return this.users;
        }

        internal User? GetAuthorisedUser(Authorisation authorisation) {
            if (users.ContainsKey(authorisation.authorisation)) {
                return users[authorisation.authorisation];
            }
            return null;
        }

        internal bool IsAuthorisedUser(Authorisation authorisation) {
            if (!users.ContainsKey(authorisation.authorisation)) {
                return false;
            }
            return true;
        }
    }
}
