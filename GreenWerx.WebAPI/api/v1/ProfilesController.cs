// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using GreenWerx.Managers.General;
using GreenWerx.Managers.Geo;
using GreenWerx.Managers.Membership;

using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Geo;
using GreenWerx.Models.Membership;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Web;
using GreenWerx.Web.api;
using GreenWerx.Web.Filters;
using WebApiThrottle;
using TMG = GreenWerx.Models.General;

namespace GreenWerx.WebAPI.api.v1
{
    public class ProfilesController : ApiBaseController
    {
        private ProfileMemberManager _profileMemberManager = null;

        public ProfilesController()
        {
        }

        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Profiles/{profileUUID}/Delete")]
        public ServiceResult DeleteProfile(string profileUUID)
        {
            ProfileManager profileManager = new ProfileManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            profileManager.DeleteProfile(profileUUID);
            return ServiceResponse.OK("");
        }

        //  [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Profiles")] // was AllProfiles
        public ServiceResult GetAllUserProfiles()
        {
            ProfileManager profileManager = new ProfileManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            UserManager userManager = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            LocationManager locationManager = new LocationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            _profileMemberManager = new ProfileMemberManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<Profile> tmp = new List<Profile>();

            DataFilter filter = this.GetFilter(Request);
            if (CurrentUser == null) //not logged in.
                tmp = profileManager.GetPublicProfiles(ref filter);
            else
                tmp = profileManager.GetAllProfiles(ref filter);

            if (tmp == null)
                return ServiceResponse.OK("", new List<Profile>());

            List<dynamic> profiles = tmp.Cast<dynamic>().ToList(); //profileManager.GetAllProfiles().Cast<dynamic>().ToList();

            profiles = profiles.Filter(ref filter);

            var defaultFilter = new DataFilter();
            // todo add profile Members? or rename profileLogs to profileMembers? so you can map multiple users to one profile
            // if profileLogs remember to sort by sortOrder
            _profileMemberManager = new ProfileMemberManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            profiles = profiles.Select(s => new
            {
                Name = s.Name,
                UUID = s.UUID,
                AccountUUID = s.AccountUUID,
                UUIDType = s.UUIDType,
                Image = s.Image,
                NSFW = s.NSFW,
                // Email = s.Email,
                Active = s.Active,
                Description = s.Description,
                Members = string.IsNullOrWhiteSpace(s.MembersCache) ?
                                _profileMemberManager.GetProfileMembers(s.UUID, s.AccountUUID, ref filter) :
                                JsonConvert.DeserializeObject<List<ProfileMember>>(s.MembersCache),
                User = string.IsNullOrWhiteSpace(s.UserCache) ?
                                ConvertResult(userManager.Get(s.UserUUID)) :
                                JsonConvert.DeserializeObject<User>(s.UserCache),
                LocationDetail = string.IsNullOrWhiteSpace(s.LocationDetailCache) ?
                                ConvertLocationResult(locationManager.Get(s.LocationUUID)) :
                                JsonConvert.DeserializeObject<Location>(s.LocationDetailCache),

                //  this.selectedProfile.LocationUUID = data.UUID;
                //this.selectedProfile.LocationType = data.LocationType;
                //single,married,ply, + genders by age? so ply-mfmfm
                // Profile.cs todo add to profileLogs/Members

                //Location { get; set; }
                //LocationType { get; set; }
                //Theme { get; set; }
                //View { get; set; }
                //UserUUID { get; set; }
                //
                // Node.cs
                //Status = string.Empty;
                //AccountUUID = string.Empty;
                //Deleted = false;
                //Private = true;
                //SortOrder = 0;
                //CreatedBy = string.Empty;
                //DateCreated = DateTime.MinValue;
                //RoleWeight = RoleFlags.MemberRoleWeights.Member;
                //RoleOperation = ">=";
                //Image = "";
            }).Cast<dynamic>().ToList();

            return ServiceResponse.OK("", profiles, filter.TotalRecordCount);
        }

        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Profiles/Screens")]
        public ServiceResult GetProfileScreens()
        {
            List<DataScreen> screens = new List<DataScreen>();
            var screen = new DataScreen();
            //screen.Command = 'SEARCHBY';
            //screen.Field = 'CATEGORY';
            //screen.Selected = false;
            //screen.Caption = name;
            //screen.Value = name; // 'false';
            //screen.Type = 'category';
            screens.Add(screen);
            return ServiceResponse.OK("", screens);
        }

        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Profile")]
        public ServiceResult GetUserProfile()
        {
            ProfileManager profileManager = new ProfileManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            var result = profileManager.GetProfile(CurrentUser.UUID, CurrentUser.AccountUUID, true);
            if (result.Code != 200)
                return result;
            return ServiceResponse.OK("", (Profile)result.Result);
        }

        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/ProfilesBy/{profileUUID}")]
        public ServiceResult GetUserProfileBy(string profileUUID)
        {
            ProfileManager profileManager = new ProfileManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            var res = profileManager.Get(profileUUID); //.GetProfile(CurrentUser.UUID, CurrentUser.AccountUUID);
            if (res.Code != 200)
                return res;

            var profile = (Profile)res.Result;
            if (profile.Private == true && string.IsNullOrWhiteSpace(this.GetAuthToken(Request)))
                return ServiceResponse.Error("This profile is private.");

            string requestorReason;
            profile.Blocked = !profileManager.ProfileAccessAuthorized(profile, out requestorReason);
            profile.BlockDescription = requestorReason;

            return ServiceResponse.OK("", profile);
        }

        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Profiles/Users/{userName}")]
        public ServiceResult GetUserProfileByUserName(string userName)
        {
            // DataFilter filter = this.GetFilter(Request);
            ProfileManager profileManager = new ProfileManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);
            return profileManager.GetProfile(userName);
        }

        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpPatch]
        [System.Web.Http.Route("api/Profiles/Save")]
        public ServiceResult SaveProfile(GreenWerx.Models.Membership.Profile p)
        {
            if (p == null)
                return ServiceResponse.Error("Invalid form sent to server.");

            ProfileManager profileManager = new ProfileManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            if (_profileMemberManager == null)
                _profileMemberManager = new ProfileMemberManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            var dbProfile = profileManager.Get(p.UUID);
            if (dbProfile == null)
            {
                if (string.IsNullOrWhiteSpace(p.CreatedBy))
                    p.UUID = CurrentUser.UUID;

                p.Private = true;
                var res = profileManager.InsertProfile(p);
                if (res.Code != 200)
                    return ServiceResponse.Error("Failed to create profile.");

                foreach (var profileMember in p.Members)
                {
                    profileMember.CreatedBy = CurrentUser.UUID;
                    profileMember.DateCreated = DateTime.UtcNow;
                    profileMember.Private = true;
                    profileMember.ProfileUUID = p.UUID;
                    _profileMemberManager.Save(profileMember);
                }
                profileManager.UpdateProfile(p);//this will update the cache fields
                return res;
            }
            foreach (var profileMember in p.Members)
            {
                _profileMemberManager.Update(profileMember);
            }

            return profileManager.UpdateProfile(p);
        }

        //[ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        //[System.Web.Http.HttpGet]
        //[System.Web.Http.Route("api/Profiles")]
        //public ServiceResult GetUserProfiles()
        //{
        //    UserManager profileManager = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

        //the profile list searches by profile.name which is incorrect,
        // it needs to search by user.name and return the profiles
        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Profiles/Search/User")]
        public ServiceResult SearchUsersReturnProfiles()
        {
            DataFilter filter = this.GetFilter(Request);
            ProfileManager profileManager = new ProfileManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var profiles = profileManager.GetUserProfiles(CurrentUser.AccountUUID, ref filter);

            LocationManager locationManager = new LocationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            UserManager userManager = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            _profileMemberManager = new ProfileMemberManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            profiles = profiles.Select(s => new
            {
                Name = s.Name,
                UUID = s.UUID,
                UUIDType = s.UUIDType,
                Image = s.Image,
                Active = s.Active,
                Description = s.Description,
                Members = string.IsNullOrWhiteSpace(s.MembersCache) ? _profileMemberManager.GetProfileMembers(s.UUID, s.AccountUUID, ref filter) : JsonConvert.DeserializeObject<List<ProfileMember>>(s.MembersCache),
                User = string.IsNullOrWhiteSpace(s.UserCache) ? userManager.Get(s.UserUUID) : JsonConvert.DeserializeObject<User>(s.UserCache),
                LocationDetail = string.IsNullOrWhiteSpace(s.LocationDetailCache) ? locationManager.Get(s.LocationUUID) : JsonConvert.DeserializeObject<Location>(s.LocationDetailCache),
            }).Cast<dynamic>();
            return ServiceResponse.OK("", profiles, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPatch]
        [System.Web.Http.Route("api/Profiles/{profileUUID}/SetActive")]
        public ServiceResult SetActiveProfile(string profileUUID)
        {
            ProfileManager profileManager = new ProfileManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            GreenWerx.Models.Membership.Profile profile = profileManager.SetActiveProfile(profileUUID, CurrentUser.UUID, CurrentUser.AccountUUID);
            return ServiceResponse.OK("", profile);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPatch]
        [System.Web.Http.Route("api/Profiles/{profileUUID}/SetImage/{attributeUUID}")]
        public ServiceResult SetActiveProfileImageFromAttribute(string attributeUUID)
        {
            AttributeManager atm = new AttributeManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            var res = atm.Get(attributeUUID);
            if (res.Code != 200)
                return res;
            var attribute = res.Result as TMG.Attribute;

            if (attribute.ValueType.EqualsIgnoreCase("ImagePath") == false)
            {
                return ServiceResponse.Error("Attribute is not an image path type.");
            }
            ProfileManager profileManager = new ProfileManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            var tmp = profileManager.GetProfile(CurrentUser.UUID, CurrentUser.AccountUUID, true);
            if (tmp.Code != 200)
                return tmp;

            GreenWerx.Models.Membership.Profile profile = (Profile)tmp.Result;

            profile.Image = attribute.Image;
            return ServiceResponse.OK("", profile.Image);
        }

        private Location ConvertLocationResult(ServiceResult res)
        {
            if (res == null || res.Result == null)
                return new Location();

            return ((Location)res.Result);
        }

        //    List<GreenWerx.Models.Membership.Profile> profiles = profileManager.GetProfiles(CurrentUser.UUID, CurrentUser.AccountUUID);
        //    return ServiceResponse.OK("", profiles);
        //}
        private User ConvertResult(ServiceResult res)
        {
            if (res == null || res.Result == null)
                return new GreenWerx.Models.Membership.User();

            return ((User)res.Result);
        }
    }
}