using Newtonsoft.Json;
using System;
using System.Linq;
using GreenWerx.Data;
using GreenWerx.Managers.Logging;
using GreenWerx.Managers.Membership;
using GreenWerx.Managers.Tools;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Logging;
using GreenWerx.Models.Membership;
using GreenWerx.Models.Tools;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Web;
using GreenWerx.Web.api;
using GreenWerx.Web.api.Helpers;
using GreenWerx.Web.Filters;
using WebApiThrottle;
using TMG = GreenWerx.Models.General;

namespace GreenWerx.WebAPI.api.v1
{
    public class LogsController : ApiBaseController
    {
        public LogsController()
        {
        }

        //// [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        //[ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        //[System.Web.Http.HttpGet]
        //[System.Web.Http.Route("api/LogsBy/{uuid}")]
        //public ServiceResult GetBy(string uuid)
        //{
        //    if (string.IsNullOrWhiteSpace(uuid))
        //        return ServiceResponse.Error("You must provide a uuid for the strain.");

        //    LogManager strainManager = new LogManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

        //    return strainManager.Get(uuid);

        //}

        //todo bookmark latest. none of the paging is working
        //assets/strains
        //   assets/products
        //    not paging, maybe check the filter in the angular page

        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/AffiliateLog/Add")]
        public ServiceResult AddLog(AffiliateLog access)
        {
            if (access == null)
                return ServiceResponse.Error("No data sent.");

            if (string.IsNullOrWhiteSpace(access.Name))
                return ServiceResponse.Error("Target name not set.");

            NetworkHelper network = new NetworkHelper();
            access.ClientIp = network.GetClientIpAddress(this.Request);

            AffliateManager affliateManager = new AffliateManager(Globals.DBConnectionKey);

            var logRes = affliateManager.Insert(access);

            if (CurrentUser == null || string.IsNullOrWhiteSpace(CurrentUser.UUParentID) == false)
                return logRes;

            UserManager um = new UserManager(Globals.DBConnectionKey, this.GetAuthorizationToken(this.Request));
            User referringUser = new User();
            string type = access.NameType?.ToUpper();
            switch (type)
            {
                case "USER":
                    referringUser = um.Search(access.Name, false).FirstOrDefault(x => x.Name.EqualsIgnoreCase(access.Name));
                    if (referringUser == null)
                        return logRes;
                    break;
                    //default:
                    //    referringUser = um.Search(access.Name, false).FirstOrDefault(x => x.Name.EqualsIgnoreCase(access.Name));
                    //    if (referringUser == null)
                    //        return logRes;
                    //    break;
            }
            var userRes = um.GetUser(CurrentUser.UUID, false);
            if (userRes.Code == 200)
            {
                var targetUser = (User)userRes.Result;
                targetUser.UUParentID = referringUser.UUID;
                targetUser.UUParentIDType = referringUser.UUIDType;
                um.Update(targetUser);
            }
            return logRes;
        }

        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Logs")]
        public ServiceResult GetLogs()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (CurrentUser.RoleWeight < 92 && CurrentUser.SiteAdmin == false)
                return ServiceResponse.Error("You authorized access to this function.");

            int count;

            DataFilter filter = this.GetFilter(Request);
            AffliateManager al = new AffliateManager(Globals.DBConnectionKey);

            switch (filter.ViewType?.ToLower())
            {
                case "accesslog":
                    var als = al.GetAllAccessLogs(ref filter);
                    return ServiceResponse.OK("", als, filter.TotalRecordCount);

                case "requestlog":
                    var rls = al.GetAllRequestLogs(ref filter);
                    return ServiceResponse.OK("", rls, filter.TotalRecordCount);

                case "systemlog":
                    var sl = al.GetAllSystemLogs(ref filter);
                    return ServiceResponse.OK("", sl, filter.TotalRecordCount);
                case "stageddata":
                    StageDataManager sdm = new StageDataManager(Globals.DBConnectionKey, "");
                    var sd = sdm.GetAll(ref filter);
                    return ServiceResponse.OK("", sd, filter.TotalRecordCount);
            }

            return ServiceResponse.Error("Invalid log request.");
        }


        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/StagedData/DataTypes")]
        public ServiceResult GetStagedDataTypes()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (CurrentUser.RoleWeight < 92 && CurrentUser.SiteAdmin == false)
                return ServiceResponse.Error("You authorized access to this function.");


            StageDataManager sdm = new StageDataManager(Globals.DBConnectionKey, "");
                    var sd = sdm.GetDataTypes();
          return ServiceResponse.OK("", sd);
          
          
        }


        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/StagedData/{uuid}")]
        public ServiceResult DeleteStagedData(string uuid)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (CurrentUser.RoleWeight < 92 && CurrentUser.SiteAdmin == false)
                return ServiceResponse.Error("You authorized access to this function.");

            string error = "";
            try
            {

                StageDataManager sdm = new StageDataManager(Globals.DBConnectionKey, "");
                var res = sdm.Get(uuid);
                if (res.Code != 200)
                    return res;

                var result = sdm.Get(uuid);

                if (result.Code != 200)
                    return result;

                var stagedItem = result.Result as StageData;


                //var delRes = sdm.Delete((INode)stagedItem);

                //if (delRes.Code != 200)
                //    return delRes;
                using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
                {
                    context.Delete<StageData>(stagedItem);

                    var stagedAttributes = context.GetAll<StageData>().Where(w => w.DataType.EqualsIgnoreCase("attribute"));

                    foreach (var stageAttribute in stagedAttributes)
                    {

                        var attribute = JsonConvert.DeserializeObject<TMG.Attribute>(stageAttribute.StageResults);
                        if (attribute.ReferenceUUID == uuid)
                        {
                            if (context.Delete<TMG.Attribute>(attribute) == false)
                            {
                                stageAttribute.Result = "delete failed";
                                context.Update<StageData>(stageAttribute);
                            }
                        }

                    }
                }

                return ServiceResponse.OK();
            }
            catch (Exception ex)
            {
                error = ex.DeserializeException();
            }
            return ServiceResponse.Error(error);
        }


        /// <summary>
        /// This takes the stagedUUID and adds it to the 
        /// appropriate table along with its attributes.
        /// </summary>
        /// <returns></returns>
        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/StagedData/Import/{uuid}")]
        public ServiceResult ImportStagedItem(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("No uuid sent.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (CurrentUser.RoleWeight < 92 && CurrentUser.SiteAdmin == false)
                return ServiceResponse.Error("You authorized access to this function.");


            StageDataManager sdm = new StageDataManager(Globals.DBConnectionKey, "");

            var result = sdm.Get(uuid);

            if (result.Code != 200)
                return result;

            var stagedItem = result.Result as StageData;
            dynamic stagedType = null;
            using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
            {
            
                switch (stagedItem.DataType) {
                    case "account":
                        stagedType = JsonConvert.DeserializeObject<Account>(stagedItem.StageResults);
                        stagedType.Status = "new";
                        Initialize(ref stagedType, this.CurrentUser.UUID, this.CurrentUser.AccountUUID, this.CurrentUser.RoleWeight);

                        if (!context.Insert<Account>((Account)stagedType))
                            return ServiceResponse.Error("Failed to import item.");
                        break;
                }
                context.Delete<StageData>(stagedItem);

                var stagedAttributes = context.GetAll<StageData>().Where(w => w.DataType.EqualsIgnoreCase("attribute"));

                foreach (var stageAttribute in stagedAttributes) {

                    var attribute = JsonConvert.DeserializeObject<TMG.Attribute>(stageAttribute.StageResults);
                    if (attribute.ReferenceUUID == stagedType.UUID)
                    {
                        if (!context.Insert<TMG.Attribute>((TMG.Attribute)attribute))
                        {
                            stageAttribute.Result = "import failed";
                            context.Update<StageData>(stageAttribute);
                        }
                    }

                }
            }
            return ServiceResponse.OK("", stagedType);
        }
        public void Initialize(ref dynamic item, string userUUID, string accountUUID, int roleWeight)
        {
            if (string.IsNullOrWhiteSpace(item.CreatedBy))
                item.CreatedBy = userUUID;

            if (string.IsNullOrWhiteSpace(item.AccountUUID))
                item.AccountUUID = accountUUID;

            if (string.IsNullOrWhiteSpace(item.UUID))
                item.UUID = Guid.NewGuid().ToString("N");

            if (string.IsNullOrWhiteSpace(item.UUIDType))
                item.UUIDType = item.GetType().Name;

            if (string.IsNullOrWhiteSpace(item.GUUID))
                item.GUUID = item.UUID;

            if (string.IsNullOrWhiteSpace(item.GuuidType))
                item.GuuidType = item.GetType().Name;

            if (item.DateCreated == DateTime.MinValue)
                item.DateCreated = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(item.RoleOperation))
                item.RoleOperation = ">=";

            item.RoleWeight = roleWeight;
            item.Deleted = false;
            // item.Private = true;
        }



        //[ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        //[System.Web.Http.HttpPost]
        //[System.Web.Http.HttpDelete]
        //[System.Web.Http.Route("api/Logs/Delete")]
        //public ServiceResult Delete(Log s)
        //{
        //    if (s == null || string.IsNullOrWhiteSpace(s.UUID))
        //        return ServiceResponse.Error("Invalid account was sent.");

        //    LogManager strainManager = new LogManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

        //    return strainManager.Delete(s);

        //}

        //[ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        //[System.Web.Http.HttpPost]
        //[System.Web.Http.HttpDelete]
        //[System.Web.Http.Route("api/Logs/Delete/{uuid}")]
        //public ServiceResult Delete(string uuid)
        //{
        //    if (string.IsNullOrWhiteSpace(uuid))
        //        return ServiceResponse.Error("Invalid id was sent.");

        //    LogManager strainManager = new LogManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
        //    var res = strainManager.Get(uuid);
        //    if (res.Code != 200)
        //        return res;

        //    Log fa = (Log)res.Result;

        //    return strainManager.Delete(fa);
        //}
    }
}