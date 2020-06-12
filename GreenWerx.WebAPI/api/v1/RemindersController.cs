// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Managers;
using GreenWerx.Managers.Events;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Events;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Web.Filters;
using GreenWerx.Web.Models;

namespace GreenWerx.Web.api.v1
{
    public class RemindersController : ApiBaseController
    {
        public RemindersController()
        {
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Reminders/Delete")]
        public ServiceResult Delete(Reminder n)
        {
            if (n == null || string.IsNullOrWhiteSpace(n.UUID))
                return ServiceResponse.Error("Invalid account was sent.");

            ReminderManager reminderManager = new ReminderManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return reminderManager.Delete(n);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Reminders/{name}")]
        public ServiceResult Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResponse.Error("You must provide a name for the Reminder.");

            ReminderManager reminderManager = new ReminderManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<Reminder> s = reminderManager.Search(name);

            if (s == null || s.Count == 0)
                return ServiceResponse.Error("Reminder could not be located for the name " + name);

            return ServiceResponse.OK("", s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/ReminderBy/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("You must provide a name for the Reminder.");

            ReminderManager reminderManager = new ReminderManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return reminderManager.Get(uuid);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 0)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Reminders")]
        public ServiceResult GetReminders()
        {
            if (Request.Headers.Authorization == null || string.IsNullOrWhiteSpace(this.GetAuthToken(Request)))
                return ServiceResponse.Error("You must be logged in to access this functionality.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            ReminderManager reminderManager = new ReminderManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            var tmp = reminderManager.GetReminders(CurrentUser.UUID, CurrentUser.AccountUUID);
            if (tmp == null)
                return ServiceResponse.OK("", null, 0);

            List<dynamic> Reminders = tmp.Cast<dynamic>()?.ToList();

            DataFilter filter = this.GetFilter(Request);
            Reminders = Reminders.Filter(ref filter);

            return ServiceResponse.OK("", Reminders, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Reminders/Add")]
        [System.Web.Http.Route("api/Reminders/Insert")]
        public ServiceResult Insert(ReminderForm s)
        {
            if (s == null)
                return ServiceResponse.Error("Invalid data sent.");

            string authToken = Request.Headers.Authorization?.Parameter;
            SessionManager sessionManager = new SessionManager(Globals.DBConnectionKey);

            UserSession us = sessionManager.GetSession(authToken);
            if (us == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (us.Captcha?.ToUpper() != s.Captcha?.ToUpper())
                return ServiceResponse.Error("Invalid code.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(s.AccountUUID) || s.AccountUUID == SystemFlag.Default.Account)
                s.AccountUUID = CurrentUser.AccountUUID;

            if (string.IsNullOrWhiteSpace(s.CreatedBy))
                s.CreatedBy = CurrentUser.UUID;

            if (s.DateCreated == DateTime.MinValue)
                s.DateCreated = DateTime.UtcNow;

            s.Active = true;
            s.Deleted = false;
            s.ReminderCount = 0; //its new, now reminders/notifications have taken place
            if (s.EventDateTime == null || s.EventDateTime == DateTime.MinValue)
                return ServiceResponse.Error("You must provide a valid event date.");

            if (string.IsNullOrWhiteSpace(s.Frequency))
                return ServiceResponse.Error("You must provide a valid frequency.");

            if (s.RepeatForever == false && s.RepeatCount <= 0)
                return ServiceResponse.Error("You must set  repeat count or repeat forver.");

            #region Convert to Reminder from ReminderForm because entity frameworks doesn't recognize casting.

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ReminderForm, Reminder>();
            });

            IMapper mapper = config.CreateMapper();
            var dest = mapper.Map<ReminderForm, Reminder>(s);

            #endregion Convert to Reminder from ReminderForm because entity frameworks doesn't recognize casting.

            ReminderManager reminderManager = new ReminderManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            ServiceResult sr = reminderManager.Insert(dest);
            if (sr.Code != 200)
                return sr;

            StringBuilder ruleErrors = new StringBuilder();

            int index = 1;
            foreach (ReminderRule r in s.ReminderRules)
            {
                DateTime dt;

                switch (r.RangeType?.ToUpper())
                {
                    case "DATE":
                        if (!DateTime.TryParse(r.RangeStart, out dt))
                            ruleErrors.AppendLine("Rule " + index + " is not a valid start date.");
                        if (!DateTime.TryParse(r.RangeEnd, out dt))
                            ruleErrors.AppendLine("Rule " + index + " is not a valid end date.");
                        break;

                    case "TIME":
                        if (!DateTime.TryParse(r.RangeStart, out dt))
                            ruleErrors.AppendLine("Rule " + index + " is not a valid start time.");
                        if (!DateTime.TryParse(r.RangeEnd, out dt))
                            ruleErrors.AppendLine("Rule " + index + " is not a valid end time.");
                        break;
                }
                index++;
                Debug.Assert(false, "CHECK MAKE SURE UUID IS SET");
                r.ReminderUUID = s.UUID;
                r.CreatedBy = CurrentUser.UUID;
                r.DateCreated = DateTime.UtcNow;
            }

            if (ruleErrors.Length > 0)
                return ServiceResponse.Error(ruleErrors.ToString());

            index = 1;
            foreach (ReminderRule r in s.ReminderRules)
            {
                ServiceResult srr = reminderManager.Insert(r);
                if (srr.Code != 200)
                {
                    ruleErrors.AppendLine("Rule " + index + " failed to save.");
                }
            }
            if (ruleErrors.Length > 0)
                return ServiceResponse.Error(ruleErrors.ToString());

            return sr;
        }

        /// <summary>
        /// Fields updated..
        ///     Category
        ///     Name
        ///     Cost
        ///     Price
        ///     Weight
        ///     WeightUOM
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpPatch]
        [System.Web.Http.Route("api/Reminders/Update")]
        public GreenWerx.Models.App.ServiceResult Update(Reminder s)
        {
            if (s == null)
                return ServiceResponse.Error("Invalid Reminder sent to server.");

            ReminderManager reminderManager = new ReminderManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = reminderManager.Get(s.UUID);
            if (res.Code != 200)
                return res;

            var dbS = (Reminder)res.Result;

            if (dbS.DateCreated == DateTime.MinValue)
                dbS.DateCreated = DateTime.UtcNow;
            dbS.Deleted = s.Deleted;
            dbS.Name = s.Name;
            dbS.Status = s.Status;
            dbS.SortOrder = s.SortOrder;
            dbS.Active = s.Active;
            dbS.Body = s.Body;
            dbS.EventDateTime = s.EventDateTime;
            dbS.RepeatForever = s.RepeatForever;
            dbS.RepeatCount = s.RepeatCount;
            dbS.Frequency = s.Frequency;
            dbS.EventUUID = s.EventUUID;
            dbS.Favorite = s.Favorite;
            return reminderManager.Update(dbS);
        }
    }
}