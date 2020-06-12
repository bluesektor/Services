// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using GreenWerx.Managers.Medical;
using GreenWerx.Models.App;
using GreenWerx.Models.Medical;
using GreenWerx.Web.Filters;

namespace GreenWerx.Web.api.v1
{
    public class PollsController : ApiBaseController
    {
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpPatch]
        [System.Web.Http.Route("api/Ratings/{type}/{uuid}/Save/{score}")]
        public ServiceResult SaveRating(string uuid, string type, float score)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("You must supply a unique identifier.");
            if (string.IsNullOrWhiteSpace(type))
                return ServiceResponse.Error("You must supply a type identifier.");

            type = type?.ToUpper().Trim();
            switch (type)
            {
                case "SYMPTOMLOG":
                    SymptomManager symptomManager = new SymptomManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                    SymptomLog sl = symptomManager.GetSymptomLogBy(uuid);
                    if (sl == null)
                        return ServiceResponse.Error("Could not find symptom.");

                    if (sl.CreatedBy != CurrentUser.UUID)
                        return ServiceResponse.Error("You are not authorized to rate this item.");

                    sl.Efficacy = score;
                    return symptomManager.Update(sl);
            }

            return ServiceResponse.Error("Invalid type:" + type);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Ratings/{type}/{uuid}/Set/{score}")]
        public ServiceResult SetRating(string uuid, string type, float score)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("You must supply a unique identifier.");
            if (string.IsNullOrWhiteSpace(type))
                return ServiceResponse.Error("You must supply a type identifier.");

            type = type.ToUpper().Trim();
            switch (type)
            {
                case "SYMPTOMLOG":
                    SymptomManager symptomManager = new SymptomManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                    SymptomLog sl = symptomManager.GetSymptomLogBy(uuid);
                    if (sl == null)
                        return ServiceResponse.Error("Could not find symptom.");

                    if (sl.CreatedBy != CurrentUser.UUID)//|| sl.AccountId != currentUser.AccountId)
                        return ServiceResponse.Error("You are not authorized to rate this item.");

                    sl.Efficacy = score;
                    return symptomManager.Update(sl);
            }

            return ServiceResponse.Error("Invalid type:" + type);
        }
    }
}