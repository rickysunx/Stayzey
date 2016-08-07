using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Stayzey.Controllers
{
    public class RoomController : StayzeyAbstractController
    {
        // GET: Room
        public ActionResult Index(string id)
        {
            if(IsEmpty(id))
            {
                return HttpNotFound();
            }

            int nId = int.Parse(id);

            List<Hashtable> rooms = db.Query("select * from Rooms r,Users u where r.UserId=u.Userid and roomid="+id);
            if(rooms.Count==0)
            {
                return HttpNotFound();
            }
            else
            {
                ViewBag.Room = rooms[0];
                string roomIntro = (string)(rooms[0]["RoomIntro"]);
                roomIntro = Server.HtmlEncode(roomIntro);
                roomIntro = roomIntro.Replace("\r\n", "<br>");
                roomIntro = roomIntro.Replace("\n", "<br>");
                rooms[0]["RoomIntro"] = roomIntro;

                string amenities = (string)(rooms[0]["Amenities"]);
                ViewBag.AmenityArray = amenities.Split(",".ToCharArray());


                List<Hashtable> roomImages = db.Query("select * from RoomImages where roomid="+id);
                ViewBag.RoomImages = roomImages;

                List<Hashtable> roomReviews = db.Query("select * from Reviews r,Users u where r.UserId=u.UserId and r.BookingId in (select b.BookingId from Bookings b where b.RoomId=" + id + " )");
                ViewBag.RoomReviews = roomReviews;

                List<Hashtable> roomBookings = db.Query("select * from Bookings where roomid=" + id + " and BookingStatus in (0,1,3,6) ");
                string bookingArrayScript = "var RoomBookings = [ ";
                foreach(Hashtable item in roomBookings)
                {
                    DateTime startDate = (DateTime)item["StartDate"];
                    DateTime endDate = (DateTime)item["EndDate"];
                    bookingArrayScript += "\r\n{\"startDate\":new Date(" + startDate.Year + "," + (startDate.Month - 1) + "," + startDate.Day + "),";
                    bookingArrayScript += "\"endDate\":new Date(" + endDate.Year + "," + (endDate.Month - 1) + "," + endDate.Day + ")},";
                }


                bookingArrayScript = bookingArrayScript.Remove(bookingArrayScript.Length - 1, 1);
                bookingArrayScript += "];";

                ViewBag.BookingArrayScript = bookingArrayScript;
            }

            return View("Room");
        }
    }
}