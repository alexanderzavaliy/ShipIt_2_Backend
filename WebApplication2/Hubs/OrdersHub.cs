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
                Clients.Caller.jsValidCookie(dbCookie.ownerId);
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
                List<ExtendedOrder> extOrders = new List<ExtendedOrder>();
                foreach (DBOrder order in orders)
                {
                    ExtendedOrder extOrder = NewExtendedOrder(order);
                    extOrders.Add(extOrder);
                }
                Clients.Caller.jsAddOrders(extOrders);                
            }
            else
            {
                HandleInvalidCookies(authCookieValue);
            }
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
                            ExtendedOrder extendedOrder = NewExtendedOrder(insertedOrder[0]);
                            Clients.All.jsAddOrder(extendedOrder);
                        }
                    }
                }
            }
            else
            {
                HandleInvalidCookies(authCookieValue);
            }
        }

        public void DeleteOrder(long id)
        {
            System.Diagnostics.Debug.WriteLine("DeleteOrder called");
            string authCookieValue = GetAuthCookieValue();
            DBCookie dbCookie = null;
            if (isValidAuthCookieValue(authCookieValue, ref dbCookie))
            {
                List<DBOrder> orders = dbWorker.Orders.SelectOrdersById(id);
                if (orders.Count > 0)
                {
                    DBOrder order = orders[0];
                    if (dbCookie.ownerId.Equals(order.ownerId))
                    {
                        int deleteResult = OrdersHub.dbWorker.Orders.DeleteOrder(id);
                        if (deleteResult > 0)
                        {
                            Clients.All.jsRemoveOrder(order);
                        }
                    }
                }
            }
            else
            {
                HandleInvalidCookies(authCookieValue);
            }
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
                        string lockedStatus = DBOrder.Status.LOCKED;
                        long lockedExecutorId = dbCookie.ownerId;
                        long lockedExecutionDate = DateTime.Now.Ticks;
                        int updateResult = dbWorker.Orders.UpdateOrder(order.id, order.ownerId, order.instrumentId, order.createdDate, order.endDate, order.type, order.price, order.amount, lockedStatus, lockedExecutorId, lockedExecutionDate);
                        if (updateResult > 0)
                        {
                            order.status = lockedStatus;
                            order.executorId = lockedExecutorId;
                            order.executionDate = lockedExecutionDate;
                            ExtendedOrder extOrder = NewExtendedOrder(order);
                            Clients.Client(Context.ConnectionId).jsLockOrderAccepted(extOrder, "true");
                            foreach (string connectionId in lockerConnectionIds)
                            {
                                if (connectionId != Context.ConnectionId)
                                {
                                    Clients.Client(connectionId).jsLockOrderAccepted(extOrder, "true_other");
                                }
                            }
                            Clients.Clients(othersConnectionIds).jsLockOrderAccepted(extOrder, "false");
                            UnlockAfterTimeout(order);
                        }
                    }
                    else if (orderIsLocked)
                    {
                        ExtendedOrder extOrder = NewExtendedOrder(order);
                        bool callerIsExecutor = dbCookie.ownerId.Equals(order.executorId);
                        if (callerIsExecutor)
                        {
                            Clients.Clients(lockerConnectionIds).jsLockOrderAccepted(extOrder, "true_other");
                        }
                        else
                        {
                            Clients.Clients(lockerConnectionIds).jsLockOrderAccepted(extOrder, "false");
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
                        string newStatus = DBOrder.Status.NEW;
                        long executorId = 0;
                        long executionDate = 0;
                        int updateResult = dbWorker.Orders.UpdateOrder(lockedOrder.id, lockedOrder.ownerId, lockedOrder.instrumentId, lockedOrder.createdDate, lockedOrder.endDate, lockedOrder.type, lockedOrder.price, lockedOrder.amount, newStatus, executorId, executionDate);
                        if (updateResult > 0)
                        {
                            orders[0].status = newStatus;
                            orders[0].executorId = executorId;
                            orders[0].executionDate = executionDate;
                            ExtendedOrder extOrder = NewExtendedOrder(orders[0]);
                            Clients.All.jsUnlockOrder(extOrder);
                        }
                    }
                }
            });
        }

        public void UnlockOrder(long id)
        {
            System.Diagnostics.Debug.WriteLine("UnLockOrder called");
            string authCookieValue = GetAuthCookieValue();
            DBCookie dbCookie = null;
            if (isValidAuthCookieValue(authCookieValue, ref dbCookie))
            {
                List<DBOrder> orders = dbWorker.Orders.SelectOrdersById(id);
                if (orders.Count > 0)
                {
                    DBOrder order = orders[0];
                    bool orderIsLocked = order.status.Equals(DBOrder.Status.LOCKED);
                    if (orderIsLocked)
                    {
                        if (dbCookie.ownerId.Equals(order.executorId))
                        {
                            string unlockedStatus = DBOrder.Status.NEW;
                            long unlockedExecutorId = 0;
                            long unlockedExecutionDate = 0;

                            int updateResult = dbWorker.Orders.UpdateOrder(order.id, order.ownerId, order.instrumentId, order.createdDate, order.endDate, order.type, order.price, order.amount, unlockedStatus, unlockedExecutorId, unlockedExecutionDate);
                            if (updateResult > 0)
                            {
                                order.status = unlockedStatus;
                                order.executorId = unlockedExecutorId;
                                order.executionDate = unlockedExecutionDate;
                                ExtendedOrder extOrder = NewExtendedOrder(orders[0]);
                                Clients.All.jsUnlockOrder(extOrder);
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

        public void PendingOrder(long id)
        {
            System.Diagnostics.Debug.WriteLine("PendingOrder called");
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
                        string pendingStatus = DBOrder.Status.PENDING;
                        long executorId = dbCookie.ownerId;
                        long executionDate = DateTime.Now.Ticks;
                        int updateResult = dbWorker.Orders.UpdateOrder(order.id, order.ownerId, order.instrumentId, order.createdDate, executionDate, order.type, order.price, order.amount, pendingStatus, executorId, executionDate);
                        if (updateResult > 0)
                        {
                            order.status = pendingStatus;
                            order.executorId = executorId;
                            order.executionDate = executionDate;
                            ExtendedOrder extOrder = NewExtendedOrder(order);
                            Clients.All.jsPendingOrderDone(extOrder);
                        }
                    }
                }
            }
            else
            {
                HandleInvalidCookies(authCookieValue);
            }
        }

        public void ExecuteOrder(long id, bool accepted)
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
                    bool orderIsPending = order.status.Equals(DBOrder.Status.PENDING);
                    IList<string> executorConnectionIds = connectionManager.GetConnectionIds(dbCookie.ownerId);
                    IList<string> ownerConnectionIds = connectionManager.GetConnectionIdsExcept(dbCookie.ownerId);
                    if (orderIsPending)
                    {
                        long executorId = order.executorId;
                        long executionDate = DateTime.Now.Ticks;
                        string executionStatus = "";
                        if (accepted)
                        {
                            executionStatus = DBOrder.Status.EXECUTED;
                        }
                        else
                        {
                            executionStatus = DBOrder.Status.NEW;
                        }

                        int updateResult = dbWorker.Orders.UpdateOrder(order.id, order.ownerId, order.instrumentId, order.createdDate, executionDate, order.type, order.price, order.amount, executionStatus, executorId, executionDate);
                        if (updateResult > 0)
                        {
                            order.status = executionStatus;
                            order.executorId = executorId;
                            order.executionDate = executionDate;
                            ExtendedOrder extOrder = NewExtendedOrder(order);
                            if (accepted)
                            {
                                Clients.All.jsRemoveOrder(extOrder);
                                Clients.Clients(executorConnectionIds).jsAddOrderHistory(extOrder);
                                Clients.Clients(ownerConnectionIds).jsAddOrderHistory(extOrder);
                            }
                            else
                            {
                                Clients.All.jsExecuteOrderDeclined(extOrder);
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
                    List<ExtendedOrder> ordersHistory = new List<ExtendedOrder>();
                    foreach (DBOrder order in orders)
                    {
                        ExtendedOrder orderHistory = NewExtendedOrder(order);
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

        private ExtendedOrder NewExtendedOrder(DBOrder order)
        {
            ExtendedOrder extendedOrder = new ExtendedOrder();
            extendedOrder.id = order.id;
            extendedOrder.instrumentId = order.instrumentId;
            extendedOrder.ownerId = order.ownerId;
            extendedOrder.executorId = order.executorId;
            extendedOrder.type = order.type;
            extendedOrder.price = order.price;
            extendedOrder.amount = order.amount;
            extendedOrder.status = order.status;
            extendedOrder.createdDate = order.createdDate;
            extendedOrder.createdDateStr = new DateTime(order.createdDate).ToString();
            extendedOrder.endDate = order.endDate;
            extendedOrder.endDateStr = new DateTime(order.endDate).ToString();
            extendedOrder.executionDate = order.executionDate;
            extendedOrder.executionDateStr = new DateTime(order.executionDate).ToString();

            List<DBInstrument> instruments = dbWorker.Instruments.SelectInstrumentById(order.instrumentId);
            if (instruments.Count > 0)
            {
                extendedOrder.instrumentShortName = instruments[0].shortName;
            }
            List<DBUser> owners = dbWorker.Users.SelectUserById(order.ownerId);
            if (owners.Count > 0)
            {
                extendedOrder.ownerEmail = owners[0].email;
                extendedOrder.ownerScype = owners[0].scype;
            }
            List<DBUser> executors = dbWorker.Users.SelectUserById(order.executorId);
            if (executors.Count > 0)
            {
                extendedOrder.executorEmail = executors[0].email;
                extendedOrder.executorScype = executors[0].scype;
            }
            return extendedOrder;
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