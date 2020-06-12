// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using GreenWerx.Managers;
using GreenWerx.Managers.Medical;
using GreenWerx.Managers.Store;
using GreenWerx.Models;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Medical;
using GreenWerx.Models.Store;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Web.Filters;
using GreenWerx.Web.Models;

namespace GreenWerx.Web.api.v1
{
    public class DosesController : ApiBaseController
    {
        public DosesController()
        {
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/DoseLogs/Delete")]
        public ServiceResult Delete(DoseLog n)
        {
            if (n == null || string.IsNullOrWhiteSpace(n.UUID))
                return ServiceResponse.Error("Invalid account was sent.");

            DoseManager DoseManager = new DoseManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return DoseManager.Delete(n);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/DoseLogs/{name}")]
        public ServiceResult Get(string name)
        {
            if (Request.Headers.Authorization == null || string.IsNullOrWhiteSpace(this.GetAuthToken(Request)))
                return ServiceResponse.Error("You must be logged in to access this functionality.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            DoseManager DoseManager = new DoseManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<DoseLog> s = DoseManager.Search(name);
            return ServiceResponse.OK("", s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/DoseLogsBy/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            if (Request.Headers.Authorization == null || string.IsNullOrWhiteSpace(this.GetAuthToken(Request)))
                return ServiceResponse.Error("You must be logged in to access this functionality.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            DoseManager DoseManager = new DoseManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            return DoseManager.Get(uuid);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/DoseLogs")]
        public ServiceResult GetLogs()
        {
            if (Request.Headers.Authorization == null || string.IsNullOrWhiteSpace(this.GetAuthToken(Request)))
                return ServiceResponse.Error("You must be logged in to access this functionality.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            int count = 0;
            DoseManager DoseManager = new DoseManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            List<dynamic> Doses = DoseManager.GetDoses(CurrentUser.AccountUUID).Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            if (filter != null)
            {
                Doses = Doses.Filter(ref filter);

                //todo move the code below to the filter input
                string sortField = filter.SortBy?.ToUpper();
                string sortDirection = filter.SortDirection?.ToUpper();

                if (sortDirection == "ASC")
                {
                    switch (sortField)
                    {
                        case "DOSEDATETIME":
                            Doses = Doses.OrderBy(uob => uob.DoseDateTime).ToList();
                            break;

                        case "NAME":
                            Doses = Doses.OrderBy(uob => uob.Name).ToList();
                            break;
                    }
                }
                else
                {
                    switch (sortField)
                    {
                        case "DOSEDATETIME":
                            Doses = Doses.OrderByDescending(uob => uob.DoseDateTime).ToList();
                            break;

                        case "NAME":
                            Doses = Doses.OrderByDescending(uob => uob.Name).ToList();
                            break;
                    }
                }
            }

            return ServiceResponse.OK("", Doses, count);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/DoseLog/Add")]
        [System.Web.Http.Route("api/DoseLog/Insert")]
        public ServiceResult Insert(DoseLogForm d)
        {
            string authToken = this.GetAuthToken(Request);
            //d.UserUUID  <= patient id. for now make this a hidden field and use the cookie value.
            //               for an app that uses multiple patients then we'll need make a combobox or some list to select
            //whom we're logging for.

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            UserSession us = SessionManager.GetSession(authToken);
            if (us == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (us.Captcha?.ToUpper() != d.Captcha?.ToUpper())
                return ServiceResponse.Error("Invalid code.");

            if (string.IsNullOrWhiteSpace(d.AccountUUID))
                d.AccountUUID = CurrentUser.AccountUUID;

            if (string.IsNullOrWhiteSpace(d.CreatedBy))
                d.CreatedBy = us.UUID;

            if (d.DateCreated == DateTime.MinValue)
                d.DateCreated = DateTime.UtcNow;

            d.Active = true;
            d.Deleted = false;

            if (d.DoseDateTime == null || d.DoseDateTime == DateTime.MinValue)
                return ServiceResponse.Error("You must a date time for the dose.");

            if (string.IsNullOrWhiteSpace(d.ProductUUID))
                return ServiceResponse.Error("You must select a product.");

            ProductManager productManager = new ProductManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = productManager.Get(d.ProductUUID);
            if (res.Code != 200)
                return res;// return ServiceResponse.Error("Product could not be found. You must select a product, or create one from the products page.");

            Product p = (Product)res.Result;

            if (string.IsNullOrWhiteSpace(d.Name))
                d.Name = string.Format("{0} {1} {2}", p.Name, d.Quantity, d.UnitOfMeasure);

            if (d.Quantity <= 0)
                return ServiceResponse.Error("You must enter a quantity");

            if (string.IsNullOrWhiteSpace(d.UnitOfMeasure))
                return ServiceResponse.Error("You must select a unit of measure.");

            UnitOfMeasureManager uomm = new UnitOfMeasureManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            if (uomm.Get(d.UnitOfMeasure) == null)
            {
                var filter = new DataFilter();
                UnitOfMeasure uom = (UnitOfMeasure)uomm.Search(d.UnitOfMeasure, ref filter)?.FirstOrDefault();
                if (uom == null)
                {
                    uom = new UnitOfMeasure();
                    uom.Name = d.UnitOfMeasure.Trim();
                    uom.AccountUUID = CurrentUser.AccountUUID;
                    uom.Active = true;
                    uom.Deleted = false;
                    uom.Private = true;
                    ServiceResult uomSr = uomm.Insert(uom);
                    if (uomSr.Code != 200)
                        return uomSr;
                }
                d.UnitOfMeasure = uom.UUID;
            }

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<DoseLogForm, DoseLog>();
            });

            IMapper mapper = config.CreateMapper();
            var dest = mapper.Map<DoseLogForm, DoseLog>(d);

            DoseManager DoseManager = new DoseManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            ServiceResult sr = DoseManager.Insert(dest);
            if (sr.Code != 200)
                return sr;

            SymptomManager sm = new SymptomManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            StringBuilder symptomErrors = new StringBuilder();

            int index = 1;
            foreach (SymptomLog s in d.Symptoms)
            {
                if (string.IsNullOrWhiteSpace(s.UUID))
                {
                    symptomErrors.AppendLine("Symptom " + index + " UUID must be set!");
                    Debug.Assert(false, "SYMPTOM UUID MUST BE SET!!");
                    continue;
                }

                var res2 = sm.Get(s.UUID);
                if (res2.Code != 200)
                    continue;

                Symptom stmp = (Symptom)res2.Result;

                s.Name = stmp.Name;

                if (s.SymptomDate == null || s.SymptomDate == DateTime.MinValue)
                {
                    symptomErrors.AppendLine("Symptom " + s.UUID + " date must be set!");
                    continue;
                }
                //s.Status
                s.AccountUUID = CurrentUser.AccountUUID;
                s.Active = true;
                s.CreatedBy = CurrentUser.UUID;
                s.DateCreated = DateTime.UtcNow;
                s.Deleted = false;
                s.DoseUUID = dest.UUID;
                s.Private = true;
                ServiceResult slSr = sm.Insert(s);

                if (slSr.Code != 200)
                    symptomErrors.AppendLine("Symptom " + index + " failed to save. " + slSr.Message);
                index++;
            }

            if (symptomErrors.Length > 0)
                return ServiceResponse.Error(symptomErrors.ToString());

            return ServiceResponse.OK("", dest);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpPatch]
        [System.Web.Http.Route("api/DoseLogs/Update")]
        public ServiceResult Update(DoseLog form)
        {
            if (form == null)
                return ServiceResponse.Error("Invalid Strain sent to server.");

            DoseManager DoseManager = new DoseManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = DoseManager.Get(form.UUID);
            if (res.Code != 200)
                return res;

            var dbS = (DoseLog)res.Result;

            if (dbS.DateCreated == DateTime.MinValue)
                dbS.DateCreated = DateTime.UtcNow;

            dbS.Name = form.Name;
            dbS.Deleted = form.Deleted;
            dbS.Status = form.Status;
            dbS.SortOrder = form.SortOrder;
            return DoseManager.Update(dbS);
        }
    }
}