using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Stayzey.Controllers
{
    public class ListController : StayzeyAbstractController
    {
        // GET: List
        public ActionResult Index(
            string lng,
            string lat,
            string sortby,
            string minPrice,
            string maxPrice,
            string distance
        )
        {
            string orderString = " order by distance ";
            if(sortby=="price_lth")
            {
                orderString = " order by price ";
            }
            else if(sortby=="price_htl")
            {
                orderString = " order by price desc ";
            }

            int nMinPrice = 0;
            int nMaxPrice = int.MaxValue;

            if(!IsEmpty(minPrice))
            {
                int.TryParse(minPrice, out nMinPrice);
            }

            if(!IsEmpty(maxPrice))
            {
                if(!int.TryParse(maxPrice, out nMaxPrice))
                {
                    nMaxPrice = int.MaxValue;
                }
            }

            //check min price and max price
            if(nMinPrice==nMaxPrice)
            {
                nMinPrice = 0;
            }

            //swap min and max price if min price greater than max
            if(nMinPrice>nMaxPrice)
            {
                int temp = nMinPrice;
                nMinPrice = nMaxPrice;
                nMaxPrice = temp;
            }

            ViewBag.minPrice = nMinPrice;
            ViewBag.maxPrice = nMaxPrice;

            int nDistance = 30;
            if(!IsEmpty(distance))
            {
                if(!int.TryParse(distance,out nDistance))
                {
                    nDistance = 30;
                }
            }


            string sql = "select *,dbo.CalcDistance(" + lat + ", " + lng + ", Latitude, Longitude) distance from Rooms r,Users u "
                + " where r.UserId=u.UserId and r.Available=1 "
                + " and r.price>=" + nMinPrice + " and r.price<=" + nMaxPrice
                + " and dbo.CalcDistance(" + lat + ", " + lng + ", Latitude, Longitude)<=" + nDistance
                + orderString;

            List<Hashtable> rooms = db.Query(sql);
            ViewBag.Rooms = rooms;
            return View("List");
        }
    }
}