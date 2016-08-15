/*
 * Home Controller
 * Author: Ricky Sun
 * Date: 08/06/2016
 * 
 * Display the index page of Stayzey website
 */

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
        // GET: Display the index page
        public ActionResult Index()
        {
            //Show login page for trial version
            /*
            if(Session["login_user"]==null)
            {
                return View("Login");
            }
            */

            //Just display the index page
            return View();
        }

        
    }
}