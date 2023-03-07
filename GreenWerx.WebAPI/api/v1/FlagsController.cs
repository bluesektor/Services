using GreenWerx.Managers.Events;
using GreenWerx.Models.App;
using GreenWerx.Models.Events;
using GreenWerx.Web;
using GreenWerx.Web.api;
using GreenWerx.Web.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GreenWerx.WebAPI.api.v1
{
    public class FlagsController :  ApiBaseController
    {
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPut]
        [System.Web.Http.Route("api/flags/{type}/{field}/{value}/{uuid}")]
        public ServiceResult UpdateFlag(string type, string field, bool value, string uuid)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (CurrentUser.SiteAdmin == false)
                return ServiceResponse.Error("You are not authorized this functioanity.");

            if (string.IsNullOrWhiteSpace(type))
                return ServiceResponse.BadRequest("Type is missing.");

            if (string.IsNullOrWhiteSpace(field))
                return ServiceResponse.BadRequest("Field is missing.");

            if (string.IsNullOrWhiteSpace(field))
                return ServiceResponse.BadRequest("UUID is missing.");

            switch (type.ToLower())
            {
                case "event":
                    EventManager eventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                    var eres=  eventManager.Get(uuid);
                    if (eres.Code != 200)
                        return ServiceResponse.BadRequest(eres.Message);
                    var evnt = eres.Result as Event;

                    switch (field.ToLower())
                    {
                        case "private":
                            evnt.Private = value;
                            break;
                        case "active":
                        case "published":
                            evnt.Status = "";
                            evnt.Reference = "";
                            evnt.Active = value;
                            break;
                        case "takeover":
                            evnt.TakeOver = value;
                            break;
                        case "virtual":
                            evnt.Virtual = value;
                            break;
                        case "isaffiliate":
                            evnt.IsAffiliate = value;
                            break;
                    }
                    return eventManager.Update(evnt);
            }

            return ServiceResponse.OK();

        }
    }
}