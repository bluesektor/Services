// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.Collections.Generic;
using System.Linq;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Managers.Events;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Events;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Web.Filters;
using WebApi.OutputCache.V2;

namespace GreenWerx.Web.api.v1
{
    [CacheOutput(ClientTimeSpan = 100, ServerTimeSpan = 100)]
    public class NotificationsController : ApiBaseController
    {
        public NotificationsController()
        {
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Notifications/Delete")]
        public ServiceResult Delete(Notification n)
        {
            if (n == null || string.IsNullOrWhiteSpace(n.UUID))
                return ServiceResponse.Error("Invalid account was sent.");

            NotificationManager NotificationManager = new NotificationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return NotificationManager.Delete(n);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Notifications/{name}")]
        public ServiceResult Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResponse.Error("You must provide a name for the Notification.");

            NotificationManager NotificationManager = new NotificationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<Notification> s = NotificationManager.Search(name);

            if (s == null || s.Count == 0)
                return ServiceResponse.Error("Notification could not be located for the name " + name);

            return ServiceResponse.OK("", s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/NotificationsBy/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("You must provide a name for the Notification.");

            NotificationManager NotificationManager = new NotificationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return NotificationManager.Get(uuid);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Notifications")]
        public ServiceResult GetNotifications()
        {
            if (Request.Headers.Authorization == null || string.IsNullOrWhiteSpace(this.GetAuthToken(Request)))
                return ServiceResponse.Error("You must be logged in to access this functionality.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            NotificationManager NotificationManager = new NotificationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<dynamic> Notifications = NotificationManager.GetNotifications(CurrentUser.AccountUUID).Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            Notifications = Notifications.Filter(ref filter);

            return ServiceResponse.OK("", Notifications, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Notifications/Add")]
        [System.Web.Http.Route("api/Notifications/Insert")]
        public ServiceResult Insert(Notification s)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(s.AccountUUID) || s.AccountUUID == SystemFlag.Default.Account)
                s.AccountUUID = CurrentUser.AccountUUID;

            if (string.IsNullOrWhiteSpace(s.CreatedBy))
                s.CreatedBy = CurrentUser.UUID;

            if (s.DateCreated == DateTime.MinValue)
                s.DateCreated = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(s.FromUUID))
            {
                s.FromUUID = GetClientIpAddress(Request);
                s.FromType = "ip";
            }
            NotificationManager NotificationManager = new NotificationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return NotificationManager.Insert(s);
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
        /// <param name = "p" ></ param >
        /// < returns ></ returns >
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpPatch]
        [System.Web.Http.Route("api/Notifications/Update")]
        public ServiceResult Update(Notification s)
        {
            if (s == null)
                return ServiceResponse.Error("Invalid Notification sent to server.");

            NotificationManager NotificationManager = new NotificationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = NotificationManager.Get(s.UUID);
            if (res.Code != 200)
                return res;

            var dbS = (Notification)res.Result;

            if (dbS.DateCreated == DateTime.MinValue)
                dbS.DateCreated = DateTime.UtcNow;

            dbS.Deleted = s.Deleted;
            dbS.Name = s.Name;
            dbS.Status = s.Status;
            dbS.SortOrder = s.SortOrder;

            return NotificationManager.Update(dbS);
        }
    }
}