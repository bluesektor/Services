using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Managers.Documents;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Document;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Web;
using GreenWerx.Web.api;
using GreenWerx.Web.api.v1;
using GreenWerx.Web.Filters;
using WebApiThrottle;


namespace GreenWerx.WebAPI.api.v1
{
    public class PostsController : ApiBaseController
    {
        public PostsController()
        {
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Posts/Delete")]
        public ServiceResult Delete(Post s)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.UUID))
                return ServiceResponse.Error("Invalid account was sent.");

            PostManager postManager = new PostManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return postManager.Delete(s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Posts/Delete/{uuid}")]
        public ServiceResult Delete(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("Invalid id was sent.");

            PostManager postManager = new PostManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = postManager.Get(uuid);
            if (res.Code != 200)
                return res;

            Post fa = (Post)res.Result;

            return postManager.Delete(fa);
        }

        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Posts/{name}")]
        public ServiceResult Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResponse.Error("You must provide a name for the post.");

            PostManager postManager = new PostManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<Post> s = postManager.Search(name);

            if (s == null || s.Count == 0)
                return ServiceResponse.Error("Post could not be located for the name " + name);

            return ServiceResponse.OK("", s);
        }

        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        //   [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/PostsBy/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("You must provide a uuid for the post.");

            PostManager postManager = new PostManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return postManager.Get(uuid);
        }

        // [EnableThrottling(PerSecond = 1, PerMinute = 20, PerHour = 200, PerDay = 1500, PerWeek = 3000)]
        // [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Posts")]
        public ServiceResult GetPosts()
        {
         
            PostManager postManager = new PostManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            DataFilter filter = this.GetFilter(Request);

            if (CurrentUser == null)
            {
                if (filter != null)
                    filter.IncludePrivate = false;
            }


            List<dynamic> Posts = (List<dynamic>)postManager.GetPosts(CurrentUser?.AccountUUID, ref filter).Cast<dynamic>().ToList();

            Posts = Posts.Filter(ref filter);
            return ServiceResponse.OK("", Posts, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Posts/Add")]
        [System.Web.Http.Route("api/Posts/Insert")]
        public async Task<ServiceResult> Insert(Post s)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.Name))
                return ServiceResponse.Error("Invalid Post sent to server.");

            PostManager postManager = new PostManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);

            if (!string.IsNullOrWhiteSpace(s.UUID))
            {
                var res = postManager.Get(s.UUID);
                if(res.Code ==200)
                    return this.Update(s);
            }

            string authToken = this.GetAuthToken(Request);

            UserSession us = SessionManager.GetSession(authToken);
            if (us == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            //if (us.Captcha?.ToUpper() != s.Captcha?.ToUpper())
            //    return ServiceResponse.Error("Invalid code.");

            if (string.IsNullOrWhiteSpace(us.UserData))
                return ServiceResponse.Error("Couldn't retrieve user data.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            s.Author = CurrentUser.Name;

            if (s.Status.EqualsIgnoreCase("publish") && (s.PublishDate == DateTime.MinValue || s.PublishDate == null))
                s.PublishDate = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(s.AccountUUID) || s.AccountUUID == SystemFlag.Default.Account)
                s.AccountUUID = CurrentUser.AccountUUID;

            if (string.IsNullOrWhiteSpace(s.CreatedBy))
                s.CreatedBy = CurrentUser.UUID;

            if (s.DateCreated == DateTime.MinValue)
                s.DateCreated = DateTime.UtcNow;

            if (s.Sticky == true && CurrentUser.SiteAdmin != true)
                s.Sticky = false;

            if (s.PublishDate < DateTime.Now &&
                   CurrentUser.SiteAdmin == false)
                return ServiceResponse.Error("Publish date cannot be in the past.");

    
            var result = postManager.Insert(s);
            if (result.Code != 200)
                return result;

            SiteController site = new SiteController();
            await site.SendMessage(new GreenWerx.Models.Logging.EmailMessage()
            {
                Subject = "New Post by:" + CurrentUser.Name,
                Body = "Moderate new post by:" + CurrentUser.Name + "<br/>" +
                        s.Name + "<br/>" +
                        "link to post" + "<br/>" +
                        s.Body + "<br/>" +
                    "",

                DateCreated = DateTime.UtcNow,
                EmailTo = Globals.Application.AppSetting("SiteEmail"),
                EmailFrom = Globals.Application.AppSetting("SiteEmail")
            });

            return result;
        }

        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Posts/Search")]
        public ServiceResult SearchPosts()
        {
            DataFilter filter = this.GetFilter(Request);

            if (filter == null || filter.Screens == null || filter.Screens.Count == 0)
                return ServiceResponse.Error("You must send a filter to search posts.");

            PostManager postManager = new PostManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);

            string name = filter.Screens.FirstOrDefault(w => w.Field.EqualsIgnoreCase("name")).Value;
            name = Uri.UnescapeDataString(name);
            var posts = postManager.Search(name).OrderByDescending(o => o.PublishDate);

            int count = 0;

            // var resposts = posts.Filter(filter, out count);

            return ServiceResponse.OK("", posts, posts.Count());
        }

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
        [System.Web.Http.Route("api/Posts/Update")]
        public ServiceResult Update(Post form)
        {
            if (form == null)
                return ServiceResponse.Error("Invalid Post sent to server.");

            PostManager postManager = new PostManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            var res = postManager.Get(form.UUID);
            if (res.Code != 200)
                return res;
            var dbS = (Post)res.Result;

            if (dbS.DateCreated == DateTime.MinValue)
                dbS.DateCreated = DateTime.UtcNow;

            dbS.Name = form.Name;

            //below are not on Post.cshtml form
            dbS.Deleted = form.Deleted;
            dbS.Status = form.Status;
            dbS.SortOrder = form.SortOrder;
            dbS.Private = form.Private;

            if (form.PublishDate != dbS.PublishDate &&
                form.PublishDate < DateTime.Now &&
                CurrentUser.SiteAdmin == false)
                return ServiceResponse.Error("Publish date cannot be in the past.");

            if (form.PublishDate != DateTime.MinValue)
                dbS.PublishDate = form.PublishDate;

            if (form.Status.EqualsIgnoreCase("publish"))
            {
                if (dbS.PublishDate == DateTime.MinValue || dbS.PublishDate == null)
                    dbS.PublishDate = DateTime.UtcNow;
            }

            dbS.Body = form.Body;
            dbS.AllowComments = form.AllowComments;
            dbS.Category = form.Category;
            dbS.KeyWords = form.KeyWords;

            if (dbS.Sticky == true && CurrentUser.SiteAdmin != true)
                dbS.Sticky = false;

            if (string.IsNullOrWhiteSpace(dbS.Author))
                dbS.Author = CurrentUser.Name;

            return postManager.Update(dbS);
        }
    }
}