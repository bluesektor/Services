using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using GreenWerx.Managers.Membership;
using GreenWerx.Models.App;
using GreenWerx.Models.General;
using GreenWerx.Models.Membership;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Web;
using GreenWerx.Web.api;
using GreenWerx.Web.Filters;

//create tables by copying usersinroles table
//  rolesBlocked - blocks roles
//  rolesBlockedUsers - blocks specific users/profiles

//  crud endpoints to get from new tables
//

//update the is profile data access authorized code

namespace GreenWerx.WebAPI.api.v1
{
    public class PermissionsController : ApiBaseController
    {
        #region Blocked Roles

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Permissions/Blocked/Roles/Delete")]
        public ServiceResult DeleteBlockedRole(BlockedRole n)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, CurrentUser);

            return roleManager.DeleteBlockedRole(n, CurrentUser);
        }

        /// <summary>
        /// returns roles the user has blocked
        /// </summary>
        /// <returns></returns>
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Permissions/Blocked/Roles/Get")]
        public ServiceResult GetBlockedRoles()
        {
            RoleManager rm = new RoleManager(Globals.DBConnectionKey, CurrentUser);
            var blockedRoles = rm.GetBlockedRoles(CurrentUser.UUID, CurrentUser.AccountUUID);
            return ServiceResponse.OK("", blockedRoles);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Permissions/Blocked/Roles/Add")]
        [System.Web.Http.Route("api/Permissions/Block/Roles/Insert")]
        public ServiceResult InsertBlockedRole(BlockedRole n)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            n.AccountUUID = CurrentUser.AccountUUID;
            n.CreatedBy = CurrentUser.UUID;
            n.DateCreated = DateTime.UtcNow;

            RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, CurrentUser);
            return roleManager.AddBlockedRole(n, CurrentUser);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Permissions/Blocked/Roles/Save")]
        public ServiceResult SaveBlockedRoles()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            string body = Request.Content.ReadAsStringAsync().Result;

            if (string.IsNullOrEmpty(body))
                return ServiceResponse.Error("No roles were sent.");

            var commands = JsonConvert.DeserializeObject<List<BatchCommand>>(body);

            if (commands == null)
                return ServiceResponse.Error("Invalid roles posted to server.");

            RoleManager rm = new RoleManager(Globals.DBConnectionKey, CurrentUser);

            ServiceResult res = ServiceResponse.OK("");

            foreach (var command in commands)
            {
                if (string.IsNullOrWhiteSpace(command.Command) || string.IsNullOrWhiteSpace(command.UUID))
                    continue;

                var rs = rm.Get(command.UUID);
                if (rs == null || rs.Code != 200)
                    return ServiceResponse.Error("Role not found for id:" + command.UUID);

                var r = (Role)rs.Result;

                switch (command.Command.ToLower())
                {
                    case "add":
                        var br = new BlockedRole()
                        {
                            Name = r.Name,
                            AccountUUID = CurrentUser.AccountUUID,
                            Active = true,
                            CreatedBy = CurrentUser.UUID,
                            DateCreated = DateTime.UtcNow,
                            RoleUUID = r.UUID,
                            RoleOperation = r.RoleOperation,
                            RoleWeight = r.RoleWeight,
                            ReferenceUUID = CurrentUser.UUID,
                            ReferenceType = CurrentUser.UUIDType,
                            TargetUUID = r.UUID,
                            TargetType = r.UUIDType
                        };

                        rm.AddBlockedRole(br, CurrentUser);

                        break;

                    case "remove":
                        var blockedRole = rm.GetBlockedRoles(CurrentUser.UUID, CurrentUser.AccountUUID).FirstOrDefault(w => w.RoleUUID == command.UUID);
                        if (blockedRole == null)
                            continue;

                        res = rm.DeleteBlockedRole(blockedRole, CurrentUser);
                        break;
                }

                if (res.Code != 200)
                    return res;
            }

            return ServiceResponse.OK("");
        }

        #endregion Blocked Roles

        #region Blocked Users

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Permissions/Blocked/Users/{targetUserUUID}/Delete")]
        public ServiceResult DeleteBlockedUser(string targetUserUUID)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, CurrentUser);
            return roleManager.DeleteBlockedUser(targetUserUUID, CurrentUser);
        }

        /// <summary>
        /// returns users the user has blocked
        /// </summary>
        /// <returns></returns>
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Permissions/Blocked/Users/Get")]
        public ServiceResult GetBlockedUsers()
        {
            RoleManager rm = new RoleManager(Globals.DBConnectionKey, CurrentUser);

            var bu = rm.GetBlockedUsers(CurrentUser.UUID, CurrentUser.AccountUUID).Where(w => w.TargetType.EqualsIgnoreCase("user"));

            return ServiceResponse.OK("", bu);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Permissions/Blocked/Users/{targetUserUUID}/Add")]
        [System.Web.Http.Route("api/Permissions/Block/Users/{targetUserUUID}/Insert")]
        public ServiceResult InsertBlockedUser(string targetUserUUID)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, CurrentUser);
            return roleManager.AddBlockedUser(targetUserUUID, CurrentUser);
        }

        #endregion Blocked Users
    }
}