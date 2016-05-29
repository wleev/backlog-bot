using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static Backlog.Client.Models;

namespace BacklogBot.Models
{
    [Serializable]
    public sealed class BacklogUser : IEquatable<BacklogUser>
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Password { get; set; }

        public override string ToString()
        {
            return $"{Name} ({Id})";
        }

        public override int GetHashCode()
        {
            return $"{Email}{Id}".GetHashCode();
        }

        public bool Equals(BacklogUser other)
        {
            return Id.Equals(other.Id)
                && Email.Equals(other.Email)
                && Name.Equals(other.Name)
                && Role.Equals(other.Role);
        }

        public User ToApiUser()
        {
            int roleId = BacklogRole.FindMatchingRoleByName(Role).Id;
            return new User(Id, Name, Password, roleId, Email);
        }
    }

    public class RoleNotFoundException : Exception { }

    [Serializable]
    public sealed class BacklogRole : IEquatable<BacklogRole>
    {
        public int Id { get; }
        public string Name { get; }

        private BacklogRole()
        {
        }

        private BacklogRole(int id, string name)
        {
            Id = id;
            Name = name;
        }

        private static readonly BacklogRole Default = new BacklogRole(-1, "None");
        private static readonly BacklogRole Administrator = new BacklogRole(1, "Administrator");
        private static readonly BacklogRole NormalUser = new BacklogRole(2, "Normal User");
        private static readonly BacklogRole Reporter = new BacklogRole(3, "Reporter");
        private static readonly BacklogRole Viewer = new BacklogRole(4, "Viewer");
        private static readonly BacklogRole GuestReporter = new BacklogRole(5, "Guest Reporter");
        private static readonly BacklogRole GuestViewer = new BacklogRole(6, "Guest Viewer");

        public static List<BacklogRole> AllRoles = new List<BacklogRole>() { Administrator, NormalUser, Reporter, Viewer, GuestReporter, GuestViewer };

        public static BacklogRole GetRoleById(int id)
        {
            if (id > AllRoles.Count)
                return null;

            return AllRoles[id - 1];
        }

        public static BacklogRole FindMatchingRoleByName(string roleName)
        {
            if (String.IsNullOrEmpty(roleName))
                return Default;
            return AllRoles.SingleOrDefault(role => String.Equals(role.Name, roleName, StringComparison.InvariantCultureIgnoreCase)) ?? Default;
        }

        public bool Equals(BacklogRole other)
        {
            return this.Id.Equals(other.Id);
        }
    }
}