using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using System.Text;
using System.Threading.Tasks;
using WebApplication2.Models;
using DBApi;


namespace WebApplication2.Hubs
{
    public class OrdersHub : Hub
    {
        private static ConnectionManager connectionManager = new ConnectionManager();
        private static DBWorker dbWorker;
        private static string LOGIN_COOKIE_NAME = "ordersAuthInfo";
        private static long ORDERS_BACKTIME_PERIOD = new TimeSpan(30, 0, 0, 0).Ticks;

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

        public override Task OnConnected()
        {
            string authCookieValue = GetAuthCookieValue();
            DBCookie dbCookie = null;
            if (isValidAuthCookieValue(authCookieValue, ref dbCookie))
            {
                connectionManager.Add(dbCookie.ownerId, Context.ConnectionId);
                Clients.Caller.jsValidCookie();
                return base.OnConnected();
            }
            else
            {
                HandleInvalidCookies(authCookieValue);
            }
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            string authCookieValue = GetAuthCookieValue();
            DBCookie dbCookie = null;
            if (isValidAuthCookieValue(authCookieValue, ref dbCookie))
            {
                connectionManager.Remove(dbCookie.ownerId, Context.ConnectionId);
            }
            else
            {
                HandleInvalidCookies(authCookieValue);
            }
            return base.OnDisconnected(stopCalled);
        }

        public void ValidateCookies()
        {
            string authCookieValue = GetAuthCookieValue();
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

        public void GetAllOrders()
        {
            System.Diagnostics.Debug.WriteLine("GetAllOrders called");
            string authCookieValue = GetAuthCookieValue();
            DBCookie dbCookie = null;
            if (isValidAuthCookieValue(authCookieValue, ref dbCookie))
            {
                List<DBOrder> orders = dbWorker.Orders.SelectAllNotExecutedOrders();
                Clients.Caller.jsAddOrders(orders);                
            }
            else
            {
                HandleInvalidCookies(authCookieValue);
            }
         }

        public List<DBOrder> GetOrdersForOwner()
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

        public void InsertOrder(string instrumentShortName, string type, double price, int amount)
        {
            System.Diagnostics.Debug.WriteLine("InsertOrder called");
            string authCookieValue = GetAuthCookieValue();
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
                        }
                    }
                }
            }
            else
            {
                HandleInvalidCookies(authCookieValue);
            }
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

        public void LockOrder(long id)
        {
            System.Diagnostics.Debug.WriteLine("LockOrder called");
            string authCookieValue = GetAuthCookieValue();
            DBCookie dbCookie = null;
            if (isValidAuthCookieValue(authCookieValue, ref dbCookie))
            {
                List<DBOrder> orders = dbWorker.Orders.SelectOrdersById(id);
                if (orders.Count > 0)
                {
                    DBOrder order = orders[0];
                    bool orderIsNew = order.status.Equals(DBOrder.Status.NEW);
                    bool orderIsLocked = order.status.Equals(DBOrder.Status.LOCKED);
                    IList<string> lockerConnectionIds = connectionManager.GetConnectionIds(dbCookie.ownerId);
                    IList<string> othersConnectionIds = connectionManager.GetConnectionIdsExcept(dbCookie.ownerId);
                    if (orderIsNew)
                    {
                        int updateResult = dbWorker.Orders.UpdateOrder(order.id, order.ownerId, order.instrumentId, order.createdDate, order.endDate, order.type, order.price, order.amount, DBOrder.Status.LOCKED, dbCookie.ownerId, DateTime.Now.Ticks);
                        if (updateResult > 0)
                        {
                            List<DBOrder> updatedOrders = dbWorker.Orders.SelectOrdersById(order.id);
                            if (updatedOrders.Count > 0)
                            {
                                order = updatedOrders[0];
                                Clients.Client(Context.ConnectionId).jsLockOrderAccepted(order, "true");
                                foreach (string connectionId in lockerConnectionIds)
                                {
                                    if (connectionId != Context.ConnectionId)
                                    {
                                        Clients.Client(connectionId).jsLockOrderAccepted(order, "true_other");
                                    }
                                }
                                Clients.Clients(othersConnectionIds).jsLockOrderAccepted(order, "false");
                                UnlockAfterTimeout(order);
                            }
                        }
                    }
                    else if (orderIsLocked)
                    {
                        bool callerIsExecutor = dbCookie.ownerId.Equals(order.executorId);
                        if (callerIsExecutor)
                        {
                            Clients.Clients(lockerConnectionIds).jsLockOrderAccepted(order, "true_other");
                        }
                        else
                        {
                            Clients.Clients(lockerConnectionIds).jsLockOrderAccepted(order, "false");
                        }
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

        public void ExecuteOrder(long id)
        {
            System.Diagnostics.Debug.WriteLine("ExecuteOrder called");
            string authCookieValue = GetAuthCookieValue();
            DBCookie dbCookie = null;
            if (isValidAuthCookieValue(authCookieValue, ref dbCookie))
            {
                List<DBOrder> orders = dbWorker.Orders.SelectOrdersById(id);
                if (orders.Count > 0)
                {
                    DBOrder order = orders[0];
                    bool orderIsNew = order.status.Equals(DBOrder.Status.NEW);
                    bool orderIsLockedByCaller = (order.status.Equals(DBOrder.Status.LOCKED) && dbCookie.ownerId.Equals(order.executorId));
                    IList<string> executorConnectionIds = connectionManager.GetConnectionIds(dbCookie.ownerId);
                    IList<string> ownerConnectionIds = connectionManager.GetConnectionIdsExcept(dbCookie.ownerId);
                    if (orderIsNew || orderIsLockedByCaller)
                    {
                        long executionDate = DateTime.Now.Ticks;
                        int updateResult = dbWorker.Orders.UpdateOrder(order.id, order.ownerId, order.instrumentId, order.createdDate, executionDate, order.type, order.price, order.amount, DBOrder.Status.EXECUTED, dbCookie.ownerId, executionDate);
                        if (updateResult > 0)
                        {
                            List<DBOrder> updatedOrders = dbWorker.Orders.SelectOrdersById(order.id);
                            if (updatedOrders.Count > 0)
                            {
                                order = updatedOrders[0];
                                Clients.All.jsExecuteOrderDone(order);
                                OrderHistory orderHistory = NewOrderHistory(order);
                                Clients.Clients(executorConnectionIds).jsAddOrderHistory(orderHistory);
                                Clients.Clients(ownerConnectionIds).jsAddOrderHistory(orderHistory);
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

        public void GetOrdersHistory()
        {
            System.Diagnostics.Debug.WriteLine("GetOrdersHistory called");
            string authCookieValue = GetAuthCookieValue();
            DBCookie dbCookie = null;
            if (isValidAuthCookieValue(authCookieValue, ref dbCookie))
            {
                List<DBOrder> orders = dbWorker.Orders.SelectOrdersRelatedToUser(dbCookie.ownerId, ORDERS_BACKTIME_PERIOD);
                if (orders.Count > 0)
                {
                    List<OrderHistory> ordersHistory = new List<OrderHistory>();
                    foreach (DBOrder order in orders)
                    {
                        OrderHistory orderHistory = NewOrderHistory(order);
                        ordersHistory.Add(orderHistory);
                    }
                    Clients.Caller.jsAddOrdersHistory(ordersHistory);
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

        public void Logout()
        {
            System.Diagnostics.Debug.WriteLine("Logout called");
            string authCookieValue = GetAuthCookieValue();
            HandleInvalidCookies(authCookieValue);
        }

        private string GetAuthCookieValue()
        { 
            IDictionary<string, Cookie> cookies = Context.RequestCookies;
            Cookie authCookie = null;
            if (cookies.TryGetValue(LOGIN_COOKIE_NAME, out authCookie))
            {
                return authCookie.Value;
            }
            return "";
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

        private void HandleInvalidCookies(string authCookieValue)
        {
            if (authCookieValue != null)
            {
                dbWorker.Cookies.DeleteCookie(authCookieValue);
            }
            Clients.Caller.jsDeleteCookie(OrdersHub.LOGIN_COOKIE_NAME);
        }

        private OrderHistory NewOrderHistory(DBOrder order)
        {
            OrderHistory orderHistory = new OrderHistory();
            orderHistory.id = order.id;
            orderHistory.instrumentId = order.instrumentId;
            orderHistory.ownerId = order.ownerId;
            orderHistory.executorId = order.executorId;
            orderHistory.type = order.type;
            orderHistory.price = order.price;
            orderHistory.amount = order.amount;
            orderHistory.status = order.status;
            orderHistory.createdDate = order.createdDate;
            orderHistory.createdDateStr = new DateTime(order.createdDate).ToString();
            orderHistory.endDate = order.endDate;
            orderHistory.endDateStr = new DateTime(order.endDate).ToString();
            orderHistory.executionDate = order.executionDate;
            orderHistory.executionDateStr = new DateTime(order.executionDate).ToString();

            List<DBInstrument> instruments = dbWorker.Instruments.SelectInstrumentById(order.instrumentId);
            if (instruments.Count > 0)
            {
                orderHistory.instrumentShortName = instruments[0].shortName;
            }
            List<DBUser> owners = dbWorker.Users.SelectUserById(order.ownerId);
            if (owners.Count > 0)
            {
                orderHistory.ownerEmail = owners[0].email;
                orderHistory.ownerScype = owners[0].scype;
            }
            List<DBUser> executors = dbWorker.Users.SelectUserById(order.executorId);
            if (executors.Count > 0)
            {
                orderHistory.executorEmail = executors[0].email;
                orderHistory.executorScype = executors[0].scype;
            }
            return orderHistory;
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