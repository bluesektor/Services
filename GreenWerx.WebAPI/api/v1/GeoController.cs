// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Managers;
using GreenWerx.Managers.Events;
using GreenWerx.Managers.Geo;
using GreenWerx.Managers.Membership;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Events;
using GreenWerx.Models.Geo;
using GreenWerx.Models.Membership;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Web;
using GreenWerx.Web.api;
using GreenWerx.Web.api.Helpers;
using GreenWerx.Web.Filters;

using WebApiThrottle;

namespace GreenWerx.WebAPI.api.v1
{
    public class GeoController : ApiBaseController
    {
        public GeoController()
        {
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Locations/Delete")]
        public ServiceResult Delete(Location n)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (n == null || string.IsNullOrWhiteSpace(n.UUID))
                return ServiceResponse.Error("Invalid account was sent.");

            LocationManager locationManager = new LocationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return locationManager.Delete(n);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Locations/Delete/{locationUUID}")]
        public ServiceResult Delete(string locationUUID)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            LocationManager locationManager = new LocationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = locationManager.Get(locationUUID);
            if (res.Code != 200)
                return res;
            Location p = (Location)res.Result;

            return locationManager.Delete(p);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Locations/{name}")]
        public ServiceResult Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResponse.Error("You must provide a name for the location.");

            LocationManager locationManager = new LocationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<Location> s = locationManager.Search(name);

            if (s == null || s.Count == 0)
                return ServiceResponse.Error("Location could not be located for the name " + name);

            return ServiceResponse.OK("", s);
        }

        /// <summary>
        /// gets location set in accounts.LocationUUID
        /// </summary>
        /// <param name="accountUUID"></param>
        /// <returns></returns>
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Account/{accountUUID}/Location")]
        public ServiceResult GetAccountLocation(string accountUUID)
        {
            if (string.IsNullOrWhiteSpace(accountUUID))
                return ServiceResponse.Error("No account id sent.");

            AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = accountManager.Get(accountUUID);
            if (res.Code != 200)
                return ServiceResponse.Error(res.Message);

            var account = (Account)res.Result;
            if (string.IsNullOrWhiteSpace(account.LocationUUID))
                return ServiceResponse.Error("No location is set for this account.");

            LocationManager locationManager = new LocationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            res = locationManager.Get(account.LocationUUID);

            if (res.Code != 200)
                return ServiceResponse.Error(res.Message);

            return ServiceResponse.OK("", (Location)res.Result);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Locations/Account/{accountUUID}")]
        public ServiceResult GetAccountLocations(string accountUUID)
        {
            LocationManager locationManager = new LocationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<dynamic> Geo = (List<dynamic>)locationManager.GetLocations(accountUUID).Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            Geo = Geo.Filter(ref filter);

            return ServiceResponse.OK("", Geo, filter.TotalRecordCount);
        }

        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [System.Web.Http.AllowAnonymous]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Locations/InArea/lat/{latitude}/lon/{longitude}/range/{range}")]
        public ServiceResult GetAreaData(double latitude, double longitude, double range)
        {
            if (range > 25)
                range = 25;

            LocationManager locationManager = new LocationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            GeoCoordinate geo = locationManager.GetLocationsIn(latitude, longitude, range);

            //int count;
            // DataFilter filter = this.GetFilter(Request);
            // geo = geo.Filter(ref filter);

            return ServiceResponse.OK("", geo);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/LocationsBy/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("You must provide an id for the location.");

            LocationManager locationManager = new LocationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return locationManager.Get(uuid);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/ChildLocations/{parentUUID}")]
        public ServiceResult GetChildLocations(string parentUUID)
        {
            LocationManager locationManager = new LocationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<dynamic> Geo = (List<dynamic>)locationManager.GetAll()?.Where(w => w.UUParentID == parentUUID
                                                                                    && w.LocationType != "coordinate"
                                                                                  && (w.AccountUUID == CurrentUser.AccountUUID ||
                                                                                        w.AccountUUID.EqualsIgnoreCase(SystemFlag.Default.Account)
                                                                                  )).Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            Geo = Geo.Filter(ref filter);
            return ServiceResponse.OK("", Geo, filter.TotalRecordCount);
        }

        // [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Locations/Current")]
        public ServiceResult GetCurrentLocation()
        {
            NetworkHelper network = new NetworkHelper();
            string ip = "70.175.111.49";//network.GetClientIpAddress(this.Request);// //"2404:6800:4001:805::1006";
            UInt64 ipNum;
            NetworkHelper.TryConvertIP(ip, out ipNum);
            if (ipNum < 0)
                return ServiceResponse.Error("Unable to get location.");

            LocationManager locationManager = new LocationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            float version = NetworkHelper.GetIpVersion(ip);
            Location s = locationManager.Search(ipNum, version);

            if (s == null)
                return ServiceResponse.Error("Unable to get location.");

            return ServiceResponse.OK("", s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Locations/Custom")]
        public ServiceResult GetCustomLocations()
        {
            LocationManager locationManager = new LocationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<dynamic> Geo = (List<dynamic>)locationManager.GetAll()?.Where(w => w.LocationType?.ToUpper() != "COUNTRY" && w.LocationType?.ToUpper() != "STATE" && w.LocationType?.ToUpper() != "CITY" && w.LocationType?.ToUpper() != "REGION").Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            Geo = Geo.Filter(ref filter);
            return ServiceResponse.OK("", Geo, filter.TotalRecordCount);
        }

        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [System.Web.Http.AllowAnonymous]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Locations/{locationUUID}/Types/{locationType}")]
        public ServiceResult GetLocation(string locationUUID, string locationType)
        {
            if (string.IsNullOrWhiteSpace(locationUUID))
                return ServiceResponse.Error("Invalid location UUID");

            if (!string.IsNullOrWhiteSpace(locationType))
                locationType = locationType.ToUpper();

            switch (locationType)
            {
                case "EVENTLOCATION":
                    EventManager eventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                    var tmp1 = eventManager.GetEventLocation(locationUUID);
                    if (tmp1.Code != 200)
                        return tmp1;

                    return ServiceResponse.OK("", (Location)tmp1.Result);

                default:
                    LocationManager locationManager = new LocationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                    var tmp = locationManager.Get(locationUUID);
                    if (tmp.Code != 200)
                        return tmp;
                    return ServiceResponse.OK("", (Location)tmp.Result);
            }
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Locations")]
        public ServiceResult GetLocations()
        {
            LocationManager locationManager = new LocationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<dynamic> Geo = (List<dynamic>)locationManager.GetAll().Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            Geo = Geo.Filter(ref filter);

            return ServiceResponse.OK("", Geo, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Locations/LocationTypes")]
        public ServiceResult GetLocationTypes()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            LocationManager locationManager = new LocationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<string> geoTypes = locationManager.GetLocationTypes(CurrentUser.AccountUUID);
            return ServiceResponse.OK("", geoTypes, geoTypes.Count);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Locations/LocationType/{geoType}")]
        public ServiceResult GetLocatonsByLocationType(string geoType)
        {
            if (string.IsNullOrWhiteSpace(geoType))
                return ServiceResponse.Error("You must pass in a geo type.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            LocationManager locationManager = new LocationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<dynamic> locations = (List<dynamic>)locationManager.GetAll()
                                                                    .Where(pw => (pw.AccountUUID == CurrentUser.AccountUUID || pw.AccountUUID == SystemFlag.Default.Account)
                                                                                    && (pw.LocationType?.EqualsIgnoreCase(geoType) ?? false)
                                                                                    && pw.Deleted == false
                                                                                    && string.IsNullOrWhiteSpace(pw.LocationType) == false)
                                                                                    .Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            locations = locations.Filter(ref filter);

            if (locations == null || locations.Count == 0)
                return ServiceResponse.Error("No locations available.");

            if (filter?.PrependTop > 0)
            {
                var topLocations = locations.Where(w => w.SortOrder > 0).OrderByDescending(o => o.SortOrder).Take(filter.PrependTop);
                if (topLocations.Count() > 0)
                    locations.InsertRange(0, topLocations);
            }

            return ServiceResponse.OK("", locations, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Locations/Add")]
        [System.Web.Http.Route("api/Locations/Insert")]
        public ServiceResult Insert(Location n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid location posted to server.");

            string authToken = this.GetAuthToken(Request);
            SessionManager sessionManager = new SessionManager(Globals.DBConnectionKey);

            UserSession us = sessionManager.GetSession(authToken);
            if (us == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(us.UserData))
                return ServiceResponse.Error("Couldn't retrieve user data.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            n.UUID = Guid.NewGuid().ToString("N");
            n.CreatedBy = CurrentUser.UUID;
            n.AccountUUID = CurrentUser.AccountUUID;
            n.DateCreated = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(n.UUIDType) || n.UUIDType == "Location")
            {
                LocationManager locationManager = new LocationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                return locationManager.Insert(n);
            }
            else
            {
                var el = new EventLocation(n);
                EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                return EventManager.Save(el);
            }
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Locations/Save")]
        public ServiceResult Save()
        {
            try
            {
                string body = Request.Content.ReadAsStringAsync().Result;
                //  if (content == null)
                //      return ServiceResponse.Error("No permissions were sent.");

                // string body = content.Result;

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("No permissions were sent.");

                Location geo = JsonConvert.DeserializeObject<Location>(body);

                if (geo == null)
                    return ServiceResponse.Error("Invalid location posted to server.");

                string authToken = this.GetAuthToken(Request);
                SessionManager sessionManager = new SessionManager(Globals.DBConnectionKey);

                UserSession us = sessionManager.GetSession(authToken);
                if (us == null)
                    return ServiceResponse.Error("You must be logged in to access this function.");

                if (string.IsNullOrWhiteSpace(us.UserData))
                    return ServiceResponse.Error("Couldn't retrieve user data.");

                if (CurrentUser == null)
                    return ServiceResponse.Error("You must be logged in to access this function.");

                if (string.IsNullOrWhiteSpace(geo.CreatedBy))
                {
                    geo.CreatedBy = CurrentUser.UUID;
                    geo.AccountUUID = CurrentUser.AccountUUID;
                    geo.DateCreated = DateTime.UtcNow;
                }

                geo.Country = geo.Country?.Trim();
                geo.Postal = geo.Postal?.Trim();
                geo.State = geo.State?.Trim();
                geo.City = geo.City?.Trim();
                geo.Name = geo.Name?.Trim();

                LocationManager locationManager = new LocationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

                return locationManager.Save(geo);
            }
            catch (Exception ex)
            {
                return ServiceResponse.Error("Failed to save location.");
            }
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Locations/Account/Save")]
        public ServiceResult SaveAccountLocation()
        {
            if (CurrentUser == null)
            {
                string authToken = this.GetAuthToken(Request);
                SessionManager sessionManager = new SessionManager(Globals.DBConnectionKey);

                UserSession us = sessionManager.GetSession(authToken);
                if (us == null)
                    return ServiceResponse.Error("You must be logged in to access this function.");

                if (string.IsNullOrWhiteSpace(us.UserData))
                    return ServiceResponse.Error("Couldn't retrieve posted data.");
            }

            #region event save location

            try
            {
                string body = Request.Content.ReadAsStringAsync().Result;

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("No locations was sent.");

                var clientLocation = JsonConvert.DeserializeObject<Location>(body);

                if (clientLocation == null)
                    return ServiceResponse.Error("Invalid location posted to server.");

                if (!string.IsNullOrWhiteSpace(clientLocation.Name))
                    clientLocation.Name = clientLocation.Name.Trim();

                LocationManager LocationManager = new LocationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

                var dbEventLocationRes = LocationManager.Get(clientLocation.UUID, clientLocation.LocationType);

                if (dbEventLocationRes.Code != 200 || dbEventLocationRes.Result == null)
                {   // this event location doesn't exist so create it.
                    clientLocation.CreatedBy = CurrentUser.UUID;
                    clientLocation.AccountUUID = CurrentUser.AccountUUID;
                    clientLocation.DateCreated = DateTime.UtcNow;

                    clientLocation.UUID = Guid.NewGuid().ToString("N");

                    //if (string.IsNullOrWhiteSpace(clientLocation.Email) && clientLocation.CreatedBy == CurrentUser.UUID)
                    //    clientLocation.Email = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), CurrentUser.Email.ToLower(), true);

                    return LocationManager.Insert(clientLocation);
                }

                var dbLocation = (Location)dbEventLocationRes.Result;

                clientLocation.CreatedBy = dbLocation.CreatedBy;
                clientLocation.DateCreated = dbLocation.DateCreated;

                return LocationManager.Update(clientLocation);
            }
            catch (Exception ex)
            {
                return ServiceResponse.Error("Failed to save event location.");
            }

            #endregion event save location

            //        geo.CreatedBy = CurrentUser.UUID;
            //        geo.AccountUUID = CurrentUser.AccountUUID;
            //        geo.DateCreated = DateTime.UtcNow;
            //    geo.Country = geo.Country?.Trim();
            //    geo.Postal      = geo.Postal?.Trim();
            //    geo.State   = geo.State?.Trim();
            //    geo.City     = geo.City?.Trim();
            //    geo.Address1 = geo.Address1?.Trim();
            //    geo.Address2  = geo.Address2?.Trim();
            //    geo.Name    = geo.Name?.Trim();
            //    dbGeo.Name = geo.Name;
            //    dbGeo.Country  = geo.Country;
            //    dbGeo.Postal  = geo.Postal;
            //    dbGeo.State = geo.State;
            //    dbGeo.City = geo.City;
            //    dbGeo.Longitude = geo.Longitude;
            //    dbGeo.Latitude = geo.Latitude;
            //    dbGeo.isDefault = geo.isDefault;
            //    dbGeo.Description = geo.Description;
            //    dbGeo.Category = geo.Category;
            //    dbGeo.Address1 = geo.Address1?.Trim();
            //    dbGeo.Address2 = geo.Address2?.Trim();
            //    dbGeo.TimeZone = geo.TimeZone;
            //    return locationManager.Update(dbGeo);
        }

        //
        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [System.Web.Http.AllowAnonymous]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Locations/search/Types/{uuidType}/searchText/{searchTerm}")]
        public ServiceResult SearchLocation(string uuidType, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return ServiceResponse.Error("Invalid search text");

            LocationManager locationManager = new LocationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = locationManager.SearchByType(searchTerm, uuidType, false);

            return ServiceResponse.OK("", res);
        }

        //
        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [System.Web.Http.AllowAnonymous]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Locations/search/Types/{uuidType}/searchText/{searchTerm}/all")]
        public ServiceResult SearchLocationReturnAll(string uuidType, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return ServiceResponse.Error("Invalid search text");

            LocationManager locationManager = new LocationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = locationManager.SearchByType(searchTerm, uuidType, true);

            return ServiceResponse.OK("", res);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpPatch]
        [System.Web.Http.Route("api/Locations/Update")]
        public ServiceResult Update(Location pv)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (pv == null)
                return ServiceResponse.Error("Invalid location sent to server.");

            LocationManager locationManager = new LocationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var dbP = locationManager.GetAll()?.FirstOrDefault(pw => pw.UUID == pv.UUID);

            if (dbP == null)
                return ServiceResponse.Error("Location was not found.");

            dbP.Name = pv.Name;
            dbP.Address1 = pv.Address1;
            dbP.Address2 = pv.Address2;
            dbP.City = pv.City;
            dbP.State = pv.State;
            dbP.Postal = pv.Postal;
            dbP.LocationType = pv.LocationType;
            dbP.Latitude = pv.Latitude;
            dbP.Longitude = pv.Longitude;
            dbP.Virtual = pv.Virtual;
            dbP.Active = pv.Active;
            dbP.Category = pv.Category;
            dbP.Description = pv.Description;
            dbP.Image = pv.Image;
            dbP.LocationType = pv.LocationType;
            dbP.Private = pv.Private;
            dbP.SortOrder = pv.SortOrder;
            dbP.TimeZone = pv.TimeZone;

            dbP.isDefault = pv.isDefault;//todo update other locations with same LocationType, account, default = false. only one default per type.
            return locationManager.Update(dbP);
        }
    }
}