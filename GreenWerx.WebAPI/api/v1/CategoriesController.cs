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
using WebApi.OutputCache.V2;

namespace GreenWerx.Web.api.v1
{
    public class CategoriesController : ApiBaseController
    {
        public CategoriesController()
        {
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Categories/Delete")]
        public ServiceResult Delete(Category n)
        {
            if (n == null || string.IsNullOrWhiteSpace(n.UUID))
                return ServiceResponse.Error("Invalid account was sent.");

            CategoryManager CategoryManager = new CategoryManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return CategoryManager.Delete(n);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Categories/Delete/{categoryUUID}")]
        public ServiceResult Delete(string categoryUUID)
        {
            if (string.IsNullOrWhiteSpace(categoryUUID))
                return ServiceResponse.Error("Invalid category was sent.");
            CategoryManager CategoryManager = new CategoryManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = CategoryManager.Get(categoryUUID);
            if (res.Code != 200)
                return res;

            return CategoryManager.Delete((Category)res.Result);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Categories/{name}")]
        public ServiceResult Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResponse.Error("You must provide a name for the Category.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            CategoryManager CategoryManager = new CategoryManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<Category> s = CategoryManager.Search(name);

            if (s == null || s.Count == 0)
                return ServiceResponse.Error("Category could not be located for the name " + name);

            return ServiceResponse.OK("", s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/CategoryBy/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("You must provide a UUID for the Category.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            CategoryManager CategoryManager = new CategoryManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return CategoryManager.Get(uuid);
        }

        [CacheOutput(ClientTimeSpan = 100, ServerTimeSpan = 100)]
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Categories")]
        public ServiceResult GetCategories()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");
            DataFilter filter = this.GetFilter(Request);
            CategoryManager CategoryManager = new CategoryManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<dynamic> Categories = (List<dynamic>)CategoryManager.GetCategories(CurrentUser.AccountUUID, false, true).Cast<dynamic>().ToList();
            Categories = Categories.Filter(ref filter);
            return ServiceResponse.OK("", Categories, filter.TotalRecordCount);
        }

        [CacheOutput(ClientTimeSpan = 100, ServerTimeSpan = 100)]
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Categories/{name}/{type}")]
        public ServiceResult GetCategory(string name = "", string type = "")
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResponse.Error("You must provide a name for the Category.");

            if (string.IsNullOrWhiteSpace(type))
                return ServiceResponse.Error("You must provide a type for the Category.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            CategoryManager CategoryManager = new CategoryManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            Category s = (Category)CategoryManager.GetCategory(name, type, CurrentUser.AccountUUID);

            if (s == null)
                return ServiceResponse.Error("Category could not be located for the name " + name);

            return ServiceResponse.OK("", s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Categories/Add")]
        [System.Web.Http.Route("api/Categories/Insert")]
        public ServiceResult Insert(Category n)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(n.AccountUUID) || n.AccountUUID == SystemFlag.Default.Account)
                n.AccountUUID = CurrentUser.AccountUUID;

            if (string.IsNullOrWhiteSpace(n.CreatedBy))
                n.CreatedBy = CurrentUser.UUID;

            if (n.DateCreated == DateTime.MinValue)
                n.DateCreated = DateTime.Now;

            CategoryManager CategoryManager = new CategoryManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return CategoryManager.Insert(n);
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
        [System.Web.Http.Route("api/Categories/Update")]
        public ServiceResult Update(Category s)
        {
            if (s == null)
                return ServiceResponse.Error("Invalid Category sent to server.");

            CategoryManager CategoryManager = new CategoryManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = CategoryManager.Get(s.UUID);
            if (res.Code != 200)
                return res;

            var dbS = (Category)res.Result;

            if (dbS.DateCreated == DateTime.MinValue)
                dbS.DateCreated = DateTime.Now;

            dbS.Deleted = s.Deleted;
            dbS.Name = s.Name;
            dbS.Status = s.Status;
            dbS.SortOrder = s.SortOrder;
            dbS.UsesStrains = s.UsesStrains;
            dbS.CategoryType = s.CategoryType;

            return CategoryManager.Update(dbS);
        }
    }
}