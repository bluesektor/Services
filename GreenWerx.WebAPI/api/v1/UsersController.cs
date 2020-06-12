// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenWerx.Data.Logging;
using GreenWerx.Managers.Membership;

using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Membership;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Utilites.Security;
using GreenWerx.Web.api.Helpers;
using GreenWerx.Web.Filters;
using GreenWerx.Web.Models;
using WebApiThrottle;

namespace GreenWerx.Web.api.v1
{
    public class UsersController : ApiBaseController
    {
        private readonly NetworkHelper network = null;

        public UsersController()
        {
            network = new NetworkHelper();
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Users/Delete/{UUID}")]
        public ServiceResult Delete(string UUID)
        {
            if (string.IsNullOrWhiteSpace(UUID))
                return ServiceResponse.Error("No UUID sent.");

            UserManager userManager = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            ServiceResult delResult = userManager.Delete(UUID);

            if (delResult.Code != 200)
                return delResult;

            AccountManager ac = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            ac.RemoveUserFromAllAccounts(UUID);
            return ServiceResponse.OK();
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Users/Delete")]
        public ServiceResult Delete(User n)
        {
            if (n == null || string.IsNullOrWhiteSpace(n.UUID))
                return ServiceResponse.Error("Invalid account was sent.");

            UserManager userManager = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            ServiceResult delResult = userManager.Delete(n.UUID);

            if (delResult.Code != 200)
                return delResult;

            AccountManager ac = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return ac.RemoveUserFromAllAccounts(n.UUID);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/AllUsers")]
        public ServiceResult GetAllUsers()
        {
            UserManager um = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            DataFilter filter = this.GetFilter(Request);
            List<dynamic> users = (List<dynamic>)um.GetAllUsers(ref filter).Cast<dynamic>().ToList();

            users = users.Select(s => new
            {
                Name = s.Name,
                UUID = s.UUID,
                UUIDType = s.UUIDType,
                Image = s.Image,
                Email = s.Email,
                Banned = s.Banned,
                LockedOut = s.LockedOut
            }).Cast<dynamic>().ToList();

            return ServiceResponse.OK("", users, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/UsersBy/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            try
            {
                UserManager userManager = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                return userManager.GetUser(uuid, true);
            }
            catch (Exception ex)
            {
                SystemLogger logger = new SystemLogger(Globals.DBConnectionKey);
                logger.InsertError(ex.Message, "UsersController", "GetBy");
            }
            return ServiceResponse.Error();
        }

        /// <summary>
        /// NOTE: This is account specific.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="startIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="sorting"></param>
        /// <returns></returns>
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Users")]
        public ServiceResult GetUsers()
        {
            AccountManager ac = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            DataFilter filter = this.GetFilter(Request);
            List<dynamic> users = (List<dynamic>)ac.GetAccountMembers(this.GetUser(this.GetAuthToken(Request)).AccountUUID, ref filter).Cast<dynamic>().ToList();

            return ServiceResponse.OK("", users, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 3)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Users/Add")]
        [System.Web.Http.Route("api/Users/Insert")]
        public ServiceResult Insert(User n)
        {
            if (n == null)
                return ServiceResponse.Error("No user sent.");

            AccountManager ac = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            UserManager userManager = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            ServiceResult res = userManager.Insert(n, GetClientIpAddress(Request));

            if (res.Code != 200)
                return res;

            //add user to account members (user is now a member of the account from which it was created).
            //
            if (string.IsNullOrWhiteSpace(n.AccountUUID) == false)
            {
                res = ac.AddUserToAccount(n.AccountUUID, n.UUID, CurrentUser);
            }
            if (res.Code != 200)
                return res;
            res.Result = n.UUID;
            return res;
        }

        //NOTE: make sure the accountUUID is set before calling this.
        //
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 3)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Users/AddAsync")]
        [System.Web.Http.Route("api/Users/InsertAsync")]
        public async Task<ServiceResult> InsertAsync(User n)
        {
            if (n == null)
                return ServiceResponse.Error("No user sent.");

            AccountManager ac = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            UserManager userManager = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            ServiceResult res = await userManager.InsertUserAsync((User)n, GetClientIpAddress(Request));

            //add user to account members (user is now a member of the account from which it was created).
            //
            if (res.Code == 200 && string.IsNullOrWhiteSpace(n.AccountUUID) == false)
            {
                ac.AddUserToAccount(n.AccountUUID, n.UUID, CurrentUser);
            }

            return res;
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Users/{name}")]
        public ServiceResult Search(string name)
        {
            UserManager userManager = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<User> u = userManager.Search(name, false);

            if (u == null)
                return ServiceResponse.Error("User not found.");

            u = UserManager.ClearSensitiveData(u);

            return ServiceResponse.OK("", u);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 3)]
        [System.Web.Http.HttpPatch]
        [System.Web.Http.Route("api/Users/{userUUID}/Flag/{userFlag}/Value/{flagValue}")]
        public ServiceResult SetUserFlag(string userUUID, string userFlag, string flagValue)
        {
            if (CurrentUser.Banned || CurrentUser.LockedOut)
                return ServiceResponse.Error("You're account is suspended, you do not have authorization.");

            if (string.IsNullOrWhiteSpace(userUUID))
                return ServiceResponse.Error("No user id sent.");

            if (string.IsNullOrWhiteSpace(userFlag))
                return ServiceResponse.Error("No flag sent.");

            if (string.IsNullOrWhiteSpace(flagValue))
                return ServiceResponse.Error("No value sent.");

            var user = this.GetUser(this.GetAuthToken(Request));
            RoleManager rm = new RoleManager(Globals.DBConnectionKey, user);
            //var role = rm.GetRole("admin", user.AccountUUID);
            //if (role == null && (!rm.IsSiteAdmin(user.Name) || !rm.IsInRole(user.UUID, user.AccountUUID, role.UUID, false)))
            //    return ServiceResponse.Error("You are not authorized this action.");

            var userRoles = rm.GetRolesForUser(user.UUID, user.AccountUUID).OrderByDescending(o => o.RoleWeight);

            if (userRoles == null || userRoles.Any() == false)
                return ServiceResponse.Error("You are not assigned a role allowing you to flag items.");

            bool isAuthorized = false;
            foreach (var userRole in userRoles)
            {
                if (userRole.RoleWeight > 90)
                {
                    isAuthorized = true;
                    break;
                }
            }

            if (!isAuthorized)
                return ServiceResponse.Error("You are not assigned a role allowing you to flag items as safe.");

            UserManager um = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            var tmp = um.Get(userUUID);

            if (tmp.Code != 200)
                return tmp;

            var dbUser = (User)tmp.Result;

            switch (userFlag.ToUpper())
            {
                case "BAN":
                    if (userUUID == user.UUID)
                        return ServiceResponse.Error("You can't ban/unban yourself.");

                    dbUser.Banned = flagValue.ConvertTo<bool>();
                    break;

                case "LOCKEDOUT":
                    if (userUUID == user.UUID)
                        return ServiceResponse.Error("You can't unlock/lock yourself.");
                    dbUser.LockedOut = flagValue.ConvertTo<bool>();
                    if (dbUser.LockedOut)
                        dbUser.LastLockoutDate = DateTime.UtcNow;
                    break;
            }

            return um.Update(dbUser);
        }

        /// <summary>
        /// Updated fields
        /// Name
        /// Private
        /// SortOrder
        /// Active
        /// LicenseNumber
        /// Anonymous
        /// Approved
        /// Banned
        /// Deleted
        /// LockedOut
        /// Email
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpPatch]
        [System.Web.Http.Route("api/Users/Update")]
        public ServiceResult Update(UserForm u)
        {
            if (u == null)
                return ServiceResponse.Error("Invalid user form sent.");

            UserSession session = SessionManager.GetSession(this.GetAuthToken(Request));
            if (session == null)
                return ServiceResponse.Error("User session has timed out. You must login to complete this action.");

            UserManager userManager = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            var res = userManager.GetUser(u.UUID, false);
            if (res.Code != 200)
                return res;
            User dbAcct = (User)res.Result;

            if (dbAcct == null)
                return ServiceResponse.Error("User was not found.");

            dbAcct.Name = u.Name;
            dbAcct.AccountUUID = u.AccountUUID;
            dbAcct.Private = u.Private;
            dbAcct.SortOrder = u.SortOrder;
            dbAcct.Active = u.Active;
            dbAcct.LicenseNumber = u.LicenseNumber;
            dbAcct.Anonymous = u.Anonymous;
            dbAcct.Approved = u.Approved;
            dbAcct.Banned = u.Banned;
            dbAcct.Deleted = u.Deleted;
            dbAcct.LockedOut = u.LockedOut;
            dbAcct.Email = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), u.Email.ToLower(), true);
            dbAcct.PasswordQuestion = u.PasswordQuestion;
            dbAcct.PasswordAnswer = u.PasswordAnswer;
            //ParentId
            //UUIDType
            //UUParentID
            //UUParentID
            //Status

            ServiceResult updateResult = userManager.Update(dbAcct);
            if (updateResult.Code == 200 && CurrentUser.UUID == dbAcct.UUID)
            {
                //update session
                session.UserData = JsonConvert.SerializeObject(UserManager.ClearSensitiveData(dbAcct));
                SessionManager.Update(session);
                return ServiceResponse.OK();
            }
            return updateResult;
        }

        ///// <summary>
        ///// This is from the app. sends just a basic message body.
        ///// subject etc. are generated.
        ///// </summary>
        ///// <returns></returns>
        //// [EnableThrottling(PerHour = 5, PerDay = 20)]
        //[System.Web.Http.HttpPost]
        //[System.Web.Http.Route("api/Users/Send/Message/{itemUUID}/{itemType}")]
        //public async Task<ServiceResult> SendMessageAsync(string itemUUID, string itemType)
        //{
        //    if(string.IsNullOrWhiteSpace(itemUUID))
        //        return ServiceResponse.Error("You must send an item id for the message.");

        //    if (string.IsNullOrWhiteSpace(itemType))
        //        return ServiceResponse.Error("You must send an item type for the message.");

        //    if(!itemType.EqualsIgnoreCase("item"))
        //        return ServiceResponse.Error("You must send a supported item type.");

        //    if (CurrentUser == null)
        //        return ServiceResponse.Error("You must be logged in to access this function.");

        //    try
        //    {
        //        string message = await Request.Content.ReadAsStringAsync();

        //        if (string.IsNullOrEmpty(message))
        //            return ServiceResponse.Error("You must send valid email info.");

        //        InventoryManager inventoryManager = new InventoryManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

        //        var res= inventoryManager.Get(itemUUID);
        //        if (res.Code != 200)
        //            return res;
        //        var item = res.Result as InventoryItem;

        //        UserManager userManager = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

        //        var res2 = userManager.GetUser(item.CreatedBy, false);
        //        if (res2.Code != 200)
        //            return res2;
        //        var toUser = res2.Result as User;

        //        //decrypt to send
        //        string emailTo = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), toUser.Email, false);

        //        EmailSettings settings = new EmailSettings();
        //        settings.EncryptionKey = Globals.Application.AppSetting("AppKey");
        //        settings.HostPassword = Globals.Application.AppSetting("EmailHostPassword");
        //        settings.HostUser = Globals.Application.AppSetting("EmailHostUser");
        //        settings.MailHost = Globals.Application.AppSetting("MailHost");
        //        settings.MailPort = StringEx.ConvertTo<int>(Globals.Application.AppSetting("MailPort"));
        //        settings.SiteDomain = Globals.Application.AppSetting("SiteDomain");
        //        settings.EmailDomain = Globals.Application.AppSetting("EmailDomain");
        //        settings.SiteEmail = Globals.Application.AppSetting("SiteEmail");
        //        settings.UseSSL = StringEx.ConvertTo<bool>(Globals.Application.AppSetting("UseSSL"));

        //        string ip = network.GetClientIpAddress(this.Request);
        //        string Subject =  CurrentUser.Name + " sent a message about " + item.Name;
        //        string msg = message;
        //        string emailFrom = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), CurrentUser.Email, false);
        //        return await userManager.SendEmailAsync(ip, emailTo, emailFrom, Subject, msg, settings);
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.Assert(false, ex.Message);
        //        return ServiceResponse.Error(ex.Message);
        //    }
        //    return ServiceResponse.OK();
        //}

        //// [EnableThrottling(PerHour = 5, PerDay = 20)]
        //[System.Web.Http.HttpPost]
        //[System.Web.Http.Route("api/Users/SendMessage")]
        //public async Task<ServiceResult> SendMessageAsync()
        //{
        //    if (CurrentUser == null)
        //        return ServiceResponse.Error("You must be logged in to access this function.");

        //    string emailTo = "";
        //    try
        //    {
        //        string body = await Request.Content.ReadAsStringAsync();

        //        if (string.IsNullOrEmpty(body))
        //            return ServiceResponse.Error("You must send valid email info.");

        //        dynamic formData = JsonConvert.DeserializeObject<dynamic>(body);

        //        if (formData == null)
        //            return ServiceResponse.Error("Invalid email info.");

        //        if( formData.MessageType == "ContactAdmin")
        //        {
        //            if ( string.IsNullOrWhiteSpace(Globals.Application.AppSetting("SiteEmail","") ))
        //            {
        //                return ServiceResponse.Error("Site email is not set.");
        //            }
        //            emailTo = Globals.Application.AppSetting("SiteEmail", "");
        //        }

        //        EmailSettings settings = new EmailSettings();
        //        settings.EncryptionKey = Globals.Application.AppSetting("AppKey");
        //        settings.HostPassword =  Globals.Application.AppSetting("EmailHostPassword");
        //        settings.HostUser = Globals.Application.AppSetting("EmailHostUser");
        //        settings.MailHost = Globals.Application.AppSetting("MailHost");
        //        settings.MailPort = StringEx.ConvertTo<int>(Globals.Application.AppSetting("MailPort"));
        //        settings.SiteDomain = Globals.Application.AppSetting("SiteDomain");
        //        settings.EmailDomain = Globals.Application.AppSetting("EmailDomain");
        //        settings.SiteEmail = Globals.Application.AppSetting("SiteEmail");
        //        settings.UseSSL = StringEx.ConvertTo<bool>(Globals.Application.AppSetting("UseSSL"));

        //        string ip = network.GetClientIpAddress(this.Request);
        //        string Subject = formData.Subject.ToString();
        //        string msg = formData.Message.ToString();
        //        UserManager userManager = new UserManager(Globals.DBConnectionKey,this.GetAuthToken(Request));
        //        string emailFrom = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), CurrentUser.Email, false);
        //        return await  userManager.SendEmailAsync(ip, emailTo, emailFrom, Subject, msg, settings);
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.Assert(false, ex.Message);
        //        return ServiceResponse.Error(ex.Message);
        //    }
        //}

        //// [EnableThrottling(PerHour = 5, PerDay = 20)]
        //[System.Web.Http.HttpPost]
        //[System.Web.Http.Route("api/Users/Message")]
        //public async Task<ServiceResult> SendMessageToUserAsync( )
        //{
        //    if (CurrentUser == null)
        //        return ServiceResponse.Error("You must be logged in to access this function.");

        //    EmailSettings settings = new EmailSettings();
        //    settings.FromUserUUID = CurrentUser.UUID;

        //    try
        //    {
        //        string content = await Request.Content.ReadAsStringAsync();

        //        if (string.IsNullOrEmpty(content))
        //            return ServiceResponse.Error("You must send valid email info.");

        //        var message = JsonConvert.DeserializeObject<EmailMessage>(content);

        //        if (string.IsNullOrWhiteSpace(message.SendTo))
        //            return ServiceResponse.Error("You must send a user id for the message.");

        //        if (string.IsNullOrWhiteSpace(message.Body))
        //            return ServiceResponse.Error("You must send comment in the message.");

        //        string emailTo = "";

        //        UserManager userManager = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

        //        switch (message.Type?.ToUpper())
        //        {
        //            case "ACCOUNT":
        //                var am = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
        //                var res = am.Get(message.SendTo);
        //                if (res.Code != 200)
        //                    return res;
        //                Account account = (Account)res.Result;

        //                emailTo = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), account.Email, false);
        //                break;
        //            case "SUPPORT":
        //                //todo call api/Site/SendMessage

        //                break;
        //            case "PROFILE":
        //                ProfileManager profileManager = new ProfileManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
        //                //todo call api/Site/SendMessage
        //                var dbProfile = profileManager.Get(message.SendTo);
        //                if (dbProfile.Code != 200)
        //                    return ServiceResponse.Error("Profile not found.");

        //                var node = userManager.Get( ((Profile)dbProfile.Result).UserUUID );
        //                if(node.Code != 200 )
        //                    return node;

        //                var user  = (User)node.Result;
        //                settings.ToUserUUID = user.UUID;
        //                emailTo = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), user.Email, false);

        //                if (string.IsNullOrWhiteSpace(message.Subject))
        //                    message.Subject = "You have received a message from " + CurrentUser.Name;
        //                break;
        //            default:

        //                var res2 = userManager.GetUser(message.SendTo, false);
        //                if (res2.Code != 200)
        //                    return res2;

        //                var toUser = (User)res2.Result;

        //                if (toUser == null)
        //                    return ServiceResponse.Error("User not found.");

        //                settings.ToUserUUID = toUser.UUID;
        //                emailTo = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), toUser.Email, false);
        //                break;
        //        }

        //        if(string.IsNullOrWhiteSpace(emailTo))
        //            return ServiceResponse.Error("Members email is not set.");

        //        string hostPassword = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), Globals.Application.AppSetting("EmailHostPassword"), false);

        //        settings.EncryptionKey = Globals.Application.AppSetting("AppKey");
        //        settings.HostPassword = hostPassword;// Globals.Application.AppSetting("EmailHostPassword");
        //        settings.HostUser = Globals.Application.AppSetting("EmailHostUser");
        //        settings.MailHost = Globals.Application.AppSetting("MailHost");
        //        settings.MailPort = StringEx.ConvertTo<int>(Globals.Application.AppSetting("MailPort"));
        //        settings.SiteDomain = Globals.Application.AppSetting("SiteDomain");
        //        settings.EmailDomain = Globals.Application.AppSetting("EmailDomain");
        //        settings.SiteEmail = Globals.Application.AppSetting("SiteEmail");
        //        settings.UseSSL = StringEx.ConvertTo<bool>(Globals.Application.AppSetting("UseSSL"));

        //        string ip = network.GetClientIpAddress(this.Request);
        //        string Subject = message.Subject;
        //        string msg = message.Body;
        //        string emailFrom = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), CurrentUser.Email, false);
        //        return await userManager.SendEmailAsync(ip, emailTo, emailFrom, Subject, msg, settings);

        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.Assert(false, ex.Message);
        //        return ServiceResponse.Error(ex.Message);
        //    }
        //}

        /// <summary>
        ////This occures after the registration on the page, the user recieves an email, and clicks a link to validate
        ////their email.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="operation"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        ///
        [System.Web.Http.AllowAnonymous]
        [System.Web.Http.HttpPost]
        // [EnableThrottling(PerSecond = 1, PerHour = 5, PerDay = 10)]
        [System.Web.Http.Route("api/Membership/Validate/type/{type}/operation/{operation}/code/{code}")]
        [System.Web.Http.Route("api/Users/Validate/type/{type}/operation/{operation}/code/{code}")]
        public ServiceResult Validate(string type = "", string operation = "", string code = "")
        {
            ServiceResult res;

            //#if DEBUG

            //            res = ServiceResponse.OK("Code validated.");
            //#else
            string ip = network.GetClientIpAddress(this.Request);
            UserManager userManager = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            res = userManager.Validate(type, operation, code, ip, CurrentUser);
            //#endif
            return res;
        }
    }
}