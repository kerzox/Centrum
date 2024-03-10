using CentrumConsole.server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WinFormsApp1.minecraft;

namespace CentrumConsole {


    internal class User {

        public enum PermissionKeys {
            INSTANCE_ACCESS,
            INSTANCE_CREATION,
            INSTANCE_DELETION
        }
        public string guid { get; set; }
        public string username { get; set; }
        public string hash { get; set; }
        public string salt { get; set; }

        public string token { get; set; }
        public Dictionary<PermissionKeys, bool> Permissions { get; set; } = new Dictionary<PermissionKeys, bool> {
            { PermissionKeys.INSTANCE_DELETION, false },
            { PermissionKeys.INSTANCE_CREATION, false },
            { PermissionKeys.INSTANCE_ACCESS, true },
        };

        public List<string> assignedInstances { get; set; } = new List<string>();

        [JsonConstructor]
        internal User(string guid, string username, string hash, string salt, List<string> assignedInstances) {

            this.guid = guid;
            this.username = username;
            this.hash = hash;
            this.salt = salt;
            this.assignedInstances = assignedInstances;
        }

        internal User(string username, string password) {

            var id = Guid.NewGuid();
            this.guid = id.ToString().Replace("-", "");
            this.username = username;
            this.salt = BCrypt.Net.BCrypt.GenerateSalt();
            this.hash = BCrypt.Net.BCrypt.HashPassword(password, this.salt);
        }

    }
}
