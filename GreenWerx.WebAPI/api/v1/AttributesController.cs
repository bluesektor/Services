// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Newtonsoft.Json;
using Omni.Base.Multimedia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using GreenWerx.Data.Logging;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Managers.Documents;
using GreenWerx.Managers.Equipment;
using GreenWerx.Managers.Events;
using GreenWerx.Managers.General;
using GreenWerx.Managers.Inventory;
using GreenWerx.Managers.Membership;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Events;
using GreenWerx.Models.Files;
using GreenWerx.Models.Flags;
using GreenWerx.Models.Inventory;
using GreenWerx.Models.Membership;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Web.Filters;
using WebApi.OutputCache.V2;
using TMG = GreenWerx.Models.General;
using GreenWerx.Managers.Store;
using GreenWerx.Models.Store;
using GreenWerx.Managers.Finance;
using GreenWerx.Models.Finance;

namespace GreenWerx.Web.api.v1
{
    public class AttributesController : ApiBaseController
    {
        private readonly SystemLogger _logger = null;

        public AttributesController()
        {
            _logger = new SystemLogger(Globals.DBConnectionKey);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpPatch]
        [System.Web.Http.Route("api/Attributes/Update/Bulk")]
        public ServiceResult BulkUpdate()
        {
            ServiceResult res = new ServiceResult();
            AttributeManager AttributeManager = new AttributeManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            try
            {
                Task<string> content = Request.Content.ReadAsStringAsync();
                if (content == null)
                    return ServiceResponse.Error("No users were sent.");

                string body = content.Result;

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("No users were sent.");

                List<TMG.Attribute> attributes = JsonConvert.DeserializeObject<List<TMG.Attribute>>(body);

          

                foreach (var att in attributes)
                {
                    if (att.Status == "new")
                        AttributeManager.Insert(att);
                    else
                        AttributeManager.Update(att); // todo more checks
                }

                return ServiceResponse.OK("", attributes);
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
                return ServiceResponse.Error(ex.Message);
            }
            
            
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Attributes/{UUID}")]
        public ServiceResult DeleteAttribute(string UUID)//todo bookmark latest test this.
        {
            if (string.IsNullOrWhiteSpace(UUID))
                return ServiceResponse.Error("No id was sent.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            AttributeManager atm = new AttributeManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = atm.Get(UUID);
            if (res.Code != 200)
                return res;

            var attribute = res.Result as TMG.Attribute;

            if (attribute.ValueType.EqualsIgnoreCase("ImagePath") == false)
            {
                return atm.Delete(attribute, true);
            }

            string root = System.Web.HttpContext.Current.Server.MapPath("~/Content/Uploads/" + this.CurrentUser.UUID);

            string fileName = attribute.Image.GetFileNameFromUrl(); //todo get folder and file from attribute.Image => https://localhost:44318/Content/Uploads/8ac0adc1e7154afda15069c822d68d6d/20190226_082504appicon.png
            string pathToFile = Path.Combine(root, fileName);
            DocumentManager dm = new DocumentManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            if (dm.DeleteFile(attribute, pathToFile).Code != 200)
                return ServiceResponse.Error("Failed to delete file " + fileName);
            DataFilter filter = this.GetFilter(Request);
            List<TMG.Attribute> attributes = atm.GetAttributes(this.CurrentUser.AccountUUID, ref filter)
                .Where(w => w.UUIDType.EqualsIgnoreCase("ImagePath") &&
                       w.Value == attribute.UUID &&
                       w.Image.Contains(fileName)).ToList();

            // Update attributes that are using this image.
            foreach (TMG.Attribute att in attributes)
            {
                // if (am.DeleteSetting(setting.UUID).Code != 200)
                //  return ServiceResponse.Error("Failed to delete image setting for file " + fileName);
                att.Image = "/assets/img/blankprofile.png"; // todo change image. Monetize?
            }

            var res1 = atm.Delete(attribute, true);

            ProfileManager profileManager = new ProfileManager(Globals.DBConnectionKey, Request.Headers.Authorization?.Parameter);
            var tmp = profileManager.GetProfile(CurrentUser.UUID, CurrentUser.AccountUUID, true);
            if (tmp.Code != 200)
                return tmp;
            GreenWerx.Models.Membership.Profile profile = (Profile)tmp.Result;

            if (profile.Image.Contains(fileName))
                profile.Image = "/assets/img/blankprofile.png";

            return profileManager.UpdateProfile(profile);
        }

       // [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Attributes/{name}")]
        public ServiceResult Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResponse.Error("You must provide a name for the Attribute.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            AttributeManager AttributeManager = new AttributeManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<TMG.Attribute> s = AttributeManager.Search(name);

            if (s == null || s.Count == 0)
                return ServiceResponse.Error("TMG.Attribute could not be located for the name " + name);

            return ServiceResponse.OK("", s);
        }

        [CacheOutput(ClientTimeSpan = 100, ServerTimeSpan = 100)]
    //    [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Attributes")]
        public ServiceResult GetAttributes()
        {
            //if (CurrentUser == null)
            //    return ServiceResponse.Error("You must be logged in to access this function.");

            AttributeManager AttributeManager = new AttributeManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            DataFilter filter = this.GetFilter(Request);
            List<dynamic> Attributes = (List<dynamic>)AttributeManager.GetAttributes(CurrentUser?.AccountUUID, ref filter).Cast<dynamic>().ToList();
            
            return ServiceResponse.OK("", Attributes, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/AttributeBy/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("You must provide a UUID for the Attribute.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            AttributeManager AttributeManager = new AttributeManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            var res = AttributeManager.Get(uuid);
            if (res.Code != 200)
                return res;
            TMG.Attribute attribute = (TMG.Attribute)res.Result;

            if (CurrentUser.AccountUUID != attribute.AccountUUID)
                return ServiceResponse.Error("You are not authorized to access this functionality.");

            return ServiceResponse.OK("", attribute);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Attributes/Add")]
        [System.Web.Http.Route("api/Attributes/Insert")]
        public ServiceResult Insert(TMG.Attribute n)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(n.AccountUUID) || n.AccountUUID == SystemFlag.Default.Account)
                n.AccountUUID = CurrentUser.AccountUUID;

            if (string.IsNullOrWhiteSpace(n.CreatedBy))
                n.CreatedBy = CurrentUser.UUID;

            if (n.DateCreated == DateTime.MinValue)
                n.DateCreated = DateTime.Now;

            AttributeManager AttributeManager = new AttributeManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return AttributeManager.Insert(n);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/File/Upload/{UUID}/{type}")]
        public async Task<ServiceResult> PostFile(string UUID, string type)
        //public ServiceResult PostFile(string UUID, string type)
        {
            var fileResult = new FileEx();
            fileResult.Default = false;
            string pathToImage = "";
            string root = "";

            string basePath = "/Content/Uploads/" + this.CurrentUser.UUID;

            if(type.EqualsIgnoreCase("user") || type.EqualsIgnoreCase("profile") || type.EqualsIgnoreCase("profilemember"))
                basePath = "/Content/Protected/" + this.CurrentUser.UUID;

            try
            {
                if (this.CurrentUser == null)
                    return ServiceResponse.Error("You must be logged in to upload.");

                #region non async

                //var httpRequest = HttpContext.Current.Request;
                //if (httpRequest.Files.Count < 1)
                //{
                //    return ServiceResponse.Error("Bad request");
                //}

                //foreach (string file in httpRequest.Files)
                //{
                //    var postedFile = httpRequest.Files[file];
                //    var filePath = HttpContext.Current.Server.MapPath("~/" + postedFile.FileName);
                //    postedFile.SaveAs(filePath);

                //}

                //return ServiceResponse.OK();

                #endregion non async

                HttpRequestMessage request = this.Request;
                if (!request.Content.IsMimeMultipartContent())
                    return ServiceResponse.Error("Unsupported media type.");

                root = System.Web.HttpContext.Current.Server.MapPath("~" + basePath);

                if (!Directory.Exists(root))
                    Directory.CreateDirectory(root);

                var provider = new MultipartFormDataStreamProvider(root);

                //  foreach (MultipartFileData file in provider.FileData)
                //{
                //    Trace.WriteLine(file.Headers.ContentDisposition.FileName);
                //    Trace.WriteLine("Server file path: " + file.LocalFileName);

                //    //AppManager app = new AppManager(Globals.DBConnectionKey, "web", this.GetAuthToken(Request));
                //    //string fileName = file.Headers.ContentDisposition.FileName;

                //    //// Clean the file name..
                //    //foreach (var c in Path.GetInvalidFileNameChars()) { fileName = fileName.Replace(c, ' '); }

                //    //if (string.IsNullOrWhiteSpace(fileName))
                //    //    continue;

                //    //fileName = fileName.ToUpper();

                //}
                //return ServiceResponse.OK();

                ServiceResult res = await request.Content.ReadAsMultipartAsync(provider).
                    ContinueWith<ServiceResult>(o =>
                    {
                        if (o.IsFaulted || o.IsCanceled)
                        {
                            _logger.InsertError("o.IsFaulted:" + o.IsFaulted, "AttributesController", "PostFile");
                            _logger.InsertError("o.IsCanceled:" + o.IsCanceled, "AttributesController", "PostFile");
                            _logger.InsertError("o.Exception:" + JsonConvert.SerializeObject(o), "AttributesController", "PostFile");

                            throw new HttpResponseException(HttpStatusCode.InternalServerError);
                        }
                        string fileName = "";
                        List<string> kvp = o.Result.Contents.First().Headers.First(w => w.Key == "Content-Disposition").Value.ToList()[0].Split(';').ToList();
                        foreach (string value in kvp)
                        {
                            if (value.Trim().StartsWith("filename"))
                            {
                                String[] tmp = value.Split('=');
                                fileName = DateTime.UtcNow.ToString("yyyyMMdd_hhmmss") + tmp[1].Trim().Replace("\"", "");
                            }

                            if (value.Contains("defaultImage"))    //value.Trim().StartsWith("name"))
                           {
                                fileResult.Default = true;
                            }
                        }
                       // this is the file name on the server where the file was saved
                       string file = provider.FileData.First().LocalFileName;
                        string originalFilename = Path.GetFileName(file);
                        string destFile = file.Replace(originalFilename, fileName);
                        try
                        {
                            if (File.Exists(destFile))
                                File.Delete(destFile);
                        }
                        catch
                        { //file may still be locked so don't worry about it.
                       }

                        try
                        {
                            string extension = Path.GetExtension(destFile)?.ToUpper();
                            if (!string.IsNullOrWhiteSpace(extension))
                                extension = extension.Replace(".", "");

                            var image = System.Drawing.Image.FromFile(file);
                            using (var resized = ImageEx.ResizeImage(image, 640, 640))
                            {
                                switch (extension)
                                {
                                    case "PNG":
                                        resized.Save(destFile, ImageFormat.Png);
                                        break;

                                    case "GIF":
                                        resized.Save(destFile, ImageFormat.Gif);
                                        break;

                                    case "JPEG":
                                    case "JPG":
                                        resized.Save(destFile, ImageFormat.Jpeg);
                                        break;

                                    default:
                                        ImageEx.SaveJpeg(destFile, resized, 90);
                                        break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Assert(false, ex.Message);
                        }

                       //try
                       //{
                       //    File.Move(file, destFile);
                       //}
                       //catch { }//ditto from above catch

                       file = destFile;

                        string thumbFile = ImageEx.CreateThumbnailImage(file, 64);
                        string ImageUrl = fileName;
                        string fullUrl = this.Request.RequestUri.Scheme + "://" + this.Request.RequestUri.Authority + basePath + "/";  // "/Content/Uploads/" + this.CurrentUser.UUID + "/";
                        pathToImage = fullUrl + "/" + ImageUrl;  // ImageUrl;

                        if (fileResult.Default)
                            this.UpdateImageURL(UUID, type, pathToImage);//Now update the database.
                       else
                        {
                           //add other images to attributes

                           AttributeManager atm = new AttributeManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

                            var attribute = new TMG.Attribute()
                            {
                                ReferenceUUID = UUID,
                                ReferenceType = type,
                                Value = thumbFile,
                                Image = thumbFile,
                                ValueType = "ImagePath",
                                Name = fileResult.Name,
                                AccountUUID = this.CurrentUser.AccountUUID,
                                DateCreated = DateTime.UtcNow,
                                CreatedBy = CurrentUser.UUID,
                                RoleOperation = ">=",
                                RoleWeight = RoleFlags.MemberRoleWeights.Member,
                                Private = false,
                                NSFW = -1
                            };
                            atm.Insert(attribute);
                        }

                        fileResult.UUID = UUID;
                        fileResult.UUIDType = type;
                        fileResult.Status = "saved";
                        fileResult.Image = fullUrl + ImageUrl;
                        fileResult.ImageThumb = fullUrl + ImageEx.GetThumbFileName(destFile); //todo check this
                       fileResult.Name = fileName;

                        return ServiceResponse.OK(fileName + " uploaded.", fileResult);
                    }
                );
                return res;
            }
            catch (Exception ex)
            {
                _logger.InsertError("root folder:" + root, "AttributesController", "PostFile");
                _logger.InsertError(ex.DeserializeException(false), "AttributesController", "PostFile");
                return ServiceResponse.Error("Upload failed.");
            }
        }

        
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Attributes/DataTypes")]
        public ServiceResult GetDataTypes( )
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            DataFilter filter = this.GetFilter(Request);
            AttributeManager AttributeManager = new AttributeManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return AttributeManager.GetDataTypes(ref filter);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Attributes/Data/Type/{typeName}")]
        public ServiceResult GetDataForType( string typeName)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            DataFilter filter = this.GetFilter(Request);
            AttributeManager AttributeManager = new AttributeManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return AttributeManager.GetDataForType(typeName, CurrentUser.AccountUUID,ref filter);
        }

        //[ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        //[System.Web.Http.HttpPost]
        //[System.Web.Http.HttpDelete]
        //[System.Web.Http.Route("api/Attributes/Delete")]
        //public ServiceResult Delete(TMG.Attribute n)
        //{
        //    if (n == null || string.IsNullOrWhiteSpace(n.UUID))
        //        return ServiceResponse.Error("Invalid account was sent.");

        //    AttributeManager AttributeManager = new AttributeManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
        //    return AttributeManager.Delete(n);
        //}

        /// <summary>
        /// Fields updated..
        ///     TMG.Attribute
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
        [System.Web.Http.Route("api/Attributes/Update")]
        public ServiceResult Update(TMG.Attribute s)
        {
            if (s == null)
                return ServiceResponse.Error("Invalid TMG.Attribute sent to server.");

            AttributeManager AttributeManager = new AttributeManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = AttributeManager.Get(s.UUID);
            if (res.Code != 200)
                return res;
            var dbS = (TMG.Attribute)res.Result;

            if (dbS.DateCreated == DateTime.MinValue)
                dbS.DateCreated = DateTime.Now;

            dbS.Deleted = s.Deleted;
            dbS.Name = s.Name;
            dbS.Status = s.Status;
            dbS.SortOrder = s.SortOrder;
            dbS.Image = s.Image;
            dbS.ReferenceType = s.ReferenceType;
            dbS.ReferenceUUID = s.ReferenceUUID;
            dbS.Value = s.Value;
            dbS.ValueType = s.ValueType;
            dbS.SafeName = s.SafeName;
            dbS.Deleted = s.Deleted;
            dbS.Active = s.Active;
            return AttributeManager.Update(dbS);
        }

        public void UpdateImageURL(string uuid, string type, string imageURL)
        {
            switch (type.ToUpper())
            {
                case "INVENTORYITEM":
                    InventoryManager inventoryManager = new InventoryManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                    var iiRes = inventoryManager.Get(uuid);
                    if (iiRes.Code != 200)
                        return;

                    InventoryItem ii = (InventoryItem)iiRes.Result;
                    ii.Image = imageURL;
                    inventoryManager.Update(ii);
                    break;
                case "CURRENCY":
                    CurrencyManager cm = new CurrencyManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                    var curRes = cm.Get(uuid);
                    if (curRes.Code != 200)
                        return;
                    Currency c = (Currency)curRes.Result;
                    c.Image = imageURL;
                    cm.Update(c);
                    break;
                case "EVENT":
                    EventManager evtManager = new EventManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                    var eres = evtManager.Get(uuid);
                    if (eres.Code != 200)
                        return;

                    Event e = (Event)eres.Result;
                    if (e != null)
                    {
                        e.Image = imageURL;
                        evtManager.Update(e);
                    }
                    break;

                case "PROFILEMEMBER":

                    break;
                case "PRODUCT":
                    ProductManager pm = new ProductManager(Globals.DBConnectionKey, Request.Headers.Authorization?.Parameter);
                    var productRes = pm.Get(uuid);
                    if (productRes.Code != 200)
                        return;
                    var product = (Product)productRes.Result;
                    product.Image = imageURL;
                    pm.Update(product);
                    break;
                case "PROFILE":
                    ProfileManager profileManager = new ProfileManager(Globals.DBConnectionKey, Request.Headers.Authorization?.Parameter);
                    var res2 = profileManager.Get(uuid);
                    if (res2.Code != 200)
                        return;

                    var tmp = (Profile)res2.Result;
                    tmp.Image = imageURL;
                    profileManager.UpdateProfile(tmp);

                    break;

                case "ACCOUNT":
                    AccountManager am = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                    var res = am.Get(uuid);
                    if (res.Code != 200)
                        return;

                    Account a = (Account)res.Result;
                    if (a != null)
                    {
                        a.Image = imageURL;
                        am.Update(a);
                    }
                    break;

                case "USER":
                    UserManager um = new UserManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                    var res52 = um.Get(uuid);
                    if (res52.Code != 200)
                        return;
                    User u = (User)res52.Result;

                    u.Image = imageURL;
                    um.UpdateUser(u, true);

                    break;

                case "ITEM":
                    InventoryManager im = new InventoryManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                    var res3 = im.Get(uuid);
                    if (res3.Code != 200)
                        return;
                    InventoryItem i = (InventoryItem)res3.Result;

                    i.Image = imageURL;
                    im.Update(i);
                    break;

                case "PLANT":
                case "BALLAST":
                case "BULB":
                case "CUSTOM":
                case "FAN":
                case "FILTER":
                case "PUMP":
                case "VEHICLE":
                    Debug.Assert(false, "TODO MAKE SURE CORRECT TABLE IS UPDATED");
                    EquipmentManager em = new EquipmentManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                    dynamic d = em.GetAll(type)?.FirstOrDefault(w => w.UUID == uuid);
                    if (d != null)
                    {
                        d.Image = imageURL;
                        em.Update(d);
                    }
                    break;
            }
        }
    }
}