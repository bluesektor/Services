// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.Collections.Generic;
using System.Linq;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Managers.General;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.General;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Web.Filters;

namespace GreenWerx.Web.api.v1
{
    public class StatusMessagesController : ApiBaseController
    {
        public StatusMessagesController()
        {
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/StatusMessages/Delete")]
        public ServiceResult Delete(StatusMessage n)
        {
            if (n == null || string.IsNullOrWhiteSpace(n.UUID))
                return ServiceResponse.Error("Invalid account was sent.");

            StatusMessageManager StatusMessageManager = new StatusMessageManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return StatusMessageManager.Delete(n);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/StatusMessage/{name}")]
        public ServiceResult Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResponse.Error("You must provide a name for the StatusMessage.");

            StatusMessageManager StatusMessageManager = new StatusMessageManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return StatusMessageManager.Get(name);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/StatusMessageBy/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("You must provide a name for the StatusMessage.");

            StatusMessageManager StatusMessageManager = new StatusMessageManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return StatusMessageManager.Get(uuid);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/StatusMessages/Type/{statusType}")]
        public ServiceResult GetStatusMessages(string statusType)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            StatusMessageManager StatusMessageManager = new StatusMessageManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            DataFilter filter = this.GetFilter(Request);
            List<dynamic> StatusMessages = StatusMessageManager.GetStatusByType(statusType, CurrentUser.UUID, CurrentUser.AccountUUID, ref filter).OrderBy(ob => ob.Status).Cast<dynamic>().ToList();

            return ServiceResponse.OK("", StatusMessages, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/StatusMessages")]
        public ServiceResult GetStatusMessages()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            StatusMessageManager StatusMessageManager = new StatusMessageManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<dynamic> StatusMessages = StatusMessageManager.GetStatusMessages(CurrentUser.AccountUUID).Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            StatusMessages = StatusMessages.Filter(ref filter);

            return ServiceResponse.OK("", StatusMessages, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/StatusMessages/Add")]
        [System.Web.Http.Route("api/StatusMessages/Insert")]
        public ServiceResult Insert(StatusMessage n)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(n.AccountUUID) || n.AccountUUID == SystemFlag.Default.Account)
                n.AccountUUID = CurrentUser.AccountUUID;

            if (string.IsNullOrWhiteSpace(n.CreatedBy))
                n.CreatedBy = CurrentUser.UUID;

            if (n.DateCreated == DateTime.MinValue)
                n.DateCreated = DateTime.UtcNow;

            StatusMessageManager StatusMessageManager = new StatusMessageManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return StatusMessageManager.Insert(n);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpPatch]
        [System.Web.Http.Route("api/StatusMessages/Update")]
        public ServiceResult Update(StatusMessage s)
        {
            if (s == null)
                return ServiceResponse.Error("Invalid StatusMessage sent to server.");

            StatusMessageManager StatusMessageManager = new StatusMessageManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = StatusMessageManager.Get(s.UUID);
            if (res.Code != 200)
                return res;

            var dbS = (StatusMessage)res.Result;

            if (dbS.DateCreated == DateTime.MinValue)
                dbS.DateCreated = DateTime.UtcNow;

            dbS.Status = s.Status;

            return StatusMessageManager.Update(dbS);
        }
    }
}