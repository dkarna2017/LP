using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using EDDY.IS.LeadPing.Service.V3.DTO;

namespace EDDY.IS.LeadPing.Service.V3
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService" in both code and config file together.
    [ServiceContract]
    public interface IService
    {
        [OperationContract]
        ValidationResponse RetrieveInstitutionValidationErrors(ValidationRequest ValidationRequest, String ServiceKey);

        [OperationContract]
        void ProcessMPICSubmission(MpicProspectInfo ProspectData, int SubmissionID);

        [OperationContract]
        void FireEmailDupeCheck(String Email, String LeadID);

        [OperationContract]
        void FirePhoneDupeCheck(String Phone);

        [OperationContract]
        Boolean IsMobile(String Phone);

        [OperationContract]
        String ClearCache(String ServiceKey);


    }


}
