//using MySql.Provider.Database;
//using MySql.Provider.Providers.WordPress.V5.Models.Core;
//using MySql.Provider.Providers.WordPress.V5.Models.WBAffiliateMaster;
using System;
using System.Linq;
using GreenWerx.Managers.Logging;
using GreenWerx.Managers.Membership;
using GreenWerx.Models.App;
using GreenWerx.Models.Logging;
using GreenWerx.Models.Membership;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Utilites.Security;
using GreenWerx.Web;
using GreenWerx.Web.api;
using GreenWerx.Web.api.Helpers;

namespace GreenWerx.WebAPI.api.v1
{
    public class AffiliateController : ApiBaseController
    {
        //private MySqlDbContext mysqlContext;
        public AffiliateController()
        {
            //mysqlContext = new MySqlDbContext("mysql");
        }

        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Affiliate/Log")]
        public ServiceResult AddLog(AffiliateLog access)
        {
            if (access == null)
                return ServiceResponse.Error("No data sent.");

            NetworkHelper network = new NetworkHelper();
            access.ClientIp = network.GetClientIpAddress(this.Request);
            AffiliateManager affliateManager = new AffiliateManager(Globals.DBConnectionKey, this.GetAuthToken(this.Request));
            access.DateCreated = DateTime.UtcNow;
            var logRes = affliateManager.InsertLog(access);

            if (CurrentUser == null)
                CurrentUser = this.GetUser(this.GetAuthToken(this.Request));

            if (CurrentUser == null)
                return logRes;

            UserManager um = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(this.Request));
            User referringUser = new User();
            string type = access.NameType?.ToUpper();
            switch (type)
            {
                case "USER":
                    referringUser = um.Search(access.Name, false).FirstOrDefault(x => x.Name.EqualsIgnoreCase(access.Name) || x.UUID == access.Name);
                    if (referringUser == null)
                        return logRes;
                    var userRes = um.GetUser(CurrentUser.UUID, false);
                    if (userRes.Code == 200)
                    {
                        var targetUser = (User)userRes.Result;

                        if (string.IsNullOrWhiteSpace(access.ClientUserUUID))
                            access.ClientUserUUID = CurrentUser.UUID;

                        access.ReferringUUID = referringUser.UUID;
                        access.ReferringUUIDType = referringUser.UUIDType;
                        affliateManager.UpdateLog(access);

                        if (referringUser.UUID == CurrentUser.UUID)
                            return logRes;

                        // if the user doesn't have a referring member set, then this user get it.
                        if (string.IsNullOrWhiteSpace(targetUser.ReferringMember))
                        {
                            targetUser.ReferringMember = referringUser.UUID;
                            targetUser.ReferralDate = DateTime.UtcNow;
                            um.Update(targetUser);
                        }
                    }
                    break;

                case "ACCOUNT"://this is most likely out bound
                case "EVENT": //this is most likely out bound
                    if (string.IsNullOrWhiteSpace(access.ClientUserUUID))
                        access.ClientUserUUID = CurrentUser.UUID;

                    var usr = um.Get(CurrentUser.UUID);
                    if (usr.Code == 200)
                        access.ReferringUUID = ((User)usr.Result).ReferringMember;// CurrentUser.ReferringMember; //whomever signed up this user (if any), gets the referral to the outbound link

                    affliateManager.UpdateLog(access);
                    break;

                case "PROFILE":

                    break;
                    //default:
                    //    referringUser = um.Search(access.Name, false).FirstOrDefault(x => x.Name.EqualsIgnoreCase(access.Name));
                    //    if (referringUser == null)
                    //        return logRes;
                    //    break;
            }

            return logRes;
        }


//        public ServiceResult AddAffiliate(string userUUID, string password)
//        {
//            if (string.IsNullOrWhiteSpace(userUUID) == true)
//                return ServiceResponse.Error("No user id was sent.");

//            UserManager userManager = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

//            var userRes = userManager.Get(userUUID);
//            if (userRes == null || userRes.Code != 200)
//                return ServiceResponse.Error("User does not exist for id.");

//            var user = (User)userRes.Result;

//            // does user exist
//            // to do flag the table(s) as affiliate
//            // need to get password so it can be encrypted correctly.
            

//            if (mysqlContext.GetAll<bswp_users>().Any<bswp_users>(w => w.user_login.EqualsIgnoreCase(user.Name)))
//                return ServiceResponse.Error("Member is already an affiliate.");

//            // encrypt the user password
//            string hashedPw = PasswordHash.wpHashPasswordForWordpress(password);



//            // insert to affiliates and other tables
//            var affiliate = new bswp_wpam_affiliates();


//            //insert to users
//            #region bswp_usermeta
//            /*
//umeta_id	user_id	    meta_key	            meta_value
//18	        2	        nickname	            GreenWerx@gmail.com
//19	        2	        first_name	            plato
//20	        2	        last_name	            playroom
//21	        2	        description	
//22	        2	        rich_editing	        true
//23	        2	        syntax_highlighting	    true
//24	        2	        comment_shortcuts	    false
//25	        2	        admin_color	fresh
//26	        2	        use_ssl	0
//27	        2	        show_admin_bar_front	true
//28	        2	        locale	
//29	        2	        bswp_capabilities	        a:2:{s:14:"wpam_affiliate";b:1;s:9:"affiliate";b:1;}
//30	        2	        bswp_user_level	        0
//31	        2	        dismissed_wp_pointers	
                                
//            */
//            #endregion

//            //try logging in after inserting the record to see if it works
//        }

        //var context = new MySqlDbContext("mysql");


        //var users = context.GetAll<bswp_users>();
        //        if (users.Count() ==false)
        //            Debug.Assert(false, "SHOULD NOT FAIL");

        //        users.ElementAt(0).display_name = "UPDATED";// + DateTime.Now.ToString();

        //    if (context.Update<bswp_users>(users.ElementAt(0)) == false)
        //        Debug.Assert(false, "FAILED SO UPDATE");

        //    context.Insert<bswp_users>(new bswp_users()
        //{
        //    display_name = DateTime.Today.ToString(),
        //        user_email = "user@test.com" + DateTime.Today.Second,
        //        user_login = "test",
        //        user_pass = "test",
        //        user_nicename = "",
        //        user_url = "",
        //        user_registered = DateTime.Now,
        //        user_activation_key = "",
        //        user_status = 0,
        //    });

        //    users = context.GetAll<bswp_users>();
        //    var deleteMe = users.ElementAt(users.Count() - 1);

        //context.Delete<bswp_users>(deleteMe);
    }
}