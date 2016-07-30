using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Stayzey.Models;
using System.Data.SqlClient;
using System.Collections;

namespace Stayzey.Controllers
{
    
    public class HomeController : StayzeyAbstractController
    {
        // GET: Home
        public ActionResult Index()
        {
            return View();
        }
        
    }
}