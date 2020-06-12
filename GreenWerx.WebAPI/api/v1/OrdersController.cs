// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.Collections.Generic;
using System.Linq;
using GreenWerx.Managers.Store;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Store;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Web;
using GreenWerx.Web.api;
using GreenWerx.Web.Filters;

namespace GreenWerx.WebAPI.api.v1
{
    public class OrdersController : ApiBaseController
    {
        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Orders/Delete")]
        public ServiceResult Delete(Order n)
        {
            if (n == null || string.IsNullOrWhiteSpace(n.UUID))
                return ServiceResponse.Error("Invalid order was sent.");

            OrderManager orderManager = new OrderManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            if (orderManager.Delete(n).Code == 200)
                return ServiceResponse.OK();

            return ServiceResponse.Error("Delete order failed.");
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Orders/Delete/{uuid}")]
        public ServiceResult Delete(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("Invalid id was sent.");

            OrderManager orderManager = new OrderManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = orderManager.Get(uuid);
            if (res.Code != 200)
                return res;

            Order fa = (Order)res.Result;

            return orderManager.Delete(fa);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Orders/{name}")]
        public ServiceResult Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResponse.Error("You must provide a name for the order.");

            OrderManager orderManager = new OrderManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<Order> s = orderManager.Search(name);

            if (s == null || s.Count == 0)
                return ServiceResponse.Error("Order could not be located for the name " + name);

            return ServiceResponse.OK("", s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/OrdersBy/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("You must provide an id for the order.");

            OrderManager orderManager = new OrderManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return orderManager.Get(uuid);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Orders")]
        public ServiceResult GetOrders()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            OrderManager orderManager = new OrderManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<dynamic> Orders = (List<dynamic>)orderManager.GetOrders(CurrentUser.AccountUUID, false).Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);

            Orders = Orders.Filter(ref filter);
            return ServiceResponse.OK("", Orders, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Orders/Add")]
        [System.Web.Http.Route("api/Orders/Insert")]
        public ServiceResult Insert(Order n)
        {
            if (n == null || string.IsNullOrWhiteSpace(n.Name))
                return ServiceResponse.Error("Invalid Order sent to server.");

            if (!string.IsNullOrWhiteSpace(n.UUID) && this.Get(n.UUID).Code == 200)
            {
                return this.Update(n);
            }

            OrderManager orderManager = new OrderManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return orderManager.Insert(n);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpPatch]
        [System.Web.Http.Route("api/Orders/Update")]
        public ServiceResult Update(Order n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid Order sent to server.");

            OrderManager orderManager = new OrderManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = orderManager.Get(n.UUID);
            if (res.Code != 200)
                return res;

            var dbS = (Order)res.Result;

            if (dbS.DateCreated == DateTime.MinValue)
                dbS.DateCreated = DateTime.UtcNow;

            var form = (Order)n;
            dbS.Name = form.Name;
            dbS.AddedBy = form.AddedBy;
            dbS.AffiliateUUID = form.AffiliateUUID;
            dbS.BillingLocationUUID = form.BillingLocationUUID;
            dbS.CurrencyUUID = form.CurrencyUUID;
            dbS.CustomerEmail = form.CustomerEmail;
            dbS.Discount = form.Discount;
            dbS.FinancAccountUUID = form.FinancAccountUUID;
            dbS.ReconciledToAffiliate = form.ReconciledToAffiliate;
            dbS.ShippingCost = form.ShippingCost;
            dbS.ShippingDate = form.ShippingDate;
            dbS.ShippingLocationUUID = form.ShippingLocationUUID;
            dbS.ShippingMethodUUID = form.ShippingMethodUUID;
            dbS.ShippingSameAsBiling = form.ShippingSameAsBiling;
            dbS.SubTotal = form.SubTotal;
            dbS.Taxes = form.Taxes;
            dbS.Total = form.Total;
            dbS.TrackingUUID = form.TrackingUUID;
            dbS.TransactionID = form.TransactionID;
            dbS.UserUUID = form.UserUUID;
            dbS.CartUUID = form.CartUUID;
            dbS.PayStatus = form.PayStatus;
            //below are not on Order.cshtml form
            dbS.Deleted = form.Deleted;
            dbS.Status = form.Status;
            dbS.SortOrder = form.SortOrder;
            return orderManager.Update(dbS);
        }
    }
}