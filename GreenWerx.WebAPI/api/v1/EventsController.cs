// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GreenWerx.Data.Logging;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Managers;
using GreenWerx.Managers.Events;
using GreenWerx.Managers.General;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Events;
using GreenWerx.Models.Flags;
using GreenWerx.Models.General;
using GreenWerx.Models.Membership;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Utilites.Helpers;
using GreenWerx.Utilites.Security;
using GreenWerx.Web.Filters;
using WebApi.OutputCache.V2;
using WebApiThrottle;

namespace GreenWerx.Web.api.v1
{
    public class EventsController : ApiBaseController
    {
        private readonly SystemLogger _fileLogger = new SystemLogger(null, true);

        public EventsController()
        {
            _fileLogger.InsertInfo("EventsController", "EventsController", "EventsController()");
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 0)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Events/{eventUUID}/Favorite")]
        public ServiceResult AddEventToFavorites(string eventUUID)
        {
            if (string.IsNullOrWhiteSpace(eventUUID))
                return ServiceResponse.Error("No event id sent.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var temp = EventManager.Get(eventUUID);
            if (temp.Code != 200)
                return temp;

            var e = (Event)temp.Result;

            Favorite r = new Favorite()
            {
                RoleOperation = e.RoleOperation,
                RoleWeight = e.RoleWeight,
                Private = true,
                Name = e.Name,
                UUIDType = e.UUIDType,
                AccountUUID = CurrentUser.AccountUUID,
                CreatedBy = CurrentUser.UUID,
                DateCreated = DateTime.UtcNow,
                Active = true,
                Deleted = false,
            };

            ReminderManager reminderManager = new ReminderManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return reminderManager.Insert(r);
        }

        [System.Web.Http.AllowAnonymous]
        // [EnableThrottling(PerSecond = 1)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Events/Location/Add")]
        public ServiceResult AddLocation()//EventLocation eventLocation)
        {
            try
            {
                string body = Request.Content.ReadAsStringAsync().Result;
                //  if (content == null)
                //      return ServiceResponse.Error("No location was sent.");

                // string body = content.Result;

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("No location was sent.");

                EventLocation eventLocation = JsonConvert.DeserializeObject<EventLocation>(body);

                if (eventLocation == null)
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

                if (string.IsNullOrWhiteSpace(eventLocation.CreatedBy))
                {
                    eventLocation.CreatedBy = CurrentUser.UUID;
                    eventLocation.AccountUUID = CurrentUser.AccountUUID;
                    eventLocation.DateCreated = DateTime.UtcNow;
                }

                if (string.IsNullOrWhiteSpace(eventLocation.Email) && eventLocation.CreatedBy == CurrentUser.UUID)
                    eventLocation.Email = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), CurrentUser.Email.ToLower(), true);

                EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                return EventManager.InsertEventLocation(eventLocation);
            }
            catch (Exception ex)
            {
                return ServiceResponse.Error("Failed to save event location.");
            }
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Events/{eventUUID}/Users/Add")]
        public async Task<ServiceResult> AddUsersToEvent(string eventUUID)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");
            List<EventMember> members = new List<EventMember>();
            ServiceResult res;
            try
            {
                string content = await Request.Content.ReadAsStringAsync();
                if (content == null)
                    return ServiceResponse.Error("No users were sent.");

                string body = content;

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("No users were sent.");

                List<User> users = JsonConvert.DeserializeObject<List<User>>(body);

                EventManager eventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                var res2 = eventManager.Get(eventUUID);
                var evt = res2.Result as Event;
                foreach (var user in users)
                {
                    var member = new EventMember
                    {
                        AccountUUID = CurrentUser.AccountUUID,
                        Active = true,
                        CreatedBy = CurrentUser.UUID,
                        RoleOperation = "=",
                        UserUUID = user.UUID,
                        Image = user.Image,
                        Name = user.Name,
                        EventUUID = eventUUID,
                        Private = evt.Private,
                        DateCreated = DateTime.UtcNow,
                        NSFW = evt.NSFW,
                        RoleWeight = RoleFlags.MemberRoleWeights.Member
                    };

                    eventManager.InsertEventMember(member);

                    members.Add(member);
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error(ex.Message);
            }
            return ServiceResponse.OK("", members);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Events/Delete/{eventUUID}")]
        public ServiceResult Delete(string eventUUID)
        {
            if (string.IsNullOrWhiteSpace(eventUUID))
                return ServiceResponse.Error("Invalid event was sent.");

            EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            var n = EventManager.Get(eventUUID);
            if (n.Code != 200)
                return n;

            DataFilter filter = this.GetFilter(Request);

            var e = n.Result as Event;
            var res = EventManager.Delete(e);
            if (res.Code > 200)
                return res;

            ReminderManager reminderManager = new ReminderManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            reminderManager.DeleteForEvent(e.UUID, "The event has been deleted.", false);

            return res;
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 0)]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Events/{eventUUID}/Favorite")]
        public ServiceResult DeleteEventFromFavorites(string eventUUID)
        {
            if (string.IsNullOrWhiteSpace(eventUUID))
                return ServiceResponse.Error("No id sent.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            FavoritesManager reminderManager = new FavoritesManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var r = reminderManager.GetByEvent(eventUUID);
            if (r == null)
                return ServiceResponse.Error("Record does not exist for id.");
            return reminderManager.Delete(r, true);
        }

        //[CacheOutput(ClientTimeSpan = 100, ServerTimeSpan = 100)]
        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [System.Web.Http.AllowAnonymous]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Events/{name}")]
        public ServiceResult Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResponse.Error("You must provide a name for the Event.");

            EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<Event> s = EventManager.Search(name);

            if (s == null || s.Count == 0)
                return ServiceResponse.Error("Event could not be located for the name " + name);

            return ServiceResponse.OK($"Found {s.Count} events for {name}", s);
        }

        /// <summary>
        /// gets locations the "host" used in the past.
        /// </summary>
        /// <param name="eventLocationUUID"></param>
        /// <returns></returns>
        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Events/Locations/Account")]
        public ServiceResult GetAccountEventLocations()
        {
            EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var locations = EventManager.GetAccountEventLocations(CurrentUser?.AccountUUID);
            return ServiceResponse.OK("", locations);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Events/{eventUUID}/Locations/Account")]
        public ServiceResult GetAccountEventLocations(string eventUUID)
        {
            EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var locations = EventManager.GetAccountEventLocations(CurrentUser?.AccountUUID, eventUUID);
            return ServiceResponse.OK("", locations);
        }


        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Events/Locations/Search/{name}/{partialMatch}")]
        public ServiceResult SearchEventLocation(string name, bool partialMatch)
        {
            EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return EventManager.SearchEventLocation(name, partialMatch);
        }

        //  [CacheOutput(ClientTimeSpan = 100, ServerTimeSpan = 100)]
        [System.Web.Http.AllowAnonymous]
        // [EnableThrottling(PerSecond = 3)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Events/Hosts/{accountUUID}")]
        public ServiceResult GetAccountEvents(string accountUUID)
        {
            if (string.IsNullOrWhiteSpace(accountUUID))
                return ServiceResponse.Error("You must send an account uuid.");

            DataFilter filter = this.GetFilter(Request);

            int count;
            EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<dynamic> Events;

            TimeZoneInfo tzInfo = null;

            try
            {
                if (string.IsNullOrWhiteSpace(filter.TimeZone))
                {
                    var defaultTimeZone = TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(w => w.BaseUtcOffset.TotalHours < -9 && w.BaseUtcOffset.TotalHours > -12);
                    tzInfo = TimeZoneInfo.CreateCustomTimeZone(defaultTimeZone.StandardName, new TimeSpan(Convert.ToInt32(defaultTimeZone.BaseUtcOffset.TotalHours), 0, 0),
                        defaultTimeZone.StandardName, defaultTimeZone.StandardName);
                }
                else
                {
                    float offSet = this.GetTimezoneOffset(filter.TimeZone);
                    tzInfo = TimeZoneInfo.CreateCustomTimeZone(filter.TimeZone, new TimeSpan(Convert.ToInt32(offSet), 0, 0), filter.TimeZone, filter.TimeZone);
                }
            }
            catch
            {
                // tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            }

            DateTime adjustedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.Date, tzInfo);
            Events = EventManager.GetHostEvents(accountUUID, filter.IncludeDeleted, filter.IncludePrivate)?.Where(w => w.StartDate >= adjustedDate.Date)
                                    .Cast<dynamic>().ToList();

            Events = Events.Filter(ref filter);
            return ServiceResponse.OK("", Events, filter.TotalRecordCount);
        }

        //  [CacheOutput(ClientTimeSpan = 100, ServerTimeSpan = 100)]
        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [System.Web.Http.AllowAnonymous]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/EventBy/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("You must provide a name for the Event.");

            EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = EventManager.Get(uuid);
            if (res.Code != 200)
                return res;
            Event s = (Event)res.Result;

            //s.EventLocationUUID = EventManager.GetEventLocations(s.UUID)?.FirstOrDefault()?.UUID;
            return ServiceResponse.OK("", s);
        }


        [System.Web.Http.AllowAnonymous]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/EventByGUUID/{guuid}")]
        public ServiceResult GetByGUUID(string guuid)
        {
            if (string.IsNullOrWhiteSpace(guuid))
                return ServiceResponse.Error("You must provide a guuid for the Event.");

            EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = EventManager.GetByGUUID(guuid);
            if (res.Code != 200)
                return res;
            Event s = (Event)res.Result;

            //s.EventLocationUUID = EventManager.GetEventLocations(s.UUID)?.FirstOrDefault()?.UUID;
            return ServiceResponse.OK("", s);
        }



        //    [CacheOutput(ClientTimeSpan = 100, ServerTimeSpan = 100)]
        [System.Web.Http.AllowAnonymous]
        // [EnableThrottling(PerSecond = 3)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Events/Categories")]
        public ServiceResult GetEventCategories()
        {
            EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<string> categories = EventManager.GetEventCategories();
            string pathToFile = Path.Combine(EnvironmentEx.AppDataFolder.Replace("\\\\", "\\"), "WordLists\\categories.events.csv");
            if (File.Exists(pathToFile))
            {
                string csvCats = File.ReadAllText(pathToFile);
                categories.AddRange(csvCats.Split(','));
                categories = categories.GroupBy(x => x?.ToUpper()).Select(group => group.First()).ToList();
            }
            return ServiceResponse.OK("", categories, categories?.Count ?? 0);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 0)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Events/{eventUUID}/Members")]
        public ServiceResult GetEventMembers(string eventUUID)
        {
            if (string.IsNullOrWhiteSpace(eventUUID))
                return ServiceResponse.Error("No event id sent.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            var res = EventManager.GetEventMembers(eventUUID, CurrentUser.AccountUUID);
            return ServiceResponse.OK("", res);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 0)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Events/{eventUUID}/NonMembers")]
        public ServiceResult GetEventNonMembers(string eventUUID)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<dynamic> accountMembers = EventManager.GetEventNonMembers(eventUUID, CurrentUser.AccountUUID)?.Cast<dynamic>()?.ToList();
            int count = accountMembers.Count;
            //  DataFilter filter = this.GetFilter(Request);
            //  accountMembers = accountMembers.Filter(ref filter);
            return ServiceResponse.OK("", accountMembers, count);
        }

        // [CacheOutput(ClientTimeSpan = 100, ServerTimeSpan = 100)]
        [System.Web.Http.AllowAnonymous]
        // [EnableThrottling(PerSecond = 3)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Events")]
        public ServiceResult GetEvents()
        {
            // _fileLogger.InsertInfo("EventsController", "EventsController", "GetEvents()");

            DataFilter filter = this.GetFilter(Request);

            int count;
            string devaultEventUUID = Globals.Application.AppSetting("DefaultEvent");
            EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            //   List<dynamic> Events;

            TimeZoneInfo tzInfo = null;

            try
            {
                if (string.IsNullOrWhiteSpace(filter.TimeZone))
                {
                    var defaultTimeZone = TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(w => w.BaseUtcOffset.TotalHours < -9 && w.BaseUtcOffset.TotalHours > -12);
                    tzInfo = TimeZoneInfo.CreateCustomTimeZone(defaultTimeZone.StandardName, new TimeSpan(Convert.ToInt32(defaultTimeZone.BaseUtcOffset.TotalHours), 0, 0),
                        defaultTimeZone.StandardName, defaultTimeZone.StandardName);
                }
                else
                {
                    float offSet = this.GetTimezoneOffset(filter.TimeZone);
                    tzInfo = TimeZoneInfo.CreateCustomTimeZone(filter.TimeZone, new TimeSpan(Convert.ToInt32(offSet), 0, 0), filter.TimeZone, filter.TimeZone);
                }
            }
            catch
            {
                // tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            }

            DateTime adjustedDate = DateTime.Now;

            if (tzInfo != null)
                adjustedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.Date, tzInfo);

            var Events = EventManager.GetSubEvents(devaultEventUUID, adjustedDate.Date, ref filter);

            return ServiceResponse.OK("", Events, filter.TotalRecordCount); // todo the count needs to be fixed
        }

        // [EnableThrottling(PerSecond = 3)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Events/Favorites")]
        public ServiceResult GetFavoriteEvents()
        {
            DataFilter filter = this.GetFilter(Request);

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to view favorites.");

            EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<dynamic> Events = EventManager.GetFavoriteEvents(CurrentUser.UUID, CurrentUser.AccountUUID);
            Events = Events.Filter(ref filter);
            return ServiceResponse.OK("", Events, filter.TotalRecordCount);
        }

        //    [CacheOutput(ClientTimeSpan = 100, ServerTimeSpan = 100)]
        [System.Web.Http.AllowAnonymous]
        // [EnableThrottling(PerSecond = 3)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Events/Locations/{eventLocationUUID}")]
        public ServiceResult GetLocation(string eventLocationUUID)
        {
            EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return EventManager.GetEventLocation(eventLocationUUID);
        }

        //    [CacheOutput(ClientTimeSpan = 100, ServerTimeSpan = 100)]
        [System.Web.Http.AllowAnonymous]
        // [EnableThrottling(PerSecond = 3)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Events/{eventUUID}/Location")]
        public ServiceResult GetLocationByEventUUID(string eventUUID)
        {
            EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return EventManager.GetEventLocationByEventUUID(eventUUID);
        }

        [System.Web.Http.AllowAnonymous]
        // [EnableThrottling(PerSecond = 3)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Events/Locations/guuid/{guuid}")]
        public ServiceResult GetLocationByGUUID(string guuid)
        {
            EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return EventManager.GetEventLocationByGUUID(guuid);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Events/Add")]
        [System.Web.Http.Route("api/Events/Insert")]
        public ServiceResult Insert()//Event s)
        {
            try
            {
                string body = Request.Content.ReadAsStringAsync().Result;
                //  if (content == null)
                //      return ServiceResponse.Error("No event was sent.");

                // string body = content.Result;

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("No events were sent.");

                Event s = JsonConvert.DeserializeObject<Event>(body);

                if (s == null)
                    return ServiceResponse.Error("Invalid event posted to server.");

                string authToken = Request.Headers.Authorization?.Parameter;
                SessionManager sessionManager = new SessionManager(Globals.DBConnectionKey);

                UserSession us = sessionManager.GetSession(authToken);
                if (us == null)
                    return ServiceResponse.Error("You must be logged in to access this function.");

                //  if (us.Captcha?.ToUpper() != s.Captcha?.ToUpper())                return ServiceResponse.Error("Invalid code.");

                if (CurrentUser == null)
                    return ServiceResponse.Error("You must be logged in to access this function.");

                if (string.IsNullOrWhiteSpace(s.AccountUUID) || s.AccountUUID == SystemFlag.Default.Account)
                    s.AccountUUID = CurrentUser.AccountUUID;

                if (string.IsNullOrWhiteSpace(s.CreatedBy))
                    s.CreatedBy = CurrentUser.UUID;

                if (s.DateCreated == DateTime.MinValue)
                    s.DateCreated = DateTime.UtcNow;

                if (string.IsNullOrWhiteSpace(s.HostAccountUUID))
                    s.HostAccountUUID = CurrentUser.AccountUUID;

                s.Active = true;
                s.Deleted = false;
                if (s.EventDateTime == DateTime.MinValue)
                {
                    if (s.StartDate == DateTime.MinValue)
                        return ServiceResponse.Error("You must provide a valid event start date.");
                    else
                        s.EventDateTime = s.StartDate;
                }

                //if (string.IsNullOrWhiteSpace(s.Frequency)) return ServiceResponse.Error("You must provide a valid frequency.");

                //if (s.RepeatForever == false && s.RepeatCount <= 0) return ServiceResponse.Error("You must set  repeat count or repeat forver.");

                EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

                if (s.StartDate == DateTime.MinValue)
                    s.StartDate = DateTime.Now.AddYears(5);

                if (s.EndDate == DateTime.MinValue)
                    s.EndDate = DateTime.Now.AddYears(5); //will put at end of list so we can work with it easier.

                //if (s.Name.EqualsIgnoreCase("New Years Kiss at Club Joi"))
                //    Debug.Assert(false, "error inserting this event");
                ServiceResult sr = EventManager.Insert(s);
                if (sr.Code != 200)
                    return sr;

                return sr;
            }
            catch (Exception ex)
            {
                return ServiceResponse.Error(ex.Message);
            }
        }


        //From the admin events, the publish button sets the private = false and clears the status and reference fields
        //
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Events/{eventUUID}/Activate")]
        [System.Web.Http.Route("api/Events/{eventUUID}/Publish")]
        public ServiceResult ActivateEvent(string eventUUID)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (CurrentUser.SiteAdmin == false)
                return ServiceResponse.Error("You are not authorized this functioanity.");

            ServiceResult res = new ServiceResult();
            try
            {
                if (string.IsNullOrEmpty(eventUUID))
                    return ServiceResponse.Error("No event id was sent.");

                EventManager em = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                res=   em.Get(eventUUID);
                if (res.Code != 200)
                    return ServiceResponse.Error(res.Message);

                var evnt = res.Result as Event;

                evnt.Status = "";
                evnt.Reference = "";
                evnt.Active = true;
                return  em.Update(evnt);

            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }

            return res;
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Events/{eventUUID}/Users/Remove")]
        public ServiceResult RemoveUsersFromEvent(string eventUUID)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            ServiceResult res = new ServiceResult();
            try
            {
                Task<string> content = Request.Content.ReadAsStringAsync();
                if (content == null)
                    return ServiceResponse.Error("No users were sent.");

                string body = content.Result;

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("No users were sent.");

                List<User> users = JsonConvert.DeserializeObject<List<User>>(body);
                List<AccountMember> members = new List<AccountMember>();

                EventManager eventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                foreach (var user in users)
                {
                    // todo get eventmember by userid, currentuser.accountid, and eventid
                    var evtm = eventManager.GetEventMember(eventUUID, CurrentUser.AccountUUID).FirstOrDefault(w => w.UserUUID == user.UUID);
                    if (evtm == null)
                        continue;

                    eventManager.DeleteEventMember(evtm);
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }

            return res;
        }

        #region EventLocation 
        /// <summary>
        ///
        /// </summary>
        /// <param name="matchEvent">
        /// Because we can create multiple records for the same event location the
        /// previous locations combo doesn't always pull the matching event.
        /// So try and match  the event location to the even</param>
        /// <returns></returns>
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 0)]
        // [EnableThrottling(PerSecond = 1)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Events/Location/Insert")]
        [System.Web.Http.Route("api/Events/Location/Add")]  
        public ServiceResult AddEventLocation() 
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

            try
            {
                string body = Request.Content.ReadAsStringAsync().Result;

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("No location was sent.");

                EventLocation clientEventLocation = JsonConvert.DeserializeObject<EventLocation>(body);

                if (clientEventLocation == null)
                    return ServiceResponse.Error("Invalid location posted to server.");

                if (string.IsNullOrWhiteSpace(clientEventLocation.EventUUID))
                    return ServiceResponse.Error("You must assign an event to this location.");

                EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

                var resEvent = EventManager.Get(clientEventLocation.EventUUID);
                if (resEvent.Code != 200 || resEvent.Result == null)
                    return ServiceResponse.Error("Event was not found for this location.");

                if (!string.IsNullOrWhiteSpace(clientEventLocation.Name))
                    clientEventLocation.Name = clientEventLocation.Name.Trim();
            

                //var dbEventLocationRes = EventManager.GetEventLocation(clientEventLocation.UUID, clientEventLocation.EventUUID);

                //if (dbEventLocationRes.Code == 200)
                //    return ServiceResponse.Error("Event location already exists.");

                // this event location doesn't exist so create it.
                clientEventLocation.CreatedBy = CurrentUser.UUID;
                clientEventLocation.UUID = Guid.NewGuid().ToString("N");
                clientEventLocation.AccountUUID = CurrentUser.AccountUUID;
                clientEventLocation.DateCreated = DateTime.UtcNow;

                if (string.IsNullOrWhiteSpace(clientEventLocation.Email) && clientEventLocation.CreatedBy == CurrentUser.UUID)
                    clientEventLocation.Email = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), CurrentUser.Email.ToLower(), true);

                return EventManager.InsertEventLocation(clientEventLocation);
            }
            catch (Exception ex)
            {
                return ServiceResponse.Error("Failed to save event location.");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="matchEvent">
        /// Because we can create multiple records for the same event location the
        /// previous locations combo doesn't always pull the matching event.
        /// So try and match  the event location to the even</param>
        /// <returns></returns>
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 0)]
        // [EnableThrottling(PerSecond = 1)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Events/Location/Update")] ///{matchEvent
        public ServiceResult UpdateEventLocation()//bool matchEvent)
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

            try
            {
                string body = Request.Content.ReadAsStringAsync().Result;

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("No location was sent.");

                EventLocation clientEventLocation = JsonConvert.DeserializeObject<EventLocation>(body);

                if (clientEventLocation == null)
                    return ServiceResponse.Error("Invalid location posted to server.");

                if (string.IsNullOrWhiteSpace(clientEventLocation.EventUUID))
                    return ServiceResponse.Error("You must assign an event to this location.");

                EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

                var resEvent = EventManager.Get(clientEventLocation.EventUUID);
                if (resEvent.Code != 200 || resEvent.Result == null)
                    return ServiceResponse.Error("Event was not found for this location.");

                if (!string.IsNullOrWhiteSpace(clientEventLocation.Name))
                    clientEventLocation.Name = clientEventLocation.Name.Trim();

                var dbEventLocationRes = EventManager.GetEventLocation(clientEventLocation.UUID, clientEventLocation.EventUUID);

                if (dbEventLocationRes.Code == 200)
                {
                    clientEventLocation.DateCreated = DateTime.UtcNow;// so we get the most recent location
                    return EventManager.UpdateEventLocation(clientEventLocation);
                }

                // this event location doesn't exist so create it.
                clientEventLocation.CreatedBy = CurrentUser.UUID;
                clientEventLocation.AccountUUID = CurrentUser.AccountUUID;
                clientEventLocation.DateCreated = DateTime.UtcNow;

                if (string.IsNullOrWhiteSpace(clientEventLocation.Email) && clientEventLocation.CreatedBy == CurrentUser.UUID)
                    clientEventLocation.Email = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), CurrentUser.Email.ToLower(), true);

                return EventManager.InsertEventLocation(clientEventLocation);
            }
            catch (Exception ex)
            {
                return ServiceResponse.Error("Failed to save event location.");
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="matchEvent">
        /// Because we can create multiple records for the same event location the
        /// previous locations combo doesn't always pull the matching event.
        /// So try and match  the event location to the even</param>
        /// <returns></returns>
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 0)]
        // [EnableThrottling(PerSecond = 1)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Events/Location/Save")] ///{matchEvent
        public ServiceResult SaveLocation()//bool matchEvent)
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

            try
            {
                string body = Request.Content.ReadAsStringAsync().Result;

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("No location was sent.");

                EventLocation clientEventLocation = JsonConvert.DeserializeObject<EventLocation>(body);

                if (clientEventLocation == null)
                    return ServiceResponse.Error("Invalid location posted to server.");

                if (string.IsNullOrWhiteSpace(clientEventLocation.EventUUID))
                    return ServiceResponse.Error("You must assign an event to this location.");

                EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

                var resEvent = EventManager.Get(clientEventLocation.EventUUID);
                if (resEvent.Code != 200 || resEvent.Result == null)
                    return ServiceResponse.Error("Event was not found for this location.");

                if (!string.IsNullOrWhiteSpace(clientEventLocation.Name))
                    clientEventLocation.Name = clientEventLocation.Name.Trim();
                // var clientEvent = (Event)resEvent.Result;

                var dbEventLocationRes = EventManager.GetEventLocation(clientEventLocation.UUID, clientEventLocation.EventUUID);

                if (dbEventLocationRes.Code != 200)
                {   // this event location doesn't exist so create it.
                    clientEventLocation.CreatedBy = CurrentUser.UUID;
                    clientEventLocation.AccountUUID = CurrentUser.AccountUUID;
                    clientEventLocation.DateCreated = DateTime.UtcNow;

                    if (string.IsNullOrWhiteSpace(clientEventLocation.Email) && clientEventLocation.CreatedBy == CurrentUser.UUID)
                        clientEventLocation.Email = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), CurrentUser.Email.ToLower(), true);

                    return EventManager.InsertEventLocation(clientEventLocation);
                }
                clientEventLocation.DateCreated = DateTime.UtcNow;// so we get the most recent location
                return EventManager.UpdateEventLocation(clientEventLocation);
            }
            catch (Exception ex)
            {
                return ServiceResponse.Error("Failed to save event location.");
            }
        }
        #endregion
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
        [System.Web.Http.Route("api/Events/Update")]
        public GreenWerx.Models.App.ServiceResult Update(Event s)
        {
            if (s == null)
                return ServiceResponse.Error("Invalid Event sent to server.");

            EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = EventManager.Get(s.UUID);
            if (res.Code != 200)
                return res;
            var dbS = (Event)res.Result;

            if (dbS.DateCreated == DateTime.MinValue)
                dbS.DateCreated = DateTime.UtcNow;

            if (s.StartDate == DateTime.MinValue)
                return ServiceResponse.Error("Start date is not valid.");

            if (s.EndDate == DateTime.MinValue)
                return ServiceResponse.Error("End date is not valid.");

            if (s.EventDateTime == DateTime.MinValue)
                dbS.EventDateTime = s.StartDate;

            dbS.Deleted = s.Deleted;

            dbS.Name = s.Name;
            dbS.Url = s.Url;// todo affilite. if affiliate link and not owner role then deny update or ignore update
            dbS.Status = s.Status;
            dbS.Private = s.Private;
            dbS.Reference = s.Reference;

            dbS.Latitude = s.Latitude;
            dbS.Longitude = s.Longitude;
            dbS.Image = s.Image;
            dbS.SortOrder = s.SortOrder;
            dbS.Active = s.Active;
            dbS.Description = s.Description;
            dbS.EventDateTime = s.EventDateTime;
            dbS.StartDate = s.StartDate;
            dbS.EndDate = s.EndDate;  // save edited event. dates are failing
            dbS.RepeatForever = s.RepeatForever;
            dbS.RepeatCount = s.RepeatCount;
            dbS.Frequency = s.Frequency;
            dbS.Category = s.Category;
            dbS.NSFW = s.NSFW;
            dbS.IsAffiliate = s.IsAffiliate;
            dbS.HostAccountUUID = s.HostAccountUUID;
            return EventManager.Update(dbS);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpPatch]
        [System.Web.Http.Route("api/Events/Updates")]
        public ServiceResult UpdateEvents()
        {
            ServiceResult res = ServiceResponse.OK();
            StringBuilder msg = new StringBuilder();
            try
            {
                Task<string> content = Request.Content.ReadAsStringAsync();
                if (content == null)
                    return ServiceResponse.Error("No event was sent.");

                string body = content.Result;

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("No event was sent.");

                List<Event> changedItems = JsonConvert.DeserializeObject<List<Event>>(body);

                EventManager eventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

                foreach (Event changedItem in changedItems)
                {
                    var res2 = eventManager.Get(changedItem.UUID);
                    if (res2.Code != 200)
                        continue;

                    var databaseItem = (Event)res2.Result;

                    if (string.IsNullOrWhiteSpace(changedItem.CreatedBy))
                        changedItem.CreatedBy = this.CurrentUser.UUID;

                    if (string.IsNullOrWhiteSpace(changedItem.AccountUUID))
                        changedItem.AccountUUID = this.CurrentUser.AccountUUID;

                    if (string.IsNullOrWhiteSpace(changedItem.UUID))
                        changedItem.UUID = Guid.NewGuid().ToString("N");

                    if (databaseItem == null)
                    {
                        changedItem.UUIDType = "Event";
                        changedItem.DateCreated = DateTime.UtcNow;

                        ServiceResult sr = eventManager.Insert(changedItem);
                        if (sr.Code != 200)
                        {
                            res.Code = 500;
                            msg.AppendLine(sr.Message);
                        }
                        continue;
                    }

                    databaseItem.Name = changedItem.Name;
                    databaseItem.Deleted = changedItem.Deleted;
                    databaseItem.Name = changedItem.Name;
                    databaseItem.Status = changedItem.Status;
                    databaseItem.Private = changedItem.Private;
                    databaseItem.SortOrder = changedItem.SortOrder;
                    databaseItem.Active = changedItem.Active;
                    databaseItem.Description = changedItem.Description;
                    databaseItem.EventDateTime = changedItem.EventDateTime;
                    databaseItem.StartDate = changedItem.StartDate;
                    databaseItem.EndDate = changedItem.EndDate;
                    databaseItem.RepeatForever = changedItem.RepeatForever;
                    databaseItem.RepeatCount = changedItem.RepeatCount;
                    databaseItem.Frequency = changedItem.Frequency;
                    databaseItem.Category = changedItem.Category;
                    databaseItem.NSFW = changedItem.NSFW;
                    databaseItem.IsAffiliate = changedItem.IsAffiliate;
                    databaseItem.HostAccountUUID = changedItem.HostAccountUUID;
                    databaseItem.TakeOver = changedItem.TakeOver;
                    databaseItem.Virtual = changedItem.Virtual;

                    if (CurrentUser.SiteAdmin)
                    {
                        databaseItem.RoleOperation = changedItem.RoleOperation;
                        databaseItem.RoleWeight = changedItem.RoleWeight;
                    }
                    ServiceResult sru = eventManager.Update(changedItem);
                    if (sru.Code != 200)
                    {
                        res.Code = 500;
                        msg.AppendLine(sru.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                msg.AppendLine(ex.Message);
                Debug.Assert(false, ex.Message);
            }
            res.Message = msg.ToString();
            return res;
        }
        /*
        [System.Web.Http.AllowAnonymous]
        // [EnableThrottling(PerSecond = 1)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Events/Location/Update")]
        public ServiceResult UpdateLocation()//EventLocation eventLocation)
        {
            try
            {
                string body = Request.Content.ReadAsStringAsync().Result;
                //  if (content == null)
                //      return ServiceResponse.Error("No location was sent.");

                // string body = content.Result;

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("No location was sent.");

                EventLocation eventLocation = JsonConvert.DeserializeObject<EventLocation>(body);

                if (eventLocation == null)
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

                if (string.IsNullOrWhiteSpace(eventLocation.CreatedBy))
                {
                    eventLocation.CreatedBy = CurrentUser.UUID;
                    eventLocation.AccountUUID = CurrentUser.AccountUUID;
                    eventLocation.DateCreated = DateTime.UtcNow;
                }

                if (string.IsNullOrWhiteSpace(eventLocation.Email) && eventLocation.CreatedBy == CurrentUser.UUID)
                    eventLocation.Email = Cipher.Crypt(Globals.Application.AppSetting("AppKey"), CurrentUser.Email.ToLower(), true);

                EventManager EventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                return EventManager.UpdateEventLocation(eventLocation);
            }
            catch (Exception ex)
            {
                return ServiceResponse.Error("Failed to save event location.");
            }
        }
        */
    }
}