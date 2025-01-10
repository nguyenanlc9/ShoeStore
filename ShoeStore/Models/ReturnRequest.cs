using System;
using ShoeStore.Models.Enums;

namespace ShoeStore.Models
{
    public class ReturnRequest
    {
        public int ReturnRequestId { get; set; }
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public DateTime RequestDate { get; set; }
        public string Reason { get; set; }
        public string Images { get; set; }
        public ReturnStatus Status { get; set; }
        public string AdminNote { get; set; }

        public virtual Order Order { get; set; }
        public virtual User User { get; set; }
    }
} 