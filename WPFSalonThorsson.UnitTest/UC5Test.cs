using SalonT.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using WPFSalonThorsson.Models;

namespace WPFSalonThorsson.UnitTest
{
    public class FakeChairRepository : IChairRepository
    {
        private List<ChairRental> _fakeDatabase = new List<ChairRental>();

        public void AddForTest(ChairRental rental)
        {
            _fakeDatabase.Add(rental);
        }
        public bool ChairExists(int id) => true;

        public ChairRental GetRentalDetails(int rentalId)
        {
            return _fakeDatabase.FirstOrDefault(r => r.RentalId == rentalId);
        }

        public bool UpdateRental(ChairRental rental)
        {
            var existing = GetRentalDetails(rental.RentalId);
            if (existing != null)
            {
                _fakeDatabase.Remove(existing);
                _fakeDatabase.Add(rental);
                return true;
            }
            return false;
        }


        public int InsertRental(ChairRental rental)
        {
            _fakeDatabase.Add(rental);
            return 999;
        }

        public bool DeleteRental(int id) => false;

        public bool HasOverlap(int chairId, DateTime start, DateTime end, int? excludeRentalId = null)
        {
            return _fakeDatabase.Any(r =>
                r.ChairId == chairId &&
                r.RentalId != excludeRentalId &&
                start <= r.EndDate && end >= r.StartDate
            );
        }

        public List<ChairRental> GetUpcomingRentals(DateTime date) => new List<ChairRental>();
        public List<ChairRental> GetCompletedRentals(DateTime date) => new List<ChairRental>();
        public List<ChairRental> GetRentalsByChair(int chairId) => new List<ChairRental>();

        public List<ChairRental> GetAllRentals() => _fakeDatabase;
    }
}