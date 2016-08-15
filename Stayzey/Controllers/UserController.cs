/*
 * User Controller
 * Author: Ricky Sun
 * Date: 04/07/2016 
 * 
 * Provides user panel functions
 * 
 */
  
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
    /*
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

        public static HashSet<string> AcceptFileType;  //File types that are allowed for uploading

        //Booking status mapping
        public static string[] BookingStatuses = new string[] {
            "Pending landlord acceptance",
            "Landlord Accepted",
            "Landlord Rejected",
            "Paid","Cancelled","Deleted","Reviewed"
        };
        
        static UserController()
        {
            //Initialize the file types that are allowed for uploading
            AcceptFileType = new HashSet<string>();
            AcceptFileType.Add(".jpg");
            AcceptFileType.Add(".jpeg");
            AcceptFileType.Add(".gif");
            AcceptFileType.Add(".png");
        }

        // Index page of user panel
        // /User
        public ActionResult Index()
        {
            return View();
        }

        // Save Profile
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
            //Check user login
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
                //Prepare item object for storing to database
                Hashtable item = new Hashtable();
                item["UserId"] = userid;
                item["FirstName"] = FirstName;
                item["Surname"] = LastName;
                item["Email"] = Email;
                item["Phone"] = Phone;
                item["Gender"] = Gender;
                item["Avatar"] = ProfileImage;

                //Check the birth day
                int day = int.Parse(BirthDay);
                int month = int.Parse(BirthMonth);
                int year = int.Parse(BirthYear);
                DateTime dateOfBirth = new DateTime(year,month,day);
                item["DateOfBirth"] = dateOfBirth;

                //Update user to database
                db.Update("Users", item, "UserId");

                //Reload user object to session
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

        //Display profile page
        public ActionResult MyProfile()
        {
            //Get the logged user id
            int userid = GetLoginUserId();
            if (userid == 0)
            {
                return Redirect("/#Login");
            }

            //Query user data from database
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
        
        //Display room listings
        public ActionResult Listings()
        {
            //Check login
            int userid = GetLoginUserId();
            if (userid == 0)
            {
                return Redirect("/#Login");
            }

            //Query room data from database
            List<Hashtable> rooms = db.Query("select r.*, (select count(1) from Bookings b where r.RoomId = b.RoomId and b.BookingStatus<>5) BookingCount from Rooms r where r.UserId =" + userid);

            ViewBag.Rooms = rooms;

            return View();
        }

        //Display bookings
        public ActionResult Bookings(string RoomId)
        {
            //Check login
            int userid = GetLoginUserId();
            if (userid == 0)
            {
                return Redirect("/#Login");
            }

            //Get user object from session
            Hashtable loginUser = (Hashtable)Session["login_user"];
            int userType = (int)loginUser["UserType"];

            //Generate query sql for booking list
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
                //Student
                sql += " and b.UserId = " + userid;
            }

            sql += " order by BookingTime desc ";
            
            List<Hashtable> bookings = db.Query(sql);
            ViewBag.Bookings = bookings;
            ViewBag.BookingStatuses = BookingStatuses;
            ViewBag.UserType = userType;

            return View();
        }
        
        //Login
        public ActionResult LogIn(string email,string password)
        {
            //Check parameters
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
                //Query from database
                List<Hashtable> result = db.Query("select * from Users where email=@email and Blocked<>1",
                    new SqlParameter[] { new SqlParameter("@email",email) });

                if(result.Count==1)
                {
                    //User found, check password
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
                    //No data for user
                    checkPassed = false;
                    data["error_email"] = "Email not found";
                    data["error_password"] = "Please enter the correct password";
                }
            }

            data["success"] = checkPassed ? 1 : 0;


            return JsonAllowGet(data);
        }

        //User registration
        public ActionResult SignUp(
            string firstname,
            string lastname,
            string email,
            string password,
            string confirmpassword,
            string usertype)
        {
            //Check parameters
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

            //Check if email exists
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
                item["Avatar"] = "/images/default_profile_image.png";  //Default image for newly created user
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

        //Logout
        public ActionResult LogOut()
        {
            Hashtable data = new Hashtable();
            data["success"] = 1;
            //Remove session while logout
            Session.Remove("login_user");
            return JsonAllowGet(data);
        }

        //Display room creation page
        public ActionResult NewRoom()
        {
            //Check login
            int userid = GetLoginUserId();
            if (userid == 0)
            {
                return Redirect("/#Login");
            }

            //Show room creation page
            return View("RoomEditor");
        }

        //Display room editing page
        public ActionResult EditRoom(string id)
        {
            //Check login
            int userid = GetLoginUserId();
            if (userid == 0)
            {
                return Redirect("/#Login");
            }

            
            int nId = 0;
            if(int.TryParse(id,out nId)) //convert id to integer
            {
                //Query room data from database
                List<Hashtable> data = db.Query("select * from Rooms where RoomId=" + nId + " and UserId=" + userid);
                if(data.Count()>0)
                {
                    ViewBag.Room = data[0];

                    //Query room images attached to this room
                    List<Hashtable> dataImages = db.Query("select * from RoomImages where RoomId=" + nId);
                    ViewBag.RoomImages = dataImages;

                    return View("RoomEditor");
                }
            }

            //Return 404 while room not found
            return HttpNotFound();
        }

        //Delete a rooms
        public ActionResult DeleteRoom(string id)
        {
            Hashtable result = new Hashtable();
            try
            {
                //check login
                int userid = GetLoginUserId();
                if (userid == 0)
                {
                    result["error_code"] = 1;
                    throw new Exception("You have to login first");
                }

                //check existing bookings
                int nId = int.Parse(id);
                string sqlCheck = "select count(1) bookingCount from Bookings where RoomId=" + nId;
                List<Hashtable> data = db.Query(sqlCheck);
                int bookingCount = (int)(data[0]["bookingCount"]);
                if(bookingCount>0)
                {
                    //room with existing booking(s) cannot be deleted
                    throw new Exception("Can not delete this room which has been booked");
                }

                //Perform room deleting
                string sqlDelete = "delete from Rooms where RoomId=" + nId + " and UserId=" + userid; //Make sure user only delete the room created by itself
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

        //change room status
        public ActionResult ChangeRoomStatus(string id,string flag)
        {
            Hashtable result = new Hashtable();
            try
            {
                //Check login
                int userid = GetLoginUserId();
                if (userid == 0)
                {
                    result["error_code"] = 1;
                    throw new Exception("You have to login first");
                }

                //Update room status
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

        //Display upload image page in the iframe
        [HttpGet]
        public ActionResult UploadImage(string callback)
        {
            ViewBag.callback = callback;
            return View();
        }

        //Perform image uploading and display the upload page
        [HttpPost]
        public ActionResult UploadImage(string callback,HttpPostedFileBase file)
        {
            ViewBag.callback = callback;
            if(file!=null && file.ContentLength>0)
            {
                string oriFileName = file.FileName.ToLower();
                string fileExtension = Path.GetExtension(oriFileName);

                //Only allow some image type files. Make sure not upload malicious files
                if (AcceptFileType.Contains(fileExtension))
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

        //Save room, support create and update room according to room id field
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
            //Initialize some variables
            Hashtable data = new Hashtable();
            data["success"] = 1;
            decimal numberPrice = 0;
            double numberLatitude = 0;
            double numberLongitude = 0;

            //Login check
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
            
            //Prepare room object
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

            //Convert amentities array to string for storing in database
            string strAmenities = "";
            if (Amenities != null && Amenities.Count() > 0)
            {
                strAmenities = String.Join(",", Amenities);
            }
            room["Amenities"] = strAmenities;
            room["Available"] = 1;


            if (nRoomId > 0)
            {
                //Update room when roomid is not 0
                db.Update("Rooms", room, "RoomId");
            }
            else
            {
                //Create a new room when roomid is 0
                db.Insert("Rooms", room);
                //Return id just created
                nRoomId = db.GetLastInsertId();
            }

            //Update images that related to this room, remove all the images records first,
            //then insert the new ones.
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

        //Request to book a room
        public ActionResult RequestToBook(string roomid,string dates)
        {
            Hashtable data = new Hashtable();

            try
            {
                //Login check
                int userid = GetLoginUserId();
                if (userid == 0)
                {
                    data["success"] = 0;
                    data["error_code"] = 1;
                    data["error_info"] = "Please login first";
                    return JsonAllowGet(data);
                }

                //Check dates
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

                //Check room existance
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
                //Save booking
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

        //Delete booking
        public ActionResult DeleteBooking(string id)
        {
            Hashtable data = new Hashtable();

            try
            {
                //Login check
                int userid = GetLoginUserId();
                if (userid == 0)
                {
                    data["success"] = 0;
                    data["error_code"] = 1;
                    data["error_info"] = "Please login first";
                    return JsonAllowGet(data);
                }

                //Query the booking
                int nId = int.Parse(id);

                string sql = "select * from Bookings where RoomId=" + nId;
                List<Hashtable> bookings = db.Query(sql);
                if (bookings.Count() == 0)
                {
                    throw new Exception("Booking not found");
                }

                //Change booking status to 5 for deleting a room
                Hashtable booking = bookings[0];
                int bookingStatus = (int)booking["BookingStatus"];

                //Check the bookings if it is deletable
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

        //Cancel booking
        public ActionResult CancelBooking(string id)
        {
            Hashtable data = new Hashtable();

            try
            {
                //Login check
                int userid = GetLoginUserId();
                if (userid == 0)
                {
                    data["success"] = 0;
                    data["error_code"] = 1;
                    data["error_info"] = "Please login first";
                    return JsonAllowGet(data);
                }

                //Query the booking
                int nId = int.Parse(id);

                string sql = "select * from Bookings where BookingId=" + nId;
                List<Hashtable> bookings = db.Query(sql);
                if (bookings.Count() == 0)
                {
                    throw new Exception("Room not found");
                }
                
                Hashtable booking = bookings[0];
                int bookingStatus = (int)(booking["BookingStatus"]);
                //Check the booking if it is cancellable
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

        //Accept booking by landlord
        public ActionResult AcceptBooking(string id)
        {
            Hashtable data = new Hashtable();

            try
            {
                //Login check
                int userid = GetLoginUserId();
                if (userid == 0)
                {
                    data["success"] = 0;
                    data["error_code"] = 1;
                    data["error_info"] = "Please login first";
                    return JsonAllowGet(data);
                }

                //Update booking status
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

        //Reject booking by landlord
        public ActionResult RejectBooking(string id)
        {
            Hashtable data = new Hashtable();

            try
            {
                //Login check
                int userid = GetLoginUserId();
                if (userid == 0)
                {
                    data["success"] = 0;
                    data["error_code"] = 1;
                    data["error_info"] = "Please login first";
                    return JsonAllowGet(data);
                }

                //Update the booking status
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

        //Pay booking through PayPal
        public ActionResult PayBooking(string id)
        {
            Hashtable data = new Hashtable();

            try
            {
                //Login check
                int userid = GetLoginUserId();
                if (userid == 0)
                {
                    data["success"] = 0;
                    data["error_code"] = 1;
                    data["error_info"] = "Please login first";
                    return JsonAllowGet(data);
                }

                //Query the booking
                int nId = int.Parse(id);
                List<Hashtable> result = db.Query("select * from Bookings where BookingId=" + nId);
                if (result.Count == 0)
                {
                    throw new Exception("Booking not found");
                }

                decimal totalFee = (decimal)(result[0]["Total"]);
                
                //Prepare parameters for connecting to PayPal
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(ValidateServerCertificate);

                WebRequest request = WebRequest.Create("https://api-3t.sandbox.paypal.com/nvp");
                request.Method = "POST";
                request.Credentials = CredentialCache.DefaultCredentials;
                
                string postData = "";

                postData += "USER=" + HttpUtility.UrlEncode("sunruibox_api1.gmail.com");
                postData += "&PWD=" + HttpUtility.UrlEncode("BLWSGT6PV7R8RN3G");
                postData += "&SIGNATURE=" + HttpUtility.UrlEncode("AFcWxV21C7fd0v3bYYYRCpSSRl31AXk-xZHVWQJHmkqSIWYm-nHsHBI0");
                postData += "&METHOD=" + HttpUtility.UrlEncode("SetExpressCheckout");
                postData += "&VERSION=" + HttpUtility.UrlEncode("93");
                postData += "&PAYMENTREQUEST_0_PAYMENTACTION=" + HttpUtility.UrlEncode("SALE");
                postData += "&PAYMENTREQUEST_0_AMT=" + HttpUtility.UrlEncode("" + totalFee);
                postData += "&PAYMENTREQUEST_0_CURRENCYCODE=" + HttpUtility.UrlEncode("NZD");
                postData += "&RETURNURL=" + HttpUtility.UrlEncode("http://stayzey.azurewebsites.net/User/PaySuccess?id="+nId);
                postData += "&CANCELURL=" + HttpUtility.UrlEncode("http://stayzey.azurewebsites.net/User/PayFail?id=");

                //Send request to PayPal server
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
                
            }
            catch (Exception ex)
            {
                data["success"] = 0;
                data["error_info"] = ex.Message;
            }
            return JsonAllowGet(data);
        }

        //Save reviews
        public ActionResult SaveReview(
            string BookingId,
            string ReviewMark,
            string ReviewContent)
        {
            Hashtable data = new Hashtable();

            try
            {
                //Login check
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

                //Query the booking
                string sql = "select b.BookingId BookingId,r.UserId HostId,b.UserId UserId from Rooms r,Bookings b where r.RoomId=b.RoomId and b.BookingId=" + BookingId;

                List<Hashtable> bookings = db.Query(sql);
                if (bookings.Count() > 0)
                {
                    Hashtable booking = bookings[0];

                    //Prepare review object
                    Hashtable review = new Hashtable();
                    review["BookingId"] = booking["BookingId"];
                    review["ReviewMark"] = nReviewMark;
                    review["ReviewContent"] = ReviewContent;
                    review["HostId"] = booking["HostId"];
                    review["UserId"] = booking["UserId"];
                    review["ReviewTime"] = DateTime.Now;
                    
                    //Save the review to database
                    db.Insert("Reviews",review);

                    //Update booking status
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

