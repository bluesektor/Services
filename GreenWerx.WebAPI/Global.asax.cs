// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.UI;
using GreenWerx.Data;
using GreenWerx.Data.Logging;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Managers;
using GreenWerx.Managers.Membership;
using GreenWerx.Models.App;
using GreenWerx.Models.Logging;
using GreenWerx.Models.Membership;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Utilites.Security;
using GreenWerx.Web;
using GreenWerx.Web.api.Helpers;
using GreenWerx.WebAPI.Helpers;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace GreenWerx.WebAPI
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        private const string ROOT_DOCUMENT = "/index.html";
        private static List<RequestLog> requests = null;
        
        private readonly SystemLogger _fileLogger = new SystemLogger(null, true);
        private AppManager _appManager = new AppManager(Globals.DBConnectionKey, "web", "");

        private DateTime _lastSessionsClear = DateTime.MinValue;

        public WebApiApplication()
        { }

        protected void Application_Start()
        {
            //  _fileLogger.InsertInfo("global.asax", "global.asax", "Application_Start");

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            if (!Globals.Application.Initialized)
            {
                Globals.InitializeGlobals();
                Globals.Application.Start();
            }

            if (string.IsNullOrWhiteSpace(Globals.Application.Status) || Globals.Application.Status == "true")
                Globals.Application.Status = "RUNNING";

            if (string.IsNullOrWhiteSpace(Globals.Application.ApiStatus) || Globals.Application.ApiStatus == "true")
                Globals.Application.ApiStatus = Globals.Application.AppSetting("ApiStatus", "PRIVATE");
        }

        private void Application_BeginRequest(object sender, EventArgs e)
        {
          //  _appManager.AssignEventsToEventLocations();
            if (requests == null)
                requests = new List<RequestLog>();

            if (!HttpContext.Current.Request.IsSecureConnection)
                HttpContext.Current.Response.Redirect(HttpContext.Current.Request.Url.AbsoluteUri.Replace("http://", "https://"));

            SessionManager sessionManager = new SessionManager(Globals.DBConnectionKey);

            String strRequestedFile = HttpContext.Current.Server.MapPath(HttpContext.Current.Request.FilePath);

            if (HttpContext.Current.Request.HttpMethod == "GET" && strRequestedFile.Contains("content\\Protected", StringComparison.InvariantCultureIgnoreCase))
            {
                this.LogRequest();

                try
                {
                    string auth = string.Empty;
                    //string test =   ContextHelper.GetContextData();
                    // if (HttpContext.Current.Request.Cookies != null && HttpContext.Current.Request.Cookies["bearer"] != null ) auth = HttpContext.Current.Request.Cookies["bearer"].ToString();
                    //bearer <= get bearer cookie from client
                    //  var auth = HttpContext.Current.Request.Headers.GetValues("Authorization")?.ToString().Replace("Bearer ", "");

                    if (!strRequestedFile.Contains(".seg."))
                    { LoadImage(HttpContext.Current, "user_must_login"); return; }

                    auth = strRequestedFile.Substring(".seg.", "");

                    if (!string.IsNullOrWhiteSpace(auth))
                        auth = auth?.Trim();

                    if (string.IsNullOrWhiteSpace(auth))
                    { LoadImage(HttpContext.Current, "user_must_login"); return; }

                    UserSession us = sessionManager.GetSessionByEndToken(auth);
                    if (us == null || string.IsNullOrWhiteSpace(us.UserData))
                    { LoadImage(HttpContext.Current, "user_must_login"); return; }

                    var user = JsonConvert.DeserializeObject<User>(us.UserData);

                    if (user.Deleted)
                    { LoadImage(HttpContext.Current, "user_is_deleted"); return; }
                    if (!user.Approved)
                    { LoadImage(HttpContext.Current, "user_not_approved"); return; }

                    if (user.LockedOut)
                    { LoadImage(HttpContext.Current, "user_locked"); return; }

                    if (user.Banned)
                    { LoadImage(HttpContext.Current, "user_banned"); return; }

                    strRequestedFile = strRequestedFile.Replace(".seg." + auth, "");
                    // Since this is upLoads file the folder should be the user.UUID
                    string[] pathTokens = strRequestedFile.Split('\\');
                    string targetUserUUID = pathTokens[pathTokens.Length - 2];

                    ProfileManager profileManager = new ProfileManager(Globals.DBConnectionKey, auth);
                    var res = profileManager.GetProfile(targetUserUUID);
                    if (res.Code != 200)
                    {
                        LoadImage(HttpContext.Current, "target_profile_notfound"); return;
                    }

                    var targetProfile = (Profile)res.Result;

                    string reason = "";
                    if (profileManager.ProfileAccessAuthorized(targetProfile, out reason) == false)
                    {
                        LoadImage(HttpContext.Current, "target_profile_access_denied"); return;
                    }

                    // is private takes precedence over is public
                    // this.CurrentUser.UUID
                    // todo check if profile is public, and if the image pertains to public access (not set to protected.)
                }
                catch (Exception ex)
                {
                    LoadImage(HttpContext.Current, "500"); return;
                }
                LoadImage(HttpContext.Current, "");
            }

            if (Validator.HasCodeInjection(HttpContext.Current.Request.Url.Query) || Validator.HasSqlCommand(HttpContext.Current.Request.Url.Query))
            {
                this.LogRequest();
                Response.Write("Invalid request, security violation occured.");
            }

            if (string.IsNullOrWhiteSpace(Globals.Application.ApiStatus) || Globals.Application.ApiStatus == "true")
                Globals.Application.ApiStatus = Globals.Application.AppSetting("ApiStatus", "PRIVATE");

            // Stop clickjacking and from being loaded in a frame.
            HttpContext.Current.Response.AddHeader("x-frame-options", "DENY");

            SetCorsOptions();

            ////if (Globals.Application.Status == "REQUIRES_INSTALL" || Globals.Application.Status == "INSTALLING")
            ////{
            ////    Globals.Application.Status = "INSTALLING";
            ////    return;
            ////}

            TimeSpan ts = DateTime.UtcNow - _lastSessionsClear;

            //backlog move this to somewhere better.
            if (_lastSessionsClear == DateTime.MinValue || ts.TotalMinutes > sessionManager.SessionLength)
            {
                sessionManager.ClearExpiredSessions(sessionManager.SessionLength);
                _lastSessionsClear = DateTime.UtcNow;
            }
            this.LogRequest();

            if (Request.Url.LocalPath.StartsWith("/api") || Request.Url.LocalPath.StartsWith("/Content"))
                return;

            string url = Request.Url.LocalPath;
            if (!System.IO.File.Exists(Context.Server.MapPath(url)))
                Context.RewritePath(ROOT_DOCUMENT);
        }

        private void Application_EndRequest(object sender, EventArgs e)
        {
            try
            {
                if (!HttpContext.Current.Items.Contains("RequestIdentity"))
                    return;

                var uuid = HttpContext.Current.Items["RequestIdentity"].ToString();
                // so we can find the request in the array in the function
                for (int i = 0; i < requests.Count; i++)
                {
                    if (requests[i].UUID != uuid)
                        continue;

                    //var req = requests.FirstOrDefault(w => w.UUID == uuid);
                    if (requests[i].RequestComplete == true)
                        return;

                    requests[i].Timer.Stop();
                    requests[i].ExecutionTime = requests[i].Timer.ElapsedMilliseconds;
                    requests[i].Response = HttpContext.Current.Response.Status;
                    requests[i].RequestComplete = true;

                    using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
                    {
                        context.Insert<RequestLog>(requests[i]);

                        requests.RemoveAt(i);// requests.Remove(req);
                        return;
                    }
                }
            }
            catch { }
        }

        private void Application_Error(object sender, EventArgs e)
        {
            try
            {
                //  _fileLogger.InsertInfo(JsonConvert.SerializeObject(e), "global.asax", "Application_Error");
                // get the exception and re-throw
                Exception ex = Server.GetLastError();
                _fileLogger.InsertInfo(ex.DeserializeException(true), "global.asax", "Application_Error");

                // SystemLogger logger = new SystemLogger(Globals.DBConnectionKey);
                //logger.InsertError(ex.Message, "Golbal.asax", "Application_Error");
                throw ex;
            }
            catch (HttpException httpEx)
            {
                _fileLogger.InsertInfo(httpEx.DeserializeException(true), "global.asax", "SetCorsOptions");
            }
        }

        //void Application_PreRequestHandlerExecute(object sender, EventArgs e)
        //{
        //    var app = sender as HttpApplication;
        //    if (app == null) return;

        //    var acceptEncoding = app.Request.Headers["Accept-Encoding"];
        //    var prevUncompressedStream = app.Response.Filter;

        //    if (!(app.Context.CurrentHandler is Page ||
        //      app.Context.CurrentHandler.GetType().Name == "SyncSessionlessHandler") ||
        //    app.Request["HTTP_X_MICROSOFTAJAX"] != null)
        //        return;

        //    if (string.IsNullOrEmpty(acceptEncoding))
        //        return;

        //    acceptEncoding = acceptEncoding.ToLower();
        //    if (acceptEncoding.Contains("gzip"))
        //    {
        //        // gzip
        //        app.Response.Filter = new GZipStream(prevUncompressedStream, CompressionMode.Compress);
        //        app.Response.AppendHeader("Content-Encoding", "gzip");
        //    }
        //    else if (acceptEncoding.Contains("deflate") || acceptEncoding == "*")
        //    {
        //        // defalte
        //        app.Response.Filter = new DeflateStream(prevUncompressedStream, CompressionMode.Compress);
        //        app.Response.AppendHeader("Content-Encoding", "deflate");
        //    }

        //}

        private void SetCorsOptions()
        {
            // _fileLogger.InsertInfo("global.asax", "global.asax", "SetCorsOptions");

            if (Globals.Application.Status == "REQUIRES_INSTALL" || Globals.Application.Status == "INSTALLING")
            {
                HttpContext.Current.Response.AddHeader("Access-Control-Allow-Origin", "*");
            }
            else
            {
                switch (Globals.Application?.ApiStatus?.ToUpper())
                {
                    case "PRIVATE":
                        string domain = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);
                        HttpContext.Current.Response.AddHeader("Access-Control-Allow-Origin", domain);
                        break;

                    case "PROTECTED"://check to see if this domain is allowed to make an api request. NOTE: this is different from the request throttling code.
                      
                        var origin = HttpContext.Current.Request.Headers["Origin"];
                        //if (Globals.Application.UseDatabaseConfig && appManager.SettingExistsInDatabase("AllowedOrigin", origin))
                        //    HttpContext.Current.Response.AddHeader("Access-Control-Allow-Origin", origin);
                        //else if (!Globals.Application.UseDatabaseConfig)
                        //{
                        var value = _appManager.GetSetting("AllowedOrigins")?.Value;
                        if (!string.IsNullOrWhiteSpace(value) && value.Split(',').Any(x => x.EqualsIgnoreCase(origin)))
                            HttpContext.Current.Response.AddHeader("Access-Control-Allow-Origin", origin);
                        //}
                        break;

                    case "PUBLIC":
                        HttpContext.Current.Response.AddHeader("Access-Control-Allow-Origin", "*");
                        break;
                }
            }

            if (HttpContext.Current.Request.HttpMethod == "OPTIONS")
            {
                //specific initialization
                //These headers are handling the "pre-flight" OPTIONS call sent by the browser
                HttpContext.Current.Response.AddHeader("Access-Control-Allow-Methods", "GET,POST,PATCH,DELETE,PUT");
                HttpContext.Current.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept,authorization,content-type");
                HttpContext.Current.Response.AddHeader("Access-Control-Max-Age", "1728000");
                HttpContext.Current.Response.End();
            }
        }


        /// <summary>
        /// TODO make images for each category : not logged in, banned...
        /// </summary>
        /// <param name="context"></param>
        /// <param name="imageCategory"></param>
        private void LoadImage(HttpContext context, string imageCategory )
        {
            // Define your Domain Name Here
            String strDomainName = Globals.Application.AppSetting("SiteDomain");
            // Add the RELATIVE folder where you keep your stuff here
            String strFolder = "~/Content/Protected";
            // Add the RELATIVE PATH of your "no" image
            // todo set this to a custom image
            String strNoImage = "~/Content/Default/Images/defaultNotAuthorizedImage.png"; // if this is set to null or empty string then an empty response is return as per 'maxxnostra's comment on Codeproject. Thanks.

            if (!string.IsNullOrWhiteSpace(imageCategory)) {
                strNoImage = $"~/Content/Default/Images/{imageCategory}.png";
                // TODO create images by category, use the text below for file names
             
                context = SendContentTypeAndFile(context, strNoImage);
                return;
            }

            switch (context.Request.HttpMethod)
            {
                case "GET":
                    String strRequestedFile = context.Server.MapPath(context.Request.FilePath);
                    if (context.Request.UrlReferrer != null)
                    {
                        String strUrlRef = context.Request.UrlReferrer.ToString();   // todo put csv of leachers in file and send back our own file. for css leachers do this? =>  body{ background-image:xx.jpg !important}
                        String strUrlImageFull = ResolveUrl(strFolder);

                        // AllowedOrigins
                        // 
                        var allowedOrigins = _appManager.GetSetting("AllowedOrigins")?.Value;
                        if (!string.IsNullOrWhiteSpace(allowedOrigins) && allowedOrigins.Split(',').Any(x => x.EqualsIgnoreCase(strUrlRef)))
                        {
                            context = SendContentTypeAndFile(context, strNoImage);
                        }
                        else if (strUrlRef.Contains(strUrlImageFull))
                        {
                            context = SendContentTypeAndFile(context, strNoImage);
                        }
                        else if (strUrlRef.StartsWith(strDomainName))
                        {
                            context = SendContentTypeAndFile(context, strRequestedFile);
                        }
                        else
                        {
                            context = SendContentTypeAndFile(context, strNoImage);
                        }
                    }
                    else
                    {
                        context = SendContentTypeAndFile(context, strNoImage);
                    }
                    break;
                    //case "POST":
                    //    context = SendContentTypeAndFile(context, strNoImage);
                    //    break;
            }

        }

        public string GetContentType(string filename)
        {
            // used to set the encoding for the reponse stream
            string res = null;
            FileInfo fileinfo = new System.IO.FileInfo(filename);
            if (fileinfo.Exists)
            {
                switch (fileinfo.Extension.Remove(0, 1).ToLower())
                {
                    case "png":
                        res = "image/png";
                        break;
                    case "jpeg":
                        res = "image/jpg";
                        break;
                    case "jpg":
                        res = "image/jpg";
                        break;
                    case "js":
                        res = "application/javascript";
                        break;
                    case "css":
                        res = "text/css";
                        break;
                }
                return res;
            }
            return null;
        }

        HttpContext SendContentTypeAndFile(HttpContext context, String strFile)
        {
            if (String.IsNullOrEmpty(strFile))
            {
                return null;
            }
            else
            {
                context.Response.ContentType = GetContentType(strFile);
                context.Response.TransmitFile(strFile);
                context.Response.End();
                return context;
            }
        }

        // NOTE:: I have not written this function. I found it on the web a while back. All credits for this function go to the author (whose name I cannot remember).
        public string ResolveUrl(string originalUrl)
        {
            if (originalUrl == null)
                return null;
            // *** Absolute path - just return   
            if (originalUrl.IndexOf("://") != -1)
                return originalUrl;
            // *** Fix up image path for ~ root app dir directory    
            if (originalUrl.StartsWith("~"))
            {
                string newUrl = "";
                if (HttpContext.Current != null)
                    newUrl = HttpContext.Current.Request.ApplicationPath + originalUrl.Substring(1).Replace("//", "/");
                else // *** Not context: assume current directory is the base directory        
                    throw new ArgumentException("Invalid URL: Relative URL not allowed.");
                return newUrl;
            }// *** Just to be sure fix up any double slashes        
            return originalUrl;
        }

        private void LogRequest()
        {
            try
            {
                RequestLog req = new RequestLog();
                req.Timer = new Stopwatch();
                req.DateCreated = DateTime.UtcNow;
                req.RequestURL = HttpContext.Current.Request.Url.AbsoluteUri;
                req.AbsolutePath = HttpContext.Current.Request.Url.AbsolutePath;
                req.Referrer = HttpContext.Current.Request.UrlReferrer.AbsoluteUri; // ?.DnsSafeHost;
                req.RequestComplete = false;
                req.IPAddress = new NetworkHelper().GetClientIpAddress(HttpContext.Current.Request);
                req.Method = HttpContext.Current.Request.HttpMethod;
                req.RequestLocalPath = HttpContext.Current.Request.Path;
                req.RequestURL = HttpContext.Current.Request.Url.ToString();
                var auth = HttpContext.Current.Request.Headers.GetValues("Authorization")?.ToString().Replace("Bearer ", "");
                auth = auth?.Trim();

                //IEnumerable<KeyValuePair<string, string>> kvp = actionContext.Request.GetQueryNameValuePairs();
                //KeyValuePair<string, string> apiKVP = kvp.FirstOrDefault(w => w.Key.EqualsIgnoreCase("KEY"));

                //if (Globals.AddRequestPermissions)
                //{
                //    //if we need to pass the user object in, move this down below after it gets the current user.
                //    RoleManager roleManager = new RoleManager(Globals.DBConnectionKey, null);
                //    string name = roleManager.NameFromPath(absolutePath);
                //    roleManager.CreatePermission(name, method, absolutePath, "api");
                //}

                if (!string.IsNullOrWhiteSpace(auth))
                {
                    JwtClaims requestorClaims = null;

                    try
                    {
                        _appManager = new AppManager(Globals.DBConnectionKey, "web", auth);
                        string appSecret = _appManager.GetSetting("AppKey")?.Value;

                        var payload = JWT.JsonWebToken.Decode(auth, appSecret, false);
                        requestorClaims = JsonConvert.DeserializeObject<JwtClaims>(payload);

                        string[] tokens = requestorClaims.aud.Replace(SystemFlag.Default.Account, "systemdefaultaccount").Split('.');
                        if (tokens.Length >= 2)
                        {
                            req.UserUUID = tokens[0];
                            req.AccountUUID = tokens[1];
                        }
                    }
                    catch { }
                }
                HttpContext.Current.Items.Add("RequestIdentity", req.UUID); // so we can find the request in the array in the function

                if (requests.Count < 200)
                {
                    requests.Add(req);
                }
                else
                {
                    for (int i = 0; i < requests.Count; i++)
                    {
                        if (requests[i].RequestComplete)
                        {
                            requests.Remove(requests[i]);
                            continue;
                        }

                        TimeSpan elaps = DateTime.UtcNow - requests[i].DateCreated;

                        if (elaps.TotalMinutes > 10)
                        {
                            requests.Remove(requests[i]);
                        }
                    }
                }

                req.Timer.Start();
            }
            catch { }
        }
    }
}