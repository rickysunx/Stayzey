using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Stayzey.Controllers
{
    /**
     * Booking Status: 
     *  0-Newly Created
     *  1-Landlord Accepted
     *  2-Landlord Rejected
     *  3-Paid
     *  4-Cancelled
     *  5-Deleted
     *  6-Reviewed
     */

    
    public class UserController : StayzeyAbstractController
    {

        public static HashSet<string> AcceptFileType;
        public static string[] BookingStatuses = new string[] {
            "Pending landlord acceptance",
            "Landlord Accepted",
            "Landlord Rejected",
            "Paid","Cancelled","Deleted","Reviewed"
        };
        
        static UserController()
        {
            AcceptFileType = new HashSet<string>();
            AcceptFileType.Add(".jpg");
            AcceptFileType.Add(".jpeg");
            AcceptFileType.Add(".gif");
            AcceptFileType.Add(".png");
        }

        // GET: User
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult SaveProfile(
            string FirstName,
            string LastName,
            string Gender,
            string BirthDay,
            string BirthMonth,
            string BirthYear,
            string Email,
            string Phone,
            string ProfileImage
        )
        {
            Hashtable result = new Hashtable();
            int userid = GetLoginUserId();
            if (userid == 0)
            {
                result["success"] = 0;
                result["error_info"] = "Please log in first";
                return JsonAllowGet(result);
            }

            try
            {
                Hashtable item = new Hashtable();
                item["UserId"] = userid;
                item["FirstName"] = FirstName;
                item["Surname"] = LastName;
                item["Email"] = Email;
                item["Phone"] = Phone;
                item["Gender"] = Gender;
                item["Avatar"] = ProfileImage;

                int day = int.Parse(BirthDay);
                int month = int.Parse(BirthMonth);
                int year = int.Parse(BirthYear);
                DateTime dateOfBirth = new DateTime(year,month,day);
                item["DateOfBirth"] = dateOfBirth;

                db.Update("Users", item, "UserId");

                //Reload User Object
                List<Hashtable> userList = db.Query("select * from Users where UserId="+userid);
                if(userList.Count>0)
                {
                    Session["login_user"] = userList[0];
                }
                
                result["success"] = 1;
            }
            catch(Exception ex)
            {
                result["success"] = 0;
                result["error_info"] = ex.Message;
            }
            
            return JsonAllowGet(result);
        }

        public ActionResult MyProfile()
        {
            int userid = GetLoginUserId();
            if (userid == 0)
            {
                return Redirect("/#Login");
            }
            string sql = "select * from Users where UserId="+userid;
            List<Hashtable> data = db.Query(sql);
            if(data.Count>0)
            {
                ViewBag.User = data[0];
            }
            else
            {
                return HttpNotFound();
            }
            return View();
        }

        public ActionResult Inbox()
        {
            int userid = GetLoginUserId();
            if (userid == 0)
            {
                return Redirect("/#Login");
            }
            return View();
        }

        public ActionResult Listings()
        {
            int userid = GetLoginUserId();
            if (userid == 0)
            {
                return Redirect("/#Login");
            }

            List<Hashtable> rooms = db.Query("select r.*, (select count(1) from Bookings b where r.RoomId = b.RoomId and b.BookingStatus<>5) BookingCount from Rooms r where r.UserId =" + userid);

            ViewBag.Rooms = rooms;

            return View();
        }

        public ActionResult Bookings(string RoomId)
        {
            int userid = GetLoginUserId();
            if (userid == 0)
            {
                return Redirect("/#Login");
            }

            Hashtable loginUser = (Hashtable)Session["login_user"];
            int userType = (int)loginUser["UserType"];

            string sql = "select b.BookingId,b.RoomId,b.Price,b.StartDate,b.EndDate,b.BookingStatus," +
                " r.RoomTitle,r.Location,r.RoomImage " +
                " from bookings b,rooms r where b.BookingStatus<>5 and b.roomid=r.roomid ";
            if(userType==1)
            {
                //Landlord
                if (IsEmpty(RoomId))
                {
                    sql += " and r.UserId = " + userid;
                }
                else
                {
                    int nRoomId = int.Parse(RoomId);
                    sql += " and r.RoomId = " + nRoomId;
                }
            }
            else
            {
                sql += " and b.UserId = " + userid;
            }

            sql += " order by BookingTime desc ";


            List<Hashtable> bookings = db.Query(sql);
            ViewBag.Bookings = bookings;
            ViewBag.BookingStatuses = BookingStatuses;
            ViewBag.UserType = userType;

            return View();
        }

        public ActionResult WishLists()
        {
            int userid = GetLoginUserId();
            if (userid == 0)
            {
                return Redirect("/#Login");
            }
            return View();
        }

        public ActionResult LogIn(string email,string password)
        {
            Hashtable data = new Hashtable();
            bool checkPassed = true;
            data["success"] = 1;

            if (IsEmpty(email))
            {
                checkPassed = false;
                data["error_email"] = "Should not be empty";
            }

            if(IsEmpty(password))
            {
                checkPassed = false;
                data["error_password"] = "Should not be empty";
            }

            if (checkPassed)
            {
                List<Hashtable> result = db.Query("select * from Users where email=@email and Blocked<>1",
                    new SqlParameter[] { new SqlParameter("@email",email) });
                if(result.Count==1)
                {
                    string passwordInDB = (string)(result[0]["Password"]);
                    if(password==passwordInDB)
                    {
                        Session["login_user"] = result[0];
                    }
                    else
                    {
                        checkPassed = false;
                        data["error_password"] = "Please enter the correct password";
                    }
                }
                else
                {
                    checkPassed = false;
                    data["error_email"] = "Email not found";
                    data["error_password"] = "Please enter the correct password";
                }
            }

            data["success"] = checkPassed ? 1 : 0;


            return JsonAllowGet(data);
        }

        public ActionResult SignUp(
            string firstname,
            string lastname,
            string email,
            string password,
            string confirmpassword,
            string usertype)
        {
            Hashtable data = new Hashtable();
            data["success"] = 1;
            bool checkPassed = true;

            if (IsEmpty(firstname))
            {
                checkPassed = false;
                data["error_firstname"] = "Should not be empty";
            }

            if(IsEmpty(lastname))
            {
                checkPassed = false;
                data["error_lastname"] = "Should not be empty";
            }

            if(IsEmpty(email))
            {
                checkPassed = false;
                data["error_email"] = "Should not be empty";
            }
            else if(!IsValidEmail(email))
            {
                checkPassed = false;
                data["error_email"] = "Not a valid email";
            }

            if(IsEmpty(password))
            {
                checkPassed = false;
                data["error_password"] = "Should not be empty";
            }
            else if(password.Length<6)
            {
                checkPassed = false;
                data["error_password"] = "Length of the password should be more than 6";
            }

            if(IsEmpty(confirmpassword))
            {
                checkPassed = false;
                data["error_confirmpassword"] = "Should not be empty";
            }
            else if(confirmpassword!=password)
            {
                checkPassed = false;
                data["error_confirmpassword"] = "Please enter the same password";
            }

            List<Hashtable> result = db.Query("select * from Users where email=@email",new SqlParameter[] { new SqlParameter("@email",email)});
            if(result.Count>0)
            {
                checkPassed = false;
                data["error_email"] = "Email already exists, use a different one";
            }

            if(checkPassed)
            {
                Hashtable item = new Hashtable();
                item["FirstName"] = firstname;
                item["Surname"] = lastname;
                item["Email"] = email;
                item["Password"] = password;
                item["Avatar"] = "/images/default_profile_image.png";
                item["UserType"] = int.Parse(usertype);
                item["Blocked"] = 0;
                int count = db.Insert("Users", item);
            }
            else
            {
                data["success"] = 0;
            }
            
            return JsonAllowGet(data);
        }

        public ActionResult LogOut()
        {
            Hashtable data = new Hashtable();
            data["success"] = 1;
            Session.Remove("login_user");
            return JsonAllowGet(data);
        }

        public ActionResult NewRoom()
        {
            int userid = GetLoginUserId();
            if (userid == 0)
            {
                return Redirect("/#Login");
            }
            return View("RoomEditor");
        }

        public ActionResult EditRoom(string id)
        {
            int userid = GetLoginUserId();
            if (userid == 0)
            {
                return Redirect("/#Login");
            }

            int nId = 0;

            if(int.TryParse(id,out nId))
            {
                List<Hashtable> data = db.Query("select * from Rooms where RoomId=" + nId + " and UserId=" + userid);
                if(data.Count()>0)
                {
                    ViewBag.Room = data[0];

                    List<Hashtable> dataImages = db.Query("select * from RoomImages where RoomId=" + nId);
                    ViewBag.RoomImages = dataImages;

                    return View("RoomEditor");
                }
            }

            return HttpNotFound();
        }

        public ActionResult DeleteRoom(string id)
        {
            Hashtable result = new Hashtable();
            try
            {
                int userid = GetLoginUserId();
                if (userid == 0)
                {
                    result["error_code"] = 1;
                    throw new Exception("You have to login first");
                }

                int nId = int.Parse(id);
                string sqlCheck = "select count(1) bookingCount from Bookings where RoomId=" + nId;
                List<Hashtable> data = db.Query(sqlCheck);
                int bookingCount = (int)(data[0]["bookingCount"]);
                if(bookingCount>0)
                {
                    throw new Exception("Can not delete this room which has been booked");
                }

                string sqlDelete = "delete from Rooms where RoomId=" + nId + " and UserId=" + userid;
                db.Query(sqlDelete);
                result["success"] = 1;
            }
            catch (Exception ex)
            {
                result["success"] = 0;
                result["error_info"] = ex.Message;
            }
            return JsonAllowGet(result);
        }

        public ActionResult ChangeRoomStatus(string id,string flag)
        {
            Hashtable result = new Hashtable();
            try
            {
                int userid = GetLoginUserId();
                if (userid == 0)
                {
                    result["error_code"] = 1;
                    throw new Exception("You have to login first");
                }

                int nFlag = int.Parse(flag);
                int nId = int.Parse(id);

                string sql = "update Rooms set Available = " + flag + " where RoomId=" + id + " and UserId=" + userid;
                db.Update(sql);

                result["success"] = 1;
            }
            catch (Exception ex)
            {
                result["success"] = 0;
                result["error_info"] = ex.Message;
            }
            

            return JsonAllowGet(result);
        }

        [HttpGet]
        public ActionResult UploadImage(string callback)
        {
            ViewBag.callback = callback;
            return View();
        }

        [HttpPost]
        public ActionResult UploadImage(string callback,HttpPostedFileBase file)
        {
            ViewBag.callback = callback;
            if(file!=null && file.ContentLength>0)
            {
                string oriFileName = file.FileName.ToLower();
                string fileExtension = Path.GetExtension(oriFileName);

                if(AcceptFileType.Contains(fileExtension))
                {
                    string imageFolder = Server.MapPath("~/UploadImages");
                    if(!Directory.Exists(imageFolder))
                    {
                        Directory.CreateDirectory(imageFolder);
                    }

                    string newFileName = Guid.NewGuid().ToString() + fileExtension;
                    string newFilePath = Path.Combine(imageFolder, newFileName);

                    file.SaveAs(newFilePath);
                    
                    
                    ViewBag.FileName = "/UploadImages/" + newFileName;
                    ViewBag.Success = true;
                    
                }
                else
                {
                    ViewBag.Success = false;
                    ViewBag.ErrInfo = "File should be jpeg, png or gif file";
                }
            }
            else
            {
                ViewBag.Success = false;
                ViewBag.ErrInfo = "Please select a file to upload";
            }
            return View();
        }


        public ActionResult SaveRoom(
            string RoomId,
            string Title,
            string Address,
            string Price,
            string Intro,
            string Latitude,
            string Longitude,
            string[] Amenities,
            string[] RoomImages)
        {
            Hashtable data = new Hashtable();
            data["success"] = 1;
            decimal numberPrice = 0;
            double numberLatitude = 0;
            double numberLongitude = 0;

            int loginUserId = GetLoginUserId();
            if(loginUserId==0)
            {
                data["success"] = 0;
                data["error_info"] = "Please login first";
                return JsonAllowGet(data);
            }

            if (IsEmpty(Title))
            {
                data["success"] = 0;
                data["error_info"] = "Title should not be empty";
                return JsonAllowGet(data);
            }

            if (IsEmpty(Address))
            {
                data["success"] = 0;
                data["error_info"] = "Address should not be empty";
                return JsonAllowGet(data);
            }

            if (IsEmpty(Latitude) || IsEmpty(Longitude))
            {
                data["success"] = 0;
                data["error_info"] = "Please enter a valid address";
                return JsonAllowGet(data);
            }
            else
            {
                bool parsed1 = Double.TryParse(Latitude, out numberLatitude);
                bool parsed2 = Double.TryParse(Longitude, out numberLongitude);
                if (!(parsed1 && parsed2))
                {
                    data["success"] = 0;
                    data["error_info"] = "Address was not parsed correctly";
                    return JsonAllowGet(data);
                }
            }

            if (IsEmpty(Price))
            {
                data["success"] = 0;
                data["error_info"] = "Price should not be empty";
                return JsonAllowGet(data);
            }
            else
            {
                bool priceParsed = Decimal.TryParse(Price, out numberPrice);
                if(!priceParsed)
                {
                    data["success"] = 0;
                    data["error_info"] = "Price should be a number";
                    return JsonAllowGet(data);
                }
            }

            if (IsEmpty(Intro))
            {
                data["success"] = 0;
                data["error_info"] = "Introduction should not be empty";
                return JsonAllowGet(data);
            }
            
            if(RoomImages==null || RoomImages.Count()==0)
            {
                data["success"] = 0;
                data["error_info"] = "Upload at least one image";
                return JsonAllowGet(data);
            }

            int nRoomId = 0;
            int.TryParse(RoomId, out nRoomId);
            
            Hashtable room = new Hashtable();
            if (nRoomId > 0) room["RoomId"] = nRoomId;
            room["RoomTitle"] = Title;
            room["Location"] = Address;
            room["Price"] = Price;
            room["Latitude"] = numberLatitude;
            room["Longitude"] = numberLongitude;
            room["RoomIntro"] = Intro;
            room["RoomImage"] = RoomImages[0];
            room["UserId"] = loginUserId;
            room["CreateTime"] = DateTime.Now;

            string strAmenities = "";
            if (Amenities != null && Amenities.Count() > 0)
            {
                strAmenities = String.Join(",", Amenities);
            }
            room["Amenities"] = strAmenities;
            room["Available"] = 1;


            if (nRoomId > 0)
            {
                db.Update("Rooms", room, "RoomId");
            }
            else
            {
                db.Insert("Rooms", room);
                nRoomId = db.GetLastInsertId();
            }

            db.Update("delete from RoomImages where RoomId=" + nRoomId);

            foreach(string image in RoomImages)
            {
                Hashtable roomImage = new Hashtable();
                roomImage["RoomId"] = nRoomId;
                roomImage["RoomImage"] = image;
                db.Insert("RoomImages", roomImage);
            }
            
            return JsonAllowGet(data);
        }

        public ActionResult RequestToBook(string roomid,string dates)
        {
            Hashtable data = new Hashtable();

            try
            {
                int userid = GetLoginUserId();
                if (userid == 0)
                {
                    data["success"] = 0;
                    data["error_code"] = 1;
                    data["error_info"] = "Please login first";
                    return JsonAllowGet(data);
                }
                //check dates Available
                string[] dataArray = dates.Split("-".ToCharArray());
                DateTime startDate = new DateTime();
                DateTime endDate = new DateTime();
                CultureInfo provider = CultureInfo.InvariantCulture;

                if (dataArray.Count()==2)
                {
                    string strStartDate = dataArray[0].Trim();
                    string strEndDate = dataArray[1].Trim();
                    startDate =  DateTime.ParseExact(strStartDate, "dd/MM/yyyy", provider);
                    endDate = DateTime.ParseExact(strEndDate, "dd/MM/yyyy", provider);
                    if(endDate.Subtract(startDate).TotalDays<7)
                    {
                        throw new Exception("You should book more than one week.");
                    }
                }
                else
                {
                    throw new Exception("Please select the correct check in and check out dates.");
                }

                List<Hashtable> roomList = db.Query("select * from Rooms where RoomId=@RoomId",
                    new SqlParameter[] { new SqlParameter("RoomId",roomid)});

                if(roomList.Count()==0)
                {
                    throw new Exception("Room not found");
                }

                decimal roomPrice = (decimal)roomList[0]["Price"];

                Hashtable item = new Hashtable();

                item["RoomId"] = roomid;
                item["StartDate"] = startDate;
                item["EndDate"] = endDate;
                item["UserId"] = userid;
                item["BookingTime"] = DateTime.Now;
                item["Price"] = roomPrice;
                item["LettingFee"] = roomPrice;
                item["Deposit"] = roomPrice * 2;
                item["Total"] = roomPrice * 4;
                item["BookingStatus"] = 0;

                db.Insert("Bookings",item);

                data["success"] = 1;
            }
            catch (Exception ex)
            {
                data["success"] = 0;
                data["error_code"] = 2;
                data["error_info"] = ex.Message;
            }
            

            return JsonAllowGet(data);
        }


        public ActionResult DeleteBooking(string id)
        {
            Hashtable data = new Hashtable();

            try
            {
                int userid = GetLoginUserId();
                if (userid == 0)
                {
                    data["success"] = 0;
                    data["error_code"] = 1;
                    data["error_info"] = "Please login first";
                    return JsonAllowGet(data);
                }

                int nId = int.Parse(id);

                string sql = "select * from Bookings where RoomId=" + nId;
                List<Hashtable> bookings = db.Query(sql);
                if (bookings.Count() == 0)
                {
                    throw new Exception("Room not found");
                }
                Hashtable booking = bookings[0];
                int bookingStatus = (int)booking["BookingStatus"];
                if (bookingStatus == 5 || bookingStatus == 3 || bookingStatus==1)
                {
                    throw new Exception("Current status does not allow cancellation");
                }
                else
                {
                    sql = "update Bookings set BookingStatus=5 where BookingId=" + nId;
                    db.Update(sql);
                }
                data["success"] = 1;
            }
            catch(Exception ex)
            {
                data["success"] = 0;
                data["error_info"] = ex.Message;
            }
            return JsonAllowGet(data);
        }

        public ActionResult CancelBooking(string id)
        {
            Hashtable data = new Hashtable();

            try
            {
                int userid = GetLoginUserId();
                if (userid == 0)
                {
                    data["success"] = 0;
                    data["error_code"] = 1;
                    data["error_info"] = "Please login first";
                    return JsonAllowGet(data);
                }

                int nId = int.Parse(id);

                string sql = "select * from Bookings where BookingId=" + nId;
                List<Hashtable> bookings = db.Query(sql);
                if (bookings.Count() == 0)
                {
                    throw new Exception("Room not found");
                }
                Hashtable booking = bookings[0];
                int bookingStatus = (int)(booking["BookingStatus"]);
                if(bookingStatus==5 || bookingStatus==3 || bookingStatus == 1)
                {
                    throw new Exception("Current status does not allow cancellation");
                }
                else
                {
                    sql = "update Bookings set BookingStatus=4 where BookingId=" + nId;
                    db.Update(sql);
                }
                data["success"] = 1;
            }
            catch (Exception ex)
            {
                data["success"] = 0;
                data["error_info"] = ex.Message;
            }
            return JsonAllowGet(data);
        }

        public ActionResult AcceptBooking(string id)
        {
            Hashtable data = new Hashtable();

            try
            {
                int userid = GetLoginUserId();
                if (userid == 0)
                {
                    data["success"] = 0;
                    data["error_code"] = 1;
                    data["error_info"] = "Please login first";
                    return JsonAllowGet(data);
                }
                int nId = int.Parse(id);
                db.Update("Update Bookings set BookingStatus=1 where BookingId=" + nId);
                data["success"] = 1;
            }
            catch (Exception ex)
            {
                data["success"] = 0;
                data["error_info"] = ex.Message;
            }
            return JsonAllowGet(data);
        }

        public ActionResult RejectBooking(string id)
        {
            Hashtable data = new Hashtable();

            try
            {
                int userid = GetLoginUserId();
                if (userid == 0)
                {
                    data["success"] = 0;
                    data["error_code"] = 1;
                    data["error_info"] = "Please login first";
                    return JsonAllowGet(data);
                }
                int nId = int.Parse(id);
                db.Update("Update Bookings set BookingStatus=2 where BookingId=" + nId);
                data["success"] = 1;
            }
            catch (Exception ex)
            {
                data["success"] = 0;
                data["error_info"] = ex.Message;
            }
            return JsonAllowGet(data);
        }

        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public ActionResult PayBooking(string id)
        {
            Hashtable data = new Hashtable();

            try
            {
                int userid = GetLoginUserId();
                if (userid == 0)
                {
                    data["success"] = 0;
                    data["error_code"] = 1;
                    data["error_info"] = "Please login first";
                    return JsonAllowGet(data);
                }

                int nId = int.Parse(id);
                List<Hashtable> result = db.Query("select * from Bookings where BookingId=" + nId);
                if (result.Count == 0)
                {
                    throw new Exception("Booking not found");
                }

                decimal totalFee = (decimal)(result[0]["Total"]);
                
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(ValidateServerCertificate);

                WebRequest request = WebRequest.Create("https://api-3t.sandbox.paypal.com/nvp");
                request.Method = "POST";
                request.Credentials = CredentialCache.DefaultCredentials;
                
                string postData = "";

                postData += "USER=" + HttpUtility.UrlEncode("################");
                postData += "&PWD=" + HttpUtility.UrlEncode("#####################");
                postData += "&SIGNATURE=" + HttpUtility.UrlEncode("####################################");
                postData += "&METHOD=" + HttpUtility.UrlEncode("SetExpressCheckout");
                postData += "&VERSION=" + HttpUtility.UrlEncode("93");
                postData += "&PAYMENTREQUEST_0_PAYMENTACTION=" + HttpUtility.UrlEncode("SALE");
                postData += "&PAYMENTREQUEST_0_AMT=" + HttpUtility.UrlEncode("" + totalFee);
                postData += "&PAYMENTREQUEST_0_CURRENCYCODE=" + HttpUtility.UrlEncode("NZD");
                postData += "&RETURNURL=" + HttpUtility.UrlEncode("http://stayzey.azurewebsites.net/User/PaySuccess?id="+nId);
                postData += "&CANCELURL=" + HttpUtility.UrlEncode("http://stayzey.azurewebsites.net/User/PayFail?id=");

                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = byteArray.Length;
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse response = request.GetResponse();
                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();
                response.Close();

                //parse token
                string[] responseValues = responseFromServer.Split('&');
                string tokenValue = null;
                foreach(string responseValue in responseValues)
                {
                    string [] keyValuePair = responseValue.Split('=');
                    string key = keyValuePair[0];
                    string value = keyValuePair[1];

                    if (key == "TOKEN")
                    {
                        tokenValue = HttpUtility.UrlDecode(value);
                    }
                }

                data["success"] = 1;
                data["url"] = "https://www.sandbox.paypal.com/cgi-bin/webscr?cmd=_express-checkout&token=" + tokenValue;

                //int nId = int.Parse(id);
                //db.Update("Update Bookings set BookingStatus=3 where BookingId=" + nId);
                //data["success"] = 1;
            }
            catch (Exception ex)
            {
                data["success"] = 0;
                data["error_info"] = ex.Message;
            }
            return JsonAllowGet(data);
        }

        public ActionResult SaveReview(
            string BookingId,
            string ReviewMark,
            string ReviewContent)
        {
            Hashtable data = new Hashtable();

            try
            {
                int userid = GetLoginUserId();
                if (userid == 0)
                {
                    data["success"] = 0;
                    data["error_code"] = 1;
                    data["error_info"] = "Please login first";
                    return JsonAllowGet(data);
                }

                int nBookingId = int.Parse(BookingId);
                int nReviewMark = int.Parse(ReviewMark);
                string sql = "select b.BookingId BookingId,r.UserId HostId,b.UserId UserId from Rooms r,Bookings b where r.RoomId=b.RoomId and b.BookingId=" + BookingId;

                List<Hashtable> bookings = db.Query(sql);
                if (bookings.Count() > 0)
                {
                    Hashtable booking = bookings[0];
                    Hashtable review = new Hashtable();
                    review["BookingId"] = booking["BookingId"];
                    review["ReviewMark"] = nReviewMark;
                    review["ReviewContent"] = ReviewContent;
                    review["HostId"] = booking["HostId"];
                    review["UserId"] = booking["UserId"];
                    review["ReviewTime"] = DateTime.Now;

                    db.Insert("Reviews",review);

                    sql = "update Bookings set BookingStatus=6 where BookingId = " + nBookingId;
                    db.Update(sql);

                    data["success"] = 1;
                }
                else
                {
                    throw new Exception("Booking not found");
                }

            }
            catch (Exception ex)
            {
                data["success"] = 0;
                data["error_info"] = ex.Message;
            }
            return JsonAllowGet(data);
        }

        public ActionResult PaySuccess(string id)
        {

            //TODO check the payment 


            int nId = int.Parse(id);
            db.Update("Update Bookings set BookingStatus=3 where BookingId=" + nId);
            return Redirect("/User/Bookings");
        }

        public ActionResult PayFail(string id)
        {
            return View("/User/Bookings");
        }


    }
}

