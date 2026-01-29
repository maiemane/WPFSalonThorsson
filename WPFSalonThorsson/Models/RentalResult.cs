namespace WPFSalonThorsson.Models
{
    public class RentalResult
    {
        public bool Success { get; set; }
        public int RentalId { get; set; }
        public string? ErrorMessage { get; set; }

        public RentalResult() { }

        public RentalResult(int rentalId)
        {
            Success = true;
            RentalId = rentalId;
            ErrorMessage = null;
        }

        public RentalResult(string errorMessage)
        {
            Success = false;
            RentalId = 0;
            ErrorMessage = errorMessage;
        }
    }
}