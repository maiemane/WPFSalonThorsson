using WPFSalonThorsson.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalonT.Repositories
{
    public interface IChairRepository
    {
        int InsertRental(ChairRental rental);

        ChairRental? GetRentalDetails(int rentalId);

        bool UpdateRental(ChairRental rental);

        bool DeleteRental(int rentalId);

        bool ChairExists(int chairId);

        bool HasOverlap(int ChairId, DateTime StartTime, DateTime EndTime, int? excludeRentalId = null);

        List<ChairRental> GetUpcomingRentals(DateTime fromDate);

        List<ChairRental> GetCompletedRentals(DateTime beforeDate);

        List<ChairRental> GetRentalsByChair(int chairId);


    }
}
