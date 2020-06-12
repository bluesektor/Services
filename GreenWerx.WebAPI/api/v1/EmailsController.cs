using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GreenWerx.Managers;
using GreenWerx.Managers.Membership;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Logging;
using GreenWerx.Models.Membership;
using GreenWerx.Models.Services;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Utilites.Security;
using GreenWerx.Web;
using GreenWerx.Web.api;
using GreenWerx.Web.api.Helpers;
using GreenWerx.Web.Filters;
using WebApiThrottle;

namespace GreenWerx.WebAPI.api.v1
{
    public class EmailsController : ApiBaseController
    {
        private readonly NetworkHelper network = null;

        public EmailsController()
        {
            network = new NetworkHelper();
        }

        #region sse server sent events

        // todo would we create a list/dictionary of each client that has it's own streamMessage that way we can send to a specific
        //client based on the list userUUID and steam
        private static readonly ConcurrentQueue<StreamWriter> _streammessage = new ConcurrentQueue<StreamWriter>();

        //todo change this to a global? event that other controllers can call to publish their event to client
        //todo remove the null and uncomment the initialization to restart the events. I commented it out
        // so it won't keep running..
        private static readonly Lazy<Timer> _timer = null; //new Lazy<Timer>(() => new Timer(TimerCallback, null, 0, 1000));

        // [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.AllowAnonymous]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/SSE")]
        public HttpResponseMessage SSE_Test()
        {
            this.Request.Headers.Add("Access-Control-ALlow-Origin", "*");
            Timer t = _timer.Value; //todo is this needed?
            HttpResponseMessage response = this.Request.CreateResponse();
            response.Content = new PushStreamContent((Action<Stream, HttpContent, TransportContext>)streamAvailableHandler, "text/event-stream");
            return response;
        }

        private static void TimerCallback(object state)
        {
            Random randNum = new Random();
            foreach (var data in _streammessage)
            {
                try
                {
                    data.WriteLine("data:" + randNum.Next(30, 100) + "\n");
                    data.Flush();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            //To set timer with random interval
            _timer.Value.Change(TimeSpan.FromMilliseconds(randNum.Next(1, 3) * 500), TimeSpan.FromMilliseconds(-1));
        }

        private void streamAvailableHandler(Stream stream, HttpContent content, TransportContext context)
        {
            StreamWriter streamwriter = new StreamWriter(stream);
            _streammessage.Enqueue(streamwriter);
        }

        #endregion sse server sent events

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Emails/Delete/{UUID}")]
        public ServiceResult Delete(string UUID)
        {
            if (string.IsNullOrWhiteSpace(UUID))
                return ServiceResponse.Error("No UUID sent.");

            EmailMessageManager emailManager = new EmailMessageManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);

            var res = emailManager.Get(UUID);
            if (res.Code != 200)
                return ServiceResponse.Error("Email not found.");

            var email = (EmailMessage)res.Result;
            email.Deleted = true;
            return emailManager.Update(email);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/EmailsBy/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            throw new NotImplementedException();
            //try
            //{
            //    EmailMessageManager emailManager = new EmailMessageManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);
            //    return emailManager.GetEmail(uuid, true);

            //}
            //catch (Exception ex)
            //{
            //    SystemLogger logger = new SystemLogger(Globals.DBConnectionKey);
            //    logger.InsertError(ex.Message, "MessagesController", "GetBy");
            //}
            //    return ServiceResponse.Error();
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
        [System.Web.Http.Route("api/Emails")]
        public ServiceResult GetMessages()
        {
            EmailMessageManager um = new EmailMessageManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);

            int count;

            DataFilter tmpFilter = this.GetFilter(Request);
            List<dynamic> emails = um.GetEmailMessages(CurrentUser.UUID, tmpFilter, out count);

            return ServiceResponse.OK("", emails, count);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 3)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Emails/Add")]
        [System.Web.Http.Route("api/Emails/Insert")]
        public ServiceResult Insert(EmailMessage n)
        {
            throw new NotImplementedException();
            //if (n == null)
            //    return ServiceResponse.Error("No user sent.");

            //AccountManager ac = new AccountManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);
            //EmailMessageManager emailManager = new EmailMessageManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);

            //ServiceResult res = emailManager.Insert(n, GetClientIpAddress(Request));

            //if (res.Code != 200)
            //    return res;

            ////add user to account members (user is now a member of the account from which it was created).
            ////
            //if (string.IsNullOrWhiteSpace(n.AccountUUID) == false)
            //{
            //    res = ac.AddEmailToAccount(n.AccountUUID, n.UUID, CurrentEmail);
            //}
            //if (res.Code != 200)
            //    return res;
            //res.Result = n.UUID;
            //return res;
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPatch]
        [System.Web.Http.Route("api/Emails/Opened/{UUID}")]
        public ServiceResult OpenedEmail(string UUID)
        {
            if (string.IsNullOrWhiteSpace(UUID))
                return ServiceResponse.Error("No UUID sent.");

            EmailMessageManager emailManager = new EmailMessageManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);

            var res = emailManager.Get(UUID);
            if (res.Code != 200)
                return ServiceResponse.Error("Email not found.");

            var email = (EmailMessage)res.Result;
            email.DateOpened = DateTime.UtcNow;
            return emailManager.Update(email);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Emails/{name}")]
        public ServiceResult Search(string name)
        {
            throw new NotImplementedException();
            //EmailMessageManager emailManager = new EmailMessageManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);

            //List<Email> u = emailManager.Search(name, false);

            //if (u == null)
            //    return ServiceResponse.Error(" EmailMessage eot found.");

            //u = EmailMessageManager.ClearSensitiveData(u);

            //return ServiceResponse.OK("", u);
        }

        //[ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        //[System.Web.Http.HttpPost]
        //[System.Web.Http.HttpDelete]
        //[System.Web.Http.Route("api/Emails/Delete")]
        //public ServiceResult Delete( EmailMessage e)
        //{
        //    throw new NotImplementedException();
        //    //if (n == null || string.IsNullOrWhiteSpace(n.UUID))
        //    //    return ServiceResponse.Error("Invalid account was sent.");

        //    //EmailMessageManager emailManager = new EmailMessageManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);

        //    //ServiceResult delResult = emailManager.Delete(n.UUID);

        //    //if (delResult.Code != 200)
        //    //    return delResult;

        //    //AccountManager ac = new AccountManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);
        //    //return ac.RemoveEmailFromAllAccounts(n.UUID);
        //}
        // [EnableThrottling(PerHour = 5, PerDay = 20)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Emails/Send")]
        public async Task<ServiceResult> SendEmailAsync()
        {
            var app = new AppManager(Globals.DBConnectionKey, "web", "");
            string secret = app.GetSetting("AppKey")?.Value;
            var gsecred = Globals.Application.AppSetting("AppKey");
            if (gsecred != secret)
                throw new NotImplementedException();

            var encemail = Cipher.Crypt(secret, "GreenWerx+ant@gmail.com".ToLower(), true);
            encemail = Cipher.Crypt(secret, "GreenWerx+blockedSM@gmail.com".ToLower(), true);
            encemail = Cipher.Crypt(secret, "GreenWerx+sand@gmail.com".ToLower(), true);
            encemail = Cipher.Crypt(secret, "GreenWerx+GatosLocos@gmail.com".ToLower(), true);

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            EmailSettings settings = new EmailSettings();
            settings.FromUserUUID = CurrentUser.UUID;

            try
            {
                string content = await Request.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(content))
                    return ServiceResponse.Error("You must send valid email info.");

                var message = JsonConvert.DeserializeObject<EmailMessage>(content);

                if (string.IsNullOrWhiteSpace(message.SendTo))
                    return ServiceResponse.Error("You must send a user id for the message.");

                if (string.IsNullOrWhiteSpace(message.Body))
                    return ServiceResponse.Error("You must send comment in the message.");

                UserManager userManager = new UserManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);

                switch (message.Type?.ToUpper())
                {
                    case "ACCOUNT":
                        var am = new AccountManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);
                        var res = am.Get(message.SendTo);
                        if (res.Code != 200)
                            return res;
                        // Account account = (Account)res.Result;

                        break;

                    case "SUPPORT":
                        //todo call api/Site/SendMessage

                        break;

                    case "USER":

                        var resUserTO = userManager.Get(message.SendTo);
                        if (resUserTO == null || resUserTO.Code != 200)
                            return ServiceResponse.Error("User not found for user uuid.");

                        var userTO = (User)resUserTO.Result; // THIS SHOULD BE sand, EMAILLOG.userUUID should be this.
                        if (message.SendTo != userTO.UUID)
                            return ServiceResponse.Error("User id doesn't match the addressed user id.");

                        //if (message.SendFrom != CurrentUser.UUID)
                        //    return ServiceResponse.Error("Current user doesn't match logged in user."); //may just set the from user = currentuser
                        break;

                    case "PROFILE":
                        ProfileManager profileManager = new ProfileManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);
                        var dbProfile = profileManager.Get(message.SendTo);
                        if (dbProfile.Code != 200)
                            return ServiceResponse.Error("Profile not found.");

                        var node = userManager.Get(((Profile)dbProfile.Result).UserUUID);
                        if (node.Code != 200)
                            return node;

                        var user = (User)node.Result;
                        settings.ToUserUUID = user.UUID;

                        if (string.IsNullOrWhiteSpace(message.Subject))
                            message.Subject = "You have received a message from " + CurrentUser.Name;
                        break;

                    default:

                        var res2 = userManager.GetUser(message.SendTo, false);
                        if (res2.Code != 200)
                            return res2;

                        var toUser = (User)res2.Result;

                        if (toUser == null)
                            return ServiceResponse.Error("User not found.");

                        settings.ToUserUUID = toUser.UUID;
                        break;
                }

                string hostPassword = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), Globals.Application.AppSetting("EmailHostPassword"), false);

                settings.EncryptionKey = Globals.Application.AppSetting("AppKey");
                settings.HostPassword = hostPassword;// Globals.Application.AppSetting("EmailHostPassword");
                settings.HostUser = Globals.Application.AppSetting("EmailHostUser");
                settings.MailHost = Globals.Application.AppSetting("MailHost");
                settings.MailPort = StringEx.ConvertTo<int>(Globals.Application.AppSetting("MailPort"));
                settings.SiteDomain = Globals.Application.AppSetting("SiteDomain");
                settings.EmailDomain = Globals.Application.AppSetting("EmailDomain");
                settings.SiteEmail = Globals.Application.AppSetting("SiteEmail");
                settings.UseSSL = StringEx.ConvertTo<bool>(Globals.Application.AppSetting("UseSSL"));

                message.IpAddress = network.GetClientIpAddress(this.Request);
                return await userManager.SendEmailAsync(message, settings);
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error("Failed to send message.", ex.DeserializeException());
            }
        }

        //[ApiAuthorizationRequired(Operator = ">=", RoleWeight = 3)]
        //[System.Web.Http.HttpPatch]
        //[System.Web.Http.Route("api/Messages/{userUUID}/Flag/{userFlag}/Value/{flagValue}")]
        //public ServiceResult SetEmailFlag(string userUUID, string userFlag, string flagValue)
        //{
        //    if (CurrentEmail.Banned || CurrentEmail.LockedOut)
        //        return ServiceResponse.Error("You're account is suspended, you do not have authorization.");

        //    if (string.IsNullOrWhiteSpace(userUUID))
        //        return ServiceResponse.Error("No user id sent.");

        //    if (string.IsNullOrWhiteSpace(userFlag))
        //        return ServiceResponse.Error("No flag sent.");

        //    if (string.IsNullOrWhiteSpace(flagValue))
        //        return ServiceResponse.Error("No value sent.");

        //    var user = this.GetEmail(Request.Headers?.Authorization?.Parameter);
        //    RoleManager rm = new RoleManager(Globals.DBConnectionKey, user);
        //    //var role = rm.GetRole("admin", user.AccountUUID);
        //    //if (role == null && (!rm.IsSiteAdmin(user.Name) || !rm.IsInRole(user.UUID, user.AccountUUID, role.UUID, false)))
        //    //    return ServiceResponse.Error("You are not authorized this action.");

        //    var userRoles = rm.GetRolesForEmail(user.UUID, user.AccountUUID).OrderByDescending(o => o.RoleWeight);

        //    if (userRoles == null || userRoles.Any() == false)
        //        return ServiceResponse.Error("You are not assigned a role allowing you to flag items.");

        //    bool isAuthorized = false;
        //    foreach (var userRole in userRoles)
        //    {
        //        if (userRole.RoleWeight > 90)
        //        {
        //            isAuthorized = true;
        //            break;
        //        }
        //    }

        //    if (!isAuthorized)
        //        return ServiceResponse.Error("You are not assigned a role allowing you to flag items as safe.");

        //    EmailMessageManager um = new EmailMessageManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);

        //    var tmp = um.Get(userUUID);

        //    if (tmp.Code != 200)
        //        return tmp;

        //    var dbEmailMessage= (Email)tmp.Result;

        //    switch (userFlag.ToUpper())
        //    {
        //        case "BAN":
        //            if (userUUID == user.UUID)
        //                return ServiceResponse.Error("You can't ban/unban yourself.");

        //            dbEmail.Banned = flagValue.ConvertTo<bool>();
        //            break;
        //        case "LOCKEDOUT":
        //            if (userUUID == user.UUID)
        //                return ServiceResponse.Error("You can't unlock/lock yourself.");
        //            dbEmail.LockedOut = flagValue.ConvertTo<bool>();
        //            if (dbEmail.LockedOut)
        //                dbEmail.LastLockoutDate = DateTime.UtcNow;
        //            break;
        //    }

        //    return um.Update(dbEmail);
        //}

        ///// <summary>
        ///// Updated fields
        ///// Name
        ///// Private
        ///// SortOrder
        ///// Active
        ///// LicenseNumber
        ///// Anonymous
        ///// Approved
        ///// Banned
        ///// Deleted
        ///// LockedOut
        ///// Email
        ///// </summary>
        ///// <param name="u"></param>
        ///// <returns></returns>
        //[ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        //[System.Web.Http.HttpPost]
        //[System.Web.Http.HttpPatch]
        //[System.Web.Http.Route("api/Messages/Update")]
        //public ServiceResult Update(EmailForm u)
        //{
        //    if (u == null)
        //        return ServiceResponse.Error("Invalid user form sent.");

        //    EmailSession session = SessionManager.GetSession(Request.Headers?.Authorization?.Parameter);
        //    if (session == null)
        //        return ServiceResponse.Error("EmailMessagesession has timed out. You must login to complete this action.");

        //    EmailMessageManager emailManager = new EmailMessageManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);

        //    var res = emailManager.GetEmail(u.UUID, false);
        //    if (res.Code != 200)
        //        return res;
        //    EmailMessagedbAcct = (Email)res.Result;

        //    if (dbAcct == null)
        //        return ServiceResponse.Error("EmailMessagewas not found.");

        //    dbAcct.Name = u.Name;
        //    dbAcct.AccountUUID = u.AccountUUID;
        //    dbAcct.Private = u.Private;
        //    dbAcct.SortOrder = u.SortOrder;
        //    dbAcct.Active = u.Active;
        //    dbAcct.LicenseNumber = u.LicenseNumber;
        //    dbAcct.Anonymous = u.Anonymous;
        //    dbAcct.Approved = u.Approved;
        //    dbAcct.Banned = u.Banned;
        //    dbAcct.Deleted = u.Deleted;
        //    dbAcct.LockedOut = u.LockedOut;
        //    dbAcct.Email = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), u.Email.ToLower(), true);
        //    dbAcct.PasswordQuestion = u.PasswordQuestion;
        //    dbAcct.PasswordAnswer = u.PasswordAnswer;
        //    //ParentId
        //    //UUIDType
        //    //UUParentID
        //    //UUParentID
        //    //Status

        //    ServiceResult updateResult = emailManager.Update(dbAcct);
        //    if (updateResult.Code == 200 && CurrentEmail.UUID == dbAcct.UUID)
        //    {
        //        //update session
        //        session.EmailData = JsonConvert.SerializeObject(EmailMessageManager.ClearSensitiveData(dbAcct));
        //        SessionManager.Update(session);
        //        return ServiceResponse.OK();
        //    }
        //    return updateResult;
        //}

        ///// <summary>
        ///// This is from the app. sends just a basic message body.
        ///// subject etc. are generated.
        ///// </summary>
        ///// <returns></returns>
        //// [EnableThrottling(PerHour = 5, PerDay = 20)]
        //[System.Web.Http.HttpPost]
        //[System.Web.Http.Route("api/Messages/Send/Message/{itemUUID}/{itemType}")]
        //public async Task<ServiceResult> SendMessageAsync(string itemUUID, string itemType)
        //{
        //    if (string.IsNullOrWhiteSpace(itemUUID))
        //        return ServiceResponse.Error("You must send an item id for the message.");

        //    if (string.IsNullOrWhiteSpace(itemType))
        //        return ServiceResponse.Error("You must send an item type for the message.");

        //    if (!itemType.EqualsIgnoreCase("item"))
        //        return ServiceResponse.Error("You must send a supported item type.");

        //    if (CurrentEmailMessage== null)
        //        return ServiceResponse.Error("You must be logged in to access this function.");

        //    try
        //    {
        //        string message = await Request.Content.ReadAsStringAsync();

        //        if (string.IsNullOrEmpty(message))
        //            return ServiceResponse.Error("You must send valid email info.");

        //        InventoryManager inventoryManager = new InventoryManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);

        //        var res = inventoryManager.Get(itemUUID);
        //        if (res.Code != 200)
        //            return res;
        //        var item = res.Result as InventoryItem;

        //        EmailMessageManager emailManager = new EmailMessageManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);

        //        var res2 = emailManager.GetEmail(item.CreatedBy, false);
        //        if (res2.Code != 200)
        //            return res2;
        //        var toEmailMessage= res2.Result as Email;

        //        //decrypt to send
        //        string emailTo = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), toEmail.Email, false);

        //        EmailSettings settings = new EmailSettings();
        //        settings.EncryptionKey = Globals.Application.AppSetting("AppKey");
        //        settings.HostPassword = Globals.Application.AppSetting("EmailHostPassword");
        //        settings.HostEmailMessage= Globals.Application.AppSetting("EmailHostEmail");
        //        settings.MailHost = Globals.Application.AppSetting("MailHost");
        //        settings.MailPort = StringEx.ConvertTo<int>(Globals.Application.AppSetting("MailPort"));
        //        settings.SiteDomain = Globals.Application.AppSetting("SiteDomain");
        //        settings.EmailDomain = Globals.Application.AppSetting("EmailDomain");
        //        settings.SiteEmail = Globals.Application.AppSetting("SiteEmail");
        //        settings.UseSSL = StringEx.ConvertTo<bool>(Globals.Application.AppSetting("UseSSL"));

        //        string ip = network.GetClientIpAddress(this.Request);
        //        string Subject = CurrentEmail.Name + " sent a message about " + item.Name;
        //        string msg = message;
        //        string emailFrom = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), CurrentEmail.Email, false);
        //        return await emailManager.SendEmailAsync(ip, emailTo, emailFrom, Subject, msg, settings);
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
        //[System.Web.Http.Route("api/Messages/SendMessage")]
        //public async Task<ServiceResult> SendMessageAsync()
        //{
        //    if (CurrentEmailMessage== null)
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

        //        if (formData.MessageType == "ContactAdmin")
        //        {
        //            if (string.IsNullOrWhiteSpace(Globals.Application.AppSetting("SiteEmail", "")))
        //            {
        //                return ServiceResponse.Error("Site email is not set.");
        //            }
        //            emailTo = Globals.Application.AppSetting("SiteEmail", "");
        //        }

        //        EmailSettings settings = new EmailSettings();
        //        settings.EncryptionKey = Globals.Application.AppSetting("AppKey");
        //        settings.HostPassword = Globals.Application.AppSetting("EmailHostPassword");
        //        settings.HostEmailMessage= Globals.Application.AppSetting("EmailHostEmail");
        //        settings.MailHost = Globals.Application.AppSetting("MailHost");
        //        settings.MailPort = StringEx.ConvertTo<int>(Globals.Application.AppSetting("MailPort"));
        //        settings.SiteDomain = Globals.Application.AppSetting("SiteDomain");
        //        settings.EmailDomain = Globals.Application.AppSetting("EmailDomain");
        //        settings.SiteEmail = Globals.Application.AppSetting("SiteEmail");
        //        settings.UseSSL = StringEx.ConvertTo<bool>(Globals.Application.AppSetting("UseSSL"));

        //        string ip = network.GetClientIpAddress(this.Request);
        //        string Subject = formData.Subject.ToString();
        //        string msg = formData.Message.ToString();
        //        EmailMessageManager emailManager = new EmailMessageManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);
        //        string emailFrom = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), CurrentEmail.Email, false);
        //        return await emailManager.SendEmailAsync(ip, emailTo, emailFrom, Subject, msg, settings);
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.Assert(false, ex.Message);
        //        return ServiceResponse.Error(ex.Message);
        //    }
        //}

        //// [EnableThrottling(PerHour = 5, PerDay = 20)]
        //[System.Web.Http.HttpPost]
        //[System.Web.Http.Route("api/Messages/Message")]
        //public async Task<ServiceResult> SendMessageToEmailAsync()
        //{
        //    if (CurrentEmailMessage== null)
        //        return ServiceResponse.Error("You must be logged in to access this function.");

        //    EmailSettings settings = new EmailSettings();
        //    settings.FromEmailUUID = CurrentEmail.UUID;

        //    try
        //    {
        //        string content = await Request.Content.ReadAsStringAsync();

        //        if (string.IsNullOrEmpty(content))
        //            return ServiceResponse.Error("You must send valid email info.");

        //        var message = JsonConvert.DeserializeObject<Message>(content);

        //        if (string.IsNullOrWhiteSpace(message.SendTo))
        //            return ServiceResponse.Error("You must send a user id for the message.");

        //        if (string.IsNullOrWhiteSpace(message.Comment))
        //            return ServiceResponse.Error("You must send comment in the message.");

        //        string emailTo = "";

        //        EmailMessageManager emailManager = new EmailMessageManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);

        //        switch (message.Type?.ToUpper())
        //        {
        //            case "ACCOUNT":
        //                var am = new AccountManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);
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
        //                ProfileManager profileManager = new ProfileManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);
        //                //todo call api/Site/SendMessage
        //                var dbProfile = profileManager.Get(message.SendTo);
        //                if (dbProfile.Code != 200)
        //                    return ServiceResponse.Error("Profile not found.");

        //                var node = emailManager.Get(((Profile)dbProfile.Result).EmailUUID);
        //                if (node.Code != 200)
        //                    return node;

        //                var user = (Email)node.Result;
        //                settings.ToEmailUUID = user.UUID;
        //                emailTo = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), user.Email, false);

        //                if (string.IsNullOrWhiteSpace(message.Subject))
        //                    message.Subject = "You have received a message from " + CurrentEmail.Name;
        //                break;
        //            default:

        //                var res2 = emailManager.GetEmail(message.SendTo, false);
        //                if (res2.Code != 200)
        //                    return res2;

        //                var toEmailMessage= (Email)res2.Result;

        //                if (toEmailMessage== null)
        //                    return ServiceResponse.Error(" EmailMessage not found.");

        //                settings.ToEmailUUID = toEmail.UUID;
        //                emailTo = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), toEmail.Email, false);
        //                break;
        //        }

        //        if (string.IsNullOrWhiteSpace(emailTo))
        //            return ServiceResponse.Error("Members email is not set.");

        //        string hostPassword = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), Globals.Application.AppSetting("EmailHostPassword"), false);

        //        settings.EncryptionKey = Globals.Application.AppSetting("AppKey");
        //        settings.HostPassword = hostPassword;// Globals.Application.AppSetting("EmailHostPassword");
        //        settings.HostEmailMessage= Globals.Application.AppSetting("EmailHostEmail");
        //        settings.MailHost = Globals.Application.AppSetting("MailHost");
        //        settings.MailPort = StringEx.ConvertTo<int>(Globals.Application.AppSetting("MailPort"));
        //        settings.SiteDomain = Globals.Application.AppSetting("SiteDomain");
        //        settings.EmailDomain = Globals.Application.AppSetting("EmailDomain");
        //        settings.SiteEmail = Globals.Application.AppSetting("SiteEmail");
        //        settings.UseSSL = StringEx.ConvertTo<bool>(Globals.Application.AppSetting("UseSSL"));

        //        string ip = network.GetClientIpAddress(this.Request);
        //        string Subject = message.Subject;
        //        string msg = message.Comment;
        //        string emailFrom = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), CurrentEmail.Email, false);
        //        return await emailManager.SendEmailAsync(ip, emailTo, emailFrom, Subject, msg, settings);

        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.Assert(false, ex.Message);
        //        return ServiceResponse.Error(ex.Message);
        //    }
        //}
    }
}