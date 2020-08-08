// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.

//using MySql.Provider;
//using MySql.Provider;
//using MySql.Provider.Database;
//using MySql.Provider.Providers.WordPress.V5.Models.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using GreenWerx.Data;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Managers;
using GreenWerx.Managers.Events;
using GreenWerx.Managers.General;
using GreenWerx.Managers.Geo;
using GreenWerx.Managers.Logging;
using GreenWerx.Managers.Membership;
using GreenWerx.Managers.Tools;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Events;
using GreenWerx.Models.Flags;
using GreenWerx.Models.Geo;
using GreenWerx.Models.Membership;
using GreenWerx.Models.Tools;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Utilites.Helpers;
using GreenWerx.Utilites.Security;
using GreenWerx.Web.api.Helpers;
using GreenWerx.Web.Filters;
using GreenWerx.Web.Models;
using WebApiThrottle;
using TMG = GreenWerx.Models.General;

namespace GreenWerx.Web.api.v1
{
    public class ToolsController : ApiBaseController
    {
        private readonly NetworkHelper network = new NetworkHelper();

        public ToolsController()
        {
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Tools/Database/Backup")]
        public async Task<ServiceResult> BackupDatabaseAsync()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (!CurrentUser.SiteAdmin)
                return ServiceResponse.Error("You are not authorized this action.");

            AppManager app = new AppManager(Globals.DBConnectionKey, "web", this.GetAuthToken(Request));
            ServiceResult res = await app.BackupDatabase(Globals.Application.AppSetting("DBBackupKey"));

            if (res.Code != 200)
                return res;

            ClearTempFiles();
            return res;
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Tools/Cipher/{text}/Encrypt/{encrypt}")]
        public ServiceResult Cipher(string text, bool encrypt)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (!CurrentUser.SiteAdmin)
                return ServiceResponse.Error("You are not authorized this action.");

            string data = "";
            try
            {
                if (string.IsNullOrWhiteSpace(Globals.Application.AppSetting("AppKey", "")))
                    return ServiceResponse.Error("The appkey is not set in the config file. It must have a value to use the encrypt string.");

                data = GreenWerx.Utilites.Security.Cipher.Crypt(Globals.Application.AppSetting("AppKey"), text, encrypt);
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error(ex.Message);
            }
            return ServiceResponse.OK("", data);
        }

        public Location findCity(string countryAbbr, string cityName)
        {
            string[] vowels = new string[] { "a", "e", "i", "o", "u", "y" };

            foreach (string vowel in vowels)
            {
                string name = cityName.Replace("�", vowel);

                using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
                {
                    var location = context.GetAll<Location>()?.FirstOrDefault(w => w.Abbr == countryAbbr && w.Name.EqualsIgnoreCase(name));
                    if (location == null)
                        continue;

                    return location;
                }
            }
            return null;
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Tools/Dashboard")]
        public ServiceResult GetToolsDashboard()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (!CurrentUser.SiteAdmin)
                return ServiceResponse.Error("You are not authorized this action.");

            AppManager app = new AppManager(Globals.DBConnectionKey, "web", this.GetAuthToken(Request));

            ToolsDashboard frm = new ToolsDashboard();
            frm.Backups = app.GetDatabaseBackupFiles();
            frm.DefaultDatabase = app.GetDefaultDatabaseName();
            frm.ImportFiles = new DirectoryInfo(Path.Combine(EnvironmentEx.AppDataFolder, "Install\\SeedData\\")).GetFiles().Select(o => o.Name).ToList();
            return ServiceResponse.OK("", frm);
        }

        // [EnableThrottling( PerDay = 200)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Tools/CreateCache/{type}")]
        public ServiceResult CreateCacheFile(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return ServiceResponse.Error("No type was sent.");

            switch (type) {
                case "event":
                    string devaultEventUUID = Globals.Application.AppSetting("DefaultEvent");
                    var defaultTimeZone = TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(w => w.BaseUtcOffset.TotalHours < -9 && w.BaseUtcOffset.TotalHours > -12);
                    TimeZoneInfo tzInfo = TimeZoneInfo.CreateCustomTimeZone(defaultTimeZone.StandardName, new TimeSpan(Convert.ToInt32(defaultTimeZone.BaseUtcOffset.TotalHours), 0, 0),
                        defaultTimeZone.StandardName, defaultTimeZone.StandardName);
                    DateTime adjustedDate = adjustedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.Date, tzInfo);
                    EventManager eventManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                    DataFilter filter = new DataFilter();
                    filter.PageSize = 50;
                    var Events = eventManager.GetSubEvents(devaultEventUUID, adjustedDate.Date, ref filter);

                    var jsonEvents = JsonConvert.SerializeObject(Events, Formatting.None);

                    string pathToFile = System.Web.HttpContext.Current.Server.MapPath("~/Content/Cache/events.json");

                    File.WriteAllText(pathToFile, jsonEvents);
                    return ServiceResponse.OK();
                default:
                    return ServiceResponse.Error("Invalid type.");
            }
        }


        /// <summary>
        /// /
        /// </summary>
        /// <param name="type"></param>
        /// <param name="validate">NOTE:Does record exist by name && accountUUID. This will check the item being imported by name for the account of the currently logged in user.</param>
        /// <param name="validateGlobally">NOTE:Does record exist by name regardless of account.</param>
        /// <returns></returns>
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Tools/ImportFile/{type}/Validate/{validate}/ValidateGlobally/{validateGlobally}")]
        public async Task<ServiceResult> ImportFile(string type, bool validate = true, bool validateGlobally = false)
        {
            if (string.IsNullOrEmpty(type))
                return ServiceResponse.Error("You must select a type.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (!CurrentUser.SiteAdmin)
                return ServiceResponse.Error("You are not authorized this action.");

            StringBuilder statusMessage = new StringBuilder();

            try
            {
                if (!Request.Content.IsMimeMultipartContent())
                {                // Check if the request contains multipart/form-data.
                    return ServiceResponse.Error("Unsupported media type.");
                }

                string root = HttpContext.Current.Server.MapPath("~/App_Data/temp");
                var provider = new MultipartFormDataStreamProvider(root);

                // This is a work around for max content length, so you don't have to put it in the web.config!
                var content = new StreamContent(HttpContext.Current.Request.GetBufferlessInputStream(true));
                foreach (var header in Request.Content.Headers)
                {
                    content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                await content.ReadAsMultipartAsync(provider);
                //End work around.
                // Request.Content.ReadAsMultipartAsync(provider); //<== if you remove the work around you'll need to add this back in.

                //  get the file names.
                foreach (MultipartFileData file in provider.FileData)
                {
                    Trace.WriteLine(file.Headers.ContentDisposition.FileName);
                    Trace.WriteLine("Server file path: " + file.LocalFileName);

                    AppManager app = new AppManager(Globals.DBConnectionKey, "web", this.GetAuthToken(Request));
                    string fileName = file.Headers.ContentDisposition.FileName;

                    // Clean the file name..
                    foreach (var c in Path.GetInvalidFileNameChars()) { fileName = fileName.Replace(c, ' '); }

                    if (string.IsNullOrWhiteSpace(fileName))
                        continue;

                    fileName = fileName.ToUpper();
                    type = type.ToUpper();
                    if (!fileName.Contains(type))
                    {   //this is to keep the user from selecting a Product type and the selecting a user file etc.
                        //The file has to match the type.
                        return ServiceResponse.Error("File does not match the type selected.");
                    }

                    statusMessage.AppendLine(app.ImportFile(type, file.LocalFileName, fileName, network.GetClientIpAddress(this.Request), validate, validateGlobally).Message);
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error(ex.Message);
            }

            return ServiceResponse.OK(statusMessage.ToString());
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Tools/Import/{type}")]
        public ServiceResult ImportType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return ServiceResponse.Error("No file sent.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (!CurrentUser.SiteAdmin)
                return ServiceResponse.Error("You are not authorized this action.");

            AppManager app = new AppManager(Globals.DBConnectionKey, "web", this.GetAuthToken(Request));

            return app.ImportData(type);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Tools/Database/Restore")]
        public async Task<ServiceResult> RestoreDatabaseAsync()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (!CurrentUser.SiteAdmin)
                return ServiceResponse.Error("You are not authorized this action.");

            ServiceResult res;
            try
            {
                string body = await Request.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("You must send valid email info.");

                dynamic formData = JsonConvert.DeserializeObject<dynamic>(body);

                if (formData == null || formData[0] == null)
                    return ServiceResponse.Error("Invalid info.");

                string file = formData[0].FileName;

                AppManager app = new AppManager(Globals.DBConnectionKey, "web", this.GetAuthToken(Request));
                res = await app.RestoreDatabase(file, Globals.Application.AppSetting("DBBackupKey"));

                ClearTempFiles();
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error(ex.Message);
            }
            return res;
        }

        //This scans names in a table for duplicates
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/App/Tables/ScanNames/{table}")]
        public ServiceResult ScanNames(string table)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (!CurrentUser.SiteAdmin)
                return ServiceResponse.Error("You are not authorized to use this function.");
            string processId = Guid.NewGuid().ToString();

            AppManager app = new AppManager(Globals.DBConnectionKey, "web", this.GetAuthToken(Request));
            return app.ScanForDuplicates(table, processId);
        }

        //This scans names in a table for duplicates
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/App/Tables/Search/{value}")]
        public ServiceResult SearchTables(string value)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (!CurrentUser.SiteAdmin)
                return ServiceResponse.Error("You are not authorized to use this function.");

            Task<string> content = Request.Content.ReadAsStringAsync();
            if (content == null)
                return ServiceResponse.Error("No users were sent.");

            string body = content.Result;

            if (string.IsNullOrEmpty(body))
                return ServiceResponse.Error("No users were sent.");

            List<string> values = JsonConvert.DeserializeObject<List<string>>(body);
            AppManager app = new AppManager(Globals.DBConnectionKey, "web", this.GetAuthToken(Request));
            return app.SearchTables(values.ToArray());
        }

        // static readonly string[] Columns = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "AA", "AB", "AC", "AD", "AE", "AF", "AG", "AH", "AI", "AJ", "AK", "AL", "AM", "AN", "AO", "AP", "AQ", "AR", "AS", "AT", "AU", "AV", "AW", "AX", "AY", "AZ", "BA", "BB", "BC", "BD", "BE", "BF", "BG", "BH" };
        //private string GetSortableName(int accountIndex)
        //{
        //    string result = string.Empty;
        //    while (--accountIndex >= 0)
        //    {
        //        result = (char)('A' + accountIndex % 26) + result;
        //        accountIndex /= 26;
        //    }
        //    return result;
        //}

        private List<string> names = new List<string>();
        static readonly string[] defaultName = new[] { "A", "A", "A", "A", "A", "A", "A", "A", "A", "A", "A", "A" };
        static readonly string[] alphabet = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

        //private string BuildNamesArray(int total )
        //{

        //    int namePrefixSize = total / 26; //how long the prefi should be AAA... AAAB....
        //    for (int i = 0; i < total; i++)
        //    {
        //        string name = "";
        //        for (int j = namePrefixSize; j > namePrefixSize; j--)
        //        {
        //            defaultName = 

        //        }
        //        names.Add(name);
        //    }


        //    //if (accountIndex <= 0)
        //    //    return string.Join("",   defaultName );


        //    //// accountIndex = 1 - 26 replace first letter
        //    //// update from end to start
        //    //// accountIndex = 27 - 53 replace last and second to last


        //    //int prefixUpdateCount  = ( accountIndex / 26) + 1;


        //    //for (int i = 0; i < prefixUpdateCount; i++) {
        //    //    int nameIndex = 11 - i;// should start at the end and move towards fron big to small
        //    //    int alphabetIndex = ( accountIndex  + (25- prefixUpdateCount )) % 26;
        //    //    defaultName[nameIndex] = alphabet[alphabetIndex];
        //    //}

        //    //return string.Join("", defaultName);

        //}

        // 314 Accounts
        // 26 letters in aplhabet
        // 314/26 = 12 of letter columns




        [System.Web.Http.HttpGet]
    //    [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.AllowAnonymous]
        // [EnableThrottling(PerSecond = 2, PerDay = 500)]
        [System.Web.Http.Route("api/Tools/TestCode")]
        public ServiceResult Test()
        {
            #region todo run in prod
            //using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
            //{
            //    var accounts = context.GetAll<Account>().Where(w => w.Latitude == null).OrderBy(o => o.Name).ToList();
            //    foreach (var account in accounts)
            //    {
            //        if (!string.IsNullOrWhiteSpace(account.Image) && !account.Image.Contains(".tmb"))
            //        {
            //            try
            //            {
            //                FileInfo fi = new FileInfo(account.Image);
            //                account.Image = account.Image.Replace(fi.Extension, ".tmb" + fi.Extension);
            //            }
            //            catch
            //            {
            //              var Extension =    account.Image.GetFileExtensionFromUrl();
            //                if(!string.IsNullOrWhiteSpace(Extension))
            //                    account.Image = account.Image.Replace(Extension, ".tmb" + Extension);

            //            }
            //        }


            //        var location = context.GetAll<Location>().FirstOrDefault(w => w.UUID == account.LocationUUID && w.Latitude != null);
            //        if (location == null || location.Latitude == null)
            //        {
            //            context.Update<Account>(account);
            //            continue;
            //        }
            //        account.Latitude = location.Latitude;
            //        account.Longitude = location.Longitude;

            //        context.Update<Account>(account);
            //    }

            //    var events = context.GetAll<Event>().Where(w => w.Latitude == null).OrderBy(o => o.Name).ToList();
            //    foreach (var e in events) {

            //        if (!string.IsNullOrWhiteSpace(e.Image) && !e.Image.Contains(".tmb"))
            //        {
            //            try
            //            {
            //                FileInfo fi = new FileInfo(e.Image);
            //                e.Image = e.Image.Replace(fi.Extension, ".tmb" + fi.Extension);
            //            }
            //            catch
            //            {
            //                var Extension = e.Image.GetFileExtensionFromUrl();
            //                if (!string.IsNullOrWhiteSpace(Extension))
            //                    e.Image = e.Image.Replace(Extension, ".tmb" + Extension);
            //            }
            //        }
            //        e.Category = e.Category.Trim();

            //        var el = context.GetAll<EventLocation>().FirstOrDefault(w => w.EventUUID ==e.UUID && w.Latitude != null ); //  e.UUID = l.EventUUID
            //        if (el == null || el.Latitude == null)
            //        {
            //            context.Update<Event>(e);
            //            continue;
            //        }
            //        e.Latitude = el.Latitude;
            //        e.Longitude = el.Longitude;
            //        context.Update<Event>(e);
            //    }

            //}
            //return ServiceResponse.OK();
            #endregion


            // create spanish entries for testing
            //using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
            //{
            //    var events = context.GetAll<Event>().Where(w => w.UUParentIDType?.ToLower() == "esp").ToList();
            //    foreach (var e in events)
            //    {
            //        e.Name = e.Name.Replace("ESP ", "") + " - ESP";
            //        context.Update<Event>(e);
            //    }
            //}
            return ServiceResponse.OK();


             
                //  AffiliateManager affm = new AffiliateManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                //  var user = new User()
                //  {
                //      UUID = Guid.NewGuid().ToString("N"),
                //      Name = DateTime.Now.ToString(),
                //      Password = "password", //PasswordHash.ExtractHashPassword(tmpHashPassword),
                //      DateCreated = DateTime.UtcNow,
                //      Deleted = false,
                //      //PasswordSalt = PasswordHash.ExtractSalt(tmpHashPassword),PasswordHashIterations = PasswordHash.ExtractIterations(tmpHashPassword),
                //      Email = Guid.NewGuid().ToString("N") + "@test.com",
                //      SiteAdmin = false,
                //      Approved = true,
                //      AccountUUID = SystemFlag.Default.Account
                //  };
                //return   affm.RegisterAffiliate_WordPress(user);


                // MySqlTest();
                // UpdateUsersInRolesNames();
                //this.ImportClubs();

                //using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
                //{
                //    Random r = new Random(DateTime.Now.Millisecond);
                //    var events = context.GetAll<Event>();

                //    foreach (var e in events)
                //    {
                //        e.Latitude = Convert.ToDouble("33." + r.Next(3000000, 369998931).ToString());
                //        e.Longitude = Convert.ToDouble("-112." + r.Next(30000000, 379997253).ToString());
                //        context.Update<Event>(e);
                //    }
                //}

                #region old code

                //EventManager EventManager = new EventManager(Globals.DBConnectionKey, Request.Headers?.Authorization?.Parameter);
                //var events = EventManager.GetAllEventsByDistance();

                // using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
                //{
                //    int i = 1;
                ////    var events = context.GetAll<Event>().OrderBy( o => o.StartDate).ToList();
                //   foreach (var evt in events)
                // {
                ////       // if (evt.StartDate < DateTime.Now)
                ////      //  {
                ////            //evt.StartDate = DateTime.Now.AddDays(new Random(i).Next(5, 465));
                ////            //evt.EndDate = evt.StartDate.AddDays(new Random(i).Next(1, 7));
                ////            //evt.EventDateTime = evt.StartDate;
                ////       // }
                ////       // var names = evt.Name.Split("--");1
                ////       // evt.Name = names[names.Length - 1];

                //         evt.Name = "D:" + i.ToString() + " *** " + evt.Name;

                //        context.Update<Event>(evt);
                //         i++;
                //   }
                //}
                //todo reimplement
                //if (CurrentUser == null || CurrentUser.Name.EqualsIgnoreCase("gatoslocos") == false ||
                //    CurrentUser.Name.EqualsIgnoreCase("plato") == false)
                //    return ServiceResponse.Unauthorized();

                //using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
                //{
                //    var roles = context.GetAll<Role>();
                //    foreach (var r in roles)
                //    {
                //        r.RoleWeight = r.Weight;
                //        context.Update<Role>(r);
                //    }
                //}

                //    update the lat and lon for the types below
                //           --account
                //   -- event
                //-- profile
                //    using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
                //{
                //    var accounts = context.GetAll<Account>().ToList();
                //    foreach (var account in accounts)
                //    {
                //       var loc =  context.GetAll<Location>(null).FirstOrDefault(w => w.UUID == account.LocationUUID);
                //        if (loc == null)
                //            continue;

                //        account.Latitude = loc.Latitude;
                //        account.Longitude = loc.Longitude;
                //        context.Update<Account>(account);
                //    }

                //    var events = context.GetAll<Event>().ToList();
                //    foreach (var evt in events)
                //    {
                //        var loc = context.GetAll<EventLocation>().FirstOrDefault(w => w.EventUUID == evt.UUID);
                //        if (loc == null)
                //            continue;

                //        evt.HostName = context.GetAll<Account>().FirstOrDefault(w => w.UUID == evt.HostAccountUUID)?.Name;
                //        if(!string.IsNullOrWhiteSpace(evt.HostName))
                //            evt.Name = evt.Name + " - " + evt.HostName;

                //        evt.Longitude = loc.Longitude;
                //        evt.Latitude = loc.Latitude;
                //        context.Update<Event>(evt);
                //    }

                //    var profiles = context.GetAll<Profile>();
                //    foreach (var prof in profiles)
                //    {
                //        var loc = context.GetAll<Location>().FirstOrDefault(w => w.UUID == prof.LocationUUID);
                //        if (loc == null)
                //            continue;

                //        prof.Latitude = loc.Latitude;
                //        prof.Longitude = loc.Longitude;

                //    }
                //}

                #endregion old code

                #region geoip UPDATE code

                //    string directory = EnvironmentEx.AppDataFolder;
                //string root = EnvironmentEx.AppDataFolder;
                //string pathToFile = "";
                //pathToFile = Path.Combine(root, "geoip\\blocks.csv");

                //string[] blockLines = File.ReadAllLines(pathToFile);

                //pathToFile = Path.Combine(root, "geoip\\location.csv");

                //string[] locationLines = File.ReadAllLines(pathToFile);

                //Dictionary<int, GeoIp> locations = new Dictionary<int, GeoIp>();

                //int index = 0;
                //foreach (string block in blockLines)
                //{
                //    if (index == 0)
                //    {
                //        index++;
                //        continue; //skip headers.
                //    }
                //    string[] tokens = block.Split(',');
                //    int locId = tokens[2].ConvertTo<int>();

                //    if (locations.ContainsKey(locId))
                //        continue;

                //    GeoIp gip = new GeoIp();
                //    gip.LocationId = locId;
                //    gip.StartIpNum = tokens[0];
                //    gip.EndIpNum = tokens[1];
                //    locations.Add(locId, gip);
                //    index++;
                //}

                //index = 0;
                //foreach (string location in locationLines)
                //{
                //    if (index == 0)
                //    {
                //        index++;
                //        continue; //skip headers.
                //    }

                //    string[] tokens = location.Split(',');
                //    int locId = tokens[0].ConvertTo<int>();

                //    //if (!locations.ContainsKey(locId))
                //    //    continue;

                //    //locations[locId].Country = tokens[1];
                //    //locations[locId].Region = tokens[2];
                //    //locations[locId].City = tokens[3];
                //    //locations[locId].PostalCode = tokens[4];
                //    //locations[locId].Latitude = tokens[5];
                //    //locations[locId].Longitude = tokens[6];
                //    //locations[locId].MetroCode = tokens[7];
                //    //locations[locId].AreaCode = tokens[8];

                //    double lat = tokens[5].ConvertTo<double>();
                //    double lon = tokens[6].ConvertTo<double>();

                //    // Update IP addresses for matching coordinates
                //    using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
                //    {
                //        var coordLocations = context.GetAll<Location>().Where(w =>  ( w.Latitude != 0 && w.Longitude != 0 ) &&
                //                                                                    w.Latitude ==lat &&
                //                                                                    w.Longitude == lon);
                //        foreach (var coordLoc in coordLocations)
                //        {
                //            coordLoc.IpNumStart = locations[locId].StartIpNum.Trim();
                //            coordLoc.IpNumEnd = locations[locId].EndIpNum.Trim();

                //            if (coordLoc.RootId == 0 && coordLoc.LocationType.EqualsIgnoreCase("city"))
                //            {
                //                coordLoc.LocationType = "country";
                //                if (coordLoc.Country.Length == 2)
                //                {
                //                    coordLoc.Abbr = coordLoc.Country;
                //                    var cName = context.GetAll<Location>().FirstOrDefault(w => w.Abbr.EqualsIgnoreCase(coordLoc.Country) && w.Country.Length > 2)?.Country;

                //                    if (!string.IsNullOrWhiteSpace(cName))
                //                    {
                //                        coordLoc.Country = cName;
                //                        coordLoc.Name = cName;
                //                    }
                //                }
                //            }
                //            context.Update<Location>(coordLoc);
                //        }
                //    }
                //    /*

                //    // bool addCoordinate = false;

                //    Location loc = new Location();
                //    if (!string.IsNullOrWhiteSpace(locations[locId].City) && locations[locId].City.Contains("�"))
                //    {
                //        loc = findCity(locations[locId].Country, locations[locId].City);

                //        if (loc == null)
                //            continue;//addCoordinate = true;
                //    }
                //    else if (!string.IsNullOrWhiteSpace(locations[locId].City))
                //    {//regular city name (no accent in characters)
                //        using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
                //        {
                //            loc = context.GetAll<Location>()?.FirstOrDefault(w => w.Abbr.EqualsIgnoreCase(locations[locId].Country) && w.Name.EqualsIgnoreCase(locations[locId].City));
                //            if (loc == null)
                //                continue;// addCoordinate = true;
                //        }
                //    }
                //    else //just the country
                //    {
                //        using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
                //        {
                //            loc = context.GetAll<Location>()?.FirstOrDefault(w => w.Abbr.EqualsIgnoreCase(locations[locId].Country));
                //            if (loc == null)
                //                continue;//addCoordinate = true;
                //        }
                //    }

                //  // sync loc fields and update

                //    loc.IpNumStart = string.IsNullOrWhiteSpace(loc.IpNumStart)  ? locations[locId].StartIpNum : loc.IpNumStart;
                //    loc.IpNumEnd = string.IsNullOrWhiteSpace(loc.IpNumEnd)? locations[locId].EndIpNum : loc.IpNumEnd;

                //    using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
                //    {
                //        context.Update<Location>(loc);
                //    }
                //    */

                //}

                #endregion geoip UPDATE code

                #region old code

                //////////////////////////////////////////////////////////////////
                //AccountManager accountManager = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

                ////var accounts = accountManager.GetAllAccounts();
                ////foreach (var account in accounts)

                //var res = accountManager.Get("c66185a39e1c475a93ba5b053ae31d23");
                //if(res.Code == 200)
                //   return  accountManager.CreateDefaultRolesForAccount((Account)res.Result);

                //return res;
                ///////////////////////////////////////////////////////
                // this works
                //Task.Run(async () =>
                //{
                //    UserManager m = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                //User u = new User()
                //{
                //    Name = "derp alpha 5.14",
                //    /////  ProviderUserKey = Cipher.RandomString(12),
                //    Email = "stephen.osterhoudt@gmail.com" // ConfigurationManager.AppSettings["SiteEmail"].ToString()
                //};
                //EmailSettings settings = new EmailSettings();
                //settings.EncryptionKey = ConfigurationManager.AppSettings["AppKey"];
                //settings.MailHost = ConfigurationManager.AppSettings["MailHost"];
                //settings.MailPort = StringEx.ConvertTo<int>(ConfigurationManager.AppSettings["MailPort"]);
                //settings.HostUser = ConfigurationManager.AppSettings["EmailHostUser"];
                //settings.HostPassword =  ConfigurationManager.AppSettings["EmailHostPassword"];
                //settings.SiteEmail = ConfigurationManager.AppSettings["SiteEmail"];
                //settings.UseSSL = StringEx.ConvertTo<bool>(ConfigurationManager.AppSettings["UseSSL"]);
                //settings.SiteDomain = ConfigurationManager.AppSettings["SiteDomain"];
                //ServiceResult res = await m.SendUserInfoAsync(u, "127.1.1.3", settings);

                //}).GetAwaiter().GetResult();

                //return ServiceResponse.OK();

                //string authToken = this.GetAuthToken(Request);

                //LocationManager locationManager = new LocationManager(Globals.DBConnectionKey, authToken);

                //string directory = EnvironmentEx.AppDataFolder;
                //string root = EnvironmentEx.AppDataFolder;
                //string pathToFile = "";
                //int index = 0;

                // pathToFile = Path.Combine(root, "WordLists\\worldcities.csv");

                #endregion old code

                #region worldcities.csv processing

                //pathToFile = Path.Combine(root, "WordLists\\worldcities.csv");
                //IEnumerable<string> geoLocations = pathToFile.ReadAsLines();

                //var data = new DataTable();

                ////this assume the first record is filled with the column names
                ////city	city_ascii	lat	lng	country	iso2	iso3	admin_name	capital	population	id
                //var headers = geoLocations.First().Split(',');
                //foreach (var header in headers)
                //    data.Columns.Add(header);

                //var records = geoLocations.Skip(1);//skip past the header.
                //foreach (var record in records)
                //    data.Rows.Add(record.Split(','));

                // remove duplicates
                //var dtCountries = data.AsEnumerable().GroupBy(row => row.Field<string>("country")).Select(group => group.First()).CopyToDataTable();

                // var dtCountries = from row in data.AsEnumerable() select row;

                //todo reinstate after state and country stuff is done
                //var dtCountries = locationManager.GetAll().Where(w => w.LocationType.EqualsIgnoreCase("country")
                //        && ( w.Country.EqualsIgnoreCase("NULL") ||
                //             string.IsNullOrWhiteSpace(w.Country))
                //        );

                //still has 71 rows
                //var dtCountries = locationManager.GetAll().Where(w => w.LocationType.EqualsIgnoreCase("state")
                //       && (w.Country.EqualsIgnoreCase("NULL") ||
                //            string.IsNullOrWhiteSpace(w.State))
                //       );

                //var dtCountries = locationManager.GetAll().Where(w => w.LocationType.EqualsIgnoreCase("city")
                //      && (w.City.EqualsIgnoreCase("NULL") ||
                //           string.IsNullOrWhiteSpace(w.City))
                //      );

                //if (dtCountries == null)
                //{ // || dtResult.Count() == 0
                //    Debug.WriteLine("No countries.");
                //    return ServiceResponse.Error("no countries.");
                //}
                //List<string> missingCountries = new List<string>();
                //List<string> missingCities = new List<string>();

                //Debug.WriteLine("Count:" + dtCountries.Count());
                // index = 0;
                //string currentCountryName = "";
                ////  foreach (DataRow rowCountry in dtCountries.Rows)
                //foreach (var rowData in dtCountries)
                //{
                //    Debug.WriteLine("index:" + index);
                //    index++;
                //    string dtCountryName = rowData.Name; // rowData["country"].ToString();

                //    if (currentCountryName == dtCountryName)
                //        continue;

                //    currentCountryName = dtCountryName;
                //    var dbCountry = locationManager.GetAll().FirstOrDefault(w => w.Name.EqualsIgnoreCase(currentCountryName));

                //    if (dbCountry == null)
                //    {
                //        Debug.WriteLine("missingCountries:" + currentCountryName);
                //        missingCountries.Add(currentCountryName);
                //        continue;
                //    }

                //    dbCountry.Country =   dbCountry.Name;
                //    if (dbCountry.LocationType.EqualsIgnoreCase("country"))
                //    {
                //        dbCountry.City = "";
                //        dbCountry.State = "";
                //    }

                //    rowData.City = dtCountryName;
                //    // rowData.State = dtCountryName; still has 71 rows

                //    using (var contextA = new GreenWerxDbContext(Globals.DBConnectionKey))
                //    {
                //        //still has 71 rows var country = contextA.GetAll<Location>().FirstOrDefault(w => w.UUID == rowData.UUParentID && w.LocationType.EqualsIgnoreCase("country"));
                //        //still has 71 rowsrowData.Country = country?.Country;

                //        Location state = new Location();

                //        if (string.IsNullOrWhiteSpace(rowData.State) || rowData.State.EqualsIgnoreCase("NULL"))
                //        {
                //            state = contextA.GetAll<Location>().FirstOrDefault(w => w.UUID == rowData.UUParentID && w.LocationType.EqualsIgnoreCase("state"));
                //            rowData.State = state?.State;
                //        }

                //        if (state !=null && ( string.IsNullOrWhiteSpace(rowData.Country) || rowData.Country.EqualsIgnoreCase("NULL")))
                //        {
                //            var country = contextA.GetAll<Location>().FirstOrDefault(w => w.UUID == state.UUParentID && w.LocationType.EqualsIgnoreCase("country"));
                //            rowData.Country = country?.Country;
                //        }

                //        Debug.WriteLine("- Updating city:" + rowData.Country + "-:-" + rowData.State + "-:-" + rowData.City );

                //        locationManager.Update(rowData);

                //    //Debug.WriteLine("- Updating country:" + currentCountryName);
                //    //  contextA.Update<Location>(dbCountry);

                //    //    var dbStates = contextA.GetAll<Location>().Where(w => w.UUParentID == dbCountry.UUID);
                //    //    foreach (var dbState in dbStates)
                //    //    {
                //    //        dbState.Country = currentCountryName;
                //    //        dbState.State = dbState.Name;
                //    //        Debug.WriteLine("-- Updating state:" + dbState.Name);
                //    //        contextA.Update<Location>(dbState);

                //    //        var dbCities = contextA.GetAll<Location>().Where(w => w.UUParentID == dbState.UUID);
                //    //        foreach (var dbCity in dbCities)
                //    //        {
                //    //            dbCity.City = dbCity.Name;
                //    //            dbCity.State = dbState.Name;
                //    //            dbCity.Country = currentCountryName;
                //    //            Debug.WriteLine("--- Updating city:" + dbCity.Name);
                //    //            contextA.Update<Location>(dbCity);
                //    //        }

                //    //    }
                //}

                //    //============= lat lng processing
                //    //        // city_ascii,lat,lng
                //    //        // get where country matches and name matches city name
                //    //        // then get parent of city where LocationType == state
                //    //        string cityName = rowData["city_ascii"].ToString();

                //    //    using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))  {
                //    //        var cities = context.GetAll<Location>().Where(w =>
                //    //        ( w.Name.EqualsIgnoreCase(cityName)  || w.City.EqualsIgnoreCase(cityName) )
                //    //        &&
                //    //        (w.Country.EqualsIgnoreCase(currentCountryName) ||
                //    //         w.UUParentID ==   dbCountry.UUID));

                //    //        if (cities == null || cities.Count() ==false)
                //    //        {
                //    //            Debug.WriteLine("-------------------- no cities found for country:" + currentCountryName);
                //    //            continue;
                //    //        }

                //    //        foreach (var city in cities)
                //    //        {
                //    //            var state = context.GetAll<Location>().FirstOrDefault(w => w.UUID == city.UUParentID && w.LocationType.EqualsIgnoreCase("state"));

                //    //            city.City = cityName;
                //    //            city.State = state?.Name;
                //    //            city.Country = currentCountryName;

                //    //            if (city.Latitude == 0 && city.Longitude == 0)
                //    //            {
                //    //                bool converted = false;
                //    //                try
                //    //                {
                //    //                    city.Latitude = rowData["lat"].ToString().ConvertTo<double>(out converted);
                //    //                    city.Longitude = rowData["lng"].ToString().ConvertTo<double>(out converted);
                //    //                }
                //    //                catch
                //    //                {
                //    //                    city.Latitude = rowData["lat"].ToString().ConvertTo<float>(out converted);
                //    //                    city.Longitude = rowData["lng"].ToString().ConvertTo<float>(out converted);
                //    //                }
                //    //            }

                //    //            Debug.WriteLine("Updating city:" + city.Name);
                //    //            context.Update<Location>(city);
                //    //        }
                //    //    }
                //} //end foreach (var rowData in dtCountries)

                //return ServiceResponse.OK();

                #endregion worldcities.csv processing

                #region geo_spatial.csv file processing

                /*
                pathToFile = Path.Combine(root, "WordLists\\geo_spatial.csv");

                IEnumerable<string> geoLocations = pathToFile.ReadAsLines();

                var data = new DataTable();

                //this assume the first record is filled with the column names
                var headers = geoLocations.First().Split('\t');
                foreach (var header in headers)
                    data.Columns.Add(header);

                var records = geoLocations.Skip(1);
                foreach (var record in records)
                    data.Rows.Add(record.Split('\t'));

                //locationManager.GetCities

                using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
                {
                    var unitedStates = context.GetAll<Location>().FirstOrDefault(w => w.UUID == "6e9e15bbe8d44cd2a77b1bf9d51e9f40" && w.LocationType.EqualsIgnoreCase("country"));

                    var usStates = context.GetAll<Location>().Where(w => w.UUParentID == "6e9e15bbe8d44cd2a77b1bf9d51e9f40");
                    var stateCount = usStates.Count();

                    foreach (var usState in usStates)
                    {
                        Debug.WriteLine("usState :" + usState.Name);
                        var cities = context.GetAll<Location>().Where(w =>
                                    w.UUParentID == usState.UUID);
                        //var cities = context.GetAll<Location>().Where(w => w.LocationType.EqualsIgnoreCase("city") &&
                        //   (string.IsNullOrWhiteSpace(w.Country) ||
                        //    w.Country.EqualsIgnoreCase("NULL") ||
                        //    w.Country.EqualsIgnoreCase("US")
                        //   )
                        //);
                        var count = cities.Count();
                        foreach (var city in cities)
                        {
                            string stateAbbr = this.GetState(usState.Name); //returns abbr by default

                            if (string.IsNullOrWhiteSpace(stateAbbr))
                            {
                                Debug.WriteLine("continuing no result in datatable for :" + stateAbbr);
                                continue;
                            }

                            usState.Abbr = stateAbbr;

                            //NOTE state is abbreviated
                            // ZipCode	Type	PrimaryCity	AcceptableCities	UnacceptableCities	State	County	Timezone	AreaCodes
                            // Latitude	Longitude	WorldRegion	Country	Decommissioned	EstimatedPopulation	Notes	SpatialData
                            var dtResult = from row in data.AsEnumerable()
                                           where row.Field<string>("PrimaryCity").EqualsIgnoreCase(city.Name)
                                                 && row.Field<string>("State").EqualsIgnoreCase(stateAbbr)
                                           select row;

                            if (dtResult == null)
                            { // || dtResult.Count() == 0
                                if (!string.IsNullOrWhiteSpace(usState.Abbr))
                                    context.Update<Location>(usState);
                                Debug.WriteLine("continuing no result in datatable for city name and state abbr :" + city.Name + stateAbbr);
                                continue;
                            }
                            Debug.WriteLine("processing city details..");
                            //// or if does not work
                            //myData = tableRow.Field<string>(table.Columns.IndexOf(myColumn));

                            if (string.IsNullOrWhiteSpace(city.Country) || city.Country.EqualsIgnoreCase("NULL"))
                                city.Country = dtResult.FirstOrDefault()?.Field<string>("Country");

                            if (string.IsNullOrWhiteSpace(city.City) || city.City.EqualsIgnoreCase("NULL"))
                                city.City = dtResult.FirstOrDefault()?.Field<string>("PrimaryCity");

                            if (string.IsNullOrWhiteSpace(city.State) || city.State.EqualsIgnoreCase("NULL"))
                                city.State = usState.Name;

                            if (string.IsNullOrWhiteSpace(city.County) || city.County.EqualsIgnoreCase("NULL"))
                                city.County = dtResult.FirstOrDefault()?.Field<string>("County");

                            if (string.IsNullOrWhiteSpace(city.TimeZone) || city.TimeZone.EqualsIgnoreCase("NULL") || city.TimeZone == "0" )
                                city.TimeZone = dtResult.FirstOrDefault()?.Field<string>("Timezone");
                            if (city.Latitude == 0 && city.Longitude == 0)
                            {
                                try
                                {
                                    bool converted = false;
                                    city.Latitude = dtResult.FirstOrDefault()?.Field<string>("Latitude").ConvertTo<double>(out converted);
                                    city.Longitude = dtResult.FirstOrDefault()?.Field<string>("Longitude").ConvertTo<double>(out converted);
                                }
                                catch
                                {
                                    city.Latitude = dtResult.FirstOrDefault()?.Field<float>("Latitude");
                                    city.Longitude = dtResult.FirstOrDefault()?.Field<float>("Longitude");
                                }
                            }

                            Debug.WriteLine("Updating city:" + city.Name);
                            context.Update<Location>(city);
                            context.Update<Location>(usState);
                        }
                    }

                //var coordinates = context.GetAll<Location>().Where(w => w.LocationType == "coordinate");

                //foreach (var coordinate in coordinates)
                //{
                //    var dtPostalResult = from row in data.AsEnumerable()
                //                         where row.Field<string>("ZipCode").EqualsIgnoreCase(coordinate.Postal)
                //                         select row;

                //    if (string.IsNullOrWhiteSpace(coordinate.County) || coordinate.County.EqualsIgnoreCase("NULL"))
                //        coordinate.County =  dtPostalResult.FirstOrDefault()?.Field<string>("County");

                //    if (string.IsNullOrWhiteSpace(coordinate.TimeZone) || coordinate.TimeZone.EqualsIgnoreCase("NULL"))
                //        coordinate.TimeZone =  dtPostalResult.FirstOrDefault()?.Field<string>("Timezone");

                //    if (string.IsNullOrWhiteSpace(coordinate.City) || coordinate.City.EqualsIgnoreCase("NULL"))
                //        coordinate.City =  dtPostalResult.FirstOrDefault()?.Field<string>("PrimaryCity");

                //    var stateAbbr = dtPostalResult.FirstOrDefault()?.Field<string>("State");
                //    var stateName = this.GetState(stateAbbr, true);
                //    if(!string.IsNullOrWhiteSpace(stateName))
                //        coordinate.State =  stateName;

                //    if (string.IsNullOrWhiteSpace(coordinate.Country) || coordinate.Country.EqualsIgnoreCase("NULL"))
                //        coordinate.Country =  dtPostalResult.FirstOrDefault()?.Field<string>("Country");

                //    Console.WriteLine("Updating coordinate:" + coordinate.Name);
                //    context.Update<Location>(coordinate);
                //}
            }

              return ServiceResponse.OK();
                    */

                #endregion geo_spatial.csv file processing

                //PostalCodeManager pcm = new PostalCodeManager(Globals.DBConnectionKey, authToken);
                //pcm.ImportZipCodes(Path.Combine(root, "geoip\\zip.txt"));

                #region Country updates

                // pathToFile = Path.Combine(root, "geoip\\countryinfo.csv");

                // string[] countryLines = File.ReadAllLines(pathToFile);

                // foreach (string countryLine in countryLines)
                // {
                //     if (index == 0)
                //     {
                //         index++;
                //         continue; //skip headers.
                //     }

                //     string[] tokens = countryLine.Split(',');
                //     if (tokens.Length < 2)
                //         continue;

                //     string name = tokens[0];
                //     string code = tokens[1];

                //     using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
                //     {
                //        var location = context.GetAll<Location>()?.FirstOrDefault(w => w.LocationType == "country" && w.Name.EqualsIgnoreCase(name) && string.IsNullOrWhiteSpace(w.Abbr));
                //         if (location == null)
                //             continue;
                //         location.Abbr = code;

                //         context.Update<Location>(location);
                //     }
                //}

                #endregion Country updates

                #region US STates updates

                //pathToFile = Path.Combine(root, "geoip\\usstates.csv");

                //string[] stateLines = File.ReadAllLines(pathToFile);
                //foreach (string state in stateLines)
                //{
                //    string[] tokens = state.Split(',');
                //    if (tokens.Length < 2)
                //        continue;

                //    string name = tokens[0];
                //    string code = tokens[1];

                //    using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
                //    {
                //        var location = context.GetAll<Location>()?.FirstOrDefault(w => w.LocationType == "state" && w.UUParentID  == "6e9e15bbe8d44cd2a77b1bf9d51e9f40" && w.Name.EqualsIgnoreCase(name) && string.IsNullOrWhiteSpace(w.Abbr));
                //        if (location == null)
                //            continue;
                //        location.Abbr = code;

                //        context.Update<Location>(location);
                //    }
                //}

                #endregion US STates updates

                #region canada updates

                //pathToFile = Path.Combine(root, "geoip\\canada.csv");

                //string[] canadaLines = File.ReadAllLines(pathToFile);
                //foreach (string region in canadaLines)
                //{
                //    string[] tokens = region.Split(',');
                //    if (tokens.Length < 2)
                //        continue;

                //    string name = tokens[0];
                //    string code = tokens[1];

                //    using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
                //    {
                //        var location = context.GetAll<Location>()?.FirstOrDefault(w => w.LocationType == "state" && w.UUParentID == "a3f8adc547be4da9a73b000cb36a1b76" && w.Name.EqualsIgnoreCase(name) && string.IsNullOrWhiteSpace(w.Abbr));
                //        if (location == null)
                //            continue;
                //        location.Abbr = code;

                //        context.Update<Location>(location);
                //    }
                //}

                #endregion canada updates

                #region geoip code

                //pathToFile = Path.Combine(root, "geoip\\blocks.csv");

                //string[] blockLines = File.ReadAllLines(pathToFile);

                //pathToFile = Path.Combine(root, "geoip\\location.csv");

                //string[] locationLines = File.ReadAllLines(pathToFile);

                //Dictionary<int, GeoIp> locations = new Dictionary<int, GeoIp>();

                //index = 0;
                //foreach (string block in blockLines)
                //{
                //    if (index == 0)
                //    {
                //        index++;
                //        continue; //skip headers.
                //    }
                //    string[] tokens = block.Split(',');
                //    int locId = tokens[2].ConvertTo<int>();

                //    if (locations.ContainsKey(locId))
                //        continue;

                //    GeoIp gip = new GeoIp();
                //    gip.LocationId = locId;
                //    gip.StartIpNum = tokens[0].ConvertTo<float>();
                //    gip.EndIpNum = tokens[1].ConvertTo<float>();
                //    locations.Add(locId, gip);
                //    index++;
                //}

                //index = 0;
                //foreach (string location in locationLines)
                //{
                //    if (index == 0)
                //    {
                //        index++;
                //        continue; //skip headers.
                //    }

                //    string[] tokens = location.Split(',');
                //    int locId = tokens[0].ConvertTo<int>();

                //    if (!locations.ContainsKey(locId))
                //        continue;

                //    locations[locId].Country = tokens[1];
                //    locations[locId].Region = tokens[2];
                //    locations[locId].City = tokens[3];
                //    locations[locId].PostalCode = tokens[4];
                //    locations[locId].Latitude = tokens[5];
                //    locations[locId].Longitude = tokens[6];
                //    locations[locId].MetroCode = tokens[7];
                //    locations[locId].AreaCode = tokens[8];

                //    bool addCoordinate = false;

                //    Location loc = new Location();
                //    if (!string.IsNullOrWhiteSpace(locations[locId].City) && locations[locId].City.Contains("�"))
                //    {
                //        loc = findCity(locations[locId].Country, locations[locId].City);

                //        if (loc == null)
                //            addCoordinate = true;
                //    }
                //    else if (!string.IsNullOrWhiteSpace(locations[locId].City))
                //    {//regular city name (no accent in characters)
                //        using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
                //        {
                //            loc = context.GetAll<Location>()?.FirstOrDefault(w => w.Abbr.EqualsIgnoreCase(locations[locId].Country) && w.Name.EqualsIgnoreCase(locations[locId].City));
                //            if (loc == null)
                //                addCoordinate = true;
                //        }
                //    }
                //    else //just the country
                //    {
                //        using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
                //        {
                //            loc = context.GetAll<Location>()?.FirstOrDefault(w => w.Abbr.EqualsIgnoreCase(locations[locId].Country));
                //            if (loc == null)
                //                addCoordinate = true;
                //        }
                //    }

                //    if (addCoordinate)
                //    {
                //        loc = new Location();
                //        loc.Name = string.IsNullOrWhiteSpace(locations[locId].City) ? locations[locId].Country : locations[locId].City;
                //        loc.City = locations[locId].City;
                //        loc.Country = locations[locId].Country;
                //        loc.Postal = locations[locId].PostalCode;
                //        loc.Latitude = locations[locId].Latitude.ConvertTo<float>();
                //        loc.Longitude = locations[locId].Longitude.ConvertTo<float>();
                //        loc.LocationType = "coordinate";
                //        loc.RoleOperation = ">=";
                //        loc.RoleWeight =  RoleFlags.;
                //        loc.IpNumStart = locations[locId].StartIpNum;
                //        loc.IpNumEnd = locations[locId].EndIpNum;
                //        loc.DateCreated = DateTime.Now;
                //        loc.CreatedBy = SystemFlag.Default.Account;
                //        loc.AccountUUID = SystemFlag.Default.Account;
                //        loc.Active = true;

                //        using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
                //        { context.Insert<Location>(loc); }
                //    }
                //    else
                //    {   // sync loc fields and update
                //        //locId,country, latitude,longitude,metroCode,areaCode
                //        loc.City = string.IsNullOrWhiteSpace(loc.City) && !string.IsNullOrWhiteSpace(locations[locId].City) ? locations[locId].City : loc.City;
                //        loc.Postal = string.IsNullOrWhiteSpace(loc.Postal) && !string.IsNullOrWhiteSpace(locations[locId].PostalCode) ? locations[locId].PostalCode : loc.Postal;

                //        loc.Latitude = loc.Latitude == null ? locations[locId].Latitude.ConvertTo<float>() : loc.Latitude;
                //        loc.Longitude = loc.Longitude == null ?  locations[locId].Longitude.ConvertTo<float>() : loc.Longitude;
                //        loc.IpNumStart = loc.IpNumStart == null ? locations[locId].StartIpNum : loc.IpNumStart;
                //        loc.IpNumEnd = loc.IpNumEnd == null ? locations[locId].EndIpNum : loc.IpNumEnd;

                //        using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
                //        { context.Update<Location>(loc); }

                //        //if any of these don't match then insert a coordinates record.
                //        if (loc.Latitude    !=  locations[locId].Latitude.ConvertTo<float>() ||
                //            loc.Longitude   != locations[locId].Longitude.ConvertTo<float>() ||
                //            loc.IpNumStart  != locations[locId].StartIpNum ||
                //            loc.IpNumEnd    != locations[locId].EndIpNum)
                //        {
                //            loc.UUID = Guid.NewGuid().ToString("N");
                //            loc.Latitude = locations[locId].Latitude.ConvertTo<float>();
                //            loc.Longitude = locations[locId].Longitude.ConvertTo<float>();
                //            loc.IpNumStart = locations[locId].StartIpNum;
                //            loc.IpNumEnd = locations[locId].EndIpNum;
                //            loc.IpVersion = NetworkHelper.GetIpVersion(loc.IpNumStart.ToString());
                //            loc.LocationType = "coordinate";
                //            using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
                //            { context.Insert<Location>(loc); }

                //        }
                //    }
                //}

                #endregion geoip code

                #region test code

                ////AppManager am = new AppManager(Globals.DBConnectionKey, "web", authToken);
                ////am.TestCode();

                ////Globals.Application.UseDatabaseConfig = false;
                ////string encryptionKey = Globals.Application.AppSetting("AppKey");

                ////if (string.IsNullOrWhiteSpace(encryptionKey))
                ////    return ServiceResponse.Error("Unable to get AppKey from .config.");

                ////ServiceResult  res = Globals.Application.ImportWebConfigToDatabase(authToken, encryptionKey, true);
                ////if (res.Code != 200)
                ////    return res;
                ////if (PasswordHash.IsCommonPassword("maTurE"))
                ////    return ServiceResponse.Error();

                ////if (string.IsNullOrWhiteSpace(authToken))
                ////    return new ServiceResult() { Code = 500, Status = "ERROR", Message = "You must login to view this page." };

                ////User u = Get(authToken);

                //////Test inserting default roles
                ////RoleManager rm = new RoleManager(Globals.DBConnectionKey, u);
                ////ServiceResult res = rm.InsertDefaults(u.AccountUUID, "web");
                ////if (res.Code != 200)
                ////    return res;

                //////Test seeding the database
                ////  AppManager am = new AppManager(Globals.DBConnectionKey, "web", authToken);
                ////string directory = EnvironmentEx.AppDataFolder;
                //// am.SeedDatabase(Path.Combine(directory, "Install\\SeedData\\"), u.AccountUUID);

                #endregion test code

                #region location import

                ////LocationManager lm = new LocationManager(Globals.DBConnectionKey, authToken);
                ////string pathToFile = Path.Combine(directory, "DBBackups\\geolocations.csv");
                ////if (!File.Exists(pathToFile))
                ////    return ServiceResponse.Error("File not found");

                ////string[] fileLines = File.ReadAllLines(pathToFile);

                ////foreach (string fileLine in fileLines)
                ////{
                ////    if (string.IsNullOrWhiteSpace(fileLine))
                ////        continue;

                ////    string[] locationTokens = fileLine.Split(',');

                ////    if (locationTokens.Count() < 9)
                ////        continue;

                ////    int locationID = StringEx.ConvertTo<int>(locationTokens[0]);
                ////    Location l = new Location();
                ////    l.UUID = Guid.NewGuid().ToString("N");
                ////    l.UUIDType = "Location";
                ////    l.AccountUUID = SystemFlag.Default.Account;
                ////    l.DateCreated = DateTime.UtcNow;
                ////    l.CreatedBy = CurrentUser.UUID;
                ////    l.RoleWeight =  RoleFlags. ;
                ////    l.RoleOperation= ">=";
                ////    l.RootId = locationID;
                ////    l.ParentId = StringEx.ConvertTo<int>( locationTokens[1]);
                ////    l.Name = locationTokens[2];
                ////    l.Code = locationTokens[3];
                ////    l.LocationType = locationTokens[4];
                ////    l.Latitude = StringEx.ConvertTo<float>( locationTokens[5]);
                ////    l.Longitude = StringEx.ConvertTo<float>(locationTokens[6]);
                ////    l.TimeZone  = StringEx.ConvertTo<int>(locationTokens[7]);
                ////    l.CurrencyUUID = StringEx.ConvertTo<int>(locationTokens[8]);

                ////    if (lm.Insert(l, false).Code != 200)
                ////        Debug.Assert(false, "shit");
                ////}

                ////List<Location> locations = lm.GetLocations(SystemFlag.Default.Account);

                ////foreach(Location l in locations)
                ////{
                ////    List<Location> childLocations = locations.Where(w => w.ParentId == l.RootId).ToList();

                ////    foreach(Location child in childLocations)
                ////    {
                ////        child.UUParentID = l.UUID;
                ////        child.UUParentIDType = l.State;

                ////       if(  lm.Updatechild).Code != 200)
                ////            Debug.Assert(false, "shit 2");
                ////    }
                ////}

                #endregion location import

                #region Equipment test code

                ////EquipmentManager equipmentManager = new EquipmentManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

                ////List<dynamic> equipment = (List<dynamic>)equipmentManager.GetAll("BALLAST").Cast<dynamic>().ToList();

                ////equipment.AddRange((List<dynamic>)equipmentManager.GetAll("BULB").Cast<dynamic>().ToList());
                ////equipment.AddRange((List<dynamic>)equipmentManager.GetAll("CUSTOM").Cast<dynamic>().ToList());
                ////equipment.AddRange((List<dynamic>)equipmentManager.GetAll("FAN").Cast<dynamic>().ToList());
                ////equipment.AddRange((List<dynamic>)equipmentManager.GetAll("FILTER").Cast<dynamic>().ToList());
                ////equipment.AddRange((List<dynamic>)equipmentManager.GetAll("PUMP").Cast<dynamic>().ToList());
                ////equipment.AddRange((List<dynamic>)equipmentManager.GetAll("VEHICLE").Cast<dynamic>().ToList());

                ////foreach(dynamic d in equipment)
                ////{
                ////    d.AccountUUID = this.CurrentUser.AccountUUID;
                ////    d.CreatedBy = this.CurrentUser.UUID;
                ////    d.UUID = Guid.NewGuid().ToString("N");
                ////    equipmentManager.Update(d);

                ////}

                #endregion Equipment test code
            }

        private void ClearTempFiles()
        {
            string pathToBackupFolder = Path.Combine(EnvironmentEx.AppDataFolder, "DBBackups");
            string[] tempFiles = Directory.GetFiles(pathToBackupFolder, "*.tmp");
            foreach (string file in tempFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // No need to log this because the file could still be locked.
                }
            }
        }

        private TMG.Attribute GetNewAttribute(string name, string type, string refUUID)
        {
            var attribute = new TMG.Attribute()
            {
                ReferenceUUID = refUUID,
                ReferenceType = type,
                Value = name,
                ValueType = "string",
                Name = name,
                AccountUUID = this.CurrentUser.AccountUUID,
                DateCreated = DateTime.UtcNow,
                CreatedBy = CurrentUser.UUID,
                RoleOperation = ">=",
                RoleWeight = RoleFlags.MemberRoleWeights.Member,
                Private = false,
            };

            return attribute;
        }

        // returns abbreviation by default. pass true for returnName to go from abbreviation to state name
        private string GetState(string state, bool returnName = false)
        {
            if (string.IsNullOrWhiteSpace(state))
                return "";

            string stateNames = "Alabama,Alaska,Arizona,Arkansas,California,Colorado,Connecticut,Delaware,Florida," +
                                "Georgia,Hawaii,Idaho,Illinois,Indiana,Iowa,Kansas,Kentucky,Louisiana,Maine," +
                                "Maryland,Massachusetts,Michigan,Mississippi,Missouri,Minnesota,Montana,Nebraska," +
                                "Nevada,New Hampshire,New Jersey,New Mexico,New York,North Carolina,North Dakota," +
                                "Ohio,Oklahoma,Oregon,Pennsylvania,Rhode Island,South Carolina,South Dakota,Tennessee," +
                                "Texas,Utah,Vermont,Virginia,Washington,West Virginia,Wisconsin,Wyoming".ToUpper();
            string stateAbbr = "AL,AK,AZ,AR,CA,CO,CT,DE,FL,GA,HI,ID,IL,IN,IA,KS,KY,LA,ME,MD,MA,MI,MS,MO,MN,MT," +
                                "NE,NV,NH,NJ,NM,NY,NC,ND,OH,OK,OR,PA,RI,SC,SD,TN,TX,UT,VT,VA,WA,WV,WI,WY";
            List<string> states = stateNames.Split(',').ToList();
            List<string> abbreviations = stateAbbr.Split(',').ToList();

            if (returnName)
            {
                for (int i = 0; i < abbreviations.Count; i++)
                {
                    if (abbreviations[i].ToUpper() == state.ToUpper())
                        return states[i];
                }
            }

            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].ToUpper() == state.ToUpper())
                    return abbreviations[i];
            }

            return "";
        }

        private void ImportClubs()
        {
            StageDataManager sdm = new StageDataManager(Globals.DBConnectionKey, "");
            string directory = EnvironmentEx.AppDataFolder;
            string root = EnvironmentEx.AppDataFolder;
            string pathToFile = "";
            pathToFile = Path.Combine(root, "install\\SeedData\\clubs.csv");

            string[] lines = File.ReadAllLines(pathToFile);

            int clubBlockLines = 6;
            LocationManager locations = new LocationManager(Globals.DBConnectionKey, "");
            AccountManager am = new AccountManager(Globals.DBConnectionKey, "");
            Account account = new Account();
            account.UUID = Guid.NewGuid().ToString("N");
            int lineCount = 0;
            int insertCount = 0;
            // int index = 0;
            string chunk = "";
            List<string> chunks = new List<string>();
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    chunks.Add(chunk);
                    chunk = "";
                    continue;
                }
                chunk += line + "|";
            }

            AttributeManager atm = new AttributeManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            foreach (string tmpChunk in chunks)
            {
                if (tmpChunk.Contains("Local nuiqsut Swingers"))
                    insertCount = insertCount;

                string[] tmpLines = tmpChunk.Split("|");

                string[] tokens = tmpLines[0].Split("\t");
                string name = tokens[0].Trim();
                for (int j = 100; j >= 0; j--)
                {
                    if (name.Contains(j.ToString() + " Events"))
                        name = name.Replace(j.ToString() + " Events", "");
                    // events
                }

                name = name.Replace(" 1 Event", "");
                name = name.Replace(" Events", "");
                name = name.Replace("Swinger Club Partner", "");
                account.Name = name.Trim();

                //PARSE LOCATION
                if (tmpLines[1].Contains("("))
                    tokens = tmpLines[1].Split(" ");
                else
                    tokens = tmpLines[2].Split(" ");

                string cityName = tokens[2].Trim().Replace(",", "");
                string stateAbv = tokens[3].Trim();
                var state = locations.GetAll().FirstOrDefault(w => w.Abbr.EqualsIgnoreCase(stateAbv) && w.LocationType.EqualsIgnoreCase("state"));
                // note this will be a temporary location uuid until we get coordinates for it
                // then create a new location and set the type to Club etc
                if (state != null)
                {
                    string stateUUID = state.UUID;

                    var city = locations.GetAll().FirstOrDefault(w => w.Name.EqualsIgnoreCase(cityName) && w.UUParentID == stateUUID);
                    if (city != null)
                    {
                        account.LocationUUID = city.UUID;
                    }
                }
                //todo this is added last..
                //
                string attribute = tokens[4].Trim();
                string hasBar = "";
                if (tokens.Length >= 7)
                {
                    for (int b = 6; b < tokens.Length; b++)
                        hasBar += tokens[b].Trim() + " ";
                }
                //string tmp = tokens[4].Trim();
                //if (tmp.Contains("On-Premise")) {
                //    attribute = "On Premise";
                //    hasBar = tmp.Replace("On-Premise - ", "");
                //}
                //else {
                //    attribute = "Off Premise";
                //    hasBar = tmp.Replace("Off-Premise - ", "");
                //}

                var attA = this.GetNewAttribute(attribute, "Account", account.UUID);

                TMG.Attribute barAttribute = null;
                if (!string.IsNullOrWhiteSpace(hasBar))
                    barAttribute = this.GetNewAttribute(hasBar.Trim(), "Account", account.UUID);

                tokens = tmpLines[4].Split(" ");
                if (tokens.Length >= 3)
                {
                    account.WebSite = "https://" + tokens[0].Trim().Replace("WWW.", "").Replace("www.", ""); ; ;
                    account.Phone = tokens[1].Trim();
                    account.Email = tokens[2].Trim();
                    account.Phone = account.Phone.GetNumbers();
                    account.Phone = Regex.Replace(account.Phone, @"(\d{3})(\d{3})(\d{4})", "$1-$2-$3");
                }
                else
                {
                    account.WebSite = "https://" + tokens[0].Trim().Replace("WWW.", "").Replace("www.", "");
                    if (tokens.Length >= 2)
                        account.Email = tokens[1].Trim();
                }
                if (account.WebSite.EndsWith("/"))
                    account.WebSite = account.WebSite.Remove(account.WebSite.Length - 1);
                // am.Insert(account);

                #region add record

                string confidence = "3/3";
                using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
                {
                    string domain = account.WebSite.Replace("https://", "");

                    var match = context.GetAll<Account>().FirstOrDefault(w =>
                                            w.Name.Contains(account.Name, StringComparison.CurrentCultureIgnoreCase) &&
                                            w.WebSite.Contains(domain, StringComparison.CurrentCultureIgnoreCase) &&
                                            w.Email.Contains(account.Email, StringComparison.CurrentCultureIgnoreCase));
                    if (match == null)
                    {
                        match = context.GetAll<Account>().FirstOrDefault(w =>
                                                w.Name.Contains(account.Name, StringComparison.CurrentCultureIgnoreCase) ||
                                                w.WebSite.Contains(domain, StringComparison.CurrentCultureIgnoreCase) &&
                                                w.Email.Contains(account.Email, StringComparison.CurrentCultureIgnoreCase));
                        confidence = "2/3";
                    }

                    if (match == null)
                    {
                        match = context.GetAll<Account>().FirstOrDefault(w =>
                                                w.Name.Contains(account.Name, StringComparison.CurrentCultureIgnoreCase) ||
                                                w.WebSite.Contains(domain, StringComparison.CurrentCultureIgnoreCase) ||
                                                w.Email.Contains(account.Email, StringComparison.CurrentCultureIgnoreCase));
                        confidence = "1/3";
                    }

                    string localMatch = "";
                    if (match == null)
                        confidence = "0/3";
                    else
                        localMatch = JsonConvert.SerializeObject(match);

                    context.Insert<StageData>(new StageData()
                    {
                        DataType = "account",
                        DateParsed = DateTime.Now,
                        Domain = "clubs.csv",
                        LocalMatch = localMatch,
                        MatchConfidence = confidence,
                        NSFW = "-1",
                        StageResults = JsonConvert.SerializeObject(account),
                        UUID = Guid.NewGuid().ToString("N")
                    });

                    //Add any attributes
                    context.Insert<StageData>(new StageData()
                    {
                        DataType = "attribute",
                        DateParsed = DateTime.Now,
                        Domain = "clubs.csv",
                        NSFW = "-1",
                        StageResults = JsonConvert.SerializeObject(attA),
                        UUID = Guid.NewGuid().ToString("N")
                    });

                    if (barAttribute != null)
                    {
                        context.Insert<StageData>(new StageData()
                        {
                            DataType = "attribute",
                            DateParsed = DateTime.Now,
                            Domain = "clubs.csv",
                            NSFW = "-1",
                            StageResults = JsonConvert.SerializeObject(barAttribute),
                            UUID = Guid.NewGuid().ToString("N")
                        });
                    }

                    insertCount++;
                }

                #endregion add record

                account = new Account();
                account.UUID = Guid.NewGuid().ToString("N");
                lineCount = 1;
            }
        }

        private void ScanTableNames(string table, string processId)
        {
            AppManager app = new AppManager(Globals.DBConnectionKey, "web", this.GetAuthToken(Request));
            app.ScanForDuplicates(table, processId);
        }

        private void SendUsingAeNet()
        {
            //var EmailCount = 0;
            //try
            //{
            //    using (AE.Net.Mail.Pop3Client pop3 = new AE.Net.Mail.Pop3Client(_host, _hostUser, _password, _port, _useSsl))
            //    {
            //        EmailCount = pop3.GetMessageCount();
            //        for (var i = EmailCount - 1; i >= 0; i--)
            //        {
            //            MailMessage msg = null;
            //            try
            //            {
            //                msg = pop3.GetMessage(i);
            //                DamageReportEmail drdb = this.GetAll<DamageReportEmail>().Where(drmw => drmw.MessageID == msg.MessageID).FirstOrDefault();
            //                if (drdb != null)
            //                {
            //                    if (_logHD)
            //                        LogQueries.InsertInfo(string.Format("already have this message in mail cache:{0} ", msg.MessageID + " " + msg.Subject), "MailData", "LoadAndSaveMessagesFromPopServer");
            //                    continue;//already have this message so move on.
            //                }
            //            }
            //            catch (Exception ex)
            //            {
            //                Debug.Assert(false, ex.Message);
            //                LogQueries.InsertError(ex.Message, "MailData", "LoadAndSaveMessagesFromPopServer");
            //                continue;
            //            }
            //            if (msg.Raw.Contains("Damage Report") == false)//msg.Subject.Contains("Ingress Damage Report:") == false &&
            //            {
            //                if (_logHD)
            //                    LogQueries.InsertInfo(string.Format("Doesn't contain damag report:{0} ", msg.MessageID + " " + msg.Subject), "MailData", "LoadAndSaveMessagesFromPopServer");
            //                continue;
            //            }
            //            MessageCount++;
            //            if (_logHD)
            //                LogQueries.InsertInfo(string.Format("****MessageCount:{0} ", MessageCount), "MailData", "LoadAndSaveMessagesFromPopServer");
            //            DamageReportEmail dr = ConvertEmailToDamageReport(msg.Raw);//if coming from omniports.dr pass in the raw data. it has all the info.
            //            if (string.IsNullOrWhiteSpace(dr.EmailTo) == true && string.IsNullOrWhiteSpace(dr.From) == false && dr.From.Contains("omniports") == false && dr.From.Contains("ingress") == false)
            //                dr.EmailTo = dr.From;
            //            dr.HostUser = _hostUser;
            //            dr.MessageID = msg.MessageID;
            //            dr.Hash = Parser.GenerateKey(dr);
            //            dr.Parsed = false;
            //            if (_logHD)
            //                LogQueries.InsertInfo(string.Format("****Converted Email. DamageReport Hash:{0} ", dr.Hash), "MailData", "LoadAndSaveMessagesFromPopServer");
            //            _damageReportMessages.Add(dr);
            //            if (_hostUser == "omniports.dr@outlook.com")
            //            {
            //                try
            //                {
            //                    pop3.DeleteMessage(msg);
            //                    if (_logHD)
            //                        LogQueries.InsertInfo(string.Format("Deleted Message:{0} ", msg.MessageID + " " + msg.Subject), "MailData", "LoadAndSaveMessagesFromPopServer");
            //                }
            //                catch (Exception ex)
            //                {
            //                    string error = ex.Message;
            //                    if (ex.InnerException != null)
            //                        error += ex.InnerException;
            //                    LogQueries.InsertError(string.Format("host user: {0} error:{1}", _hostUser, error), "MailData", "LoadAndSaveMessagesFromPopServer.DeleteMessage");
            //                }
            //            }
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Debug.Assert(false, ex.Message);
            //    _logger.InsertError(Message, "SMTP", MethodInfo.GetCurrentMethod().Name);
            //}
        }

        //this updates the UsersInRoles.Name table with the roles.name using the roleUUID field. so the role name is in users in roles
        private void UpdateUsersInRolesNames()
        {
            using (var context = new GreenWerxDbContext(Globals.DBConnectionKey))
            {
                var userRoles = context.GetAll<UserRole>();

                foreach (var userRole in userRoles)
                {
                    var role = context.GetAll<Role>().FirstOrDefault(w => w.UUID == userRole.RoleUUID);
                    if (role == null)
                        continue;

                    userRole.Name = role.Name;
                    context.Update<UserRole>(userRole);
                }
            }
        }

        private void MySqlTest()
        {

           
            //var context = new MySqlDbContext("mysql");
            

            //    var users = context.GetAll<bswp_users>();
            //    if (users.Count() ==false)
            //        Debug.Assert(false, "SHOULD NOT FAIL");

            //    users.ElementAt(0).display_name = "UPDATED";// + DateTime.Now.ToString();

            //if (context.Update<bswp_users>(users.ElementAt(0)) == false)
            //    Debug.Assert(false, "FAILED SO UPDATE");

            //context.Insert<bswp_users>(new bswp_users() {
            //    display_name = DateTime.Today.ToString(),
            //    user_email = "user@test.com" + DateTime.Today.Second,
            //    user_login = "test",
            //    user_pass = "test",
            //    user_nicename = "",
            //    user_url = "",
            //    user_registered = DateTime.Now,
            //    user_activation_key = "",
            //    user_status = 0,
            //});

            //users = context.GetAll<bswp_users>();
            //var deleteMe = users.ElementAt(users.Count() - 1);

            //context.Delete<bswp_users>(deleteMe);


        }
    }
}