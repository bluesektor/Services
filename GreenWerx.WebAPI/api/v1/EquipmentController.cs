// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System.Collections.Generic;
using System.Linq;
using GreenWerx.Managers.Equipment;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Web;
using GreenWerx.Web.api;
using GreenWerx.Web.Filters;

namespace GreenWerx.WebAPI.api.v1
{
    public class EquipmentController : ApiBaseController
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="type">This is the class int the Equipment folder and should be the field UUIDType.. Ballast, bulb, vehicle...</param>
        /// <param name="filter"></param>
        /// <param name="startIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="sorting"></param>
        /// <returns></returns>
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Equipment/Type/{type}")]
        public ServiceResult GetEquipment(string type = "")
        {
            EquipmentManager equipmentManager = new EquipmentManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<dynamic> Equipment = (List<dynamic>)equipmentManager.GetAll(type).Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            Equipment = Equipment.Filter(ref filter);
            return ServiceResponse.OK("", Equipment, filter.TotalRecordCount);
        }
    }
}