using System.Collections.Generic;
using GreenWerx.Managers.Membership;
using GreenWerx.Models.App;
using GreenWerx.Models.Membership;
using GreenWerx.Web;
using GreenWerx.Web.api;
using GreenWerx.Web.api.Helpers;
using GreenWerx.Web.Filters;
using WebApiThrottle;

namespace GreenWerx.WebAPI.api.v1
{
    public class VerificationsController : ApiBaseController
    {
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Verifications/Delete")]
        public ServiceResult Delete(VerificationEntry s)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.UUID))
                return ServiceResponse.Error("Invalid account was sent.");

            VerificationManager verificationManager = new VerificationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return verificationManager.Delete(s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Verifications/Delete/{uuid}")]
        public ServiceResult Delete(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("Invalid id was sent.");

            VerificationManager verificationManager = new VerificationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = verificationManager.Get(uuid);
            if (res.Code != 200)
                return res;

            VerificationEntry fa = (VerificationEntry)res.Result;

            return verificationManager.Delete(fa);
        }

        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Verifications/{profileUUID}")]
        public ServiceResult Get(string profileUUID)
        {
            if (string.IsNullOrWhiteSpace(profileUUID))
                return ServiceResponse.Error("You must provide a name for the verification.");

            VerificationManager verificationManager = new VerificationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<VerificationEntry> s = verificationManager.Search(profileUUID);

            if (s == null || s.Count == 0)
                return ServiceResponse.Error("Verification could not be located for the profile.");

            return ServiceResponse.OK("", s);
        }

        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Verifications/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("You must provide a uuid for the verification.");

            VerificationManager verificationManager = new VerificationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return verificationManager.Get(uuid);
        }

        /// <summary>
        ///client sets..
        /// RecipientUUID = profileMember.UserUUID;
        /// RecipientProfileUUID = profileMember.ProfileUUID;
        /// RecipientAccountUUID = accountUUID;
        /// VerificationType = 'XXXXXXXXX';
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Verifications/Add")]
        [System.Web.Http.Route("api/Verifications/Insert")]
        public ServiceResult Insert(VerificationEntry s)
        {
            if (s == null)
                return ServiceResponse.Error("Invalid verification sent to server.");

            VerificationManager verificationManager = new VerificationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            NetworkHelper network = new NetworkHelper();
            s.VerifierIP = network.GetClientIpAddress(this.Request);
            var res = verificationManager.Insert(s);

            if (res.Code != 200)
                return res;

            // to do update verifications cache in profile class
            //Task.Run(async () =>
            //{
            //
            //profileManager.UpdateCache();
            //}

            return res;

            //UserSession us = SessionManager.GetSession(authToken);
            //if (us == null)
            //    return ServiceResponse.Error("You must be logged in to access this function.");

            //if (string.IsNullOrWhiteSpace(us.UserData))
            //    return ServiceResponse.Error("Couldn't retrieve user data.");

            //if (CurrentUser == null)
            //    return ServiceResponse.Error("You must be logged in to access this function.");

            //s.VerifierAccountUUID = CurrentUser.AccountUUID;
            //s.VerifierUUID = CurrentUser.UUID;
            //s.VerificationDate = DateTime.UtcNow;
            //s.VerifierProfileUUID = this.GetProfileUUID(authToken);
            //GreenWerx.Models.Membership.Profile verifierProfile = null;
            ////todo check if set, if not use profile -> locationUUID
            //if (string.IsNullOrWhiteSpace(s.VerifierLocationUUID)) {
            //    ProfileManager profileManager = new ProfileManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            //    var res = profileManager.Get(s.VerifierProfileUUID);
            //    try
            //    {
            //        if (res.Code == 200)
            //        {
            //            verifierProfile = (GreenWerx.Models.Membership.Profile)res.Result;
            //            s.VerifierLocationUUID = verifierProfile.LocationUUID;
            //        }
            //    } catch
            //    {//not that important.
            //    }
            //}

            //var vcts = verificationManager.GetVerificationEntries(s.RecipientProfileUUID);

            //var tmp = verificationManager.GetVerificationEntries(s.RecipientProfileUUID)
            //    .FirstOrDefault(w => w.VerifierUUID == CurrentUser.UUID &&
            //         w.VerificationType.EqualsIgnoreCase(s.VerificationType)
            //          && w.VerificationDate.AddDays(-90) < DateTime.UtcNow
            //        );//
            //if (   tmp != null    )
            //    return ServiceResponse.Error("You may only verify every ninety days.");

            //RoleManager rm = new RoleManager(Globals.DBConnectionKey, CurrentUser);
            //var userRole = rm.GetRolesForUser(CurrentUser.UUID, CurrentUser.AccountUUID)
            //                    .Where(w => w.Category.EqualsIgnoreCase("member"))
            //                    .OrderByDescending(o => o.RoleWeight).FirstOrDefault();

            //if(userRole == null)
            //    return ServiceResponse.Error("You must be assigned a role to verify.");

            //s.VerifierRoleUUID = userRole.UUID;

            ////verificationType
            //s.Weight  =  userRole.Weight; //<== role.Category of verifying user
            //var relationshipRole =  rm.GetRoles(SystemFlag.Default.Account).FirstOrDefault(w => w.CategoryRoleName.EqualsIgnoreCase(verifierProfile.RelationshipStatus));
            //s.Multiplier = relationshipRole.Weight;// <== of verifying user verifierProfile.RelationshipStatus
            //var verTypeRole = rm.GetRoles(SystemFlag.Default.Account).FirstOrDefault(w => w.Category.EqualsIgnoreCase("verified")
            //                                                    && w.CategoryRoleName.EqualsIgnoreCase(s.VerificationType));
            ////Category CategoryRoleName
            ////verified critical user
            ////verified    ambassador
            ////verified    geolocation
            ////verified    photo submission
            ////verified other member
            //s.VerificationTypeMultiplier = verTypeRole.Weight;
            //s.Points = ((s.VerificationTypeMultiplier) + s.Weight) * s.Multiplier;

            //  return verificationManager.Insert(s);
        }

        //[ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        //[System.Web.Http.HttpPost]
        //[System.Web.Http.HttpPatch]
        //[System.Web.Http.Route("api/Verifications/Update")]
        //public ServiceResult Update(VerificationEntry form)
        //{
        //    if (form == null)
        //        return ServiceResponse.Error("Invalid Verification sent to server.");

        //      VerificationManager verificationManager = new VerificationManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
        //    var res = verificationManager.Get(form.UUID);
        //    if (res.Code != 200)
        //        return res;
        //    var dbS = (VerificationEntry)res.Result;
        //    dbS.Deleted = form.Deleted;

        //    return verificationManager.Update(dbS);
        //}
    }
}