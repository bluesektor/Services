// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.Collections.Generic;
using System.Linq;
using GreenWerx.Managers.Store;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Store;
using GreenWerx.Web.Filters;

namespace GreenWerx.Web.api.v1
{
    public class VendorsController : ApiBaseController
    {
        public VendorsController()
        {
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Vendors/Delete")]
        public ServiceResult Delete(Vendor n)
        {
            if (n == null || string.IsNullOrWhiteSpace(n.UUID))
                return ServiceResponse.Error("Invalid account was sent.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            VendorManager vendorManager = new VendorManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return vendorManager.Delete(n);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Vendors/Delete/{vendorUUID}")]
        public ServiceResult Delete(string vendorUUID)
        {
            if (string.IsNullOrWhiteSpace(vendorUUID))
                return ServiceResponse.Error("Invalid account was sent.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            VendorManager vendorManager = new VendorManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = vendorManager.Get(vendorUUID);
            if (res.Code != 200)
                return res;
            Vendor v = (Vendor)res.Result;

            return this.Delete(v);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Vendors/{name}")]
        public ServiceResult Get(string name)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            VendorManager vendorManager = new VendorManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            DataFilter filter = this.GetFilter(Request);
            List<Vendor> s = vendorManager.Search(name, ref filter);

            if (s == null || s.Count == 0)
                return ServiceResponse.Error("Vendor could not be located for the name " + name);

            return ServiceResponse.OK("", s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/VendorsBy/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            VendorManager vendorManager = new VendorManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return vendorManager.Get(uuid);
            ;
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Vendors")]
        public ServiceResult GetVendors()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            VendorManager vendorManager = new VendorManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            DataFilter filter = this.GetFilter(Request);
            List<dynamic> Vendors = vendorManager.GetVendors(CurrentUser.AccountUUID, ref filter).Cast<dynamic>().ToList();
            return ServiceResponse.OK("", Vendors, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Vendors/Add")]
        public ServiceResult Insert(Vendor n)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(n.AccountUUID))
                n.AccountUUID = CurrentUser.AccountUUID;

            if (string.IsNullOrWhiteSpace(n.CreatedBy))
                n.CreatedBy = CurrentUser.UUID;

            if (n.DateCreated == DateTime.MinValue)
                n.DateCreated = DateTime.UtcNow;

            VendorManager vendorManager = new VendorManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return vendorManager.Insert(n);
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
        [System.Web.Http.Route("api/Vendors/Update")]
        public ServiceResult Update(Vendor v)
        {
            if (v == null)
                return ServiceResponse.Error("Invalid Vendor sent to server.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            VendorManager vendorManager = new VendorManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            var res = vendorManager.Get(v.UUID);
            if (res.Code != 200)
                return res;
            var dbv = (Vendor)res.Result;

            if (dbv.DateCreated == DateTime.MinValue)
                dbv.DateCreated = DateTime.UtcNow;

            dbv.Deleted = v.Deleted;
            dbv.Name = v.Name;
            dbv.Status = v.Status;
            dbv.SortOrder = v.SortOrder;

            return vendorManager.Update(dbv);
        }
    }
}