using System;
using System.Collections.Generic;
using System.Linq;
//using System.Runtime.Caching;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;


namespace EDDY.IS.LeadPing.Service
{
    public class Global : System.Web.HttpApplication
    {
        //public static ObjectCache Cache = MemoryCache.Default;
        protected void Application_Start(object sender, EventArgs e)
        {
            //Cache["EDDY.IS.LeadPing.Service.Helper"] = EDDY.IS.LeadPing.BusinessLayer.Factory.CreateLeadPingHelper();
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}