using System;

namespace WPFSalonThorsson.Models
{
    public class ChairRental
    {
        public int RentalId { get; set; }
        public int ChairId { get; set; }

        public int RenterId { get; set; }

        public RentalType RentalType { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public string RenterName { get; set; } = string.Empty;
        public int? PhoneNumber { get; set; }
    }

    public enum PaymentStatus
    {
        Ubetalt = 1,
        Betalt = 2,
        Afventer = 3,
    }

    public enum RentalType
    {
        Daglig,
        Maanedlig
    }
}