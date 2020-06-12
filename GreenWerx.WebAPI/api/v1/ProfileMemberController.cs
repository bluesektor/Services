// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using GreenWerx.Managers.General;
using GreenWerx.Managers.Membership;

using GreenWerx.Models.App;
using GreenWerx.Models.Membership;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Web;
using GreenWerx.Web.api;
using GreenWerx.Web.Filters;
using TMG = GreenWerx.Models.General;

namespace GreenWerx.WebAPI.api.v1
{
    public class ProfileMemberController : ApiBaseController
    {
        private ProfileMemberManager _profileMemberManager = null;

        public ProfileMemberController()
        {
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/ProfileMembers/{profileUUID}/Delete")]
        public ServiceResult DeleteProfile(string profileUUID)
        {
            if (_profileMemberManager == null)
                _profileMemberManager = new ProfileMemberManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            var p = _profileMemberManager.Get(profileUUID);
            if (p.Code != 200)
                return p;

            _profileMemberManager.Delete((ProfileMember)p.Result, true); // purges
            return ServiceResponse.OK("");
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/ProfileMembersBy/{profileUUID}")]
        public ServiceResult GetUserProfileBy(string profileUUID)
        {
            if (_profileMemberManager == null)
                _profileMemberManager = new ProfileMemberManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return _profileMemberManager.Get(profileUUID); //.GetProfile(CurrentUser.UUID, CurrentUser.AccountUUID);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpPatch]
        [System.Web.Http.Route("api/ProfileMembers/Save")]
        public ServiceResult SaveProfile(GreenWerx.Models.Membership.ProfileMember p)
        {
            if (p == null)
                return ServiceResponse.Error("Invalid form sent to server.");

            if (_profileMemberManager == null)
                _profileMemberManager = new ProfileMemberManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = _profileMemberManager.Get(p.UUID);
            if (res.Code != 200)
            {
                if (string.IsNullOrWhiteSpace(p.CreatedBy))
                    p.UUID = CurrentUser.UUID;

                return _profileMemberManager.Save(p);
            }
            GreenWerx.Models.Membership.ProfileMember dbProfile = (ProfileMember)res.Result;
            return _profileMemberManager.Update(p);
        }

        //[ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        //[System.Web.Http.HttpGet]
        //[System.Web.Http.Route("api/ProfileMember")]
        //public ServiceResult GetUserProfile()
        //{
        //    GreenWerx.Models.Membership.ProfileMember profile = _profileMemberManager.GetProfile(CurrentUser.UUID, CurrentUser.AccountUUID, true);
        //    return ServiceResponse.OK("", profile);
        //}

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPatch]
        [System.Web.Http.Route("api/ProfileMembers/{profileUUID}/SetImage/{attributeUUID}")]
        public ServiceResult SetActiveProfileImageFromAttribute(string attributeUUID)
        {
            AttributeManager atm = new AttributeManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = atm.Get(attributeUUID);
            if (res.Code != 200)
                return res;

            var attribute = res.Result as TMG.Attribute;

            if (attribute == null)
                return ServiceResponse.Error("Attribute was not found.");

            if (attribute.ValueType.EqualsIgnoreCase("ImagePath") == false)
            {
                return ServiceResponse.Error("Attribute is not an image path type.");
            }
            if (_profileMemberManager == null)
                _profileMemberManager = new ProfileMemberManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            var profile = _profileMemberManager.GetMemberProfile(CurrentUser.UUID, CurrentUser.AccountUUID);

            if (profile == null)
                return ServiceResponse.Error("ProfileMember was not found.");

            profile.Image = attribute.Image;
            return ServiceResponse.OK("", profile.Image);
        }

        //[ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        //[System.Web.Http.HttpPatch]
        //[System.Web.Http.Route("api/ProfileMembers/{profileUUID}/SetActive")]
        //public ServiceResult SetActiveProfile(string profileUUID)
        //{
        //    UserManager userManager = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

        //    GreenWerx.Models.Membership.ProfileMember profile = userManager.SetActiveProfile(profileUUID, CurrentUser.UUID, CurrentUser.AccountUUID);
        //    return ServiceResponse.OK("", profile);
        //}
    }
}