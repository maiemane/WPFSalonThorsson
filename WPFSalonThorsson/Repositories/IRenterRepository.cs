using WPFSalonThorsson.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalonT.Repositories
{
    public interface IRenterRepository
    {

        Renter? GetRenterByPhone(int phoneNumber);

        Renter? GetRenterById(int renterId);

        int CreateRenter(string name, int phoneNumber);

        bool UpdateRenter(int renterId, string newName, int newPhone);

    }
}
