using WPFSalonThorsson.Models;
using Salon.Repositories;
using Microsoft.Data.SqlClient;
using System;
using SalonT.Repositories;

namespace WPFSalonThorsson.Services
{
    public static class RentalValidator
    {
        public static string? ValidatePrice(RentalType type, decimal price)
        {
            if (type == RentalType.Daglig && price < 100)
                return "Minimumsprisen for dagsleje er 100 kr";

            if (type == RentalType.Maanedlig && price < 2000)
                return "Minimumsprisen for månedsleje er 2000 kr";

            return null;
        }

        public static string? ValidateSEDate(RentalType type, DateTime startDate, DateTime endDate)
        {
            if (type == RentalType.Maanedlig && startDate > endDate)
                return "´Startdatoen skal være før slutdatoen";

            return null;
        }
    }

    public class RentalService
    {
        private readonly IChairRepository _chairRepo;
        private readonly IRenterRepository _renterRepo;

        public RentalService(IChairRepository chairRepo, IRenterRepository renterRepo)
        {
            _chairRepo = chairRepo;
            _renterRepo = new RenterRepository();
        }

        public Renter? GetRenterByPhone(int phone) => _renterRepo.GetRenterByPhone(phone);
        public Renter? GetRenterById(int id) => _renterRepo.GetRenterById(id);

        public int CreateNewRenter(string name, int phoneNumber)
        {
            var existing = _renterRepo.GetRenterByPhone(phoneNumber);
            if (existing != null)
                throw new Exception($"En lejer med nummeret {phoneNumber} findes allerede (ID: {existing.RenterId})");

            return _renterRepo.CreateRenter(name, phoneNumber);
        }

        public class RenterUpdateData
        {
            public string? Name { get; set; }
            public int? PhoneNumber { get; set; }

            public bool HasAnyUpdates()
            {
                return Name != null || PhoneNumber.HasValue;
            }
        }

        public RentalResult UpdateRenterInfo(int renterId, RenterUpdateData updateData)
        {
            if (updateData == null || !updateData.HasAnyUpdates())
                return new RentalResult("Ingen ændringer valgt");

            var existing = _renterRepo.GetRenterById(renterId);
            if (existing == null)
                return new RentalResult($"Ingen lejer fundet med ID {renterId}");

            string newName = updateData.Name ?? existing.Name;
            int newPhone = updateData.PhoneNumber ?? existing.PhoneNumber;

            if (newName == existing.Name && newPhone == existing.PhoneNumber)
                return new RentalResult("Ingen ændringer valgt");

            try
            {
                bool success = _renterRepo.UpdateRenter(renterId, newName, newPhone);
                if (!success) return new RentalResult($"Kunne ikke opdatere lejer ID {renterId}");
                return new RentalResult(renterId);
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601 || ex.Number == 2627)
                    return new RentalResult($"Telefonnummeret {newPhone} er allerede i brug af en anden lejer");

                return new RentalResult("Databasefejl ved opdatering af lejer");
            }
        }

        public bool CheckChairExists(int chairId)
        {
            return _chairRepo.ChairExists(chairId);
        }


        public RentalResult CreateDailyRental(int chairId, int renterId, DateTime date, decimal dailyPrice, PaymentStatus status)
        {
            if (!_chairRepo.ChairExists(chairId))
                return new RentalResult($"Stol {chairId} eksisterer ikke");

            string? priceError = RentalValidator.ValidatePrice(RentalType.Daglig, dailyPrice);
            if (priceError != null) return new RentalResult(priceError);

            string? dateError = RentalValidator.ValidateSEDate(RentalType.Daglig, date, date);
            if (dateError != null) return new RentalResult(dateError);


            var rental = new ChairRental
            {
                ChairId = chairId,
                RenterId = renterId,
                StartDate = date,
                EndDate = date,
                RentalType = RentalType.Daglig,
                Price = dailyPrice,
                TotalPrice = dailyPrice,
                PaymentStatus = status,
                CreatedDate = DateTime.Now
            };

            return InsertRentalSafe(rental);
        }

        public RentalResult CreateMonthlyRental(int chairId, int renterId, DateTime startDate, DateTime endDate, decimal monthlyPrice, PaymentStatus status)
        {
            if (!_chairRepo.ChairExists(chairId))
                return new RentalResult($"Stol {chairId} eksisterer ikke");

            string? dateError = RentalValidator.ValidateSEDate(RentalType.Maanedlig, startDate, endDate);
            if (dateError != null) return new RentalResult(dateError);

            string? priceError = RentalValidator.ValidatePrice(RentalType.Maanedlig, monthlyPrice);
            if (priceError != null) return new RentalResult(priceError);

            decimal calculatedTotal = (decimal)Math.Ceiling((endDate - startDate).TotalDays / 30) * monthlyPrice;

            var rental = new ChairRental
            {
                ChairId = chairId,
                RenterId = renterId,
                StartDate = startDate,
                EndDate = endDate,
                RentalType = RentalType.Maanedlig,
                Price = monthlyPrice,
                TotalPrice = calculatedTotal,
                PaymentStatus = status,
                CreatedDate = DateTime.Now
            };

            return InsertRentalSafe(rental);
        }

        private RentalResult InsertRentalSafe(ChairRental rental)
        {
            try
            {
                int newId = _chairRepo.InsertRental(rental);
                return new RentalResult(newId);
            }
            catch (SqlException ex)
            {
                return HandleSqlException(ex);
            }
            catch (Exception ex)
            {
                return new RentalResult($"Uventet fejl: {ex.Message}");
            }
        }

        public RentalResult UpdateRental(int rentalId, RentalUpdateData updateData)
        {
            if (updateData == null || !updateData.HasAnyUpdates())
                return new RentalResult("Ingen ændringer valgt");

            var existing = _chairRepo.GetRentalDetails(rentalId);
            if (existing == null) return new RentalResult($"Rental ID {rentalId} findes ikke");

            existing.ChairId = updateData.ChairId ?? existing.ChairId;
            existing.StartDate = updateData.StartDate ?? existing.StartDate;
            existing.EndDate = updateData.EndDate ?? existing.EndDate;
            existing.Price = updateData.Price ?? existing.Price;
            existing.PaymentStatus = updateData.PaymentStatus ?? existing.PaymentStatus;

            string? dateError = RentalValidator.ValidateSEDate(existing.RentalType, existing.StartDate, existing.EndDate);
            if (dateError != null) return new RentalResult(dateError);

            string? priceError = RentalValidator.ValidatePrice(existing.RentalType, existing.Price);
            if (priceError != null) return new RentalResult(priceError);

            if (existing.RentalType == RentalType.Maanedlig)
            {
                var months = Math.Ceiling((decimal)(existing.EndDate - existing.StartDate).TotalDays / 30m);
                existing.TotalPrice = (months < 1 ? 1 : months) * existing.Price;
            }
            else
                existing.TotalPrice = existing.Price;

            try
            {
                _chairRepo.UpdateRental(existing);
                return new RentalResult(rentalId);
            }
            catch (SqlException ex)
            {
                return HandleSqlException(ex);
            }
        }

        private RentalResult HandleSqlException(SqlException ex)
        {
            string msg = ex.Message;

            if (msg.Contains("FK_ChairRentals_Chairs"))
                return new RentalResult("Fejl: Den valgte stol eksisterer ikke i systemet (DB Constraint)");

            if (msg.Contains("FK_ChairRentals_Renters"))
                return new RentalResult("Fejl: Den angivne lejer eksisterer ikke (DB Constraint)");

            if (msg.Contains("CK_ChairRentals_StartEnd"))
                return new RentalResult("Fejl: Slutdato må ikke ligge før startdato (DB Constraint)");

            if (msg.Contains("CK_ChairRentals_Status"))
                return new RentalResult("Fejl: Ugyldig betalingsstatus (DB Constraint)");

            if (msg.Contains("CK_ChairRentals_Type"))
                return new RentalResult("Fejl: Ugyldig lejetype (DB Constraint)");

            return new RentalResult($"Database fejl: {msg}");
        }

        public class RentalUpdateData
        {
            public int? ChairId { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public decimal? Price { get; set; }
            public PaymentStatus? PaymentStatus { get; set; }
            public bool HasAnyUpdates() => ChairId.HasValue || StartDate.HasValue || EndDate.HasValue || Price.HasValue || PaymentStatus.HasValue;
        }
    }
}