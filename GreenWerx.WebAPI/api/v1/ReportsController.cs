// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using GreenWerx.Data.Logging;
using GreenWerx.Managers.DataSets;
using GreenWerx.Managers.Membership;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Web.Filters;
using WebApi.OutputCache.V2;

namespace GreenWerx.Web.api.v1
{
    [CacheOutput(ClientTimeSpan = 100, ServerTimeSpan = 100)]
    public class ReportsController : ApiBaseController
    {
        public ReportsController()
        {
        }

        /// <summary>
        /// need to let the user select the series for the dataset.
        /// </summary>
        /// <param name = "category" > Database table to pull from.</param>
        /// <param name = "field" > field in the table to be returned</param>
        /// <param name = "startIndex" ></ param >
        /// < param name= "pageSize" ></ param >
        /// < param name= "sorting" ></ param >
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 5)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Reports/{type}/Dataset/{field}")]
        public ServiceResult GetDataset(string type = "", string field = "")
        {
            List<DataPoint> dataSet;

            if (string.IsNullOrWhiteSpace(type))
                return ServiceResponse.Error("You must provide a type to get the datasets.");

            if (string.IsNullOrWhiteSpace(field))
                return ServiceResponse.Error("You must provide a series to get the datasets.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            //todo log all access to this.
            //CurrentUser.RoleWeight
            //CurrentUser.AccountUUID

            if (type?.ToLower() == "users" && CurrentUser.SiteAdmin == false)
            {
                //BACKLOG  turn on the flag to log permission routes to log this.
                //add numeric value to roles s we can include multiple roles by doing math >= roleWeight
                RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, CurrentUser);
                var admin = roleManager.GetRole("admin", CurrentUser.AccountUUID);
                var owner = roleManager.GetRole("owner", CurrentUser.AccountUUID);
                if (admin == null && owner == null)
                    return ServiceResponse.Error("You are not authorized this action.");

                if (!roleManager.IsInRole(CurrentUser.UUID, CurrentUser.AccountUUID, admin.UUID, false) ||
                    !roleManager.IsInRole(CurrentUser.UUID, CurrentUser.AccountUUID, owner.UUID, false))
                {
                    return ServiceResponse.Error("You are not authorized to query the type:" + type);
                }
            }

            try
            {
                DataFilter filter = this.GetFilter(Request);
                DatasetManager dm = new DatasetManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                dataSet = dm.GetDataSet(type, ref filter);
                return ServiceResponse.OK("", dataSet);
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                SystemLogger logger = new SystemLogger(Globals.DBConnectionKey);

                logger.InsertError(ex.Message, "ReportsController", "GetDataset");
                return ServiceResponse.Error("Error retrieving dataset.");
            }
        }
    }
}