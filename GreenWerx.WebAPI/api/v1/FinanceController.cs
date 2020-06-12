// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Managers.Finance;
using GreenWerx.Managers.Geo;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Finance;
using GreenWerx.Models.Geo;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Web;
using GreenWerx.Web.api;
using GreenWerx.Web.api.Helpers;
using GreenWerx.Web.Filters;
using WebApiThrottle;

namespace GreenWerx.WebAPI.api.v1
{
    public class FinanceController : ApiBaseController
    {
        public FinanceController()
        {
        }

        #region FinanceAccount Api

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Finance/Accounts/Delete/{uuid}")]
        public ServiceResult Delete(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("Invalid account was sent.");

            FinanceAccountManager FinanceAccountManager = new FinanceAccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = FinanceAccountManager.Get(uuid);
            if (res.Code != 200)
                return res;

            FinanceAccount fa = (FinanceAccount)res.Result;

            return FinanceAccountManager.Delete(fa);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Finance/Accounts/Delete")]
        public ServiceResult Delete(FinanceAccount n)
        {
            if (n == null)
                return ServiceResponse.Error("Invalid account was sent.");

            FinanceAccountManager FinanceAccountManager = new FinanceAccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = FinanceAccountManager.Get(n.UUID);
            if (res.Code != 200)
                return res;

            FinanceAccount fa = (FinanceAccount)res.Result;

            return FinanceAccountManager.Delete(fa);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Finance/Accounts/{name}")]
        public ServiceResult Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResponse.Error("You must provide a name for the FinanceAccount.");

            FinanceAccountManager FinanceAccountManager = new FinanceAccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<FinanceAccount> s = FinanceAccountManager.Search(name);

            if (s == null || s.Count == 0)
                return ServiceResponse.Error("FinanceAccount could not be located for the name " + name);

            return ServiceResponse.OK("", s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Finance/AccountsBy/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("You must provide a uuid for the FinanceAccount.");

            FinanceAccountManager FinanceAccountManager = new FinanceAccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return FinanceAccountManager.Get(uuid);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Finance/Accounts")]
        public ServiceResult GetFinanceAccounts()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            FinanceAccountManager financeAccountManager = new FinanceAccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<dynamic> FinanceAccounts = financeAccountManager.GetFinanceAccounts(CurrentUser.AccountUUID).Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            FinanceAccounts = FinanceAccounts.Filter(ref filter);

            return ServiceResponse.OK("", FinanceAccounts, filter.TotalRecordCount);
        }

        [System.Web.Http.AllowAnonymous]
        [System.Web.Http.HttpGet]
        // [EnableThrottling(PerSecond = 3)]
        [System.Web.Http.Route("api/Finance/PaymentOptions")]
        public ServiceResult GetPaymentOptions()
        {
            //to make sure we're in sync with the locations table we'll use the default online store locations account uuid to get the payment options for the sites account.
            LocationManager lm = new LocationManager(Globals.DBConnectionKey, Request.Headers.Authorization?.Parameter);
            Location location = lm.GetAll()?.FirstOrDefault(w => w.isDefault == true && w.LocationType.EqualsIgnoreCase("ONLINE STORE"));

            if (location == null)
                return ServiceResponse.Error("Could not get location for payment option.");

            FinanceAccountManager financeAccountManager = new FinanceAccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<FinanceAccount> FinanceAccounts = financeAccountManager.GetPaymentOptions(location.AccountUUID).ToList();

            return ServiceResponse.OK("", FinanceAccounts);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Finance/Accounts/Add")]
        [System.Web.Http.Route("api/Finance/Accounts/Insert")]
        public ServiceResult Insert(FinanceAccount n)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(n.AccountUUID) || n.AccountUUID == SystemFlag.Default.Account)
                n.AccountUUID = CurrentUser.AccountUUID;

            if (string.IsNullOrWhiteSpace(n.CreatedBy))
                n.CreatedBy = CurrentUser.UUID;

            if (n.DateCreated == DateTime.MinValue)
                n.DateCreated = DateTime.UtcNow;

            FinanceAccountManager FinanceAccountManager = new FinanceAccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return FinanceAccountManager.Insert(n);
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
        [System.Web.Http.Route("api/Finance/Accounts/Update")]
        public ServiceResult Update(FinanceAccount s)
        {
            if (s == null)
                return ServiceResponse.Error("Invalid FinanceAccount sent to server.");

            FinanceAccountManager FinanceAccountManager = new FinanceAccountManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = FinanceAccountManager.Get(s.UUID);
            if (res.Code != 200)
                return res;

            var dbS = (FinanceAccount)res.Result;

            if (dbS.DateCreated == DateTime.MinValue)
                dbS.DateCreated = DateTime.UtcNow;

            dbS.Deleted = s.Deleted;
            dbS.Name = s.Name;
            dbS.Status = s.Status;
            dbS.SortOrder = s.SortOrder;
            dbS.AccountNumber = s.AccountNumber;
            dbS.CurrencyUUID = s.CurrencyUUID;
            dbS.Balance = s.Balance;
            dbS.Active = s.Active;
            dbS.LocationType = s.LocationType;
            dbS.ClientCode = s.ClientCode;

            if (string.IsNullOrWhiteSpace(s.Image) || s.Image.EndsWith("/"))
                dbS.Image = "/Content/Default/Images/bank.png";
            else
                dbS.Image = s.Image;
            //
            //   AssetClass
            // Balance
            //
            //CurrencyName
            //  IsTest
            //Password
            //ServiceAddress
            //SourceClass
            //SourceUUID
            //  UsedBy
            //UsedByClass
            return FinanceAccountManager.Update(dbS);
        }

        #endregion FinanceAccount Api

        #region PriceRule Api TODO refactor to seperate controller

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Finance/PriceRules/Delete")]
        public ServiceResult DeletePriceRule(PriceRule PriceRule)
        {
            if (PriceRule == null || string.IsNullOrWhiteSpace(PriceRule.UUID))
                return ServiceResponse.Error("Invalid account was sent.");

            PriceManager financeManager = new PriceManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return financeManager.Delete(PriceRule);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Finance/PriceRules/Delete/{uuid}")]
        public ServiceResult DeletePriceRuleBy(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("Invalid id.");

            PriceManager fm = new PriceManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = fm.Get(uuid);
            if (res.Code != 200)
                return res;
            PriceRule c = (PriceRule)res.Result;
            if (c == null)
                return ServiceResponse.Error("Invalid uuid");

            return fm.Delete(c);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Finance/PriceRules/{PriceRuleCode}")]
        public ServiceResult GetPriceRule(string PriceRuleCode)
        {
            if (string.IsNullOrWhiteSpace(PriceRuleCode))
                return ServiceResponse.Error("You must provide a code for the rule.");

            PriceManager financeManager = new PriceManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            PriceRule s = financeManager.GetPriceRuleByCode(PriceRuleCode);

            if (s == null)
                return ServiceResponse.Error("Invalid code " + PriceRuleCode);

            return ServiceResponse.OK("", s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Finance/PriceRules")]
        public ServiceResult GetPriceRules()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            PriceManager financeManager = new PriceManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<dynamic> PriceRule = (List<dynamic>)financeManager.GetPriceRules(CurrentUser.AccountUUID, false).Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            PriceRule = PriceRule.Filter(ref filter);
            return ServiceResponse.OK("", PriceRule, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Finance/PriceRules/Add")]
        [System.Web.Http.Route("api/Finance/PriceRules/Insert")]
        public ServiceResult Insert(PriceRule PriceRule)
        {
            if (PriceRule == null || string.IsNullOrWhiteSpace(PriceRule.Name))
                return ServiceResponse.Error("Invalid PriceRule sent to server.");

            string authToken = this.GetAuthToken(Request);

            UserSession us = SessionManager.GetSession(authToken);
            if (us == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(us.UserData))
                return ServiceResponse.Error("Couldn't retrieve user data.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(PriceRule.AccountUUID) || PriceRule.AccountUUID == SystemFlag.Default.Account)
                PriceRule.AccountUUID = CurrentUser.AccountUUID;

            if (string.IsNullOrWhiteSpace(PriceRule.CreatedBy))
                PriceRule.CreatedBy = CurrentUser.UUID;

            if (PriceRule.DateCreated == DateTime.MinValue)
                PriceRule.DateCreated = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(PriceRule.Image))
                PriceRule.Image = "/Content/Default/Images/PriceRule/default.png";

            PriceManager financeManager = new PriceManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return financeManager.Insert(PriceRule);
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
        [System.Web.Http.Route("api/Finance/PriceRules/Update")]
        public ServiceResult Update(PriceRule fat)
        {
            if (fat == null)
                return ServiceResponse.Error("Invalid PriceRule sent to server.");

            PriceManager financeManager = new PriceManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = financeManager.Get(fat.UUID);
            if (res.Code != 200)
                return res;
            var dbS = (PriceRule)res.Result;

            if (dbS.DateCreated == DateTime.MinValue)
                dbS.DateCreated = DateTime.UtcNow;

            dbS.Name = fat.Name;

            dbS.Image = fat.Image;
            dbS.Deleted = fat.Deleted;
            dbS.Status = fat.Status;
            dbS.SortOrder = fat.SortOrder;
            dbS.Expires = fat.Expires;
            dbS.ReferenceType = fat.ReferenceType;
            dbS.Code = fat.Code;
            dbS.Operand = fat.Operand;
            dbS.Operator = fat.Operator;
            dbS.Minimum = fat.Minimum;
            dbS.Maximum = fat.Maximum;
            dbS.Mandatory = fat.Mandatory;
            dbS.MaxUseCount = fat.MaxUseCount;

            return financeManager.Update(dbS);
        }

        #endregion PriceRule Api TODO refactor to seperate controller

        #region FinanceAccountTransaction Api TODO refactor to seperate controller

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Finance/Accounts/Transactions/Add")]
        public ServiceResult AddFinanceAccountTransaction(FinanceAccountTransaction s)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.Name))
                return ServiceResponse.Error("Invalid FinanceAccountTransaction sent to server.");

            string authToken = this.GetAuthToken(Request);

            UserSession us = SessionManager.GetSession(authToken);
            if (us == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(us.UserData))
                return ServiceResponse.Error("Couldn't retrieve user data.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(s.AccountUUID) || s.AccountUUID == SystemFlag.Default.Account)
                s.AccountUUID = CurrentUser.AccountUUID;

            if (string.IsNullOrWhiteSpace(s.CreatedBy))
                s.CreatedBy = CurrentUser.UUID;

            if (s.DateCreated == DateTime.MinValue)
                s.DateCreated = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(s.Image))
                s.Image = "/Content/Default/Images/FinanceAccountTransaction/default.png";

            FinanceAccountTransactionsManager financeManager = new FinanceAccountTransactionsManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return financeManager.Insert(s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Finance/Accounts/Transactions/Delete")]
        public ServiceResult Delete(FinanceAccountTransaction s)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.UUID))
                return ServiceResponse.Error("Invalid account was sent.");

            FinanceAccountTransactionsManager financeManager = new FinanceAccountTransactionsManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return financeManager.Delete(s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Finance/Accounts/Transactions/Delete/{uuid}")]
        public ServiceResult DeleteFinanceAccountTransactionBy(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("Invalid id.");

            FinanceAccountTransactionsManager fm = new FinanceAccountTransactionsManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = fm.Get(uuid);
            if (res.Code != 200)
                return res;
            FinanceAccountTransaction c = (FinanceAccountTransaction)res.Result;

            return fm.Delete(c);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Finance/Accounts/Transactions")]
        public ServiceResult GetAccountTransactions()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            FinanceAccountTransactionsManager financeManager = new FinanceAccountTransactionsManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<dynamic> FinanceAccountTransaction = (List<dynamic>)financeManager.GetFinanceAccountTransactions(CurrentUser.AccountUUID, false).Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            FinanceAccountTransaction = FinanceAccountTransaction.Filter(ref filter);

            return ServiceResponse.OK("", FinanceAccountTransaction, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Finance/Accounts/Transactions/{name}")]
        public ServiceResult GetFinanceAccountTransactionByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResponse.Error("You must provide a name for the transaction.");

            FinanceAccountTransactionsManager financeManager = new FinanceAccountTransactionsManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<FinanceAccountTransaction> s = financeManager.Search(name);

            if (s == null || s.Count == 0)
                return ServiceResponse.Error("FinanceAccountTransaction could not be located for the name " + name);

            return ServiceResponse.OK("", s);
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
        [System.Web.Http.Route("api/Finance/Accounts/Transactions/Update")]
        public ServiceResult Update(FinanceAccountTransaction fat)
        {
            if (fat == null)
                return ServiceResponse.Error("Invalid FinanceAccountTransaction sent to server.");

            FinanceAccountTransactionsManager financeManager = new FinanceAccountTransactionsManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = financeManager.Get(fat.UUID);
            if (res.Code != 200)
                return res;

            var dbS = (FinanceAccountTransaction)res.Result;

            if (dbS.DateCreated == DateTime.MinValue)
                dbS.DateCreated = DateTime.UtcNow;

            dbS.Name = fat.Name;

            dbS.Image = fat.Image;
            dbS.Deleted = fat.Deleted;
            dbS.Status = fat.Status;
            dbS.SortOrder = fat.SortOrder;
            //FinanceAccountUUID
            //PayToAccountUUID
            // PayFromAccountUUID
            // CreationDate
            // CustomerIp
            //LastPaymentStatusCheck
            //    OrderUUID
            //    Amount
            //  TransactionType
            // TransactionDate
            // PaymentTypeUUID
            // AmountTransferred
            // SelectedPaymentTypeSymbol
            //SelectedPaymentTypeTotal
            //        UserUUID
            //        CustomerEmail
            // CurrencyUUID
            return financeManager.Update(dbS);
        }

        #endregion FinanceAccountTransaction Api TODO refactor to seperate controller

        #region Currency Api TODO refactor to seperate controller

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Finance/Currency/Add")]
        public ServiceResult AddCurrency(Currency s)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.Name))
                return ServiceResponse.Error("Invalid Currency sent to server.");

            string authToken = this.GetAuthToken(Request);

            UserSession us = SessionManager.GetSession(authToken);
            if (us == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(us.UserData))
                return ServiceResponse.Error("Couldn't retrieve user data.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(s.AccountUUID) || s.AccountUUID == SystemFlag.Default.Account)
                s.AccountUUID = CurrentUser.AccountUUID;

            if (string.IsNullOrWhiteSpace(s.CreatedBy))
                s.CreatedBy = CurrentUser.UUID;

            if (s.DateCreated == DateTime.MinValue)
                s.DateCreated = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(s.Image))
                s.Image = "/Content/Default/Images/Currency/default.png";

            CurrencyManager financeManager = new CurrencyManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return financeManager.Insert(s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Finance/Currency/Delete")]
        public ServiceResult DeleteCurrency(Currency s)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.UUID))
                return ServiceResponse.Error("Invalid account was sent.");

            CurrencyManager financeManager = new CurrencyManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return financeManager.Delete(s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Finance/Currency/Delete/{currencyUUID}")]
        public ServiceResult DeleteCurrencyBy(string currencyUUID)
        {
            if (string.IsNullOrWhiteSpace(currencyUUID))
                return ServiceResponse.Error("Invalid id.");

            CurrencyManager fm = new CurrencyManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return fm.Get(currencyUUID);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Finance/AssetClasses")]
        public ServiceResult GetAssetClasses()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            CurrencyManager financeManager = new CurrencyManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<string> assetClasses = financeManager.GetAll()
                                                        .Where(w => w.AccountUUID == SystemFlag.Default.Account ||
                                                               w.AccountUUID == CurrentUser.AccountUUID)
                                                        .OrderBy(o => o.AssetClass)
                                                        .Select(s => s.AssetClass)
                                                        .Distinct()
                                                        .ToList();
            int count = assetClasses.Count;
            return ServiceResponse.OK("", assetClasses, count);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Finance/Currency")]
        public ServiceResult GetCurrency()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            CurrencyManager financeManager = new CurrencyManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<dynamic> currency = (List<dynamic>)financeManager.GetCurrencies(CurrentUser.AccountUUID, false, true).Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            currency = currency.Filter(ref filter);
            return ServiceResponse.OK("", currency, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Finance/Currency/{name}")]
        public ServiceResult GetCurrencyByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResponse.Error("You must provide a name for the currency.");

            CurrencyManager financeManager = new CurrencyManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<Currency> s = financeManager.Search(name);

            if (s == null || s.Count == 0)
                return ServiceResponse.Error("Currency could not be located for the name " + name);

            return ServiceResponse.OK("", s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Finance/Currency/Symbols")]
        public ServiceResult GetCurrencySymbols()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            CurrencyManager financeManager = new CurrencyManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<string> symbols = financeManager.GetAll()
                                                 .Where(w => w.AccountUUID == SystemFlag.Default.Account ||
                                                        w.AccountUUID == CurrentUser.AccountUUID)
                                                 .OrderBy(o => o.Symbol)
                                                 .Select(s => s.Symbol)
                                                 .Distinct()
                                                 .ToList();
            int count = symbols.Count;
            return ServiceResponse.OK("", symbols, count);
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
        [System.Web.Http.Route("api/Finance/Currency/Update")]
        public ServiceResult UpdateCurrency(Currency form)
        {
            if (form == null)
                return ServiceResponse.Error("Invalid Currency sent to server.");

            CurrencyManager financeManager = new CurrencyManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = financeManager.Get(form.UUID);
            if (res.Code != 200)
                return res;

            var dbS = (Currency)res.Result;

            if (dbS.DateCreated == DateTime.MinValue)
                dbS.DateCreated = DateTime.UtcNow;

            dbS.Name = form.Name;
            dbS.AssetClass = form.AssetClass;
            //// dbS.Country = form.Country;
            dbS.Symbol = form.Symbol;
            dbS.Test = form.Test;
            dbS.Image = form.Image;
            dbS.Deleted = form.Deleted;
            dbS.Status = form.Status;
            dbS.SortOrder = form.SortOrder;
            return financeManager.Update(dbS);
        }

        #endregion Currency Api TODO refactor to seperate controller

        [System.Web.Http.AllowAnonymous]
        [System.Web.Http.HttpPost]
        // [EnableThrottling(PerSecond = 5)]
        [System.Web.Http.Route("api/PayPal/IPN")]
        public async System.Threading.Tasks.Task<HttpStatusCodeResult> ProcessIPN()
        {
            PaymentGatewayManager _gatewayManager = new PaymentGatewayManager(Globals.DBConnectionKey);
            NetworkHelper network = new NetworkHelper();
            string ip = network.GetClientIpAddress(this.Request);

#if DEBUG
            string ipnSample = @"mc_gross = 19.95 & protection_eligibility = Eligible & address_status = confirmed & payer_id = LPLWNMTBWMFAY &
                                        tax = 0.00 & address_street = 1 + Main + St & payment_date = 20 % 3A12 % 3A59 + Jan + 13 % 2C + 2009 + PST & payment_status = Completed &
                                        charset = windows - 1252 & address_zip = 95131 & first_name = Test & mc_fee = 0.88 & address_country_code = US & address_name = Test + User &
                                        notify_version = 2.6 & custom = d5422cf40f364cd99cac5fb3df7c12f6 &payer_status = verified & address_country = United + States & address_city = San + Jose & quantity = 1 &
                                        verify_sign = AtkOfCXbDm2hu0ZELryHFjY - Vb7PAUvS6nMXgysbElEn9v - 1XcmSoGtf & payer_email = gpmac_1231902590_per % 40paypal.com & txn_id = 61E67681CH3238416 & payment_type = instant & last_name = User & address_state = CA & receiver_email = gpmac_1231902686_biz % 40paypal.com &
                                        payment_fee = 0.88 & receiver_id = S8XGHLYDW9T3S & txn_type = express_checkout & item_name = &mc_currency = USD & item_number = &residence_country = US & test_ipn = 1 & handling_amount = 0.00 & transaction_subject = &payment_gross = 19.95 & shipping = 0.00";

            _gatewayManager.ProcessIpn(ipnSample, ip);

#else
              byte[] paramArray = await Request.Content.ReadAsByteArrayAsync();
            var content = System.Text.Encoding.ASCII.GetString(paramArray);
           //Fire and forget verification task
            //Thread t = new Thread(() => _gatewayManager.ProcessIpn(content, ip));
            //t.Start();
#endif

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
    }
}