using System;
using System.Collections.Generic;
using System.Linq;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Managers;
using GreenWerx.Managers.General;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.General;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Web;
using GreenWerx.Web.api;
using GreenWerx.Web.Filters;

namespace GreenWerx.WebAPI.api.v1
{
    public class FavoritesController : ApiBaseController
    {
        public FavoritesController()
        {
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 1)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Favorites/Delete/{uuid}")]
        public ServiceResult Delete(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("Invalid id was sent.");

            FavoritesManager favoriteManager = new FavoritesManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var fav = favoriteManager.Get(uuid);
            if (fav.Code != 200)
                return fav;
            return favoriteManager.Delete((Favorite)fav.Result, true);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Favorites/{name}")]
        public ServiceResult Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResponse.Error("You must provide a name for the Favorite.");

            FavoritesManager favoriteManager = new FavoritesManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<Favorite> s = favoriteManager.Search(name);

            if (s == null || s.Count == 0)
                return ServiceResponse.Error("Favorite could not be located for the name " + name);

            return ServiceResponse.OK("", s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/FavoriteBy/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("You must provide a name for the Favorite.");

            FavoritesManager favoriteManager = new FavoritesManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return favoriteManager.Get(uuid);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 0)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Favorites/Types/{type}")]
        public ServiceResult GetFavorites(string type)
        {
            if (Request.Headers.Authorization == null || string.IsNullOrWhiteSpace(this.GetAuthToken(Request)))
                return ServiceResponse.Error("You must be logged in to access this functionality.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            FavoritesManager favoriteManager = new FavoritesManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            var tmp = favoriteManager.GetFavorites(type, CurrentUser.UUID, CurrentUser.AccountUUID);
            if (tmp == null)
                return ServiceResponse.OK("", null, 0);

            List<dynamic> Favorites = tmp.Cast<dynamic>()?.ToList();

            DataFilter filter = this.GetFilter(Request);
            Favorites = Favorites.Filter(ref filter);

            return ServiceResponse.OK("", Favorites, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Favorites/Add")]
        [System.Web.Http.Route("api/Favorites/Insert")]
        public ServiceResult Insert(Favorite s)
        {
            if (s == null)
                return ServiceResponse.Error("Invalid data sent.");

            string authToken = Request.Headers.Authorization?.Parameter;
            SessionManager sessionManager = new SessionManager(Globals.DBConnectionKey);

            UserSession us = sessionManager.GetSession(authToken);
            if (us == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(s.AccountUUID) || s.AccountUUID == SystemFlag.Default.Account)
                s.AccountUUID = CurrentUser.AccountUUID;

            if (string.IsNullOrWhiteSpace(s.CreatedBy))
                s.CreatedBy = CurrentUser.UUID;

            if (s.DateCreated == DateTime.MinValue)
                s.DateCreated = DateTime.UtcNow;

            s.Active = true;
            s.Deleted = false;

            FavoritesManager favoriteManager = new FavoritesManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            ServiceResult sr = favoriteManager.Insert(s);
            if (sr.Code != 200)
                return sr;

            return sr;
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
        [System.Web.Http.Route("api/Favorites/Update")]
        public GreenWerx.Models.App.ServiceResult Update(Favorite s)
        {
            if (s == null)
                return ServiceResponse.Error("Invalid Favorite sent to server.");

            FavoritesManager favoriteManager = new FavoritesManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = favoriteManager.Get(s.UUID);
            if (res.Code != 200)
                return res;

            var dbS = (Favorite)res.Result;

            if (dbS.DateCreated == DateTime.MinValue)
                dbS.DateCreated = DateTime.UtcNow;
            dbS.Deleted = s.Deleted;
            dbS.Name = s.Name;
            dbS.Status = s.Status;
            dbS.SortOrder = s.SortOrder;
            dbS.Active = s.Active;
            dbS.ItemUUID = s.ItemUUID;
            dbS.ItemType = s.ItemType;

            return favoriteManager.Update(dbS);
        }
    }
}