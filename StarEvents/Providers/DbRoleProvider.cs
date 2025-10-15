using System;
using System.Linq;
using System.Web.Security;
using StarEvents.Models;

namespace StarEvents.Providers
{
    public class DbRoleProvider : RoleProvider
    {
        public override string[] GetRolesForUser(string username)
        {
            using (var db = new StarEventsDBEntities())
            {
                var user = db.Users.FirstOrDefault(u => u.Email == username && u.IsActive);
                if (user != null && !string.IsNullOrEmpty(user.Role))
                    return new[] { user.Role };
                return new string[] { };
            }
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            using (var db = new StarEventsDBEntities())
            {
                var user = db.Users.FirstOrDefault(u => u.Email == username && u.IsActive);
                return user != null && user.Role == roleName;
            }
        }

        // The following methods are required to be implemented
        public override void CreateRole(string roleName) { throw new NotImplementedException(); }
        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole) { throw new NotImplementedException(); }
        public override bool RoleExists(string roleName) { throw new NotImplementedException(); }
        public override void AddUsersToRoles(string[] usernames, string[] roleNames) { throw new NotImplementedException(); }
        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames) { throw new NotImplementedException(); }
        public override string[] GetUsersInRole(string roleName) { throw new NotImplementedException(); }
        public override string[] GetAllRoles() { throw new NotImplementedException(); }
        public override string[] FindUsersInRole(string roleName, string usernameToMatch) { throw new NotImplementedException(); }
        public override string ApplicationName { get; set; }
    }
}