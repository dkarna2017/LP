using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Caching;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading;
using EDDY.IS.LeadPing.BusinessLayer;
using EDDY.IS.LeadPing.Service.V3.DTO;
using EDDY.IS.Core.CustomException;
using System.Reflection;
using System.Text.RegularExpressions;
using EDDY.IS.Core.Logging;

namespace EDDY.IS.LeadPing.Service.V3
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service.svc or Service.svc.cs at the Solution Explorer and start debugging.

    public class Service : IService
    {
        #region "Class Properties"
        public static ObjectCache Cache = MemoryCache.Default;
        private Helper Helper
        {
            get
            {
                if (Cache["EDDY.IS.LeadPing.Service.Helper"] == null)
                {
                    Cache["EDDY.IS.LeadPing.Service.Helper"] = EDDY.IS.LeadPing.BusinessLayer.Factory.CreateLeadPingHelper();
                }
                return (dynamic)Cache["EDDY.IS.LeadPing.Service.Helper"];
            }
        }
        #endregion


        public String ClearCache(String ServiceKey)
        {
            if (System.Configuration.ConfigurationManager.AppSettings["ServiceKey"] != ServiceKey)
            {
                return "Invalid Service Key";
            }
            else
            {
                Cache["EDDY.IS.LeadPing.Service.Helper"] = EDDY.IS.LeadPing.BusinessLayer.Factory.CreateLeadPingHelper();
                return "Cache Cleared";
            }
        }

        public Boolean IsMobile(String Phone)
        {
            DateTime RequestDate = DateTime.Now;
            Guid SessionID = Guid.NewGuid();
            EDDY.IS.Core.Logging.PerformanceLog Log = new EDDY.IS.Core.Logging.PerformanceLog();
            Log.StartLog(EDDY.IS.Core.ISApplication.Leadping,
                MethodBase.GetCurrentMethod().DeclaringType.Namespace
                + "." + MethodBase.GetCurrentMethod().DeclaringType.Name
                + "." + MethodBase.GetCurrentMethod().Name
                , null, Phone);
            Boolean ReturnValue = new Boolean();
            try
            {
                Phone = Regex.Replace(Phone, "[^.0-9]", "");
                return this.Helper.IsMobilePhone(Phone);
            }
            catch (Exception Ex)
            {
                ISException isEx = new ISException(Ex,
                MethodBase.GetCurrentMethod().DeclaringType.Namespace
                + "." + MethodBase.GetCurrentMethod().DeclaringType.Name
                + "." + MethodBase.GetCurrentMethod().Name, Phone);
                isEx.Save(true);
                return true;
            }
            finally
            {
                Log.EndLog(ReturnValue);
            }
        }



        public void ProcessMPICSubmission(MpicProspectInfo ProspectData, int SubmissionID)
        {
            try
            {
                EDDY.IS.LeadPing.Domain.MpicProspectInfo ProspectInput = TransLateToInternalProspect(ProspectData);

                this.Helper.ProcessMpicSubmission(ProspectInput, SubmissionID);
            }
            catch (Exception Ex)
            {
                ISException isEx = new ISException(Ex,
                MethodBase.GetCurrentMethod().DeclaringType.Namespace
                + "." + MethodBase.GetCurrentMethod().DeclaringType.Name
                + "." + MethodBase.GetCurrentMethod().Name, ProspectData, SubmissionID);
                isEx.Save(true);
            }
        }





        public ValidationResponse RetrieveInstitutionValidationErrors(ValidationRequest ValidationRequest, String ServiceKey)
        {

            String SoapUIRequest = "";

            //Call our SOAP UI Message Builder
            SoapUIRequest = GenerateSoapUIRequest(ValidationRequest, ServiceKey);

            DateTime RequestDate = DateTime.Now;
            Guid SessionID = Guid.NewGuid();
            EDDY.IS.Core.Logging.PerformanceLog Log = new EDDY.IS.Core.Logging.PerformanceLog();
            Log.StartLog(EDDY.IS.Core.ISApplication.Leadping,
                MethodBase.GetCurrentMethod().DeclaringType.Namespace
                + "." + MethodBase.GetCurrentMethod().DeclaringType.Name
                + "." + MethodBase.GetCurrentMethod().Name
                , null, ValidationRequest, ServiceKey);
            ValidationResponse ReturnValue = new ValidationResponse();
            ReturnValue.Status = ServiceCallStatus.Sucess;
            ReturnValue.ServerMessage = "";
            try
            {
                if (System.Configuration.ConfigurationManager.AppSettings["ServiceKey"] != ServiceKey)
                {
                    ReturnValue.ServerMessage = "Invalid Service Key";
                    ReturnValue.Status = ServiceCallStatus.Failure;
                    return ReturnValue;
                }

                String ValidationErrorCheck = this.GetValidationErrorMessage(ValidationRequest.ProspectInfo);

                if (ValidationErrorCheck != null)
                {
                    ReturnValue.ServerMessage = ValidationErrorCheck;
                    ReturnValue.Status = ServiceCallStatus.Failure;
                    return ReturnValue;
                }


                EDDY.IS.LeadPing.Domain.ProspectInfo ProspectInput = TransLateToInternalProspect(ValidationRequest.ProspectInfo);


                EDDY.IS.LeadPing.Domain.ValidationRequest Request = new LeadPing.Domain.ValidationRequest();
                Request.InsitutionConfigs = new List<Domain.InstitutionConfig>();

                foreach (InstitutionConfig InsitutionConfig in ValidationRequest.InsitutionConfigs)
                {
                    EDDY.IS.LeadPing.Domain.InstitutionConfig Biz_InsitutionConfig = new Domain.InstitutionConfig();
                    Biz_InsitutionConfig.InstitutionID = InsitutionConfig.InstitutionID;
                    Biz_InsitutionConfig.ClientRelationshipID = InsitutionConfig.ClientrelationshipID;
                    Biz_InsitutionConfig.BachelorsAvailable = InsitutionConfig.BachelorsAvailable;
                    Biz_InsitutionConfig.CampusConfigs = new List<Domain.CampusConfig>();
                    foreach (CampusConfig CampusConfig in InsitutionConfig.CampusConfigs)
                    {
                        EDDY.IS.LeadPing.Domain.CampusConfig Biz_CampusConfig = new Domain.CampusConfig();
                        Biz_CampusConfig.Address = CampusConfig.Address;
                        Biz_CampusConfig.CampusID = CampusConfig.CampusID;
                        Biz_CampusConfig.City = CampusConfig.City;
                        Biz_CampusConfig.IsOnline = CampusConfig.IsOnline;
                        Biz_CampusConfig.MilesFromUser = CampusConfig.MilesFromUser;
                        Biz_CampusConfig.State = CampusConfig.State;
                        Biz_CampusConfig.ZipCode = CampusConfig.ZipCode;
                        Biz_InsitutionConfig.CampusConfigs.Add(Biz_CampusConfig);
                    }


                    Request.InsitutionConfigs.Add(Biz_InsitutionConfig);
                }


                Request.ProspectInfo = ProspectInput;


                EDDY.IS.LeadPing.Domain.ValidationResponse Response = this.Helper.ProcessValidation(Request, SoapUIRequest);
                ReturnValue = this.TranslateValidationResponse(Response, SoapUIRequest);
            }
            catch (Exception Ex)
            {
                ISException isEx = new ISException(Ex,
                MethodBase.GetCurrentMethod().DeclaringType.Namespace
                + "." + MethodBase.GetCurrentMethod().DeclaringType.Name
                + "." + MethodBase.GetCurrentMethod().Name, ValidationRequest, ServiceKey, SoapUIRequest);
                ReturnValue.ServerMessage = GetaAllMessages(Ex);
                ReturnValue.Status = ServiceCallStatus.Failure;

                isEx.Save(true);
                return ReturnValue;
            }
            finally
            {
                Log.EndLog(ReturnValue);
            }

            return ReturnValue;

        }


        private string GetaAllMessages(Exception exp)
        {
            string Message = string.Empty;
            Exception innerException = exp;
            try
            {
                do
                {
                    Message = Message + (string.IsNullOrEmpty(innerException.Message) ? string.Empty : innerException.Message) + " ";
                    innerException = innerException.InnerException;
                }
                while (innerException != null);
            }
            catch (Exception Ex)
            {
                ISException isEx = new ISException(Ex,
                MethodBase.GetCurrentMethod().DeclaringType.Namespace
                + "." + MethodBase.GetCurrentMethod().DeclaringType.Name
                + "." + MethodBase.GetCurrentMethod().Name, exp);
                isEx.Save(true);
                return Message; //return as much info as possible
            }

            return Message;
        }



        public void FireEmailDupeCheck(String Email, String LeadID)
        {
            ThreadPool.QueueUserWorkItem(o => this.Helper.ProcessEmailDupeCheck(Email, LeadID));
        }

        public void FirePhoneDupeCheck(String Phone)
        {
            ThreadPool.QueueUserWorkItem(o => this.Helper.ProcessPhoneDupeCheck(Phone));
        }

        private String GetValidationErrorMessage(ProspectInfo ProspectInfo)
        {
            try
            {
                if (ProspectInfo.Age == 0)
                    return "Age must be greater than zero";

                if (ProspectInfo.City == null)
                    return "City is required";
                if (ProspectInfo.City.Length == 0)
                    return "A City value is required";


                if (ProspectInfo.CountryID == 0)
                    return "An I.S. system country ID value is required";

                if (ProspectInfo.EducationLevelID == 0)
                    return "An I.S. system Education Level ID value is required";

                if (ProspectInfo.Email == null)
                    return "Email is required";
                if (ProspectInfo.Email.Length == 0)
                    return "A value for Email is required";

                if (ProspectInfo.FirstName == null)
                    return "FirstName is required";
                if (ProspectInfo.FirstName.Length == 0)
                    return "A value for FirstName is required";

                if (ProspectInfo.LastName == null)
                    return "LastName is required";
                if (ProspectInfo.LastName.Length == 0)
                    return "A value for LastName is required";

                if (ProspectInfo.Phone1 == null)
                    return "Phone1 is required";

                if (ProspectInfo.HighSchoolGradyear == 0)
                    return "High School Grad year is required";


                if (ProspectInfo.Phone1.Length == 0)
                    return "A value for Phone1 is required";

                if (ProspectInfo.StateID == 0)
                    return "An I.S. system State ID value is required";

                if (ProspectInfo.ZipCode == null)
                    return "ZipCode is required";
                if (ProspectInfo.ZipCode.Length == 0)
                    return "A value for ZipCode is required";

                if (ProspectInfo.StreetAddress == null)
                    return "StreetAddress is required";

                if (ProspectInfo.StreetAddress.Length == 0)
                    return "A value for StreetAddress is required";

            }
            catch (Exception Ex)
            {
                ISException isEx = new ISException(Ex,
                MethodBase.GetCurrentMethod().DeclaringType.Namespace
                + "." + MethodBase.GetCurrentMethod().DeclaringType.Name
                + "." + MethodBase.GetCurrentMethod().Name, ProspectInfo);
                isEx.Save(true);
                throw Ex;
            }


            return null;
        }



        public String GenerateSoapUIRequest(ValidationRequest Request, String ServiceKey)
        {


            try
            {

                StringBuilder ReturnString = new StringBuilder();
                ReturnString.Append("<soapenv:Envelope xmlns:soapenv=");
                ReturnString.Append("\"http://schemas.xmlsoap.org/soap/envelope/\"");
                ReturnString.Append(" xmlns:tem=\"http://tempuri.org/\" xmlns:eddy=\"http://schemas.datacontract.org/2004/07/EDDY.IS.LeadPing.Service.V3.DTO\">");



                ReturnString.Append("<soapenv:Header/>" + "<soapenv:Body>");


                ReturnString.Append("<tem:RetrieveInstitutionValidationErrors><tem:ValidationRequest><eddy:InsitutionConfigs>");

                foreach (InstitutionConfig IConfig in Request.InsitutionConfigs)
                {

                    ReturnString.Append("<eddy:InstitutionConfig>");
                    ReturnString.Append("<eddy:CampusConfigs>");

                    foreach (CampusConfig CConfig in IConfig.CampusConfigs)
                    {
                        ReturnString.Append("<eddy:CampusConfig>");



                        if (CConfig.Address != null)
                        {
                            ReturnString.Append("<eddy:Address>" + CConfig.Address.ToString() + "</eddy:Address>");

                        }



                        ReturnString.Append("<eddy:CampusID>" + CConfig.CampusID.ToString() + "</eddy:CampusID>");




                        if (CConfig.City != null)
                        {
                            ReturnString.Append("<eddy:City>" + CConfig.City.ToString() + "</eddy:City>");


                        }



                        ReturnString.Append(CConfig.IsOnline ? "<eddy:IsOnline>1</eddy:IsOnline>" : "<eddy:IsOnline>0</eddy:IsOnline>");



                        ReturnString.Append("<eddy:MilesFromUser>" + CConfig.MilesFromUser.ToString() + "</eddy:MilesFromUser>");


                        if (CConfig.State != null)
                        {

                            ReturnString.Append("<eddy:State>" + CConfig.State.ToString() + "</eddy:State>");
                        }

                        if (CConfig.ZipCode != null)
                        {

                            ReturnString.Append("<eddy:ZipCode>" + CConfig.ZipCode.ToString() + "</eddy:ZipCode>");
                        }


                        ReturnString.Append("</eddy:CampusConfig>");

                    }

                    ReturnString.Append("</eddy:CampusConfigs>");
                    ReturnString.Append("<eddy:InstitutionID>" + IConfig.InstitutionID.ToString() + "</eddy:InstitutionID>");
                    ReturnString.Append("</eddy:InstitutionConfig>");

                }

                ReturnString.Append("</eddy:InsitutionConfigs>");
                ReturnString.Append("<eddy:ProspectInfo>" +
                                "<eddy:AddressLine2/>" +
                                "<eddy:Age>" + Request.ProspectInfo.Age.ToString() + "</eddy:Age>" +
                                "<eddy:City>" + Request.ProspectInfo.City + "</eddy:City>" +
                                "<eddy:CountryID>" + Request.ProspectInfo.CountryID.ToString() + "</eddy:CountryID>" +
                                "<eddy:EducationLevelID>" + Request.ProspectInfo.EducationLevelID.ToString() + "</eddy:EducationLevelID>" +
                                "<eddy:Email>" + Request.ProspectInfo.Email + "</eddy:Email>" +
                                "<eddy:FirstName>" + Request.ProspectInfo.FirstName + "</eddy:FirstName>" +
                                "<eddy:HighSchoolGradyear>" + Request.ProspectInfo.HighSchoolGradyear.ToString() + "</eddy:HighSchoolGradyear>" +
                                "<eddy:LastName>" + Request.ProspectInfo.LastName + "</eddy:LastName>");



                ReturnString.Append(Request.ProspectInfo.MilitaryAffiliation ? "<eddy:MilitaryAffiliation>1</eddy:MilitaryAffiliation>" : "<eddy:MilitaryAffiliation>0</eddy:MilitaryAffiliation>");


                if (Request.ProspectInfo.MilitaryStatusId != 0)
                {
                    ReturnString.Append("<eddy:MilitaryStatusId>" + Request.ProspectInfo.MilitaryStatusId.ToString() + "</eddy:MilitaryStatusId>");

                }


                ReturnString.Append("<eddy:Phone1>" + Request.ProspectInfo.Phone1.ToString() + "</eddy:Phone1>");

                if (Request.ProspectInfo.Phone2 != null)
                {
                    ReturnString.Append("<eddy:Phone2>" + Request.ProspectInfo.Phone2.ToString() + "</eddy:Phone2>");
                }


                ReturnString.Append("<eddy:StateID>" + Request.ProspectInfo.StateID.ToString() + "</eddy:StateID>" +
                "<eddy:StreetAddress>" + Request.ProspectInfo.StreetAddress + "</eddy:StreetAddress>" +
                "<eddy:ZipCode>" + Request.ProspectInfo.ZipCode.ToString() + "</eddy:ZipCode>");



                ReturnString.Append("</eddy:ProspectInfo></tem:ValidationRequest>" + "<tem:ServiceKey>" + ServiceKey + "</tem:ServiceKey>");
                ReturnString.Append("</tem:RetrieveInstitutionValidationErrors></soapenv:Body></soapenv:Envelope>");

                return ReturnString.ToString();

            }


            catch (Exception Ex)
            {

                //adding logging for exception database

                ISException isEx = new ISException(Ex,
                MethodBase.GetCurrentMethod().DeclaringType.Namespace
                + "." + MethodBase.GetCurrentMethod().DeclaringType.Name
                + "." + MethodBase.GetCurrentMethod().Name, Request, ServiceKey);
                isEx.Save(true);

                return "SOAP UI message generation failed";

            }
        }

        #region "Internal DTO Helpers"
        private EDDY.IS.LeadPing.Domain.ProspectInfo TransLateToInternalProspect(ProspectInfo ProspectInfo)
        {
            try
            {
                EDDY.IS.LeadPing.Domain.ProspectInfo ReturnValue = new EDDY.IS.LeadPing.Domain.ProspectInfo();
                ReturnValue.AddressLine2 = ProspectInfo.AddressLine2;
                ReturnValue.Age = ProspectInfo.Age;
                ReturnValue.City = ProspectInfo.City;
                ReturnValue.CountryID = ProspectInfo.CountryID;
                ReturnValue.EducationLevelID = ProspectInfo.EducationLevelID;
                ReturnValue.MilitaryAffiliation = ProspectInfo.MilitaryAffiliation;
                ReturnValue.MilitaryStatusId = ProspectInfo.MilitaryStatusId;
                ReturnValue.ExternalLeadId = ProspectInfo.ExternalLeadId;
                ReturnValue.Email = ProspectInfo.Email;
                ReturnValue.FirstName = ProspectInfo.FirstName;
                ReturnValue.LastName = ProspectInfo.LastName;
                ReturnValue.Phone1 = ProspectInfo.Phone1;
                ReturnValue.HighSchoolGradYear = ProspectInfo.HighSchoolGradyear;
                if (ProspectInfo.Phone2 == null)
                {
                    ReturnValue.Phone2 = ProspectInfo.Phone1;
                }
                else if (ProspectInfo.Phone2.Length == 0)
                {
                    ReturnValue.Phone2 = ProspectInfo.Phone1;
                }
                else
                {
                    ReturnValue.Phone2 = ProspectInfo.Phone2;
                }

                ReturnValue.StateID = ProspectInfo.StateID;
                ReturnValue.ZipCode = ProspectInfo.ZipCode;
                ReturnValue.StreetAddress = ProspectInfo.StreetAddress;
                ReturnValue.AddressLine2 = ProspectInfo.AddressLine2;
                ReturnValue.DesiredStartDate = ProspectInfo.DesiredStartDate;

                return ReturnValue;
            }
            catch (Exception Ex)
            {
                ISException isEx = new ISException(Ex,
                MethodBase.GetCurrentMethod().DeclaringType.Namespace
                + "." + MethodBase.GetCurrentMethod().DeclaringType.Name
                + "." + MethodBase.GetCurrentMethod().Name, ProspectInfo);
                isEx.Save(true);
                throw Ex;
            }

        }



        private EDDY.IS.LeadPing.Domain.MpicProspectInfo TransLateToInternalProspect(MpicProspectInfo ProspectInfo)
        {
            try
            {
                EDDY.IS.LeadPing.Domain.MpicProspectInfo ReturnValue = new EDDY.IS.LeadPing.Domain.MpicProspectInfo();
                ReturnValue.AddressLine2 = ProspectInfo.AddressLine2;
                ReturnValue.City = ProspectInfo.City;
                ReturnValue.Email = ProspectInfo.Email;
                ReturnValue.FirstName = ProspectInfo.FirstName;
                ReturnValue.LastName = ProspectInfo.LastName;
                ReturnValue.Phone1 = ProspectInfo.Phone1;
                if (ProspectInfo.Phone2 == null)
                {
                    ReturnValue.Phone2 = ProspectInfo.Phone1;
                }
                else if (ProspectInfo.Phone2.Length == 0)
                {
                    ReturnValue.Phone2 = ProspectInfo.Phone1;
                }
                else
                {
                    ReturnValue.Phone2 = ProspectInfo.Phone2;
                }

                ReturnValue.StateAbbreviation = ProspectInfo.StateCode;
                ReturnValue.ZipCode = ProspectInfo.ZipCode;
                ReturnValue.StreetAddress = ProspectInfo.StreetAddress;
                ReturnValue.AddressLine2 = ProspectInfo.AddressLine2;

                return ReturnValue;
            }
            catch (Exception Ex)
            {
                ISException isEx = new ISException(Ex,
                MethodBase.GetCurrentMethod().DeclaringType.Namespace
                + "." + MethodBase.GetCurrentMethod().DeclaringType.Name
                + "." + MethodBase.GetCurrentMethod().Name, ProspectInfo);
                isEx.Save(true);
                throw Ex;
            }

        }

        private ValidationResponse TranslateValidationResponse(EDDY.IS.LeadPing.Domain.ValidationResponse InternalResponse, String SoapUIRequest)
        {
            try
            {
                ValidationResponse ReturnValue = new ValidationResponse();
                ReturnValue.ImmediateResponses = new List<FailedInstitutionResponse>();
                ReturnValue.RelatedResponses = new List<FailedInstitutionResponse>();

                foreach (EDDY.IS.LeadPing.Domain.FailedInstitutionResponse Failure in InternalResponse.ImmediateResponses)
                {

                    if (Failure != null)
                    {


                        FailedInstitutionResponse newItem = new FailedInstitutionResponse();

                        newItem.ProductIDs = new List<Int32>();

                        if (Failure.ProductIDs != null && Failure.ProductIDs.Count != 0)
                        {

                            newItem.ProductIDs = Failure.ProductIDs;
                        }


                        newItem.InstitutionID = Failure.InstitutionID;
                        newItem.CampusID = Failure.CampusID;
                        // newItem.ProductID = Failure.ProductID;
                        newItem.Message = Failure.Message;
                        if (Failure.Type == LeadPing.Domain.ValidationType.Duplicate)
                            newItem.Type = DTO.Type.Duplicate;
                        if (Failure.Type == LeadPing.Domain.ValidationType.ScoreCheck)
                        {
                            newItem.Type = DTO.Type.ScoreCheck;
                            newItem.IsEdmcWtDupe = Failure.IsEdmcWtDupe;
                        }
                        if (Failure.Type == LeadPing.Domain.ValidationType.ValidationCheck)
                            newItem.Type = DTO.Type.ValidationCheck;
                        newItem.IsInternal = Failure.IsInternal;
                        newItem.IsOnline = Failure.IsOnline;


                        ReturnValue.ImmediateResponses.Add(newItem);

                    }
                }


                foreach (EDDY.IS.LeadPing.Domain.FailedInstitutionResponse Failure in InternalResponse.RelatedResponses)
                {

                    if (Failure != null)
                    {

                        FailedInstitutionResponse newItem = new FailedInstitutionResponse();


                        newItem.ProductIDs = new List<Int32>();

                        if (Failure.ProductIDs != null && Failure.ProductIDs.Count != 0)
                        {

                            newItem.ProductIDs = Failure.ProductIDs;
                        }

                        newItem.InstitutionID = Failure.InstitutionID;
                        newItem.CampusID = Failure.CampusID;
                        // newItem.ProductID = Failure.ProductID;
                        newItem.Message = Failure.Message;
                        if (Failure.Type == LeadPing.Domain.ValidationType.Duplicate)
                            newItem.Type = DTO.Type.Duplicate;
                        if (Failure.Type == LeadPing.Domain.ValidationType.ScoreCheck)
                        {
                            newItem.Type = DTO.Type.ScoreCheck;
                            newItem.IsEdmcWtDupe = Failure.IsEdmcWtDupe;
                        }
                        if (Failure.Type == LeadPing.Domain.ValidationType.ValidationCheck)
                            newItem.Type = DTO.Type.ValidationCheck;


                        newItem.IsInternal = Failure.IsInternal;
                        newItem.IsOnline = Failure.IsOnline;
                        ReturnValue.RelatedResponses.Add(newItem);
                    }
                }

                return ReturnValue;
            }
            catch (Exception Ex)
            {
                ISException isEx = new ISException(Ex,
                MethodBase.GetCurrentMethod().DeclaringType.Namespace
                + "." + MethodBase.GetCurrentMethod().DeclaringType.Name
                + "." + MethodBase.GetCurrentMethod().Name, InternalResponse, SoapUIRequest);
                isEx.Save(true);
                throw Ex;
            }

        }


        #endregion










    }
}
