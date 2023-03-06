// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Newtonsoft.Json;
using Omni.Managers.Services;
using System;
using System.Net.Mail;
using System.Threading.Tasks;
using GreenWerx.Data.Logging;
using GreenWerx.Managers;
using GreenWerx.Managers.Membership;
using GreenWerx.Models.App;
using GreenWerx.Models.Logging;
using GreenWerx.Models.Services;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Utilites.Security;
using GreenWerx.Web.api.Helpers;
using WebApiThrottle;

namespace GreenWerx.Web.api.v1
{
    public class SiteController : ApiBaseController
    {
        private readonly SystemLogger _fileLogger = new SystemLogger(null, true);

        public SiteController()
        {
        }

        [System.Web.Http.HttpPost]
        [System.Web.Http.AllowAnonymous]
        // [EnableThrottling(PerHour = 1, PerDay = 3)]
        [System.Web.Http.Route("api/Site/SendMessage")]
        public async Task<ServiceResult> SendMessage(EmailMessage form)//        public  ServiceResult SendMessage(Message form)
        {
            if (form == null)
                return ServiceResponse.Error("No form was posted to the server.");

            try
            {
                form.DateSent = DateTime.UtcNow;

                bool isValidFormat = Validator.IsValidEmailFormat(form.EmailFrom);
                if (string.IsNullOrWhiteSpace(form.EmailFrom) || isValidFormat == false)
                {
                    return ServiceResponse.Error("You must provide a valid email address.");
                }

                if (string.IsNullOrWhiteSpace(form.Body))
                {
                    return ServiceResponse.Error("You must provide a message.");
                }
                EmailMessageManager EmailMessageManager = new EmailMessageManager(Globals.DBConnectionKey, Request?.Headers?.Authorization?.Parameter);
                NetworkHelper network = new NetworkHelper();
                string ipAddress = network.GetClientIpAddress(this.Request);

                EmailMessage EmailMessage = new EmailMessage();
                EmailMessage.Body = form.Body + "<br/><br/><br/>Message Key:" + EmailMessage.UUID;
                EmailMessage.Subject = form.Subject;
                EmailMessage.EmailFrom = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), form.EmailFrom.ToLower(), true);
                EmailMessage.UUIDType += "." + form.Type;

                if (form.Type?.ToLower() != "contactus")
                    EmailMessage.EmailTo = Globals.Application.AppSetting("SiteEmail");

                EmailMessage.DateCreated = DateTime.UtcNow;
                EmailMessage.IpAddress = ipAddress;
                EmailMessage.Status = "not_sent";

                if (CurrentUser != null)
                {
                    EmailMessage.CreatedBy = CurrentUser.UUID;
                    EmailMessage.AccountUUID = CurrentUser.AccountUUID;
                }
                else
                {
                    EmailMessage.CreatedBy = "ContactUsForm";
                    EmailMessage.AccountUUID = "ContactUsForm";
                }

                UserManager um = new UserManager(Globals.DBConnectionKey, Request?.Headers?.Authorization?.Parameter);
                string toName = um.GetUserByEmail(EmailMessage.EmailTo)?.Name;
                string fromName = um.GetUserByEmail(form.EmailFrom)?.Name;
                EmailMessage.NameFrom = fromName;
                EmailMessage.NameTo = toName;

                if (EmailMessageManager.Insert(EmailMessage).Code == 500)
                {
                    return ServiceResponse.Error("Failed to save the email. Try again later.");
                }

                EmailSettings settings = new EmailSettings();
                settings.EncryptionKey = Globals.Application.AppSetting("AppKey");
                settings.HostPassword = Globals.Application.AppSetting("EmailHostPassword");
                settings.HostUser = Globals.Application.AppSetting("EmailHostUser");
                settings.MailHost = Globals.Application.AppSetting("MailHost");
                settings.MailPort = StringEx.ConvertTo<int>(Globals.Application.AppSetting("MailPort"));
                settings.SiteDomain = Globals.Application.AppSetting("SiteDomain");
                settings.ApiUrl = Globals.Application.AppSetting("ApiUrl");
                settings.EmailDomain = Globals.Application.AppSetting("EmailDomain");
                settings.SiteEmail = Globals.Application.AppSetting("SiteEmail");
                settings.UseSSL = StringEx.ConvertTo<bool>(Globals.Application.AppSetting("UseSSL"));

                MailAddress ma = new MailAddress(settings.SiteEmail, settings.SiteEmail);
                MailMessage mail = new MailMessage();
                mail.From = ma;
                // mail.ReplyToList.Add( ma );
                mail.ReplyToList.Add(form.EmailFrom);
                mail.To.Add(EmailMessage.EmailTo);
                mail.Subject = EmailMessage.Subject;
                mail.Body = EmailMessage.Body + "<br/><br/><br/>IP:" + ipAddress;
                mail.IsBodyHtml = true;
                SMTP svc = new SMTP(Globals.DBConnectionKey, settings);
                return svc.SendMail(mail);
            }
            catch (Exception ex)
            {
                _fileLogger.InsertError(ex.DeserializeException(true), "SiteController", "SendMessage:" + JsonConvert.SerializeObject(form));
            }
            return ServiceResponse.Error("Failed to send message.");
        }
    }
}