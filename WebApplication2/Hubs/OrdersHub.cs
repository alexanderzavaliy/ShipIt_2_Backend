using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using DBApi;
using System.Text;

namespace WebApplication2.Hubs
{
    public class OrdersHub : Hub
    {
        private static DBWorker dbWorker;
        private static string LOGIN_COOKIE_NAME = "bidsAuthInfo";
        static OrdersHub()
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(StaticDestructor);
            dbWorker = new DBWorker(@"C:\temp\mydb.sqlite");
            dbWorker.Open();
        }

        static void StaticDestructor(object sender, EventArgs e)
        {
            dbWorker.Close();
        }

        public List<DBOrder> GetAllOrders()
        {
            System.Diagnostics.Debug.WriteLine("GetAllOrders called");
            List<DBOrder> orders = dbWorker.Orders.SelectAllOrders();
            Clients.Caller.jsAddBids(orders);
            return orders;
        }

        public List<DBOrder> GetOrdersForOwner(string cookies)
        {
            System.Diagnostics.Debug.WriteLine("GetOrdersForOwner called");
            long ownerId = 0;
            //
            // TODO: 1. validate cookies, find out ownerId using them
            //       
            List<DBOrder> orders = dbWorker.Orders.SelectOrdersForOwner(ownerId);
            Clients.Caller.jsAddBids(orders);
            return orders;
        }

        public bool InsertOrder(string cookies, long instrumentId, long endDate, string type, double price, int amount)
        {
            System.Diagnostics.Debug.WriteLine("InsertOrder called");
            long ownerId = 0;
            long createdDate = DateTime.Now.ToUniversalTime().Ticks;
            string status = "New";
            //
            // TODO: 1. validate cookies, find out ownerId using them
            //       2. get currentDate
            //       3. get endDate
            int insertResult = OrdersHub.dbWorker.Orders.InsertOrder(ownerId, instrumentId, createdDate, endDate, type, price, amount, status, 0, 0);
            if (insertResult > 0)
            {
                List<DBOrder> insertedOrder = OrdersHub.dbWorker.Orders.SelectLastOrderForOwner(ownerId);
                Clients.All.jsAddBids(insertedOrder);
                return true;
            }
            return false;
        }
        public bool DeleteOrder(long id)
        {
            System.Diagnostics.Debug.WriteLine("DeleteOrder called");
            //
            // TODO: 1. implement DeleteOrder(...) in DBOrdersTable
            //       2. use DeleteOrder(...) below

            //int deleteResult = OrdersHub.dbWorker.Orders.DeleteOrder(id);
            //if (deleteResult >= 0)
            //{
            //    Clients.All.jsRemoveBid(id);
            //    return true;
            //}
            return false;
        }

        public void Login(string name, string password)
        {
            System.Diagnostics.Debug.WriteLine("Login called");
            List<DBUser> users = dbWorker.Users.SelectAllUsers();
            bool userFound = false;
            foreach (DBUser user in users)
            {
                if (user.name.Equals(name) && user.password.Equals(DBHelper.GetMD5(password)))
                {
                    userFound = true;
                    break;
                }
            }
            if (userFound)
            {
                int lifetimeDays = 1;
                string cookie = CreateCookie();
                long expirationDate = DateTime.Now.AddDays(lifetimeDays).Ticks;
                if (dbWorker.Cookies.InsertCookie(cookie, expirationDate) > 0)
                {
                    Clients.Caller.jsCreateCookie("", OrdersHub.LOGIN_COOKIE_NAME, cookie, lifetimeDays);
                }
            }
            else
            { 
                //incorrect login or password
            }
        }

        public void Logout(string cookies)
        {
            System.Diagnostics.Debug.WriteLine("Logout called" + " " + cookies);
            string cookieNameAndValue = null;
            string[] parts = cookies.Split(';');
            foreach (string part in parts)
            {
                string trimmedPart = part.Trim();
                if (trimmedPart.StartsWith(OrdersHub.LOGIN_COOKIE_NAME))
                {
                    cookieNameAndValue = trimmedPart;
                    break;
                }
            }

            string cookieValue = null;
            if (cookieNameAndValue != null)
            {
                parts = cookieNameAndValue.Split('=');
                if (parts.Length == 2)
                {
                    cookieValue = parts[1];
                    if (dbWorker.Cookies.DeleteCookie(cookieValue) >= 0)
                    {
                        Clients.Caller.jsDeleteCookie(OrdersHub.LOGIN_COOKIE_NAME);
                    }
                }
            }
        
        }

        private string CreateCookie()
        {
            List<DBCookie> dbCookies = new List<DBCookie>();
            string cookieCandidate = "";
            do
            {
                cookieCandidate = GenerateRandomString();
                dbCookies = dbWorker.Cookies.SelectCookie(cookieCandidate);
            }
            while (dbCookies.Count > 0);
            return cookieCandidate;
        }
        private string GenerateRandomString()
        {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            char[] stringChars = new char[20];
            Random random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            string finalString = new string(stringChars);
            return finalString;
        }


    }
}