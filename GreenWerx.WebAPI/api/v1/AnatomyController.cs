// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.Collections.Generic;
using System.Linq;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Medical;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Web.Filters;

namespace GreenWerx.Web.api.v1
{
    public class AnatomyController : ApiBaseController
    {
        public AnatomyController()
        {
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Anatomy/Add")]
        public ServiceResult AddAnatomy(Anatomy s)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(s.AccountUUID) || s.AccountUUID == SystemFlag.Default.Account)
                s.AccountUUID = CurrentUser.AccountUUID;

            if (string.IsNullOrWhiteSpace(s.CreatedBy))
                s.CreatedBy = CurrentUser.UUID;

            if (s.DateCreated == DateTime.MinValue)
                s.DateCreated = DateTime.UtcNow;

            AnatomyManager anatomyManager = new AnatomyManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return anatomyManager.Insert(s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Anatomy/Delete")]
        public ServiceResult Delete(Anatomy s)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.UUID))
                return ServiceResponse.Error("Invalid account was sent.");

            AnatomyManager anatomyManager = new AnatomyManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return anatomyManager.Delete(s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Anatomy")]
        public ServiceResult Get()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");
            DataFilter filter = this.GetFilter(Request);
            AnatomyManager anatomyManager = new AnatomyManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<dynamic> anatomy = anatomyManager.GetAnatomies(CurrentUser.AccountUUID, ref filter).Cast<dynamic>().ToList();

            return ServiceResponse.OK("", anatomy, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Anatomy/{name}")]
        public ServiceResult GetAnatomyByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResponse.Error("You must provide a name for the Anatomy.");

            AnatomyManager anatomyManager = new AnatomyManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<Anatomy> s = anatomyManager.Search(name);

            if (s == null || s.Count == 0)
                return ServiceResponse.Error("Anatomy could not be located for the name " + name);

            return ServiceResponse.OK("", s);
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
        [System.Web.Http.Route("api/Anatomy/Update")]
        public ServiceResult Update(Anatomy s)
        {
            if (s == null)
                return ServiceResponse.Error("Invalid Anatomy sent to server.");

            AnatomyManager anatomyManager = new AnatomyManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = anatomyManager.Get(s.UUID);
            if (res.Code != 200)
                return res;

            var dbS = (Anatomy)res.Result;

            if (dbS.DateCreated == DateTime.MinValue)
                dbS.DateCreated = DateTime.UtcNow;
            dbS.Deleted = s.Deleted;
            dbS.Name = s.Name;
            dbS.Status = s.Status;
            dbS.SortOrder = s.SortOrder;

            return anatomyManager.Update(dbS);
        }
    }
}