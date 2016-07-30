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
        protected StayzeyDatabase db
        {
            get
            {
                return (StayzeyDatabase)HttpContext.Items["StayzeyDatabase"];
            }
        }

        protected JsonResult JsonAllowGet(object data)
        {
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        protected bool IsEmpty(string s)
        {
            if (s == null) return true;
            if (s.Trim().Length == 0) return true;
            return false;
        }

        protected bool IsValidEmail(string email)
        {
            string validEmailPattern = @"^(?!\.)(""([^""\r\\]|\\[""\r\\])*""|"
                + @"([-a-z0-9!#$%&'*+/=?^_`{|}~]|(?<!\.)\.)*)(?<!\.)"
                + @"@[a-z0-9][\w\.-]*[a-z0-9]\.[a-z][a-z\.]*[a-z]$";

            Regex emailRegex = new Regex(validEmailPattern, RegexOptions.IgnoreCase);

            return emailRegex.IsMatch(email);
        }

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