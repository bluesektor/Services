// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GreenWerx.Models.App;
using GreenWerx.Web.Filters;
using WebApiThrottle;

namespace GreenWerx.Web.api.v1
{
    public class SessionsController : ApiBaseController
    {
        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Sessions/Delete")]
        public ServiceResult DeleteSesssion()
        {
            try
            {
                Task<string> content = Request.Content.ReadAsStringAsync();

                if (content == null)
                    return ServiceResponse.Error("No session data was sent.");

                string body = content.Result;

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("You must send valid session data.");

                UserSession clientSession = JsonConvert.DeserializeObject<UserSession>(body);

                if (clientSession == null)
                    return ServiceResponse.Error("Invalid session data.");

                UserSession us = SessionManager.GetSession(clientSession.AuthToken);

                if (us == null) //session already cleared.
                    return ServiceResponse.OK();

                if (us.UserUUID != clientSession.UserUUID?.ToString())
                    return ServiceResponse.Error("You are not authorized to end the session.");

                if (!SessionManager.DeleteSession(clientSession.AuthToken))
                    return ServiceResponse.Error("Error occurred deleting session.");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error(ex.Message);
            }
            return ServiceResponse.OK();
        }

        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Sessions")]
        public ServiceResult GetSession() //string sessionUUID)
        {
            string sessionUUID = string.Empty;
            try
            {
                sessionUUID = Request.Content.ReadAsStringAsync().Result;
            }
            catch (Exception ex)
            {
                return ServiceResponse.Error("No session key sent.");
            }
            if (string.IsNullOrWhiteSpace(sessionUUID))
                return ServiceResponse.Error("You must send a valid session id.");

            UserSession us = SessionManager.GetSession(sessionUUID);

            if (us == null)
                return ServiceResponse.Error("Invalid or expired user session. id:" + sessionUUID);

            us.UserData = string.Empty;

            return ServiceResponse.OK("", us);
        }

        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 0)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Sessions/User/{userUUID}/Account/{accountUUID}")]
        public ServiceResult GetSessionByUser(string userUUID, string accountUUID)
        {
            if (string.IsNullOrWhiteSpace(userUUID))
                return ServiceResponse.Error("You must send a valid user id.");

            if (string.IsNullOrWhiteSpace(accountUUID))
                return ServiceResponse.Error("You must send a valid account id.");

            if (CurrentUser == null)
            {
                CurrentUser = this.GetUser(this.GetAuthToken(Request));
                if (CurrentUser == null)
                    return ServiceResponse.Error("You must login to access this function.");
            }

            if ((CurrentUser == null || CurrentUser.AccountUUID != accountUUID && CurrentUser.UUID != userUUID))// || CurrentUser.SiteAdmin == false)
                return ServiceResponse.Unauthorized("You are not authorized access to this session.");

            UserSession us = SessionManager.GetSessionByUser(userUUID, accountUUID);

            if (us == null)
                return ServiceResponse.Error("Session not fourn for user.");

            us.UserData = string.Empty;

            return ServiceResponse.OK("", us);
        }

        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Sessions/Status/{sessionId}")]
        public ServiceResult GetStatus(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return ServiceResponse.Error("You must send a valid session id.");

            UserSession us = SessionManager.GetSession(sessionId);

            if (us == null)
                return ServiceResponse.Error("Sessions was not found for session id.");

            return ServiceResponse.OK();
        }

        // [EnableThrottling(PerSecond = 3, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Sessions/Save")]
        public ServiceResult SaveSesssion()
        {
            try
            {
                Task<string> content = Request.Content.ReadAsStringAsync();

                if (content == null)
                    return ServiceResponse.Error("No session data was sent.");

                string body = content.Result;

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("You must send valid session data.");

                UserSession clientSession = JsonConvert.DeserializeObject<UserSession>(body);

                if (clientSession == null)
                    return ServiceResponse.Error("Invalid session data.");

                UserSession us = SessionManager.GetSession(clientSession.AuthToken);

                if (us == null)
                    return ServiceResponse.Error("Session doesn't exist.");

                if ((this.GetAuthToken(Request) != us.AuthToken ||
                    this.GetAuthToken(Request) != clientSession.AuthToken))
                    return ServiceResponse.Unauthorized("You are not authorized to save the session.");

                if (us.UserUUID != clientSession.UserUUID && CurrentUser.SiteAdmin == false)
                    return ServiceResponse.Error("You are not authorized to save the session.");

                if (SessionManager.SaveSession(clientSession) != null)
                    return ServiceResponse.OK();

                return ServiceResponse.Error("Failed to save session.");
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error(ex.Message);
            }
            return ServiceResponse.OK();
        }
    }
}