using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using DBApi;
using System.Text;

namespace WebApplication2.Hubs
{
    public class BidsHub : Hub
    {
        private static DBWorker dbWorker;
        private static string LOGIN_COOKIE_NAME = "bidsAuthInfo";
        static BidsHub()
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(StaticDestructor);
            dbWorker = new DBWorker(@"C:\temp\mydb.sqlite");
            dbWorker.Open();
        }

        static void StaticDestructor(object sender, EventArgs e)
        {
            dbWorker.Close();
        }

        public List<DBBidsEntry> GetAllBids()
        {
            System.Diagnostics.Debug.WriteLine("GetAllBids called");
            List<DBBidsEntry> bids = dbWorker.Bids.SelectAllBids();
            Clients.Caller.jsAddBids(bids);
            return bids;
        }

        public List<DBBidsEntry> GetBidsForPoster(string poster)
        {
            System.Diagnostics.Debug.WriteLine("GetBids called");
            List<DBBidsEntry> bids = dbWorker.Bids.SelectBidsForPoster(poster);
            Clients.Caller.jsAddBids(bids);
            return bids;
        }

        public bool InsertBid(string operationType, string currency, int amount, double price, string poster)
        {
            System.Diagnostics.Debug.WriteLine("InsertBid called");
            int insertResult = BidsHub.dbWorker.Bids.InsertBid(operationType, currency, amount, price, poster, DateTime.Now.ToUniversalTime().Ticks);
            if (insertResult > 0)
            {
                List<DBBidsEntry> insertedBid = BidsHub.dbWorker.Bids.SelectLastBidForPoster(poster);
                Clients.All.jsAddBids(insertedBid);
                return true;
            }
            return false;
        }
        public bool DeleteBid(long id)
        {
            System.Diagnostics.Debug.WriteLine("DeleteBid called");
            int deleteResult = BidsHub.dbWorker.Bids.DeleteBid(id);
            if (deleteResult >= 0)
            {
                Clients.All.jsRemoveBid(id);
                return true;
            }
            return false;
        }

        public void Login(string login, string password)
        {
            System.Diagnostics.Debug.WriteLine("Login called");
            List<DBUser> users = dbWorker.Users.SelectAllUsers();
            bool userFound = false;
            foreach (DBUser user in users)
            {
                if (user.login.Equals(login) && user.password.Equals(password))
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
                    Clients.Caller.jsCreateCookie("", BidsHub.LOGIN_COOKIE_NAME, cookie, lifetimeDays);
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
                if (trimmedPart.StartsWith(BidsHub.LOGIN_COOKIE_NAME))
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
                        Clients.Caller.jsDeleteCookie(BidsHub.LOGIN_COOKIE_NAME);
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
        private static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

    }
}