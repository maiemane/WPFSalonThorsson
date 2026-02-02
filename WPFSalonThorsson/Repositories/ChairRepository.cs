using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Salon.Data;
using WPFSalonThorsson.Models;
using SalonT.Repositories;

namespace Salon.Repositories
{
    public class ChairRepository : IChairRepository
    {
        private const string BaseSelectSql = @"
            SELECT cr.*, r.Name as RenterName, r.PhoneNumber 
            FROM ChairRentals cr
            JOIN Renters r ON cr.RenterId = r.RenterId ";

        private ChairRental MapReaderToRental(SqlDataReader reader)
        {
            return new ChairRental
            {
                RentalId = Convert.ToInt32(reader["RentalId"]),
                ChairId = Convert.ToInt32(reader["ChairId"]),
                RenterId = Convert.ToInt32(reader["RenterId"]),
                RenterName = reader["RenterName"].ToString() ?? string.Empty,
                PhoneNumber = reader["PhoneNumber"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["PhoneNumber"]),
                RentalType = (RentalType)Convert.ToInt32(reader["RentalType"]),
                PaymentStatus = (PaymentStatus)Convert.ToInt32(reader["PaymentStatus"]),
                Price = Convert.ToDecimal(reader["Price"]),
                TotalPrice = Convert.ToDecimal(reader["TotalPrice"]),
                StartDate = Convert.ToDateTime(reader["StartDate"]),
                EndDate = Convert.ToDateTime(reader["EndDate"]),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                UpdatedDate = reader["UpdatedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["UpdatedDate"])
            };
        }

        public bool ChairExists(int chairId)
        {
            using (SqlConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string sql = "SELECT COUNT(1) FROM Chairs WHERE ChairId = @ChairId";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ChairId", chairId);
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        public int InsertRental(ChairRental rental)
        {
            using (SqlConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string sql = @"INSERT INTO ChairRentals (ChairId, RenterId, RentalType, PaymentStatus, Price, TotalPrice, StartDate, EndDate, CreatedDate)
                               VALUES (@ChairId, @RenterId, @RentalType, @PaymentStatus, @Price, @TotalPrice, @StartDate, @EndDate, @CreatedDate);
                               SELECT CAST(SCOPE_IDENTITY() AS int);";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ChairId", rental.ChairId);
                    cmd.Parameters.AddWithValue("@RenterId", rental.RenterId);
                    cmd.Parameters.AddWithValue("@RentalType", (int)rental.RentalType);
                    cmd.Parameters.AddWithValue("@PaymentStatus", (int)rental.PaymentStatus);
                    cmd.Parameters.AddWithValue("@Price", rental.Price);
                    cmd.Parameters.AddWithValue("@TotalPrice", rental.TotalPrice);
                    cmd.Parameters.AddWithValue("@StartDate", rental.StartDate);
                    cmd.Parameters.AddWithValue("@EndDate", rental.EndDate);
                    cmd.Parameters.AddWithValue("@CreatedDate", rental.CreatedDate);

                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public ChairRental? GetRentalDetails(int rentalId)
        {
            using (SqlConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string sql = BaseSelectSql + "WHERE cr.RentalId = @RentalId";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@RentalId", rentalId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read()) return MapReaderToRental(reader);
                    }
                }
            }
            return null;
        }

        public bool UpdateRental(ChairRental rental)
        {
            using (SqlConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string sql = @"
                    UPDATE ChairRentals
                    SET ChairId = @ChairId,
                        RentalType = @RentalType,
                        PaymentStatus = @PaymentStatus,
                        Price = @Price,
                        TotalPrice = @TotalPrice,
                        StartDate = @StartDate,
                        EndDate = @EndDate,
                        UpdatedDate = @UpdatedDate
                    WHERE RentalId = @RentalId";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ChairId", rental.ChairId);
                    cmd.Parameters.AddWithValue("@RentalType", (int)rental.RentalType);
                    cmd.Parameters.AddWithValue("@PaymentStatus", (int)rental.PaymentStatus);
                    cmd.Parameters.AddWithValue("@Price", rental.Price);
                    cmd.Parameters.AddWithValue("@TotalPrice", rental.TotalPrice);
                    cmd.Parameters.AddWithValue("@StartDate", rental.StartDate);
                    cmd.Parameters.AddWithValue("@EndDate", rental.EndDate);
                    cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@RentalId", rental.RentalId);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool DeleteRental(int rentalId)
        {
            using (SqlConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string sql = "DELETE FROM ChairRentals WHERE RentalId = @RentalId";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@RentalId", rentalId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public List<ChairRental> GetUpcomingRentals(DateTime fromDate)
        {
            var rentals = new List<ChairRental>();
            using (SqlConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string sql = BaseSelectSql + "WHERE cr.EndDate >= @FromDate ORDER BY cr.StartDate ASC";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@FromDate", fromDate.Date);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) rentals.Add(MapReaderToRental(reader));
                    }
                }
            }
            return rentals;
        }

        public List<ChairRental> GetCompletedRentals(DateTime beforeDate)
        {
            var rentals = new List<ChairRental>();
            using (SqlConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string sql = BaseSelectSql + "WHERE cr.EndDate < @BeforeDate ORDER BY cr.EndDate DESC";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@BeforeDate", beforeDate.Date);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) rentals.Add(MapReaderToRental(reader));
                    }
                }
            }
            return rentals;
        }

        public List<ChairRental> GetRentalsByChair(int chairId)
        {
            var rentals = new List<ChairRental>();
            using (SqlConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string sql = BaseSelectSql + "WHERE cr.ChairId = @ChairId";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ChairId", chairId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) rentals.Add(MapReaderToRental(reader));
                    }
                }
            }
            return rentals;
        }

        public bool HasOverlap(int chairId, DateTime startDate, DateTime endDate, int? excludeRentalId = null)
        {
            using (SqlConnection conn = DatabaseConnection.GetConnection())
            {
                conn.Open();

                string sql = @"
            SELECT COUNT(1)
            FROM ChairRentals
            WHERE ChairId = @ChairId
              AND StartDate <= @EndDate
              AND EndDate >= @StartDate";

                if (excludeRentalId.HasValue)
                    sql += " AND RentalId <> @ExcludeId";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ChairId", chairId);
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);

                    if (excludeRentalId.HasValue)
                        cmd.Parameters.AddWithValue("@ExcludeId", excludeRentalId.Value);

                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

    }
}