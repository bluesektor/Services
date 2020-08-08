// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.Collections.Generic;
using System.Linq;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Medical;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Web.Filters;

namespace GreenWerx.Web.api.v1
{
    public class AnatomyTagsController : ApiBaseController
    {
        public AnatomyTagsController()
        {
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/AnatomyTags/Delete")]
        public ServiceResult Delete(AnatomyTag n)
        {
            if (n == null || string.IsNullOrWhiteSpace(n.UUID))
                return ServiceResponse.Error("Invalid account was sent.");

            AnatomyManager AnatomyTagsManager = new AnatomyManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            if (AnatomyTagsManager.Delete(n) == true)
                return ServiceResponse.OK();

            return ServiceResponse.Error("An error occurred deleting this AnatomyTags.");
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/AnatomyTags")]
        public ServiceResult GetAnatomyTags()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            DataFilter filter = this.GetFilter(Request);
            AnatomyManager AnatomyTagsManager = new AnatomyManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<dynamic> AnatomyTags = AnatomyTagsManager.GetAnatomies(CurrentUser.AccountUUID, ref filter).Cast<dynamic>().ToList();

            AnatomyTags = AnatomyTags.Filter(ref filter);
            return ServiceResponse.OK("", AnatomyTags, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/AnatomyTagBy/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("You must provide a name for the AnatomyTags.");

            AnatomyManager AnatomyTagsManager = new AnatomyManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return AnatomyTagsManager.Get(uuid);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/AnatomyTags/Add")]
        [System.Web.Http.Route("api/AnatomyTags/Insert")]
        public ServiceResult Insert(AnatomyTag n)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(n.AccountUUID))
                n.AccountUUID = CurrentUser.AccountUUID;

            if (string.IsNullOrWhiteSpace(n.CreatedBy))
                n.CreatedBy = CurrentUser.UUID;

            if (n.DateCreated == DateTime.MinValue)
                n.DateCreated = DateTime.UtcNow;

            AnatomyManager AnatomyTagsManager = new AnatomyManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return AnatomyTagsManager.Insert(n);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/AnatomyTag/{name}")]
        public ServiceResult Search(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResponse.Error("You must provide a name for the AnatomyTags.");

            AnatomyManager AnatomyTagsManager = new AnatomyManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<AnatomyTag> s = AnatomyTagsManager.GetAnatomyTags(name);

            if (s == null)
                return ServiceResponse.Error("AnatomyTags could not be located for the name " + name);

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
        [System.Web.Http.Route("api/AnatomyTags/Update")]
        public ServiceResult Update(AnatomyTag n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid AnatomyTags sent to server.");

            AnatomyManager AnatomyTagsManager = new AnatomyManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = AnatomyTagsManager.Get(n.UUID);
            if (res.Code != 200)
                return res;

            var dbS = (AnatomyTag)res.Result;

            var s = (AnatomyTag)n;

            if (dbS.DateCreated == DateTime.MinValue)
                dbS.DateCreated = DateTime.UtcNow;
            dbS.Deleted = s.Deleted;
            dbS.Name = s.Name;
            dbS.Status = s.Status;
            dbS.SortOrder = s.SortOrder;

            return AnatomyTagsManager.Update(dbS);
        }
    }
}