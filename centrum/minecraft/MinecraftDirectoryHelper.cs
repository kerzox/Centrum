using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WinFormsApp1.minecraft {

    internal class MinecraftDirectoryHelper {

        public static Dictionary<string, DirectoryInfo> instances = new Dictionary<string, DirectoryInfo>();


        // minecraft dir
        internal static string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "minecraft");
        internal static string instancePath = Path.Combine(directoryPath, "instances");
        internal static string serverFile = Path.Combine(directoryPath, "server.jar");
        internal static string eulaFile = Path.Combine(directoryPath, "eula.txt");

        public static void Setup() {
            if (!Directory.Exists(instancePath)) {
                //Directory.CreateDirectory(directoryPath);
                Directory.CreateDirectory(instancePath);
            }
        }

        public static void findAllInstances() {
            instances.Clear();
            DirectoryInfo directory = new DirectoryInfo(instancePath);

            // Get all directories in the specified path and its subdirectories
            try {
                DirectoryInfo[] directories = directory.GetDirectories("*", SearchOption.TopDirectoryOnly);
                foreach (var dir in directories) {
                    instances.Add(dir.Name, dir);
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        public static string[] getInstances() {
            DirectoryInfo directory = new DirectoryInfo(instancePath);

            // Get all directories in the specified path and its subdirectories
            DirectoryInfo[] directories = directory.GetDirectories("*", SearchOption.AllDirectories);
            return directories.Select(d => d.Name).ToArray();
        }

        /**
         * Creates a new directory and adds the server files
         */

        public static string CreateNewInstance(string name) {

            string path = Path.Combine(instancePath, name);

            if (hasInstanceByName(name)) {
                throw new FileNotFoundException("Instance exists already");
            }

            // create the instance
            Directory.CreateDirectory(path);
            Console.WriteLine($"Instance has been created at {path}");

            return path;
        }

        public static async Task DownloadServerFiles(string path) {
            await DownloadFileAsync("https://piston-data.mojang.com/v1/objects/8dd1a28015f51b1803213892b50b7b4fc76e594d/server.jar", Path.Combine(path, "server.jar"));
        }

        public static void removeInstance(string name) {

            string path = Path.Combine(instancePath, name);

            if (!hasInstanceByName(name)) {
                throw new FileNotFoundException("Instance doesn't exist");
            }

            Directory.SetCurrentDirectory(directoryPath);
            Directory.Move(path, Path.Combine(directoryPath, "temp"));
            Directory.Delete(Path.Combine(directoryPath, "temp"), true);


        }

        public static void CopyDirectory(string sourceDir, string destDir) {

            // Create the destination directory if it doesn't exist
            if (!Directory.Exists(destDir)) {
                Directory.CreateDirectory(destDir);
            }

            // Copy files
            foreach (string file in Directory.GetFiles(sourceDir)) {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            // Copy subdirectories recursively
            //foreach (string subDir in Directory.GetDirectories(sourceDir)) {
                //string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                //CopyDirectory(subDir, destSubDir);
            //}
        }

        public static bool hasInstanceByName(string name) {
            return Directory.Exists(Path.Combine(instancePath, name));
        }

        public static string GetDirectoryStructure(string directory) {
            // Create an anonymous object to store the directory structure
            var directoryInfo = new {
                name = Path.GetFileName(directory),
                children = GetSubdirectories(directory)
            };

            // Convert the anonymous object to JSON
            return JsonSerializer.Serialize(directoryInfo);
        }

        public static List<object> GetSubdirectories(string directory) {
            // Get all subdirectories in the current directory
            string[] subDirectories = Directory.GetDirectories(directory);

            // Create a list to store the subdirectories
            var children = new List<object>();

            // Recursively add subdirectories to the list
            foreach (string subDirectory in subDirectories) {
                var childInfo = new {
                    name = Path.GetFileName(subDirectory),
                    children = GetSubdirectories(subDirectory)
                };
                children.Add(childInfo);
            }

            return children;
        }

        public static async Task DownloadFileAsync(string fileUrl, string destinationPath) {
            using (HttpClient client = new HttpClient()) {
                try {
                    using (HttpResponseMessage response = await client.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead)) {
                        response.EnsureSuccessStatusCode();

                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new System.IO.FileStream(destinationPath, System.IO.FileMode.Create, System.IO.FileAccess.Write)) {
                            await stream.CopyToAsync(fileStream);
                        }
                    }
                }
                catch (HttpRequestException ex) {
                    Console.WriteLine($"Error downloading file: {ex.Message}");
                }
            }
        }

        public static void CreateFile(string path, string title, Boolean replace) {
            try {

                if (File.Exists(path) && !replace) { return; }

                using (StreamWriter writer = new StreamWriter(path, false)) {
                    writer.Write(title);
                }
            }
            catch (Exception ex) {
              
            }
        }

        public static void DeleteFile(string path) {
            try {
                if (!File.Exists(path)) {
                    return;
                }
                File.Delete(path);
            }
            catch (Exception ex) {
               
            }
        }

        public static string GetProperty(string path, string property) {
            try {

                using (StreamReader reader = new StreamReader(path)) {
                    string[] content = reader.ReadToEnd().Split('\n');

                    foreach (string line in content) {
                        // Split each line into propertyName and value
                        string[] parts = line.Trim().Split('=');

                        if (parts.Length == 2) {
                            string currentPropertyName = parts[0];
                            string currentValue = parts[1];

                            // Check if the current line contains the desired property
                            if (currentPropertyName.Equals(property, StringComparison.OrdinalIgnoreCase)) {
                                return currentValue.Trim();
                            }
                        }
                    }

                }

            }
            catch (Exception ex) {
                
            }
            return null;
        }

        public static void EditPropertyOnFile(string path, string property, string value) {
            try {

                string content;
                // Read the content of the file
                using (StreamReader reader = new StreamReader(path)) {
                    content = reader.ReadToEnd();
                    content = content.Replace(property, value);
                }

                using (StreamWriter writer = new StreamWriter(path)) {
                    writer.Write(content);
                }


            }
            catch (Exception ex) {
                
            }

        }

        public static string ReadFileToEnd(string path) {
            try {
                using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    using (StreamReader reader = new StreamReader(fileStream)) {
                        // Read the file content here
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (FileNotFoundException ex) {
               
            }
            return "{}";
        }

        public static string[] ReadLatestLog(string dirPath) {
            string path = Path.Combine(dirPath, "logs/latest.log");
            List<string> lines = new List<string>();

            if (!File.Exists(path)) { return []; }

            try {
                // Open the file for reading
                using (FileStream fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    // Use StreamReader to read from the file
                    using (StreamReader reader = new StreamReader(fileStream)) {
                        // Read the file content
                        string line;
                        while ((line = reader.ReadLine()) != null) {
                            lines.Add(line);
                        }

                    }
                }
            }
            catch (Exception ex) {
                
            }

            return lines.ToArray();
        }

    }

}
