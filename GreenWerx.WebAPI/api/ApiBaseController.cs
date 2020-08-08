// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Controllers;
using GreenWerx.Data.Logging;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Managers;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Membership;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Utilites.Helpers;
using GreenWerx.Utilites.Security;
using GreenWerx.Web.api.Helpers;

namespace GreenWerx.Web.api
{
    public class ApiBaseController : ApiController
    {
        private readonly SystemLogger _logger;
        private readonly NetworkHelper _network = new NetworkHelper();
        private readonly SessionManager _sessionManager = null;
        private readonly List<GreenWerx.Models.Geo.TimeZone> _timesZones = new List<GreenWerx.Models.Geo.TimeZone>();
        private AppManager _appManager = null;

        public ApiBaseController()
        {
            Globals.InitializeGlobals();
            _logger = new SystemLogger(Globals.DBConnectionKey);
            _sessionManager = new SessionManager(Globals.DBConnectionKey);

            try
            {
                string pathToFile = Path.Combine(EnvironmentEx.AppDataFolder.Replace("\\\\", "\\"), "WordLists\\timezones.json");

                if (File.Exists(pathToFile))
                {
                    string timezones = File.ReadAllText(pathToFile);
                    _timesZones = JsonConvert.DeserializeObject<List<GreenWerx.Models.Geo.TimeZone>>(timezones);
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
        }

        public User CurrentUser { get; set; }

        public SessionManager SessionManager { get { return _sessionManager; } }

        public string GetAuthorizationToken(HttpRequestMessage request)
        {
            return request.Headers?.Authorization?.Parameter;
        }

        public string GetAuthToken(HttpRequestMessage request)
        {
            return request.Headers?.Authorization?.Parameter;
        }

        public string GetClientIpAddress(HttpRequestMessage request)
        {
            return _network.GetClientIpAddress(Request);
        }

        public DataFilter GetFilter(HttpRequestMessage request)
        {
            if (request.Method != HttpMethod.Post)
                return new DataFilter();

            string filter = this.Request.Content.ReadAsStringAsync().Result;

            if (string.IsNullOrWhiteSpace(filter))
                return new DataFilter();

            return GetFilter(filter);
        }

        public string GetProfileUUID(string authToken)
        {
            if (string.IsNullOrWhiteSpace(authToken))
                return authToken;

            _appManager = new AppManager(Globals.DBConnectionKey, "web", authToken);
            string appSecret = _appManager.GetSetting("AppKey")?.Value;

            JwtClaims requestorClaims = null;

            try
            {
                var payload = JWT.JsonWebToken.Decode(authToken, appSecret, false);
                requestorClaims = JsonConvert.DeserializeObject<JwtClaims>(payload);

                TimeSpan ts = requestorClaims.Expires - DateTime.UtcNow;

                if (ts.TotalSeconds <= 0)
                    return string.Empty;

                string[] tokens = requestorClaims.aud.Replace(SystemFlag.Default.Account, "systemdefaultaccount").Split('.');
                if (tokens.Length == 0)
                    return string.Empty;

                //string userUUID = tokens[0];
                //string accountUUID = tokens[1];
                //if ("systemdefaultaccount" == accountUUID) accountUUID = SystemFlag.Default.Account;

                string profileUUID = tokens[2];
                return profileUUID;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        public float GetTimezoneOffset(string timeZone)
        {
            float offset = 0;
            _timesZones.ForEach(tz =>
            {
                for (int i = 0; i < tz.utc.Length; i++)
                {
                    if (tz.utc[i].EqualsIgnoreCase(timeZone))
                    {
                        offset = tz.offset;
                        return;
                    }
                }
            });
            return offset;
        }

        public User GetUser(AuthenticationHeaderValue authorization)
        {
            if (authorization == null)
                return null;

            return GetUser(authorization.Parameter);
        }

        public User GetUser(string authToken)
        {
            if (string.IsNullOrWhiteSpace(authToken))
                return null;

            UserSession us = SessionManager.GetSession(authToken);
            if (us == null)
                return null;

            if (string.IsNullOrWhiteSpace(us.UserData))
                return null;

            this.CurrentUser = JsonConvert.DeserializeObject<User>(us.UserData);

            return CurrentUser;
        }

        /// <summary>
        /// note: todo we can use the difference in datecreated from submitDate to see if
        /// it was submitted too fast. Usually less than three seconds.
        /// If submittedDate or dateCreated is null then it could be an api attempt
        /// </summary>
        /// <param name="node"></param>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public bool IsSpamPost(INode node, string ipAddress)
        {
            if (node == null)
                return false;

            if (!string.IsNullOrWhiteSpace(node.SubmitValue))
            {
                SystemLogger logger = new SystemLogger(Globals.DBConnectionKey);
                logger.InsertSecurity(JsonConvert.SerializeObject(node), ipAddress + "|SPAM", "RegisterAsync");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Not logged
        /// </summary>
        /// <param name="submitValue"></param>
        /// <param name="dateCreated"></param>
        /// <param name="dateSubmitted"></param>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public bool IsSpamPost(string submitValue, DateTime dateCreated, DateTime dateSubmitted, string ipAddress)
        {
            if (!string.IsNullOrWhiteSpace(submitValue))
            {
                return true;
            }
            return false;
        }

        protected override void Initialize(HttpControllerContext controllerContext)
        {
            base.Initialize(controllerContext);
            if (controllerContext.Request == null)
                return;

            string auth = this.GetAuthToken(controllerContext.Request);
            if (!string.IsNullOrWhiteSpace(auth) && auth != "undefined")
                this.GetUser(auth);
        }

        private DataFilter GetFilter(string filter)
        {
            int currentUserRoleWeight = CurrentUser?.RoleWeight ?? 0;
            bool siteAdmin = CurrentUser?.SiteAdmin ?? false;
            DataFilter tmpFilter = Globals.DefaultDataFilter;
            tmpFilter.UserRoleWeight = currentUserRoleWeight;

            if (string.IsNullOrWhiteSpace(filter) || filter == "[]")
            {
                return tmpFilter;
            }

            try
            {
                tmpFilter = JsonConvert.DeserializeObject<DataFilter>(filter);

                if (tmpFilter == null)
                {
                    return Globals.DefaultDataFilter;
                }

                if (tmpFilter.PageSize > Globals.MaxRecordsPerRequest && siteAdmin == false)
                    tmpFilter.PageSize = Globals.MaxRecordsPerRequest;

                if (currentUserRoleWeight > 0 && siteAdmin == false)
                {   //the higher thier role the more records they can get in one call
                    tmpFilter.PageSize = currentUserRoleWeight * Globals.DefaultDataFilter.PageSize;
                }
                if (Validator.HasSqlCommand(tmpFilter.SortBy))
                    tmpFilter.SortBy = "Name";

                if (Validator.HasSqlCommand(tmpFilter.SortDirection))
                    tmpFilter.SortDirection = "ASC";

                if (Validator.HasSqlCommand(tmpFilter.TimeZone))
                    tmpFilter.TimeZone = "";

                for (int i = 0; i < tmpFilter.Screens.Count; i++)
                {
                    var tmp = tmpFilter.Screens[i].Value + " " + tmpFilter.Screens[i].Command + " " + tmpFilter.Screens[i].Field + " " + tmpFilter.Screens[i].Junction + " " +
                          tmpFilter.Screens[i].Operator + " " + tmpFilter.Screens[i].Type;

                    if (Validator.HasSqlCommand(tmp))
                    {
                        Debug.Assert(false, "SQL INJECTION! " + tmp);
                        tmpFilter.Screens[i].Value = "";
                        tmpFilter.Screens[i].Command = "";
                        tmpFilter.Screens[i].Field = "";
                        tmpFilter.Screens[i].Junction = "";
                        tmpFilter.Screens[i].Operator = "";
                        tmpFilter.Screens[i].Type = "";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.InsertError(ex.Message, "ApiBaseController", "GetFilter");
                return tmpFilter;
            }
            return tmpFilter;
        }
    }
}