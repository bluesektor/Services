// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GreenWerx.Managers.Membership;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Membership;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Web.Filters;

namespace GreenWerx.Web.api.v1
{
    // [CacheOutput(ClientTimeSpan = 100, ServerTimeSpan = 100)]
    public class RolesController : ApiBaseController
    {
        public RolesController()
        {
        }

        /// <summary>
        /// This takes a json array of permissions as input and adds them to the RolePermissions.
        /// e.g. [{ PermissionUUID: pXXX, AccountUUID: aXXX, RoleUUID: rXXX },{ PermissionUUID: pYYY, AccountUUID: aYYY, RoleUUID: rYYY }]
        /// </summary>
        /// <returns></returns>
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Roles/{roleUUID}/Permissions/Add")]
        public ServiceResult AddPermissionsToRole(string roleUUID)
        {
            try
            {
                Task<string> content = Request.Content.ReadAsStringAsync();
                if (content == null)
                    return ServiceResponse.Error("No permissions were sent.");

                string body = content.Result;

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("No permissions were sent.");

                List<Permission> perms = JsonConvert.DeserializeObject<List<Permission>>(body);

                RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, this.GetUser(this.GetAuthToken(Request)));
                return roleManager.AddPermisssionsToRole(roleUUID, perms, CurrentUser);
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
            return ServiceResponse.OK();
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Roles/{category}/{categoryRoleName}/uuid/{referenceUUID}/type/{referenceType}")]
        public ServiceResult AddToRole(string category, string categoryRoleName, string referenceUUID, string referenceType)
        {
            if (string.IsNullOrWhiteSpace(category))
                return ServiceResponse.Error("Category was not sent.");

            if (string.IsNullOrWhiteSpace(categoryRoleName))
                return ServiceResponse.Error("Role Name was not sent.");

            if (string.IsNullOrWhiteSpace(referenceUUID))
                return ServiceResponse.Error("Reference id was not sent.");

            if (string.IsNullOrWhiteSpace(referenceType))
                return ServiceResponse.Error("Reference type was not sent.");

            // if category == block then add both user and profileuuid
            //targetUUID = CurrentUser.UUID && CurrentUser.ProfileUUID < == get from bearer token
            //targetType is according to type..

            ServiceResult res;
            try
            {
                RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, this.GetUser(this.GetAuthToken(Request)));
                var role = roleManager.GetRoles(CurrentUser.AccountUUID)
                                        .FirstOrDefault(w =>
                                        w.Category.EqualsIgnoreCase(category) &&
                                        w.CategoryRoleName.EqualsIgnoreCase(categoryRoleName));
                if (role == null)
                    return ServiceResponse.Error("Role not found for category.");

                UserManager um = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                ProfileManager pm = new ProfileManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                Profile p = null;
                User u = null;
                referenceType = referenceType.ToLower();

                switch (referenceType)
                {
                    case "user":
                        res = um.Get(referenceUUID);
                        if (res.Code != 200)
                            return res;
                        u = (User)res.Result;
                        res = pm.GetProfile(u.UUID, u.AccountUUID, false);
                        if (res.Code == 200)
                            p = (Profile)res.Result;

                        break;

                    case "profile":
                        res = pm.Get(referenceUUID);
                        if (res.Code != 200)
                            return res;

                        p = (Profile)res.Result;
                        res = um.Get(p.UserUUID);
                        if (res.Code != 200) //should always be a user.
                            return res;

                        u = (User)res.Result;
                        break;

                    default:
                        return ServiceResponse.Error("Type is not supported.");
                }

                if (p != null)
                    roleManager.AddUserToRole(role.UUID, p, CurrentUser);

                if (u != null)
                    roleManager.AddUserToRole(role.UUID, u, CurrentUser);

                //UserManager um = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                //res = um.Get(userUUID);
                //if (res.Code != 200)
                //    return res;

                //res = roleManager.AddUserToRole(roleUUID, (User)res.Result, CurrentUser);
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error(ex.Message);
            }
            return res;
        }

        /// <summary>
        /// This takes a json array of users as input and adds them to the RolePermissions.
        /// e.g. [{ PermissionUUID: pXXX, AccountUUID: aXXX, RoleUUID: rXXX },{ PermissionUUID: pYYY, AccountUUID: aYYY, RoleUUID: rYYY }]
        /// </summary>
        /// <returns></returns>
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Roles/{roleUUID}/Users/Add")]
        public ServiceResult AddUsersToRole(string roleUUID)
        {
            ServiceResult res;

            try
            {
                Task<string> content = Request.Content.ReadAsStringAsync();
                if (content == null)
                    return ServiceResponse.Error("No users were sent.");

                string body = content.Result;

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("No users were sent.");

                List<User> urs = JsonConvert.DeserializeObject<List<User>>(body);
                RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, this.GetUser(this.GetAuthToken(Request)));
                res = roleManager.AddUsersToRole(roleUUID, urs, CurrentUser);
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error(ex.Message);
            }
            return res;
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Roles/{roleUUID}/Users/{userUUID}/Add")]
        public ServiceResult AddUserToRole(string roleUUID, string userUUID)
        {
            if (string.IsNullOrWhiteSpace(roleUUID))
                return ServiceResponse.Error("No role id sent.");

            if (string.IsNullOrWhiteSpace(userUUID))
                return ServiceResponse.Error("No user id sent.");

            ServiceResult res;
            try
            {
                UserManager um = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                res = um.Get(userUUID);
                if (res.Code != 200)
                    return res;
                RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, this.GetUser(this.GetAuthToken(Request)));

                res = roleManager.AddUserToRole(roleUUID, (User)res.Result, CurrentUser);
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error(ex.Message);
            }
            return res;
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Roles/Clone/{roleUUID}")]
        public ServiceResult CloneRole(string roleUUID)
        {
            RoleManager rm = new RoleManager(Globals.DBConnectionKey, CurrentUser);
            return rm.CloneRole(roleUUID);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Roles/Delete")]
        public ServiceResult Delete(Role n)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, CurrentUser);
            var res = roleManager.Get(n.UUID);
            if (res.Code != 200)
                return res;

            Role dbRole = (Role)res.Result;

            return roleManager.Delete(dbRole);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Roles/Delete/{uuid}")]
        public ServiceResult Delete(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("No uuid sent.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, CurrentUser);
            var res = roleManager.Get(uuid);
            if (res.Code != 200)
                return res;

            Role dbRole = (Role)res.Result;

            return roleManager.Delete(dbRole);
        }

        /// <summary>
        /// This takes a json array of permissions as input and removes them to the RolePermissions.
        /// e.g. [{ PermissionUUID: pXXX, AccountUUID: aXXX, RoleUUID: rXXX },{ PermissionUUID: pYYY, AccountUUID: aYYY, RoleUUID: rYYY }]
        /// </summary>
        /// <returns></returns>
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Roles/{roleUUID}/Permissions/Delete")]
        public ServiceResult DeletePermissionsFromRole(string roleUUID)
        {
            try
            {
                Task<string> content = Request.Content.ReadAsStringAsync();
                if (content == null)
                    return ServiceResponse.Error("No permissions were sent.");

                string body = content.Result;

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("No permissions were sent.");

                RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, this.GetUser(this.GetAuthToken(Request)));
                List<Permission> perms = JsonConvert.DeserializeObject<List<Permission>>(body);
                roleManager.DeletePermissionsFromRole(roleUUID, perms, CurrentUser);
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
            return ServiceResponse.OK();
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Roles/{roleUUID}/Users/{userUUID}/Remove")]
        public ServiceResult DeleteUserFromRole(string roleUUID, string userUUID)
        {
            if (string.IsNullOrWhiteSpace(roleUUID))
                return ServiceResponse.Error("No role id sent.");

            if (string.IsNullOrWhiteSpace(userUUID))
                return ServiceResponse.Error("No user id sent.");

            ServiceResult res;
            try
            {
                UserManager um = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                res = um.Get(userUUID);
                if (res.Code != 200)
                    return res;
                RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, this.GetUser(this.GetAuthToken(Request)));

                res = roleManager.DeleteUserFromRole(roleUUID, (User)res.Result, CurrentUser);
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error(ex.Message);
            }
            return res;
        }

        /// <summary>
        /// This takes a json array of permissions as input and removes them to the RolePermissions.
        /// e.g. [{ PermissionUUID: pXXX, AccountUUID: aXXX, RoleUUID: rXXX },{ PermissionUUID: pYYY, AccountUUID: aYYY, RoleUUID: rYYY }]
        /// </summary>
        /// <returns></returns>
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Roles/{roleUUID}/Users/Remove")]
        public ServiceResult DeleteUsersFromRole(string roleUUID)
        {
            ServiceResult res;
            try
            {
                Task<string> content = Request.Content.ReadAsStringAsync();
                if (content == null)
                    return ServiceResponse.Error("No users were sent.");

                string body = content.Result;

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("No users were sent.");

                RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, this.GetUser(this.GetAuthToken(Request)));
                List<User> users = JsonConvert.DeserializeObject<List<User>>(body);
                res = roleManager.DeleteUsersFromRole(roleUUID, users, CurrentUser);
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error(ex.Message);
            }
            return res;
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/RolesBy/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, CurrentUser);

            return roleManager.Get(uuid);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Roles/{roleUUID}/Permissions")]
        public ServiceResult GetPermissionsForRole(string roleUUID)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            RoleManager rm = new RoleManager(Globals.DBConnectionKey, CurrentUser);
            List<dynamic> permissions = rm.GetPermissionsForRole(roleUUID, CurrentUser.AccountUUID).Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            permissions = permissions.Filter(ref filter);

            return ServiceResponse.OK("", permissions, filter.TotalRecordCount);
        }

        /// <summary>
        /// NOTE: This is account specific.
        /// I based the decision to get the roles by account on this post
        /// http://programmers.stackexchange.com/questions/278864/role-based-rest-api
        /// So we're getting resources (roles) based on the account id.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="startIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="sorting"></param>
        /// <returns></returns>
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Roles")]
        public ServiceResult GetRoles()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, this.GetUser(this.GetAuthToken(Request)));

            List<dynamic> roles = roleManager.GetRoles(CurrentUser.AccountUUID).Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            roles = roles.Filter(ref filter);

            return ServiceResponse.OK("", roles, filter.TotalRecordCount);
        }

        /// <summary>
        /// This returns all permissions for the role and
        /// </summary>
        /// <param name="roleUUID"></param>
        /// <param name="accountUUID"></param>
        /// <param name="filter"></param>
        /// <param name="startIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="sorting"></param>
        /// <returns></returns>
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Roles/{roleUUID}/Permissions/Unassigned")]
        public ServiceResult GetUnassignedPermissionsForRole(string roleUUID)
        {
            RoleManager rm = new RoleManager(Globals.DBConnectionKey, this.GetUser(this.GetAuthToken(Request)));
            List<dynamic> availablePerms = rm.GetAvailablePermissions(roleUUID, CurrentUser.AccountUUID).OrderBy(ob => ob.Name).Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            availablePerms = availablePerms.Filter(ref filter);

            return ServiceResponse.OK("", availablePerms, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Roles/{roleUUID}/Users/Unassigned")]
        public ServiceResult GetUnassignedUsersForRole(string roleUUID)
        {
            RoleManager rm = new RoleManager(Globals.DBConnectionKey, CurrentUser);

            List<dynamic> availableUsers = rm.GetUsersNotInRole(roleUUID, CurrentUser.AccountUUID).OrderBy(ob => ob.Name).Cast<dynamic>().ToList();

            DataFilter filter = GetFilter(Request);
            if (filter != null)
            {
                availableUsers = availableUsers.Filter(ref filter);
            }

            return ServiceResponse.OK("", availableUsers, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Roles/User")]
        public ServiceResult GetUserRoles()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, CurrentUser);

            var userRoles = roleManager.GetRolesForUser(CurrentUser.UUID, CurrentUser.AccountUUID).Cast<dynamic>().ToList(); ;

            string profileUUID = this.GetProfileUUID(this.GetAuthToken(Request));

            List<Role> profileRoles = roleManager.GetRolesForUser(profileUUID, CurrentUser.AccountUUID);

            userRoles.AddRange(profileRoles);
            if (userRoles.Count > 1)
            {
                userRoles = userRoles.GroupBy(x => x.UUID).Select(group => group.First()).ToList();
            }

            DataFilter filter = this.GetFilter(Request);
            userRoles = userRoles.Filter(ref filter);

            return ServiceResponse.OK("", userRoles, filter.TotalRecordCount);
        }

        /// <summary>
        /// if getUnassigned == true it will return
        /// users that are NOT in the rol
        /// </summary>
        /// <param name="roleUUID"></param>
        /// <param name="getUnassigned"></param>
        /// <returns></returns>
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Roles/{roleUUID}/Users")]
        public ServiceResult GetUsersInRole(string roleUUID)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            RoleManager rm = new RoleManager(Globals.DBConnectionKey, CurrentUser);

            List<dynamic> usersInRole = rm.GetUsersInRole(roleUUID, CurrentUser.AccountUUID).Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            usersInRole = usersInRole.Filter(ref filter);

            return ServiceResponse.OK("", usersInRole, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Roles/Add")]
        [System.Web.Http.Route("api/Roles/Insert")]
        public ServiceResult Insert(Role n)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(n.AccountUUID))
                n.AccountUUID = CurrentUser.AccountUUID;

            n.CreatedBy = CurrentUser.UUID;
            n.DateCreated = DateTime.UtcNow;

            RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, CurrentUser);
            return roleManager.Insert(n);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Roles/IsMember/{roleName}")]
        public ServiceResult IsUserInRole(string roleName)
        {
            RoleManager rm = new RoleManager(Globals.DBConnectionKey, this.GetUser(this.GetAuthToken(Request)));
            var role = rm.GetRole(roleName, CurrentUser.AccountUUID);
            if (role == null)
                return ServiceResponse.OK("", false);

            bool isMember = rm.IsInRole(CurrentUser.UUID, CurrentUser.AccountUUID, role.UUID, false);
            return ServiceResponse.OK("", isMember);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Roles/{name}")]
        public ServiceResult Search(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResponse.Error("You must provide a name for the role.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, this.GetUser(this.GetAuthToken(Request)));

            List<Role> s = roleManager.Search(name, "member");

            if (s == null || s.Count == 0)
                return ServiceResponse.Error("Role could not be located for the name " + name);

            return ServiceResponse.OK("", s);
        }

        /// <summary>
        ///Updated fields
        ///     Name
        ///     Private
        ///     SortOrder
        ///     Active
        ///     Deleted
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpPatch]
        [System.Web.Http.Route("api/Roles/Update")]
        public ServiceResult Update(Role r)
        {
            if (string.IsNullOrWhiteSpace(r.UUID))
                Debug.Assert(false, "NO UUID FOR ROLE");

            RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, this.GetUser(this.GetAuthToken(Request)));
            var res = roleManager.Get(r.UUID);
            if (res.Code != 200)
                return res;

            Role dbRole = (Role)res.Result;

            dbRole.Name = r.Name;
            dbRole.Private = r.Private;
            dbRole.SortOrder = r.SortOrder;
            dbRole.Active = r.Active;
            dbRole.Deleted = r.Deleted;

            return roleManager.Update(dbRole);
        }
    }
}