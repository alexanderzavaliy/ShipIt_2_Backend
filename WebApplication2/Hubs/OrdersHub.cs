using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using System.Text;
using System.Threading.Tasks;
using DBApi;


namespace WebApplication2.Hubs
{
    public class OrdersHub : Hub
    {
        private static DBWorker dbWorker;
        private static string LOGIN_COOKIE_NAME = "ordersAuthInfo";
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

        public void ValidateCookies(string cookies)
        {
            string authCookieValue = ExtractAuthCookieValue(cookies);
            DBCookie dbCookie = null;
            if (isValidAuthCookieValue(authCookieValue, ref dbCookie))
            {
                Clients.Caller.jsValidCookie();
            }
            else
            {
                HandleInvalidCookies(authCookieValue);
            }
        }

        public void GetAllOrders(string cookies)
        {
            System.Diagnostics.Debug.WriteLine("GetAllOrders called");
            string authCookieValue = ExtractAuthCookieValue(cookies);
            DBCookie dbCookie = null;
            if (isValidAuthCookieValue(authCookieValue, ref dbCookie))
            {
                List<DBOrder> orders = dbWorker.Orders.SelectAllOrders();
                Clients.Caller.jsAddOrders(orders);
            }
            else 
            {
                HandleInvalidCookies(authCookieValue);
            }
         }

        public List<DBOrder> GetOrdersForOwner(string cookies)
        {
            System.Diagnostics.Debug.WriteLine("GetOrdersForOwner called");
            long ownerId = 0;
            //
            // TODO: 1. validate cookies, find out ownerId using them
            //       
            List<DBOrder> orders = dbWorker.Orders.SelectOrdersForOwner(ownerId);
            Clients.Caller.jsAddOrder(orders);
            return orders;
        }

        public bool InsertOrder(string cookies, string instrumentShortName, string type, double price, int amount)
        {
            System.Diagnostics.Debug.WriteLine("InsertOrder called");
            string authCookieValue = ExtractAuthCookieValue(cookies);
            DBCookie dbCookie = null;
            if (isValidAuthCookieValue(authCookieValue, ref dbCookie))
            {
                List<long> instrumentIds = dbWorker.Instruments.SelectInstrumentsIdsByShortName(instrumentShortName);
                if (instrumentIds.Count > 0)
                {
                    long instrumentId = instrumentIds[0];
                    int insertResult = OrdersHub.dbWorker.Orders.InsertOrder(dbCookie.ownerId, instrumentId, DateTime.Now.Ticks, 0, type, price, amount, DBOrder.Status.NEW, 0, 0);
                    if (insertResult > 0)
                    {
                        List<DBOrder> insertedOrder = OrdersHub.dbWorker.Orders.SelectLastOrderForOwner(dbCookie.ownerId);
                        if (insertedOrder.Count > 0)
                        {
                            Clients.All.jsAddOrder(insertedOrder[0]);
                            return true;
                        }
                    }
                }
            }
            else
            {
                HandleInvalidCookies(authCookieValue);
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

        public void LockOrder(string cookies, long id)
        {
            System.Diagnostics.Debug.WriteLine("LockOrder called");
            DBCookie dbCookie = null;
            string authCookieValue = ExtractAuthCookieValue(cookies);
            if (isValidAuthCookieValue(authCookieValue, ref dbCookie))
            {
                List<DBOrder> orders = dbWorker.Orders.SelectOrdersById(id);
                if (orders.Count > 0)
                {
                    DBOrder order = orders[0];
                    bool orderIsNew = order.status.Equals(DBOrder.Status.NEW);
                    bool orderIsLocked = order.status.Equals(DBOrder.Status.LOCKED);
                    if (orderIsNew)
                    {
                        int updateResult = dbWorker.Orders.UpdateOrder(order.id, order.ownerId, order.instrumentId, order.createdDate, order.endDate, order.type, order.price, order.amount, DBOrder.Status.LOCKED, dbCookie.ownerId, DateTime.Now.Ticks);
                        if (updateResult > 0)
                        {
                            List<DBOrder> updatedOrders = dbWorker.Orders.SelectOrdersById(order.id);
                            if (updatedOrders.Count > 0)
                            {
                                order = updatedOrders[0];
                                Clients.Caller.jsLockOrderAccepted(order, true);
                                Clients.Others.jsLockOrderAccepted(order, false);
                                UnlockAfterTimeout(order);
                            }
                        }
                    }
                    else if (orderIsLocked)
                    {
                        bool callerIsExecutor = dbCookie.ownerId.Equals(order.executorId);
                        Clients.Caller.jsLockOrderAccepted(order, callerIsExecutor);
                        Clients.Others.jsLockOrderAccepted(order, callerIsExecutor);
                    }
                    else
                    { 
                        // No actions if order.Status = "Executed"
                    }
                }
            }
            else
            {
                HandleInvalidCookies(authCookieValue);
            }
        }

        private void UnlockAfterTimeout(DBOrder lockedOrder)
        {

            Task.Factory.StartNew(() => 
            {
                int lockTimeout = 30000;
                System.Threading.Thread.Sleep(lockTimeout);
                List<DBOrder> orders = dbWorker.Orders.SelectOrdersById(lockedOrder.id);
                if (orders.Count > 0)
                {
                    if (orders[0].status.Equals(DBOrder.Status.LOCKED))
                    {
                        int updateResult = dbWorker.Orders.UpdateOrder(lockedOrder.id, lockedOrder.ownerId, lockedOrder.instrumentId, lockedOrder.createdDate, lockedOrder.endDate, lockedOrder.type, lockedOrder.price, lockedOrder.amount, DBOrder.Status.NEW, 0, 0);
                        if (updateResult > 0)
                        {
                            List<DBOrder> updatedOrders = dbWorker.Orders.SelectOrdersById(lockedOrder.id);
                            if (updatedOrders.Count > 0)
                            {
                                Clients.All.jsUnlockOrder(updatedOrders[0]);
                            }
                        }
                    }
                }
            });

        }

        public void ExecuteOrder(string cookies, long id)
        {
            System.Diagnostics.Debug.WriteLine("ExecuteOrder called");
            DBCookie dbCookie = null;
            string authCookieValue = ExtractAuthCookieValue(cookies);
            if (isValidAuthCookieValue(authCookieValue, ref dbCookie))
            {
                List<DBOrder> orders = dbWorker.Orders.SelectOrdersById(id);
                if (orders.Count > 0)
                {
                    DBOrder order = orders[0];
                    bool orderIsNew = order.status.Equals(DBOrder.Status.NEW);
                    bool orderIsLockedByCaller = (order.status.Equals(DBOrder.Status.LOCKED) && dbCookie.ownerId.Equals(order.executorId));
                    if (orderIsNew || orderIsLockedByCaller)
                    {
                        int updateResult = dbWorker.Orders.UpdateOrder(order.id, order.ownerId, order.instrumentId, order.createdDate, order.endDate, order.type, order.price, order.amount, DBOrder.Status.EXECUTED, dbCookie.ownerId, DateTime.Now.Ticks);
                        if (updateResult > 0)
                        {
                            List<DBOrder> updatedOrders = dbWorker.Orders.SelectOrdersById(order.id);
                            if (updatedOrders.Count > 0)
                            {
                                order = updatedOrders[0];
                                Clients.Caller.jsExecuteOrderAccepted(order);
                                UnlockAfterTimeout(order);
                            }
                        }
                    }
                }
            }
            else
            {
                HandleInvalidCookies(authCookieValue);
            }
        }

        public void Login(string name, string password)
        {
            System.Diagnostics.Debug.WriteLine("Login called");
            List<DBUser> users = dbWorker.Users.SelectAllUsers();
            long ownerId = -1;
            foreach (DBUser user in users)
            {
                if (user.name.Equals(name) && user.password.Equals(DBHelper.GetMD5(password)))
                {
                    ownerId = user.id;
                    break;
                }
            }
            if (ownerId != -1)
            {
                int lifetimeDays = 1;
                string cookie = CreateCookie();
                long expirationDate = DateTime.Now.AddDays(lifetimeDays).Ticks;
                if (dbWorker.Cookies.InsertCookie(ownerId, cookie, expirationDate) > 0)
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
            string authCookieValue = ExtractAuthCookieValue(cookies);
            HandleInvalidCookies(authCookieValue);
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

        private string ExtractAuthCookieValue(string cookies)
        {
            string cookieValue = "";
            if (cookies != null)
            {
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

                if (cookieNameAndValue != null)
                {
                    parts = cookieNameAndValue.Split('=');
                    if (parts.Length == 2)
                    {
                        cookieValue = parts[1];
                    }
                }
            }
            return cookieValue;
        }
        private bool isValidAuthCookieValue(string authCookieValue, ref DBCookie dbCookie)
        {
            if (authCookieValue != null)
            {
                List<DBCookie> dbCookies = dbWorker.Cookies.SelectCookie(authCookieValue);
                if (dbCookies.Count() > 0)
                {
                    dbCookie = dbCookies[0];
                    long currentDate = DateTime.Now.Ticks;
                    long dbCookieExpirationDate = dbCookies[0].expirationDate;
                    return (currentDate < dbCookieExpirationDate) ? true : false;
                }
            }
            return false;
        }

        private void HandleInvalidCookies(string authCookieValue)
        {
            if (authCookieValue != null)
            {
                dbWorker.Cookies.DeleteCookie(authCookieValue);
            }
            Clients.Caller.jsDeleteCookie(OrdersHub.LOGIN_COOKIE_NAME);
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