// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GreenWerx.Data.Logging;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Managers;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Web.Filters;
using WebApi.OutputCache.V2;

namespace GreenWerx.Web.api.v1
{
    [CacheOutput(ClientTimeSpan = 100, ServerTimeSpan = 100)]
    public class UnitsOfMeasureController : ApiBaseController
    {
        public UnitsOfMeasureController()
        {
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/UnitsOfMeasure/ProductCategories/Assign")]
        public ServiceResult AssignUOMsToProductCategories()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            ServiceResult res = new ServiceResult();
            res.Code = 200;
            StringBuilder msg = new StringBuilder();
            try
            {
                Task<string> content = Request.Content.ReadAsStringAsync();
                if (content == null)
                    return ServiceResponse.Error("No data was sent.");

                string body = content.Result;

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("Content body is empty.");

                List<UnitOfMeasure> uoms = JsonConvert.DeserializeObject<List<UnitOfMeasure>>(body);

                foreach (UnitOfMeasure u in uoms)
                {
                    u.AccountUUID = CurrentUser.AccountUUID;
                    u.CreatedBy = CurrentUser.UUID;
                    u.DateCreated = DateTime.UtcNow;
                    UnitOfMeasureManager UnitsOfMeasureManager = new UnitOfMeasureManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

                    ServiceResult tmpRes = UnitsOfMeasureManager.Insert(u);
                    if (tmpRes.Code != 200)
                    {
                        res.Code = tmpRes.Code;
                        msg.AppendLine(tmpRes.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                res = ServiceResponse.Error(ex.Message);
                Debug.Assert(false, ex.Message);
                SystemLogger logger = new SystemLogger(Globals.DBConnectionKey);

                logger.InsertError(ex.Message, "UnitsOfMeasureController", "AssignUOMsToProductCategories");
            }
            res.Message = msg.ToString();
            return res;
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/UnitsOfMeasure/Delete")]
        public ServiceResult Delete(UnitOfMeasure n)
        {
            if (n == null || string.IsNullOrWhiteSpace(n.UUID))
                return ServiceResponse.Error("Invalid account was sent.");

            UnitOfMeasureManager UnitsOfMeasureManager = new UnitOfMeasureManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return UnitsOfMeasureManager.Delete(n);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/UnitsOfMeasure/Delete/{uuid}")]
        public ServiceResult Delete(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("Invalid id was sent.");

            UnitOfMeasureManager UnitsOfMeasureManager = new UnitOfMeasureManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = UnitsOfMeasureManager.Get(uuid);
            if (res.Code != 200)
                return res;

            UnitOfMeasure fa = (UnitOfMeasure)res.Result;
            return UnitsOfMeasureManager.Delete(fa);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/UnitsOfMeasure/{name}")]
        public ServiceResult Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResponse.Error("You must provide a name for the UnitsOfMeasure.");

            UnitOfMeasureManager UnitsOfMeasureManager = new UnitOfMeasureManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            DataFilter filter = this.GetFilter(Request);
            List<UnitOfMeasure> s = UnitsOfMeasureManager.Search(name, ref filter);

            if (s == null || s.Count == 0)
                return ServiceResponse.Error("UnitsOfMeasure could not be located for the name " + name);

            return ServiceResponse.OK("", s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/UnitsOfMeasureBy/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("You must provide a name for the UnitsOfMeasure.");

            UnitOfMeasureManager UnitsOfMeasureManager = new UnitOfMeasureManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return UnitsOfMeasureManager.Get(uuid);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/UnitsOfMeasure")]
        public ServiceResult GetUnitsOfMeasure()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            UnitOfMeasureManager UnitsOfMeasureManager = new UnitOfMeasureManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            DataFilter filter = this.GetFilter(Request);
            List<dynamic> UnitOfMeasures = UnitsOfMeasureManager.GetUnitsOfMeasure(CurrentUser.AccountUUID, ref filter).Cast<dynamic>().ToList();
            return ServiceResponse.OK("", UnitOfMeasures, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/UnitsOfMeasure/Add")]
        [System.Web.Http.Route("api/UnitsOfMeasure/Insert")]
        public ServiceResult Insert(UnitOfMeasure n)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(n.AccountUUID) || n.AccountUUID == SystemFlag.Default.Account)
                n.AccountUUID = CurrentUser.AccountUUID;

            if (string.IsNullOrWhiteSpace(n.CreatedBy))
                n.CreatedBy = CurrentUser.UUID;

            if (n.DateCreated == DateTime.MinValue)
                n.DateCreated = DateTime.UtcNow;

            UnitOfMeasureManager UnitsOfMeasureManager = new UnitOfMeasureManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return UnitsOfMeasureManager.Insert(n);
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
        [System.Web.Http.Route("api/UnitsOfMeasure/Update")]
        public ServiceResult Update(UnitOfMeasure s)
        {
            if (s == null)
                return ServiceResponse.Error("Invalid UnitsOfMeasure sent to server.");

            UnitOfMeasureManager UnitsOfMeasureManager = new UnitOfMeasureManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            var res = UnitsOfMeasureManager.Get(s.UUID);
            if (res.Code != 200)
                return res;

            var dbS = (UnitOfMeasure)res.Result;

            if (dbS.DateCreated == DateTime.MinValue)
                dbS.DateCreated = DateTime.UtcNow;
            dbS.Deleted = s.Deleted;
            dbS.Name = s.Name;
            dbS.Status = s.Status;
            dbS.SortOrder = s.SortOrder;
            dbS.Category = s.Category;
            dbS.ShortName = s.ShortName;
            return UnitsOfMeasureManager.Update(dbS);
        }
    }
}