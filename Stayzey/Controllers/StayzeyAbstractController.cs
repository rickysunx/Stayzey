/*
 * Abstract Stayzey Controller
 * Author: Ricky Sun
 * Date: 23/06/2016
 * 
 * Provide common functions of all the controllers.s
 */

using Stayzey.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace Stayzey.Controllers
{
    public abstract class StayzeyAbstractController : Controller
    {
        //Return db instance
        protected StayzeyDatabase db
        {
            get
            {
                return (StayzeyDatabase)HttpContext.Items["StayzeyDatabase"];
            }
        }

        //Return json encoded result by the data, meanwhile allow Get request
        protected JsonResult JsonAllowGet(object data)
        {
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        //Check if the string is empty
        protected bool IsEmpty(string s)
        {
            if (s == null) return true;
            if (s.Trim().Length == 0) return true;
            return false;
        }

        //Check if the string is a valid email
        protected bool IsValidEmail(string email)
        {
            string validEmailPattern = @"^(?!\.)(""([^""\r\\]|\\[""\r\\])*""|"
                + @"([-a-z0-9!#$%&'*+/=?^_`{|}~]|(?<!\.)\.)*)(?<!\.)"
                + @"@[a-z0-9][\w\.-]*[a-z0-9]\.[a-z][a-z\.]*[a-z]$";

            Regex emailRegex = new Regex(validEmailPattern, RegexOptions.IgnoreCase);

            return emailRegex.IsMatch(email);
        }

        //Return the logined user id
        protected int GetLoginUserId()
        {
            if(Session["login_user"] !=null)
            {
                Hashtable user = (Hashtable)Session["login_user"];
                int userid = (int)user["UserId"];
                return userid;
            }
            return 0;
        }
    }
}