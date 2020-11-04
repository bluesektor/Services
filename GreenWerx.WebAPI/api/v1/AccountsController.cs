// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using GreenWerx.Data.Logging;
using GreenWerx.Managers.Events;
using GreenWerx.Managers.Geo;
using GreenWerx.Managers.Logging;
using GreenWerx.Managers.Membership;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Flags;
using GreenWerx.Models.General;
using GreenWerx.Models.Membership;
using GreenWerx.Models.Services;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Utilites.Security;
using GreenWerx.Web.api.Helpers;
using GreenWerx.Web.Filters;
using GreenWerx.Web.Models;
using WebApi.OutputCache.V2;
using WebApiThrottle;

namespace GreenWerx.Web.api.v1
{
    public class AccountsController : ApiBaseController
    {
        private readonly SystemLogger _logger = null;

        public AccountsController()
        {
            _logger = new SystemLogger(Globals.DBConnectionKey);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 0)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Accounts/{accountUUID}/Favorite")]
        public ServiceResult AddAccountToFavorites(string accountUUID)
        {
            if (string.IsNullOrWhiteSpace(accountUUID))
                return ServiceResponse.Error("No event id sent.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var temp = accountManager.Get(accountUUID);
            if (temp.Code != 200)
                return temp;
            var e = (Account)temp.Result;

            Favorite r = new Favorite()
            {
                RoleOperation = e.RoleOperation,
                RoleWeight = e.RoleWeight,
                Private = true,
                Name = e.Name,
                UUIDType = e.UUIDType,
                ItemUUID = e.UUID,
                AccountUUID = CurrentUser.AccountUUID,
                CreatedBy = CurrentUser.UUID,
                DateCreated = DateTime.UtcNow,
                Active = true,
                Deleted = false,
            };

            ReminderManager reminderManager = new ReminderManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return reminderManager.Insert(r);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Accounts/{accountUUID}/Users/Add")]
        public async Task<ServiceResult> AddUsersToAccount(string accountUUID)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            ServiceResult res;
            try
            {
                string content = await Request.Content.ReadAsStringAsync();
                if (content == null)
                    return ServiceResponse.Error("No users were sent.");

                string body = content;

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("No users were sent.");

                List<User> users = JsonConvert.DeserializeObject<List<User>>(body);
                List<AccountMember> members = new List<AccountMember>();

                foreach (var user in users)
                {
                    members.Add(new AccountMember { AccountUUID = accountUUID, MemberType = "User", MemberUUID = user.UUID, RoleWeight = 10 });
                }

                AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                res = accountManager.AddUsersToAccount(accountUUID, members, CurrentUser);
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
        [System.Web.Http.Route("api/Accounts/{accountUUID}/Users/{userUUID}/Add")]
        public ServiceResult AddUserToAccount(string accountUUID, string userUUID)
        {
            AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            return accountManager.AddUserToAccount(accountUUID, userUUID, CurrentUser);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="newPassword"></param>
        /// <param name="passwordConfirm"></param>
        /// <param name="userUUID"></param>
        /// <param name="confCode"></param>
        /// <param name="captcha"></param>
        /// <returns></returns>
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Accounts/ChangePassword/")]
        public ServiceResult ChangePassword(ChangePassword frm)
        {
            if (frm == null)
            {
                return ServiceResponse.Error("Invalid data.");
            }

            NetworkHelper network = new NetworkHelper();
            string ipAddress = network.GetClientIpAddress(this.Request);
            string sessionToken = "";
            User u = null;
            ServiceResult res = null;
            UserManager userManager = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            if (frm.ResetPassword)
            {//if a reset then the user isn't logged in, so get the user by alt means.
             //only use captcha on reset
                if (string.IsNullOrWhiteSpace(frm.ConfirmationCode))
                    return ServiceResponse.Error("Invalid confirmation code. You must use the link provided in the email in order to reset your password.");

                //string encEmail = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), frm.Email.ToLower(), true);
                u = userManager.GetUsers(false)?.FirstOrDefault(dw => (dw.ProviderUserKey == frm.ConfirmationCode));

                if (u == null)
                    return ServiceResponse.Error("Invalid confirmation code.");
            }
            else
            {
                if (Request.Headers.Authorization == null)
                    return ServiceResponse.Error("You must be logged in to change your password.");

                sessionToken = this.GetAuthToken(Request);
                u = GetUser(sessionToken);//since the user session doesn't contain the password, wi have to pull it.
                res = userManager.GetUser(u.UUID, false);

                if (res == null || res.Code != 200)
                {
                    SessionManager.DeleteSession(sessionToken);
                    return res;// ServiceResponse.Error("Session error. If your logged in try logging out and back in.");
                }
            }

            if (frm.NewPassword != frm.ConfirmPassword)
                return ServiceResponse.Error("Password don't match.");

            if (string.IsNullOrWhiteSpace(frm.NewPassword) || string.IsNullOrWhiteSpace(frm.ConfirmPassword))
                return ServiceResponse.Error("Password can't be empty. ");

            if (PasswordHash.CheckStrength(frm.NewPassword) < PasswordHash.PasswordScore.Medium)
                return ServiceResponse.Error("Password is too weak. ");

            if (frm.ResetPassword)
            {
                //string encEmail = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), frm.Email.ToLower(), true);
                if (u.ProviderName != UserFlags.ProviderName.ForgotPassword || u.ProviderUserKey != frm.ConfirmationCode) //|| u.Email != encEmail
                {//
                    string msg = "Invalid informaition posted to server";
                    SystemLogger logger = new SystemLogger(Globals.DBConnectionKey);
                    logger.InsertSecurity(msg, "AccountController", "ChangePassword");
                    return ServiceResponse.Error("Invalid confirmation code.");
                }
            }
            else //just a user updating their password.
            {   // verify old password
                if (!PasswordHash.ValidatePassword(frm.OldPassword, u.PasswordHashIterations + ":" + u.PasswordSalt + ":" + u.Password))
                {
                    return ServiceResponse.Error("Invalid password.");
                }
            }

            ServiceResult sr = userManager.IsUserAuthorized(u, ipAddress);
            if (sr.Status == "ERROR")
                return sr;

            string tmpHashPassword = PasswordHash.CreateHash(frm.NewPassword);

            u.Password = PasswordHash.ExtractHashPassword(tmpHashPassword);
            u.PasswordHashIterations = PasswordHash.ExtractIterations(tmpHashPassword);
            u.PasswordSalt = PasswordHash.ExtractSalt(tmpHashPassword);
            u.ProviderName = "";
            u.ProviderUserKey = "";
            u.LastPasswordChangedDate = DateTime.UtcNow;

            ServiceResult updateResult = userManager.UpdateUser(u, false);
            if (updateResult.Code != 200)
                return ServiceResponse.Error("Error updating password. Try again later.");

            return ServiceResponse.OK("Password has been updated.");
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Accounts/Delete")]
        public ServiceResult Delete(Account n)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            DataFilter filter = this.GetFilter(Request);

            AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return accountManager.Delete(n, filter?.Purge ?? false);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Accounts/{accountUUID}/Delete")]
        public ServiceResult Delete(string accountUUID)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            DataFilter filter = this.GetFilter(Request);

            AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return accountManager.Delete(accountUUID, filter?.Purge ?? false);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.Route("api/Accounts/{name}")]
        public ServiceResult Get(string name)
        {
            DataFilter filter = this.GetFilter(Request);
            AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<Account> a = accountManager.Search(name, ref filter);
            if (a == null || a.Count == 0)
                return ServiceResponse.Error("Invalid name.");

            return ServiceResponse.OK("", a);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Accounts/{accountUUID}/Permissions")]
        public ServiceResult GetAccountPermissons(string accountUUID = "")
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(accountUUID))
                return ServiceResponse.Error("You must provide an account id to view it's members.");

            DataFilter filter = this.GetFilter(Request);
            RoleManager rm = new RoleManager(Globals.DBConnectionKey, this.GetUser(this.GetAuthToken(Request)));
            List<dynamic> permissions = rm.GetAccountPermissions(accountUUID, ref filter).Cast<dynamic>().ToList();

            return ServiceResponse.OK("", permissions, filter.TotalRecordCount);
        }

        /// <summary>
        /// Returns accounts user is a member of in UsersInAccounts table
        /// </summary>
        /// <returns></returns>
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Accounts")]
        public ServiceResult GetAccounts()
        {
            GetUser(this.GetAuthToken(Request));
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            DataFilter filter = this.GetFilter(Request);

            AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<dynamic> accounts = accountManager.GetAccounts(CurrentUser.UUID, ref filter).Cast<dynamic>().ToList();

            return ServiceResponse.OK("", accounts, filter.TotalRecordCount);
        }

   //     [CacheOutput(ClientTimeSpan = 100, ServerTimeSpan = 100)]
   //    // // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [System.Web.Http.AllowAnonymous]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/AllAccounts")]
        public ServiceResult GetAllAccounts()
        {
            AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            DataFilter filter = this.GetFilter(Request);

            var accounts = accountManager.GetAllAccountsEx(ref filter);
     
            return ServiceResponse.OK("", accounts, filter.TotalRecordCount);
        }

       // [System.Web.Http.AllowAnonymous]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Accounts/Search/{searchText}")]
        public ServiceResult SearchAllAccounts(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return ServiceResponse.BadRequest("Invalid search text.");

            AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
        
            var accounts = accountManager.SearchEx(searchText);

            return ServiceResponse.OK("", accounts, accounts?.Count ?? 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="domainName">root domain only. google.com, yahoo.com..</param>
        /// <returns></returns>
        [System.Web.Http.AllowAnonymous]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/account/domain")]
        public ServiceResult GetAccountByDomain( )
        {
            AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            Task<string> content = Request.Content.ReadAsStringAsync();
            if (content == null)
                return ServiceResponse.Error("No domain was sent.");

            string domainName = content.Result;

            var account = accountManager.GetAccountByDomain(domainName);

            return ServiceResponse.OK("", account);
        }

       // // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [System.Web.Http.AllowAnonymous]
        [System.Web.Http.Route("api/AccountsBy/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return accountManager.Get(uuid);
        }

        [CacheOutput(ClientTimeSpan = 100, ServerTimeSpan = 100)]
        [System.Web.Http.AllowAnonymous]
       // // [EnableThrottling(PerSecond = 3)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Accounts/Categories")]
        public ServiceResult GetEventCategories()
        {
            var filter = this.GetFilter(Request);
            AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<string> categories = accountManager.GetAccountCategories(ref filter);
            return ServiceResponse.OK("", categories, filter.TotalRecordCount);
        }

       // // [EnableThrottling(PerSecond = 3)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Accounts/Favorites")]
        public ServiceResult GetFavoriteAccounts()
        {
            DataFilter filter = this.GetFilter(Request);

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to view favorites.");

            AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            var accounts = accountManager.GetFavoriteAccounts(CurrentUser.UUID, CurrentUser.AccountUUID, ref filter);
            return ServiceResponse.OK("", accounts, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Accounts/{accountUUID}/Members")]
        public ServiceResult GetMembers(string accountUUID = "")
        {
            if (string.IsNullOrWhiteSpace(accountUUID))
                return ServiceResponse.Error("You must provide an account id to view it's members.");

            DataFilter filter = this.GetFilter(Request);

            AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<dynamic> accountMembers = accountManager.GetAccountMembers(accountUUID, ref filter).Cast<dynamic>().ToList();
            return ServiceResponse.OK("", accountMembers, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Accounts/{accountUUID}/NonMembers")]
        public ServiceResult GetNonMembers(string accountUUID = "")
        {
            if (string.IsNullOrWhiteSpace(accountUUID))
                return ServiceResponse.Error("You must provide an account id to complete this action.");

            DataFilter filter = this.GetFilter(Request);
            AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<dynamic> accountMembers = accountManager.GetAccountNonMembers(accountUUID, ref filter).Cast<dynamic>().ToList();
            return ServiceResponse.OK("", accountMembers, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Accounts/Add")]
        [System.Web.Http.Route("api/Accounts/Insert")]
        public ServiceResult Insert(Account n)
        {
            AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = accountManager.Insert(n);
            if (res.Code != 200)
                return res;

            accountManager.CreateDefaultRolesForAccount((Account)res.Result);

            return res;
        }

        [System.Web.Http.AllowAnonymous]
        [System.Web.Http.HttpPost]
        // // [EnableThrottling(PerSecond = 1, PerHour = 10, PerDay = 100)]
        [System.Web.Http.Route("api/test/echo")]
        public ServiceResult EchoTest(Echo values)
        {
            if(values == null)
             return ServiceResponse.Error("NO DATA WAS SENT", values);

            return ServiceResponse.OK(values.StringValue, values);


        }

        public class Echo
        {
            public string StringValue { get; set; }
            public int NumericValue { get; set; }
            public bool BoolValue { get; set; }
        }

        [System.Web.Http.AllowAnonymous]
        [System.Web.Http.HttpPost]
       // // [EnableThrottling(PerSecond = 1, PerHour = 10, PerDay = 100)]
        [System.Web.Http.Route("api/Accounts/Login")]
        public ServiceResult Login([FromBody]LoginModel credentials)
        {
            if (credentials == null)
            {
                string body = "init";
                try
                {
                    body = "body: " + JsonConvert.SerializeObject(Request.Content); //Request.Content.ReadAsStringAsync().Result;
                }
                catch { }
                return ServiceResponse.Error("Invalid login."+ body);
            }

            if (string.IsNullOrWhiteSpace(credentials.UserName))
                return ServiceResponse.Error("Invalid username.");

            if (string.IsNullOrWhiteSpace(credentials.Password))
                return ServiceResponse.Error("Invalid password.");

            if (string.IsNullOrEmpty(credentials.ReturnUrl))
                credentials.ReturnUrl = "";

            AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, Request.Headers.Authorization?.Parameter);
            NetworkHelper network = new NetworkHelper();
            UserManager userManager = new UserManager(Globals.DBConnectionKey, Request.Headers.Authorization?.Parameter);
            RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, this.GetUser(Request.Headers.Authorization?.Parameter));
            LocationManager lm = new LocationManager(Globals.DBConnectionKey, Request.Headers.Authorization?.Parameter);

            string ipAddress = network.GetClientIpAddress(this.Request);
            string userName = credentials.UserName;
            string accountName = "";
            User user = null;
            var filter = this.GetFilter(Request);
            #region

            if (userName.Contains("/"))
            {
                accountName = userName.Split('/')[0];
                userName = userName.Split('/')[1];
                user = userManager.Search(userName, false)?.FirstOrDefault();
                if (user == null)
                    return ServiceResponse.Error("Invalid username or password.");
                string accountUUID = accountManager.Search(accountName, ref filter)?.FirstOrDefault()?.UUID;

                if (string.IsNullOrWhiteSpace(accountUUID))
                    return ServiceResponse.Error("Invalid account name " + accountName);

                if (!accountManager.IsUserInAccount(accountUUID, user.UUID))
                    return ServiceResponse.Error("You are not a member of the account.");
                if (user.AccountUUID != accountUUID)
                {
                    user.AccountUUID = accountUUID;
                    userManager.Update(user);
                }
            }
            else
            {


                user = userManager.Search(userName, false)?.FirstOrDefault();
                if (user == null)
                    return ServiceResponse.Error("Invalid username or password.");
            }
            #endregion

            ServiceResult sr = userManager.AuthenticateUser(userName, credentials.Password, ipAddress);

            if (sr.Status != "OK")
                return sr;
            #region
            //if (user.Email.EqualsIgnoreCase("JQ8E9yA7T5JRkv4I16zlGyAJryKjU9cNXRcCq/qqjeYuKJ99iSou3mC1X9T9k2e6"))
            //{
            //    Profile userProfile = new Profile();
            //    var role = roleManager.Search("owner", "member").Where(w => w.AccountUUID == user.AccountUUID).FirstOrDefault();
            //        using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
            //        {
            //        userProfile = context.GetAll<Profile>().FirstOrDefault(w => w.AccountUUID == user.AccountUUID && w.UserUUID == user.UUID);
            //            if (role == null)
            //            {
            //                role = new Role()
            //                {
            //                    AccountUUID = user.AccountUUID,
            //                    UUID = Guid.NewGuid().ToString("N"),
            //                    Active = true,
            //                    DateCreated = DateTime.UtcNow,
            //                    EndDate = DateTime.UtcNow,
            //                    StartDate = DateTime.UtcNow,
            //                    Persists = true,
            //                    Name = "owner",
            //                    Category = "member",
            //                    CategoryRoleName = "owner",
            //                    RoleOperation = "=",
            //                    RoleWeight = 1000
            //                };
            //                context.Insert<Role>(role);
            //            }
            //        }
            //    //is user in role owner?
            //    //if (!roleManager.IsInRole(user.UUID, user.AccountUUID, role.UUID, false) && !roleManager.IsInRole(userProfile.UUID, "member", "owner"))
            //    //{
            //        if (role != null)
            //        {
            //        //roleManager.AddUserToRole(role.UUID,user,
            //        using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
            //            {
            //            ////is user in role
            //              var  isInRole = context.GetAll<UserRole>().Any(w => w.Deleted == false &&
            //                                w.RoleUUID == role.UUID &&
            //                               w.ReferenceUUID == user.UUID &&
            //                               w.AccountUUID == user.AccountUUID);
            //            if (!isInRole)
            //            {
            //                UserRole ur = new UserRole()
            //                {
            //       Name = role.Name,
            //                    AccountUUID = user.AccountUUID,
            //                    Active = true,
            //                    CreatedBy = user.UUID,
            //                    DateCreated = DateTime.UtcNow,
            //                    ReferenceUUID = user.UUID,
            //                    RoleUUID = role.UUID,
            //                    RoleOperation = role.RoleOperation,
            //                    RoleWeight = role.RoleWeight
            //                };
            //                context.Insert<UserRole>(ur);
            //            }
            //            isInRole = context.GetAll<UserRole>().Any(w => w.Deleted == false &&
            //                               w.RoleUUID == role.UUID &&
            //                              w.ReferenceUUID == userProfile.UUID &&
            //                              w.AccountUUID == user.AccountUUID);
            //            if (!isInRole)
            //            {
            //                UserRole urp = new UserRole()
            //                {
            //  Name = role.Name,
            //                    AccountUUID = user.AccountUUID,
            //                    Active = true,
            //                    CreatedBy = userProfile.UUID,
            //                    DateCreated = DateTime.UtcNow,
            //                    ReferenceUUID = user.UUID,
            //                    RoleUUID = role.UUID,
            //                    RoleOperation = role.RoleOperation,
            //                    RoleWeight = role.RoleWeight
            //                };
            //                context.Insert<UserRole>(urp);
            //            }
            //            }
            //        }
            //    //}
            //}
            #endregion
            string userJson = JsonConvert.SerializeObject(user);

            UserSession us = null;
            if (credentials.ClientType == "mobile.app")//if mobile make the session persist.
                us = SessionManager.SaveSession(ipAddress, user.UUID, user.AccountUUID, userJson, true);
            else
                us = SessionManager.SaveSession(ipAddress, user.UUID, user.AccountUUID, userJson, false);

            if (us == null)
                return ServiceResponse.Error("Failed to save your session.");

            Request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", us.AuthToken);
            Dashboard dashBoard = new Dashboard();

            dashBoard.SessionLength = Convert.ToDouble(Globals.Application.AppSetting("SessionLength", "60"));

            dashBoard.Authorization = us.AuthToken;
            dashBoard.UserUUID = user.UUID;
            dashBoard.AccountUUID = user.AccountUUID;
            dashBoard.ReturnUrl = credentials.ReturnUrl;

            dashBoard.IsAdmin = user.SiteAdmin == true ? true : roleManager.IsUserRequestAuthorized(dashBoard.UserUUID, dashBoard.AccountUUID, "/Admin");
            ProfileManager profileManager = new ProfileManager(Globals.DBConnectionKey, Request.Headers.Authorization?.Parameter);
            var tmp = profileManager.GetProfile(user.UUID, user.AccountUUID, true);
            if (tmp.Code == 200)
                dashBoard.Profile = (Profile)tmp.Result;

            dashBoard.AccountRoles = roleManager.GetRolesForUser(user.UUID, user.AccountUUID);// ( // roleManager.GetRoles(user.AccountUUID);

            return ServiceResponse.OK("", dashBoard);
        }

       // [System.Web.Http.AllowAnonymous]
       // [System.Web.Http.HttpPost]
       //// // [EnableThrottling(PerSecond = 1, PerHour = 10, PerDay = 100)]
       // [System.Web.Http.Route("api/Accounts/LoginAsync")]
       // public async Task<ServiceResult> LoginAsync(LoginModel credentials)
       // {
       //     if (!ModelState.IsValid || credentials == null)
       //         return ServiceResponse.Error("Invalid login.");

       //     if (string.IsNullOrWhiteSpace(credentials.UserName))
       //         return ServiceResponse.Error("Invalid username.");

       //     if (string.IsNullOrWhiteSpace(credentials.Password))
       //         return ServiceResponse.Error("Invalid password.");

       //     AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
       //     NetworkHelper network = new NetworkHelper();
       //     UserManager userManager = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
       //     RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, CurrentUser);

       //     string ipAddress = network.GetClientIpAddress(this.Request);

       //     if (string.IsNullOrEmpty(credentials.ReturnUrl))
       //         credentials.ReturnUrl = "";

       //     string userName = credentials.UserName;
       //     string accountName = "";
       //     List<User> users;
       //     User user = null;
       //     var filter = this.GetFilter(Request);

       //     if (userName.Contains("/"))
       //     {
       //         accountName = userName.Split('/')[0];
       //         userName = userName.Split('/')[1];

       //         users = userManager.Search(userName, false);
       //         if (users == null || users.Count == 0)
       //             return ServiceResponse.Error("Invalid username or password.");

       //         user = users.FirstOrDefault();

       //         string accountUUID = accountManager.Search(accountName, ref filter)?.FirstOrDefault()?.UUID;

       //         if (!accountManager.IsUserInAccount(accountUUID, user.UUID))
       //             return ServiceResponse.Error("You are not a member of the account.");

       //         if (user.AccountUUID != accountUUID)
       //         {
       //             user.AccountUUID = accountUUID;
       //             userManager.Update(user);
       //         }
       //     }
       //     else
       //     {
       //         user = userManager.Search(userName, false)?.FirstOrDefault();
       //         if (user == null)
       //             return ServiceResponse.Error("Invalid username or password.");
       //     }

       //     userManager = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
       //     ServiceResult sr = await userManager.AuthenticateUserAsync(userName, credentials.Password, ipAddress);

       //     if (sr.Status != "OK")
       //         return sr;

       //     string userJson = JsonConvert.SerializeObject(user);

       //     UserSession us = null;
       //     if (credentials.ClientType == "mobile.app")//if mobile make the session persist.
       //         us = SessionManager.SaveSession(ipAddress, user.UUID, user.AccountUUID, userJson, true);
       //     else
       //         us = SessionManager.SaveSession(ipAddress, user.UUID, user.AccountUUID, userJson, false);

       //     if (us == null)
       //         return ServiceResponse.Error("Server was unable to create a session, try again later.");

       //     Dashboard dashBoard = new Dashboard();
       //     dashBoard.Authorization = us.AuthToken;
       //     dashBoard.UserUUID = user.UUID;
       //     dashBoard.AccountUUID = user.AccountUUID;
       //     dashBoard.ReturnUrl = credentials.ReturnUrl;
       //     dashBoard.IsAdmin = roleManager.IsUserRequestAuthorized(dashBoard.UserUUID, dashBoard.AccountUUID, "/Admin");
       //     ProfileManager profileManager = new ProfileManager(Globals.DBConnectionKey, Request.Headers.Authorization?.Parameter);
       //     var tmp = profileManager.GetProfile(user.UUID, user.AccountUUID, true);
       //     if (tmp.Code == 200)
       //         dashBoard.Profile = (Profile)tmp.Result;
       //     return ServiceResponse.OK("", dashBoard);
       // }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Accounts/LogOut")]
        public ServiceResult LogOut()
        {
            string authToken = this.GetAuthToken(Request);

            if (string.IsNullOrWhiteSpace(authToken))
            {
                return ServiceResponse.Error("Authorization token must be sent.");
            }

            UserSession us = SessionManager.GetSession(authToken);
            if (us == null)
                return ServiceResponse.OK();

            SessionManager.DeleteSession(authToken);

            UserManager userManager = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            //IMPORTANT!!! when you know an update is going to be used on the user internally,
            //DO NOT CLEAR THE SENSITIVE DATA! It will make the password blank!
            var res = userManager.GetUser(us.UserUUID, false);
            if (res.Code != 200)
                return res;
            User u = (User)res.Result;

            u.Online = false;

            userManager.Update(u);

            Request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "");

            return ServiceResponse.OK("Authorization:");
        }

        [System.Web.Http.AllowAnonymous]
        [System.Web.Http.HttpPost]
       // // [EnableThrottling(PerHour = 1, PerDay = 3)]
        [System.Web.Http.Route("api/Accounts/Register")]
        public async Task<ServiceResult> RegisterAsync(UserRegister ur)
        {
            NetworkHelper network = new NetworkHelper();
            string ip = network.GetClientIpAddress(this.Request);

            if (!ModelState.IsValid)
            {
                SystemLogger logger = new SystemLogger(Globals.DBConnectionKey);
                logger.InsertInfo(" Invalid form data.", "AccountsController", "RegisterAsync:" + ip);

                return ServiceResponse.Error("Invalid form data.");
            }

            if (IsSpamPost(ur.SubmitValue, ur.DateCreated, ur.SubmitDate, ip))
            {
                SystemLogger logger = new SystemLogger(Globals.DBConnectionKey);
                logger.InsertSecurity(JsonConvert.SerializeObject(ur), ip + "|SPAM", "RegisterAsync");
                return ServiceResponse.OK("HP");
            }

            if (!string.IsNullOrWhiteSpace(ur.SubmitValue))
            {
                SystemLogger logger = new SystemLogger(Globals.DBConnectionKey);
                logger.InsertSecurity(JsonConvert.SerializeObject(ur), ip + "|SPAM", "RegisterAsync");
                return ServiceResponse.OK("HP");
            }

            var sendValidationEmail = true; //if mobile app don't send the validation email.
            var approved = false;
            // false;TODO make this false when the email url is fixed
            //if mobile the email validation isn't going to be sent for them to validate=> approve. So auto approve.
            if (ur.ClientType == "mobile.app")
            {
                approved = true;
                sendValidationEmail = false;
            }
            UserManager userManager = new UserManager(Globals.DBConnectionKey, Request?.Headers?.Authorization?.Parameter);

            ////if (ur.ClientType != "mobile.app")
            ////{ //if not the mobile app then validate the captcha
            ////    UserSession us = SessionManager.GetSessionByUser(ur.Captcha?.ToUpper());
            ////    if (us == null)
            ////    {
            ////        us = SessionManager.GetSessionByUser(ip);//in the sitecontroller the captcha doesn't know whoe the user is when registering, so we used the ip addres as the name
            ////        if (us == null)
            ////            return ServiceResponse.Error("Invalid session.");
            ////    }
            ////    if (ur.Captcha?.ToUpper() != us.Captcha?.ToUpper())
            ////        return ServiceResponse.Error("Code doesn't match.");
            ////}

            ServiceResult res = await userManager.RegisterUserAsync(ur, approved, ip);
            if (res.Code != 200 || sendValidationEmail == false)
                return res;

            User newUser = (User)res.Result;

            #region add to affiliate db 
            //if (newUser.IsAffiliate == true)
            //{
                AffiliateManager affliateManager = new AffiliateManager(Globals.DBConnectionKey, this.GetAuthToken(this.Request));
            //TODO MAKE SURE PASSWORD IS NOT HASHED .
            //todo we'll need to add a process to check the wordpress tables for people that sign up there first.
            //                var affRes =
            var tmpEmail = newUser.Email;
            newUser.Email = ur.Email; 
            newUser.Password = ur.Password;//becuase the get function wipes sensitive data.
            affliateManager.RegisterAffiliate_WordPress(newUser);
            newUser.Password = string.Empty;
            newUser.Email = tmpEmail;
               
            //}
            #endregion

            EmailSettings settings = new EmailSettings();

            settings.HostPassword = Globals.Application.AppSetting("EmailHostPassword");
            settings.EncryptionKey = Globals.Application.AppSetting("AppKey");
            settings.HostUser = Globals.Application.AppSetting("EmailHostUser");
            settings.MailHost = Globals.Application.AppSetting("MailHost");
            settings.MailPort = StringEx.ConvertTo<int>(Globals.Application.AppSetting("MailPort"));
            settings.SiteDomain = Globals.Application.AppSetting("SiteDomain");
            settings.EmailDomain = Globals.Application.AppSetting("EmailDomain");
            settings.SiteEmail = Globals.Application.AppSetting("SiteEmail");
            settings.UseSSL = StringEx.ConvertTo<bool>(Globals.Application.AppSetting("UseSSL"));

            if (string.IsNullOrWhiteSpace(newUser.ProviderUserKey))
                newUser.ProviderUserKey = Cipher.RandomString(12);

            newUser.Email = ur.Email;//because it gets encrypted when saving
            _logger.InsertInfo("604", "accountcontroller.cs.cs", "registerasync");

            ServiceResult emailRes = await userManager.SendUserEmailValidationAsync(newUser, newUser.ProviderUserKey, ip, settings);


            if (emailRes.Code != 200)
            {
                return ServiceResponse.OK("Registration email failed to send. Check later for email confirmation.");
            }



            return emailRes;
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Accounts/{accountUUID}/Users/{userUUID}/Remove")]
        public ServiceResult RemoveUserFromAccount(string accountUUID, string userUUID)
        {
            AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            return accountManager.RemoveUserFromAccount(accountUUID, userUUID);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Accounts/{accountUUID}/Users/Remove")]
        public ServiceResult RemoveUsersFromAccount(string accountUUID)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            ServiceResult res = new ServiceResult();
            try
            {
                Task<string> content = Request.Content.ReadAsStringAsync();
                if (content == null)
                    return ServiceResponse.Error("No users were sent.");

                string body = content.Result;

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("No users were sent.");

                List<User> users = JsonConvert.DeserializeObject<List<User>>(body);
                List<AccountMember> members = new List<AccountMember>();

                foreach (var user in users)
                {
                    members.Add(new AccountMember { AccountUUID = accountUUID, MemberType = "User", MemberUUID = user.UUID, RoleWeight = 10 });
                }

                AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                res = accountManager.RemoveUsersFromAccount(accountUUID, members, CurrentUser);
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }

            return res;
        }

        [System.Web.Http.HttpGet]
       // // [EnableThrottling(PerHour = 1, PerDay = 3)]
        [System.Web.Http.Route("api/Accounts/ReSendValidationEmail/{userUUID}")]
        public async Task<ServiceResult> ReSendValidationEmail(string userUUID)
        {
            if (string.IsNullOrWhiteSpace(userUUID))
                return ServiceResponse.Error("Invalid user id.");

            UserManager userManager = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = userManager.Get(userUUID);
            if (res.Code != 200)
                return res;
            User user = (User)res.Result;

            NetworkHelper network = new NetworkHelper();
            string ip = network.GetClientIpAddress(this.Request);
            EmailSettings settings = new EmailSettings();
            settings.HostPassword = Globals.Application.AppSetting("EmailHostPassword");
            settings.EncryptionKey = Globals.Application.AppSetting("AppKey");
            settings.HostUser = Globals.Application.AppSetting("EmailHostUser");
            settings.MailHost = Globals.Application.AppSetting("MailHost");
            settings.MailPort = StringEx.ConvertTo<int>(Globals.Application.AppSetting("MailPort"));
            settings.SiteDomain = Globals.Application.AppSetting("SiteDomain");
            settings.EmailDomain = Globals.Application.AppSetting("EmailDomain");
            settings.SiteEmail = Globals.Application.AppSetting("SiteEmail");
            settings.UseSSL = StringEx.ConvertTo<bool>(Globals.Application.AppSetting("UseSSL"));

            ServiceResult res2 = await userManager.SendUserEmailValidationAsync(user, user.ProviderUserKey, ip, settings);

            if (res2.Code == 200)
            {
                return ServiceResponse.Error("Verification email will be sent. Please follow the instructions in the email. Check your spam/junk folder for the confirmation email if you have not received it.");
            }
            else
            {
                return ServiceResponse.Error("Failed to resend validation email. Try again later.");
            }
        }

       // // [EnableThrottling(PerHour = 1, PerDay = 3)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Accounts/SendInfo/")]
        public async Task<ServiceResult> SendAccountValidationEmailAsync(SendAccountInfoForm form)
        {
            if (form == null)
                return ServiceResponse.Error("Invalid form sent to server.");

            if (string.IsNullOrWhiteSpace(form.Email))
                return ServiceResponse.Error("You must provide a username or email!");

            NetworkHelper network = new NetworkHelper();
            UserManager userManager = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            string encEmail = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), form.Email.ToLower(), true);
            User u = userManager.GetUserByEmail(encEmail, false);

            if (u == null)
            {
                u = userManager.Search(form.Email, false)?.FirstOrDefault();
                if (u == null)
                    return ServiceResponse.OK("Invalid username/email.");//return ok so the client can't fish for members.
            }

            if (string.IsNullOrEmpty(u.ProviderUserKey))
                u.ProviderUserKey = Cipher.RandomString(12);

            if (userManager.SetUserFlag(u.UUID, "PROVIDERUSERKEY", u.ProviderUserKey, false).Code != 200)
                return ServiceResponse.Error("Unable to send email.");

            if (form.ForgotPassword)
            {
                u.ProviderName = UserFlags.ProviderName.ForgotPassword;
            }
            else
            {
                u.ProviderName = UserFlags.ProviderName.SendAccountInfo;
            }

            ServiceResult updateResult = userManager.SetUserFlag(u.UUID, "PROVIDERNAME", u.ProviderName, false);
            if (updateResult.Code != 200)
                return updateResult;

            EmailSettings settings = new EmailSettings();
            settings.EncryptionKey = Globals.Application.AppSetting("AppKey");
            settings.HostPassword = Globals.Application.AppSetting("EmailHostPassword");
            settings.HostUser = Globals.Application.AppSetting("EmailHostUser");
            settings.MailHost = Globals.Application.AppSetting("MailHost");
            settings.MailPort = StringEx.ConvertTo<int>(Globals.Application.AppSetting("MailPort"));
            settings.SiteDomain = Globals.Application.AppSetting("SiteDomain");
            settings.EmailDomain = Globals.Application.AppSetting("EmailDomain");
            settings.SiteEmail = Globals.Application.AppSetting("SiteEmail");
            settings.UseSSL = StringEx.ConvertTo<bool>(Globals.Application.AppSetting("UseSSL"));
            string ipAddress = network.GetClientIpAddress(this.Request);

            if (form.ForgotPassword)
                return await userManager.SendPasswordResetEmailAsync(u, ipAddress, settings);

            return await userManager.SendUserInfoAsync(u, network.GetClientIpAddress(this.Request), settings);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Accounts/SetActive/{accountUUID}")]
        public ServiceResult SetActive(string accountUUID)
        {
            if (string.IsNullOrWhiteSpace(accountUUID))
                return ServiceResponse.Error("You must pass a valid account UUID to mark as default.");

            UserSession us = SessionManager.GetSession(this.GetAuthToken(Request));
            if (us == null)
                return ServiceResponse.Error("You must log in to access this functionality.");

            AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            //this will set the accountUUID field in the users table
            ServiceResult res = accountManager.SetActiveAccount(accountUUID, CurrentUser.UUID, CurrentUser);

            if (res.Code != 200)
                return res;

            // SessionManager.DeleteByUserId(CurrentUser.UUID, CurrentUser.AccountUUID, false);

            UserSession setSession = SessionManager.GetSessionByUser(CurrentUser.UUID, accountUUID);

            // if (setSession == null || string.IsNullOrWhiteSpace(setSession.UserData))
            //  return  ServiceResponse.Unauthorized("Session not found, login to establish a session.");

            //  CurrentUser  =  JsonConvert.DeserializeObject<User>(setSession.UserData);

            return res;
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpPatch]
        [System.Web.Http.Route("api/Accounts/Update")]
        public ServiceResult Update(Account account)
        {
            if (account == null)
                return ServiceResponse.Error("No record sent.");

            AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = accountManager.Get(account.UUID);
            if (res.Code != 200)
                return res;

            var dbAcct = (Account)res.Result;

            if (dbAcct.Email == account.Email)
                dbAcct.Email = account.Email;
            else
                dbAcct.Email = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), account.Email.ToLower(), true);

            dbAcct.Name = account.Name;
            dbAcct.Active = account.Active;
            dbAcct.Status = account.Status;
            dbAcct.Private = account.Private;
            dbAcct.SortOrder = account.SortOrder;
            dbAcct.BillingPostalCode = account.BillingPostalCode;
            dbAcct.BillingAddress = account.BillingAddress;
            dbAcct.BillingCity = account.BillingCity;
            dbAcct.BillingCountry = account.BillingCountry;
            dbAcct.BillingState = account.BillingState;
            dbAcct.Description = account.Description;
            dbAcct.LocationType = account.LocationType;
            dbAcct.LocationUUID = account.LocationUUID;
            dbAcct.Category = account.Category;
            dbAcct.WebSite = account.WebSite;
            dbAcct.Image = account.Image;
            dbAcct.IsAffiliate = account.IsAffiliate;

            return accountManager.Update(dbAcct);
        }

        [System.Web.Http.AllowAnonymous]
        [System.Web.Http.HttpPost]
       // // [EnableThrottling(PerHour = 3, PerDay = 3)]
        [System.Web.Http.Route("api/Accounts/WpLogin")]
        public async Task<string> WPLogin(LoginModel credentials)
        {
            NetworkHelper network = new NetworkHelper();
            SystemLogger logger = new SystemLogger(Globals.DBConnectionKey);

            //this is just a honeypot to capture script kiddies.
            string ipAddress = network.GetClientIpAddress(this.Request);
            await logger.InsertSecurityAsync(JsonConvert.SerializeObject(credentials), ipAddress, "api/Accounts/WpLogin");
            return "Error invalid name or password";
        }
    }
}