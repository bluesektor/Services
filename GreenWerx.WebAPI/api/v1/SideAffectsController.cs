// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.Collections.Generic;
using System.Linq;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Managers.Medical;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Medical;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Web.Filters;

namespace GreenWerx.Web.api.v1
{
    public class SideAffectsController : ApiBaseController
    {
        public SideAffectsController()
        {
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/SideAffects/Delete")]
        public ServiceResult Delete(SideAffect n)
        {
            if (n == null || string.IsNullOrWhiteSpace(n.UUID))
                return ServiceResponse.Error("Invalid account was sent.");

            SideAffectManager SideAffectManager = new SideAffectManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return SideAffectManager.Delete(n);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/SideAffectBy/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("You must provide a name for the SideAffect.");

            SideAffectManager SideAffectManager = new SideAffectManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return SideAffectManager.Get(uuid);
        }

        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Doses/{doseUUID}/SideAffects/History/{parentUUID}")]
        public ServiceResult GetChildSideAffects(string doseUUID, string parentUUID = "")
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(doseUUID))
                return ServiceResponse.Error("You must send a dose uuid.");

            SideAffectManager SideAffectManager = new SideAffectManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<dynamic> SymptomsLog = SideAffectManager.GetSideAffectsByDose(doseUUID, parentUUID, CurrentUser.AccountUUID).Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            SymptomsLog = SymptomsLog.Filter(ref filter);

            return ServiceResponse.OK("", SymptomsLog, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/SideAffects/{parentUUID}")]
        public ServiceResult GetSideAffects(string parentUUID = "")
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            List<dynamic> SideAffects;

            SideAffectManager SideAffectManager = new SideAffectManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            SideAffects = SideAffectManager.GetSideAffects(parentUUID, CurrentUser.AccountUUID).Cast<dynamic>().ToList(); ;

            DataFilter filter = this.GetFilter(Request);
            SideAffects = SideAffects.Filter(ref filter);

            return ServiceResponse.OK("", SideAffects, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/SideAffects/Add")]
        [System.Web.Http.Route("api/SideAffects/Insert")]
        public ServiceResult Insert(SideAffect n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid form data sent.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(n.AccountUUID) || n.AccountUUID == SystemFlag.Default.Account)
                n.AccountUUID = CurrentUser.AccountUUID;

            if (string.IsNullOrWhiteSpace(n.CreatedBy))
                n.CreatedBy = CurrentUser.UUID;

            if (n.DateCreated == DateTime.MinValue)
                n.DateCreated = DateTime.UtcNow;

            SideAffectManager SideAffectManager = new SideAffectManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return SideAffectManager.Insert(n);
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
        [System.Web.Http.Route("api/SideAffects/Update")]
        public ServiceResult Update(SideAffect s)
        {
            if (s == null)
                return ServiceResponse.Error("Invalid SideAffect sent to server.");

            SideAffectManager SideAffectManager = new SideAffectManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = SideAffectManager.Get(s.UUID);
            if (res.Code != 200)
                return res;

            var dbS = (SideAffect)res.Result;

            if (dbS.DateCreated == DateTime.MinValue)
                dbS.DateCreated = DateTime.UtcNow;
            dbS.Deleted = s.Deleted;
            dbS.Name = s.Name;
            dbS.Status = s.Status;
            dbS.SortOrder = s.SortOrder;

            return SideAffectManager.Update(dbS);
        }
    }
}