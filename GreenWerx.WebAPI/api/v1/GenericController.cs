// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.Linq;
using GreenWerx.Managers;
using GreenWerx.Managers.Membership;
using GreenWerx.Models.App;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Web;
using GreenWerx.Web.api;
using GreenWerx.Web.Filters;

namespace GreenWerx.WebAPI.api.v1
{
    public class GenericController : ApiBaseController
    {
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 3)]
        [System.Web.Http.HttpPatch]
        [System.Web.Http.Route("api/Generic/{type}/{UUID}/Accounts/{accountUUID}/Flag/{name}/Value/{value}")]
        public ServiceResult SetFlag(string type, string UUID, string accountUUID, string name, string value)
        {
            if (string.IsNullOrWhiteSpace(type))
                return ServiceResponse.Error("No typesent.");

            if (string.IsNullOrWhiteSpace(UUID))
                return ServiceResponse.Error("No user id sent.");

            if (string.IsNullOrWhiteSpace(accountUUID))
                return ServiceResponse.Error("No account id sent.");

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResponse.Error("No name sent.");

            if (string.IsNullOrWhiteSpace(value))
                return ServiceResponse.Error("No value sent.");

            var user = this.GetUser(this.GetAuthToken(Request));
            ////var admin = rm.GetRole("admin", user.AccountUUID);
            ////var owner = rm.GetRole("owner", user.AccountUUID);
            ////if(admin == null && owner == null)
            ////    return ServiceResponse.Error("You are not authorized this action.");

            //if (!rm.IsSiteAdmin(user.Name) && !rm.IsInRole(user.UUID,  user.AccountUUID, admin.UUID, false) &&
            //    !rm.IsInRole(user.UUID,  user.AccountUUID, owner.UUID,false))
            //    return ServiceResponse.Error("You are not authorized this action.");

            switch (name.ToUpper())
            {
                case "NSFW":
                    bool converted = false;
                    //value
                    int flagValue = value.ConvertTo<int>(out converted);
                    if (!converted)
                        return ServiceResponse.Error("In valid flag value sent.");

                    GenericManager gm = new GenericManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

                    var tmp = gm.GetItem(type, UUID, accountUUID);
                 
                    if (tmp == null)
                        return ServiceResponse.Error("Item not found for id.");

                    tmp.SubmitDate = DateTime.Now;
 
                    RoleManager rm = new RoleManager(Globals.DBConnectionKey, user);
                    var userRole = rm.GetRolesForUser(user.UUID, user.AccountUUID).OrderByDescending(o => o.RoleWeight).FirstOrDefault();

                    if (userRole == null)
                        return ServiceResponse.Error("You are not assigned a role allowing you to flag items.");

                    //if the item hasnt been set
                    if (tmp.NSFW < 0)
                    {
                        if (flagValue == 0 && userRole.RoleWeight >= 90) //it's being set to safe (flagValue = 0 ; //safe)
                            tmp.NSFW = flagValue;

                        if (flagValue == 0 && userRole.RoleWeight < 90) //it's being set to safe (flagValue = 0 ; //safe)
                            return ServiceResponse.Error("You are not assigned a role allowing you to flag items as safe.");

                        //if its being set to nsfw then set ti to the role of the users highest role
                        if (flagValue > 0)
                        {
                            if (userRole.RoleWeight > tmp.NSFW)
                                tmp.NSFW = userRole.RoleWeight;
                            else
                                return ServiceResponse.OK("Already flagged.");
                        }
                    }
                    else if (flagValue > 0)
                    {
                        if (userRole.RoleWeight > tmp.NSFW)
                            tmp.NSFW = userRole.RoleWeight;
                        else
                            return ServiceResponse.OK("Already flagged.");
                    }
                    else if (flagValue == 0)
                    {  //resetting flag from nsfw to ok to view publicly
                        if (userRole.RoleWeight >= tmp.NSFW)
                            tmp.NSFW = flagValue;
                        else
                            return ServiceResponse.Error("You are not assigned a role allowing you to flag items.");
                    }
                    //else if (userRole.RoleWeight < tmp.NSFW)
                    //{ //if their highest role is less than the nsfw flagged wieght then nope out.
                    //    return ServiceResponse.Error("You are not authorized this action.");
                    //}
                    //else
                    //{
                    //    tmp.NSFW = userRole.RoleWeight;
                    //}
                    // todo log who did what.
                    return gm.Update(tmp);

                case "BAN":
                    if (UUID == user.UUID)
                        return ServiceResponse.Error("You can't ban yourself.");
                    user.Banned = true;
                    UserManager um = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                    // todo log who did what.
                    um.UpdateUser(user);

                    break;
            }
            return ServiceResponse.Error("Invalid flag.");
        }
    }
}