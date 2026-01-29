using Microsoft.Data.SqlClient;
using Salon.Data;
using WPFSalonThorsson.Models;
using SalonT.Repositories;

namespace Salon.Repositories
{
    public class RenterRepository : IRenterRepository
    {
        public Renter? GetRenterByPhone(int phoneNumber)
        {
            using (SqlConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string sql = "SELECT * FROM Renters WHERE PhoneNumber = @PhoneNumber";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@PhoneNumber", phoneNumber);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read()) return MapReaderToRenter(reader);
                    }
                }
            }
            return null;
        }

        public Renter? GetRenterById(int renterId)
        {
            using (SqlConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string sql = "SELECT * FROM Renters WHERE RenterId = @RenterId";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@RenterId", renterId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read()) return MapReaderToRenter(reader);
                    }
                }
            }
            return null;
        }

        public int CreateRenter(string name, int phoneNumber)
        {
            using (SqlConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string sql = @"INSERT INTO Renters (Name, PhoneNumber) 
                               VALUES (@Name, @PhoneNumber);
                               SELECT CAST(SCOPE_IDENTITY() AS int);";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@PhoneNumber", phoneNumber);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public bool UpdateRenter(int renterId, string newName, int newPhone)
        {
            using (SqlConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string sql = "UPDATE Renters SET Name = @Name, PhoneNumber = @Phone WHERE RenterId = @Id";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", newName);
                    cmd.Parameters.AddWithValue("@Phone", newPhone);
                    cmd.Parameters.AddWithValue("@Id", renterId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        private Renter MapReaderToRenter(SqlDataReader reader)
        {
            return new Renter
            {
                RenterId = (int)reader["RenterId"],
                Name = (string)reader["Name"],
                PhoneNumber = (int)reader["PhoneNumber"]
            };
        }
    }
}