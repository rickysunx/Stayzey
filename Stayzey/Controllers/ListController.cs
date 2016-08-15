/*
 * List Controller
 * Author: Ricky Sun
 * Date: 13/06/2016
 * 
 * Display search result of listing rooms according to search criteria
 * 
 */ 

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
            string lng,        //Longitude of the address
            string lat,        //Latitude of the address
            string sortby,     //Sort by distance or price (low to high or high to low), default is distance
            string minPrice,   //The minimum price for the result
            string maxPrice,   //The maximum price for the result
            string distance    //The distance range between the address input and listing rooms
        )
        {
            //Generate the order clause for the SQL
            string orderString = " order by distance ";
            if(sortby=="price_lth")
            {
                orderString = " order by price ";
            }
            else if(sortby=="price_htl")
            {
                orderString = " order by price desc ";
            }

            //Ensure the valid minimum price and maximum price
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

            //The default distance is 30km
            int nDistance = 30;
            if(!IsEmpty(distance))
            {
                if(!int.TryParse(distance,out nDistance))
                {
                    nDistance = 30;
                }
            }

            //Generate the search SQL statement according to the criteria
            string sql = "select *,dbo.CalcDistance(" + lat + ", " + lng + ", Latitude, Longitude) distance from Rooms r,Users u "
                + " where r.UserId=u.UserId and r.Available=1 "
                + " and r.price>=" + nMinPrice + " and r.price<=" + nMaxPrice
                + " and dbo.CalcDistance(" + lat + ", " + lng + ", Latitude, Longitude)<=" + nDistance
                + orderString;

            //Query the database
            List<Hashtable> rooms = db.Query(sql);
            ViewBag.Rooms = rooms;
            return View("List");
        }
    }
}