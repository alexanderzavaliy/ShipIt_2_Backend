using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication2.Models
{
    public class ExtendedOrder
    {
        public long id { get; set; }
        public long ownerId { get; set; }
        public string ownerEmail { get; set; }
        public string ownerScype { get; set; }
        public long instrumentId { get; set; }
        public string instrumentShortName { get; set; }
        public long createdDate { get; set; }
        public string createdDateStr { get; set; }
        public long endDate { get; set; }
        public string endDateStr { get; set; }
        public string type { get; set; }
        public double price { get; set; }
        public long amount { get; set; }
        public string status { get; set; }
        public long executorId { get; set; }
        public string executorEmail { get; set; }
        public string executorScype { get; set; }
        public long executionDate { get; set; }
        public string executionDateStr { get; set; }

        public ExtendedOrder()
        {
            id = 0;
            ownerId = 0;
            ownerEmail = "";
            ownerScype = "";
            instrumentId = 0;
            instrumentShortName = "";
            createdDate = 0;
            createdDateStr = "";
            endDate = 0;
            endDateStr = "";
            type = "";
            price = 0;
            amount = 0;
            status = "";
            executorId = 0;
            executorEmail = "";
            executorScype = "";
            executionDate = 0;
            executionDateStr = "";
        }
    }
}