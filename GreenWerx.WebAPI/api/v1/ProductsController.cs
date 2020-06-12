// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GreenWerx.Data.Logging;
using GreenWerx.Managers;
using GreenWerx.Managers.General;
using GreenWerx.Managers.Membership;
using GreenWerx.Managers.Plant;
using GreenWerx.Managers.Store;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.General;
using GreenWerx.Models.Membership;
using GreenWerx.Models.Plant;
using GreenWerx.Models.Store;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Web.Filters;
using WebApi.OutputCache.V2;

using TMG = GreenWerx.Models.General;

namespace GreenWerx.Web.api.v1
{
    [CacheOutput(ClientTimeSpan = 100, ServerTimeSpan = 100)]
    public class ProductsController : ApiBaseController
    {
        public ProductsController()
        {
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Products/Delete")]
        public ServiceResult Delete(Product n)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (n == null || string.IsNullOrWhiteSpace(n.UUID))
                return ServiceResponse.Error("Invalid account was sent.");

            ProductManager productManager = new ProductManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return productManager.Delete(n);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Products/Delete/{productUUID}")]
        public ServiceResult Delete(string productUUID)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            ProductManager productManager = new ProductManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = productManager.Get(productUUID);
            if (res.Code != 200)
                return res;
            Product p = (Product)res.Result;

            return productManager.Delete(p);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Products/{name}")]
        public ServiceResult Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResponse.Error("You must provide a name for the produc.");

            ProductManager productManager = new ProductManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<Product> s = productManager.Search(name);

            if (s == null || s.Count == 0)
                return ServiceResponse.Error("Product could not be located for the name " + name);

            return ServiceResponse.OK("", s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/ProductsBy/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("You must provide an id for the product.");

            ProductManager productManager = new ProductManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return productManager.Get(uuid);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Products/Categories")]
        public ServiceResult GetProductCategories()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            CategoryManager catManager = new CategoryManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<dynamic> categories = (List<dynamic>)catManager.GetCategories(CurrentUser.AccountUUID, false, true)?.Where(w => (w.CategoryType?.EqualsIgnoreCase("PRODUCT") ?? false)).Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            categories = categories.Filter(ref filter);
            return ServiceResponse.OK("", categories, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Product/{uuid}/{type}/Details")]
        public ServiceResult GetProductDetails(string uuid, string type)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("You must provide an id for the product.");

            string refUUID = "";
            string refType = "";
            string refAccount = "";
            string ManufacturerUUID = "";
            string strainUUID = "";

            if (type.EqualsIgnoreCase("PRODUCT"))
            {
                ProductManager productManager = new ProductManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                var res1 = productManager.Get(uuid);
                if (res1.Code != 200)
                    return res1;

                Product p = (Product)res1.Result;

                refUUID = p.UUID;
                refType = p.UUIDType;
                refAccount = p.AccountUUID;
                ManufacturerUUID = p.ManufacturerUUID;
                strainUUID = p.StrainUUID;
            }

            AccountManager am = new AccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var resa = am.Get(ManufacturerUUID);
            if (resa.Code != 200)
                return resa;

            Account a = (Account)resa.Result;

            AttributeManager atm = new AttributeManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<TMG.Attribute> attributes = atm.GetAttributes(refUUID, refType, refAccount)?.Where(w => w.Deleted == false).ToList();
            if (attributes == null)
                attributes = new List<TMG.Attribute>();
            if (a != null)
            {
                attributes.Add(new TMG.Attribute()
                {
                    Name = "Manufacturer",
                    AccountUUID = a.AccountUUID,
                    UUIDType = a.UUIDType,
                    Active = a.Active,
                    CreatedBy = a.CreatedBy,
                    DateCreated = a.DateCreated,
                    Deleted = a.Deleted,
                    Private = a.Private,
                    Status = a.Status,
                    Value = a.Name,
                    ValueType = "string"
                });
            }

            #region plant related info

            StrainManager pm = new StrainManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = pm.Get(strainUUID);
            if (res.Code != 200)
                return res;

            Strain s = (Strain)res.Result;
            attributes.Add(new TMG.Attribute()
            {
                Name = "Strain Name",
                AccountUUID = s.AccountUUID,
                UUIDType = s.UUIDType,
                Active = s.Active,
                CreatedBy = s.CreatedBy,
                DateCreated = s.DateCreated,
                Deleted = s.Deleted,
                Private = s.Private,
                Status = s.Status,
                Value = s.Name,
                ValueType = "string"
            });

            attributes.Add(new TMG.Attribute()
            {
                Name = "Indica Percent",
                AccountUUID = s.AccountUUID,
                UUIDType = s.UUIDType,
                Active = s.Active,
                CreatedBy = s.CreatedBy,
                DateCreated = s.DateCreated,
                Deleted = s.Deleted,
                Private = s.Private,
                Status = s.Status,
                Value = s.IndicaPercent.ToString(),
                ValueType = "number"
            });

            attributes.Add(new TMG.Attribute()
            {
                Name = "Sativa Percent",
                AccountUUID = s.AccountUUID,
                UUIDType = s.UUIDType,
                Active = s.Active,
                CreatedBy = s.CreatedBy,
                DateCreated = s.DateCreated,
                Deleted = s.Deleted,
                Private = s.Private,
                Status = s.Status,
                Value = s.SativaPercent.ToString(),
                ValueType = "number"
            });

            if (!string.IsNullOrWhiteSpace(s.Generation))
            {
                attributes.Add(new TMG.Attribute()
                {
                    Name = "Generation",
                    AccountUUID = s.AccountUUID,
                    UUIDType = s.UUIDType,
                    Active = s.Active,
                    CreatedBy = s.CreatedBy,
                    DateCreated = s.DateCreated,
                    Deleted = s.Deleted,
                    Private = s.Private,
                    Status = s.Status,
                    Value = s.Generation,
                    ValueType = "string"
                });
            }

            CategoryManager cm = new CategoryManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var resc = cm.Get(s.CategoryUUID);
            if (resc.Code != 200)
                return resc;

            Category c = (Category)resc.Result;
            attributes.Add(new TMG.Attribute()
            {
                Name = "Variety",
                AccountUUID = c.AccountUUID,
                UUIDType = c.UUIDType,
                Active = c.Active,
                CreatedBy = c.CreatedBy,
                DateCreated = c.DateCreated,
                Deleted = c.Deleted,
                Private = c.Private,
                Status = c.Status,
                Value = c.Name,
                ValueType = "string"
            });

            #endregion plant related info

            return ServiceResponse.OK("", attributes);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Products")]
        public ServiceResult GetProducts()
        {
            ProductManager productManager = new ProductManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<dynamic> Products = (List<dynamic>)productManager.GetAll("")?.Where(w => w.Deleted == false).Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            Products = Products.Filter(ref filter);
            return ServiceResponse.OK("", Products, filter.TotalRecordCount);
        }

        /// <summary>
        /// filter ideas: instock=true,
        /// </summary>
        /// <param name="category"></param>
        /// <param name="filter"></param>
        /// <param name="startIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="sorting"></param>
        /// <returns></returns>
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Products/Categories/{category}")]
        public ServiceResult GetProducts(string category)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            ProductManager productManager = new ProductManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<dynamic> products = (List<dynamic>)productManager.GetAll(category)
                                                                    .Where(pw => (pw.AccountUUID == CurrentUser.AccountUUID) && pw.Deleted == false)
                                                                    .Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            products = products.Filter(ref filter);

            if (products == null || products.Count == 0)
                return ServiceResponse.Error("No products available.");

            return ServiceResponse.OK("", products, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 4)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Products/Add")]
        [System.Web.Http.Route("api/Products/Insert")]
        public ServiceResult Insert(Product n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid product posted to server.");

            string authToken = this.GetAuthToken(Request);
            SessionManager sessionManager = new SessionManager(Globals.DBConnectionKey);

            UserSession us = sessionManager.GetSession(authToken);
            if (us == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(us.UserData))
                return ServiceResponse.Error("Couldn't retrieve user data.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(n.CreatedBy))
            {
                n.CreatedBy = CurrentUser.UUID;
                n.AccountUUID = CurrentUser.AccountUUID;
                n.DateCreated = DateTime.UtcNow;
            }
            ProductManager productManager = new ProductManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return productManager.Insert(n);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Products/Categories/Move")]
        public ServiceResult MoveProductsToCategory()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            ServiceResult res;

            try
            {
                Task<string> content = Request.Content.ReadAsStringAsync();
                if (content == null)
                    return ServiceResponse.Error("No products were sent.");

                string body = content.Result;

                if (string.IsNullOrEmpty(body))
                    return ServiceResponse.Error("No products were sent.");

                List<Product> products = JsonConvert.DeserializeObject<List<Product>>(body);

                ProductManager pm = new ProductManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
                res = pm.Update(products);
            }
            catch (Exception ex)
            {
                res = ServiceResponse.Error(ex.Message);
                Debug.Assert(false, ex.Message);
                SystemLogger logger = new SystemLogger(Globals.DBConnectionKey);
                logger.InsertError(ex.Message, "ProductController", "MoveProductCategories");
            }
            return res;
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
        [System.Web.Http.Route("api/Products/Update")]
        public ServiceResult Update(Product pv)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (pv == null)
                return ServiceResponse.Error("Invalid product sent to server.");

            ProductManager productManager = new ProductManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var dbP = productManager.GetAll()?.FirstOrDefault(pw => pw.UUID == pv.UUID);

            if (dbP == null)
                return ServiceResponse.Error("Product was not found.");

            dbP.CategoryUUID = pv.CategoryUUID;
            dbP.Name = pv.Name;

            dbP.Link = pv.Link;
            dbP.LinkProperties = pv.LinkProperties;

            dbP.Virtual = pv.Virtual;
            dbP.Image = pv.Image;
            dbP.DepartmentUUID = pv.DepartmentUUID;
            dbP.SKU = pv.SKU;
            dbP.Virtual = pv.Virtual;

            dbP.StrainUUID = pv.StrainUUID;
            dbP.ManufacturerUUID = pv.ManufacturerUUID;

            dbP.Price = pv.Price;

            dbP.Weight = pv.Weight;
            dbP.UOMUUID = pv.UOMUUID;

            ////dbP.Expires           =
            ////dbP.Category          =
            dbP.Description = pv.Description;

            #region future implementation. may need to be implemented in inventory.

            ////     dbP.Cost = pv.Cost;
            ////dbP.StockCount        =
            ////dbP.Discount          =
            ////dbP.MarkUp            =
            ////dbP.MarkUpType        =
            ////dbP.Condition         =
            ////dbP.Quality           =
            ////dbP.Rating            =
            ////dbP.LocationUUID      =
            ////dbP.Status            =
            ////dbP.Active            =
            ////dbP.Deleted           =
            ////dbP.Private           =
            ////dbP.SortOrder         =

            #endregion future implementation. may need to be implemented in inventory.

            return productManager.Update(dbP);
        }
    }
}