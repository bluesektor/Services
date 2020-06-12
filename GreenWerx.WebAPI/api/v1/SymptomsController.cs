// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.Collections.Generic;
using System.Linq;
using GreenWerx.Data.Logging.Models;
using GreenWerx.Managers.Medical;
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;
using GreenWerx.Models.Medical;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Web.Filters;

namespace GreenWerx.Web.api.v1
{
    public class SymptomsController : ApiBaseController
    {
        public SymptomsController()
        {
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpDelete]
        [System.Web.Http.Route("api/Symptoms/Delete")]
        public ServiceResult Delete(Symptom n)
        {
            if (n == null || string.IsNullOrWhiteSpace(n.UUID))
                return ServiceResponse.Error("Invalid account was sent.");

            SymptomManager symptomManager = new SymptomManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return symptomManager.Delete(n);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Symptom/{name}")]
        public ServiceResult Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResponse.Error("You must provide a name for the Symptom.");

            SymptomManager symptomManager = new SymptomManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<Symptom> s = symptomManager.Search(name);

            if (s == null || s.Count == 0)
                return ServiceResponse.Error("Symptom could not be located for the name " + name);

            return ServiceResponse.OK("", s);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/SymptomBy/{uuid}")]
        public ServiceResult GetBy(string uuid)
        {
            if (string.IsNullOrWhiteSpace(uuid))
                return ServiceResponse.Error("You must provide a name for the Symptom.");

            SymptomManager symptomManager = new SymptomManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return symptomManager.Get(uuid);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Symptoms")]
        public ServiceResult GetSymptoms()
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            DataFilter filter = this.GetFilter(Request);
            SymptomManager symptomManager = new SymptomManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<dynamic> Symptoms = symptomManager.GetSymptoms(CurrentUser.AccountUUID).Cast<dynamic>().ToList();

            Symptoms = Symptoms.Filter(ref filter);
            return ServiceResponse.OK("", Symptoms, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Symptoms/Add")]
        [System.Web.Http.Route("api/Symptoms/Insert")]
        public ServiceResult Insert(Symptom n)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(n.AccountUUID) || n.AccountUUID == SystemFlag.Default.Account)
                n.AccountUUID = CurrentUser.AccountUUID;

            if (string.IsNullOrWhiteSpace(n.CreatedBy))
                n.CreatedBy = CurrentUser.UUID;

            if (n.DateCreated == DateTime.MinValue)
                n.DateCreated = DateTime.UtcNow;

            SymptomManager symptomManager = new SymptomManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return symptomManager.Insert(n);
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
        [System.Web.Http.Route("api/Symptoms/Update")]
        public ServiceResult Update(Symptom s)
        {
            if (s == null)
                return ServiceResponse.Error("Invalid Symptom sent to server.");

            SymptomManager symptomManager = new SymptomManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            var res = symptomManager.Get(s.UUID);
            if (res.Code != 200)
                return res;

            var dbS = (Symptom)res.Result;

            if (dbS.DateCreated == DateTime.MinValue)
                dbS.DateCreated = DateTime.UtcNow;
            dbS.Deleted = s.Deleted;
            dbS.Name = s.Name;
            dbS.Status = s.Status;
            dbS.SortOrder = s.SortOrder;

            return symptomManager.Update(dbS);
        }

        #region SymptomLog

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/SymptomsLog/Add")]
        public ServiceResult AddSymptomLog(SymptomLog s)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(s.AccountUUID) || s.AccountUUID == SystemFlag.Default.Account)
                s.AccountUUID = CurrentUser.AccountUUID;

            if (string.IsNullOrWhiteSpace(s.CreatedBy))
                s.CreatedBy = CurrentUser.UUID;

            SymptomManager symptomManager = new SymptomManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            if (!string.IsNullOrWhiteSpace(s.UUParentID))
            {
                SymptomLog parentLog = symptomManager.GetSymptomLogBy(s.UUParentID);
                if (parentLog == null)
                    return ServiceResponse.Error("Invalid log parent id. The UUParentID must belong to a valid Symptom Log entry.");

                s.DoseUUID = parentLog.DoseUUID;
                if (string.IsNullOrWhiteSpace(s.UUParentIDType))
                    s.UUParentIDType = parentLog.UUIDType;
            }

            s.Active = true;

            if (string.IsNullOrWhiteSpace(s.UUIDType))
                s.UUIDType = "SymptomLog";

            //backlog go to dosehistory selsect a dose and in the symptomps list
            //click add . fill in the data and click
            //VERIFY the fields here.
            //rules for parent list symptom creation
            //Name <- may need to create
            //SymptomUUID <- may need to create
            if (string.IsNullOrWhiteSpace(s.Name) && string.IsNullOrWhiteSpace(s.SymptomUUID))
            {
                return ServiceResponse.Error("You must select a symptom.");
            }
            else if (string.IsNullOrWhiteSpace(s.Name) && string.IsNullOrWhiteSpace(s.SymptomUUID) == false)
            {   //get and assign the name
                var res = symptomManager.Get(s.SymptomUUID);
                if (res.Code != 200)
                    return res;

                Symptom symptom = (Symptom)res.Result;

                s.Name = symptom.Name;
            }
            else if (!string.IsNullOrWhiteSpace(s.Name) && string.IsNullOrWhiteSpace(s.SymptomUUID))
            {   //create the symptoms and assign it to the symptomuuid
                Symptom symptom = (Symptom)symptomManager.Search(s.Name)?.FirstOrDefault();

                if (symptom != null)
                {
                    s.SymptomUUID = symptom.UUID;
                }
                else
                {
                    symptom = new Symptom()
                    {
                        Name = s.Name,
                        AccountUUID = s.AccountUUID,
                        Active = true,
                        CreatedBy = CurrentUser.UUID,
                        DateCreated = DateTime.UtcNow,
                        Deleted = false,
                        UUIDType = "Symptom",
                        Category = "General"
                    };

                    ServiceResult sr = symptomManager.Insert(symptom);
                    if (sr.Code == 500)
                        return ServiceResponse.Error(sr.Message);

                    s.SymptomUUID = symptom.UUID;
                }
            }

            if (s.SymptomDate == null || s.SymptomDate == DateTime.MinValue)
                s.SymptomDate = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(s.Status))//Status start middle end. Query StatusMessage table
                return ServiceResponse.Error("You must provide a status.");

            if (s.Severity > 5) return ServiceResponse.Error("Severity must not be greater than 5.");
            if (s.Efficacy > 5) return ServiceResponse.Error("Efficacy must not be greater than 5.");

            return symptomManager.Insert(s);
        }

        //  [ApiAuthorizationRequired(Operator =">=" , RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/SymptomsLog/Delete")]
        public ServiceResult Delete(SymptomLog s)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.UUID))
                return ServiceResponse.Error("Invalid account was sent.");

            SymptomManager symptomManager = new SymptomManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            return symptomManager.Delete(s);
        }

        //[ApiAuthorizationRequired(Operator =">=" , RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Doses/{doseUUID}/SymptomsLog/History/{parentUUID}")]
        public ServiceResult GetChildSymptomLogs(string doseUUID, string parentUUID = "")
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(doseUUID))
                return ServiceResponse.Error("You must send a dose uuid.");

            SymptomManager symptomManager = new SymptomManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            List<dynamic> SymptomsLog = symptomManager.GetSymptomsByDose(doseUUID, parentUUID, CurrentUser.AccountUUID).Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            SymptomsLog = SymptomsLog.Filter(ref filter);
            return ServiceResponse.OK("", SymptomsLog, filter.TotalRecordCount);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/SymptomLog/{name}")]
        public ServiceResult GetSymptomLog(string name = "")
        {
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResponse.Error("You must provide a name for the SymptomLog.");

            SymptomManager symptomManager = new SymptomManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            SymptomLog s = symptomManager.GetSymptomLog(name);

            if (s == null)
                return ServiceResponse.Error("SymptomLog could not be located for the name " + name);

            return ServiceResponse.OK("", s);
        }

        //[ApiAuthorizationRequired(Operator =">=" , RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/SymptomsLog/{parentUUID}")]
        public ServiceResult GetSymptomLogs(string parentUUID = "")
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            List<dynamic> SymptomsLog;

            SymptomManager symptomManager = new SymptomManager(Globals.DBConnectionKey, this.GetAuthToken(Request));
            SymptomsLog = symptomManager.GetSymptomsLog(parentUUID, CurrentUser.AccountUUID).Cast<dynamic>().ToList(); ;

            DataFilter filter = this.GetFilter(Request);
            SymptomsLog = SymptomsLog.Filter(ref filter);
            return ServiceResponse.OK("", SymptomsLog, filter.TotalRecordCount);
        }

        //[ApiAuthorizationRequired(Operator =">=" , RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Doses/{doseUUID}/SymptomsLog")]
        public ServiceResult GetSymptomsLogByDose(string doseUUID)
        {
            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (string.IsNullOrWhiteSpace(doseUUID))
                return ServiceResponse.Error("You must send a dose uuid.");

            List<dynamic> SymptomsLog;

            SymptomManager symptomManager = new SymptomManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            SymptomsLog = symptomManager.GetSymptomsByDose(doseUUID, "", CurrentUser.AccountUUID).Cast<dynamic>().ToList();

            DataFilter filter = this.GetFilter(Request);
            SymptomsLog = SymptomsLog.Filter(ref filter);
            return ServiceResponse.OK("", SymptomsLog, filter.TotalRecordCount);
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
        //  [ApiAuthorizationRequired(Operator =">=" , RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpPatch]
        [System.Web.Http.Route("api/SymptomsLog/Update")]
        public ServiceResult Update(SymptomLog s)
        {
            if (s == null)
                return ServiceResponse.Error("Invalid SymptomLog sent to server.");

            SymptomManager symptomManager = new SymptomManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            var dbS = symptomManager.GetSymptomLogBy(s.UUID);

            if (dbS == null)
                return ServiceResponse.Error("SymptomLog was not found.");

            if (s.Efficacy < -5 || s.Efficacy > 5)
                return ServiceResponse.Error("Efficacy is out of range.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            if (CurrentUser.UUID != dbS.CreatedBy)
                return ServiceResponse.Error("You are not authorized to change this item.");

            dbS.Name = s.Name;
            dbS.Status = s.Status;
            dbS.Duration = s.Duration;
            dbS.DurationMeasure = s.DurationMeasure;
            dbS.Efficacy = s.Efficacy;
            dbS.Severity = s.Severity;
            dbS.SymptomDate = s.SymptomDate;

            //test this.make sure date, status, severity, efficacy etc is copied over.

            return symptomManager.Update(dbS);
        }

        [ApiAuthorizationRequired(Operator = ">=", RoleWeight = 10)]
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpPatch]
        [System.Web.Http.Route("api/SymptomsLog/UpdateField")]
        public ServiceResult UpdateSymptomLogField(string symptomLogUUID, string fieldName, string fieldValue)
        {
            if (string.IsNullOrWhiteSpace(symptomLogUUID))
                return ServiceResponse.Error("You must provide a UUID.");

            if (string.IsNullOrWhiteSpace(fieldName))
                return ServiceResponse.Error("You must provide a field name.");

            if (string.IsNullOrWhiteSpace(fieldValue))
                return ServiceResponse.Error("You must provide a field value.");

            if (CurrentUser == null)
                return ServiceResponse.Error("You must be logged in to access this function.");

            SymptomManager symptomManager = new SymptomManager(Globals.DBConnectionKey, this.GetAuthToken(Request));

            SymptomLog sl = symptomManager.GetSymptomLogBy(symptomLogUUID);

            if (sl == null)
                return ServiceResponse.Error("Could not find the log item.");

            if (CurrentUser.UUID != sl.CreatedBy)
                return ServiceResponse.Error("You are not authorized to change this item.");

            bool success = false;
            fieldName = fieldName.ToLower();

            switch (fieldName)
            {
                case "duration":
                    sl.Duration = fieldValue.ConvertTo<float>(out success);
                    if (!success)
                        return ServiceResponse.Error("Invalid field value.");
                    break;

                case "durationmeasure":
                    sl.DurationMeasure = fieldValue;
                    break;

                default:
                    return ServiceResponse.Error("Field " + fieldName + " not supported.");
            }
            return symptomManager.Update(sl);
        }

        #endregion SymptomLog
    }
}