using Stayzey.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Stayzey
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        protected void Application_BeginRequest(Object sender, EventArgs e)
        {
            //Create an individual database instance for each request
            HttpContext.Current.Items["StayzeyDatabase"] = new StayzeyDatabase();
        }

        protected void Application_EndRequest(Object sender, EventArgs e)
        {
            //Close db connection at the end of request
            StayzeyDatabase db = (StayzeyDatabase)HttpContext.Current.Items["StayzeyDatabase"];
            db.Close();
        }
    }

}