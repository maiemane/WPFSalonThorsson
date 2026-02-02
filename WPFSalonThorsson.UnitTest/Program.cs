using SalonT.Repositories;
using System;
using System.Text;
using WPFSalonThorsson.Models;
using WPFSalonThorsson.Services;
using static WPFSalonThorsson.Services.RentalService;

namespace WPFSalonThorsson.UnitTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;

            Console.WriteLine("--- Starter Udvidede Unit Tests ---\n");

            try
            {
                RunUpdateRentalTest_HappyPath();
                RunUpdateRentalTest_PartialUpdate();
                RunUpdateRentalTest_NoChanges();
                RunUpdateRentalTest_InvalidDates();
                RunUpdateRentalTest_Overlap();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nALLE TESTS BESTÅET!");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nTEST FEJLEDE: {ex.Message}");
            }

            Console.ResetColor();
            Console.WriteLine("Tryk Enter for at lukke...");
            Console.ReadLine();
        }

        static void RunUpdateRentalTest_HappyPath()
        {
            Console.Write("Test 1: Happy Path (Almindelig opdatering)... ");

            var fakeRepo = new FakeChairRepository();
            fakeRepo.AddForTest(new ChairRental { RentalId = 1, Price = 100, RentalType = RentalType.Daglig });

            var service = new RentalService(fakeRepo, null);
            var updateData = new RentalUpdateData { Price = 200 };

            var result = service.UpdateRental(1, updateData);

            if (!result.Success) throw new Exception(result.ErrorMessage);
            if (fakeRepo.GetRentalDetails(1).Price != 200) throw new Exception("Prisen blev ikke opdateret");

            Console.WriteLine("OK");
        }


        static void RunUpdateRentalTest_PartialUpdate()
        {
            Console.Write("Test 2: Partial Update (Kun ét felt ændres)... ");

            var fakeRepo = new FakeChairRepository();
            var originalDate = new DateTime(2023, 1, 1);
            fakeRepo.AddForTest(new ChairRental
            {
                RentalId = 2,
                Price = 100,
                StartDate = originalDate
            });

            var service = new RentalService(fakeRepo, null);

            var updateData = new RentalUpdateData { Price = 500 };

            service.UpdateRental(2, updateData);

            var updatedRental = fakeRepo.GetRentalDetails(2);

            if (updatedRental.Price != 500) throw new Exception("Prisen blev ikke opdateret");

            if (updatedRental.StartDate != originalDate) throw new Exception("Fejl! Den gamle startdato blev overskrevet/slettet");

            Console.WriteLine("OK");
        }

        static void RunUpdateRentalTest_NoChanges()
        {
            Console.Write("Test 3: No Changes (Tom opdatering)... ");

            var fakeRepo = new FakeChairRepository();
            fakeRepo.AddForTest(new ChairRental { RentalId = 3, Price = 300 });
            var service = new RentalService(fakeRepo, null);

            var emptyUpdate = new RentalUpdateData();

            var result = service.UpdateRental(3, emptyUpdate);

            if (result.Success) throw new Exception("Fejl! Servicen burde have afvist en tom opdatering");
            if (!result.ErrorMessage.Contains("Ingen ændringer")) throw new Exception($"Forkert fejlbesked: {result.ErrorMessage}");

            Console.WriteLine("OK");
        }

        static void RunUpdateRentalTest_InvalidDates()
        {
            Console.Write("Test 4: Invalid Date Logic (Slut før Start)... ");

            var fakeRepo = new FakeChairRepository();
            fakeRepo.AddForTest(new ChairRental
            {
                RentalId = 4,
                StartDate = new DateTime(2023, 5, 1),
                EndDate = new DateTime(2023, 5, 10)
            });
            var service = new RentalService(fakeRepo, null);

            var badDateUpdate = new RentalUpdateData { EndDate = new DateTime(2023, 1, 1) };

            var result = service.UpdateRental(4, badDateUpdate);

            if (result.Success) throw new Exception("Fejl! Systemet tillod en slutdato før startdatoen");

            Console.WriteLine("OK)");
        }

        static void RunUpdateRentalTest_Overlap()
        {
            Console.Write("Test 5: Update Overlap (Dobbeltbooking tjek)... ");

            var fakeRepo = new FakeChairRepository();

            fakeRepo.AddForTest(new ChairRental
            {
                RentalId = 10,
                ChairId = 1,
                StartDate = new DateTime(2023, 1, 10),
                EndDate = new DateTime(2023, 1, 10),
                RentalType = RentalType.Daglig,
                Price = 500
            });

            fakeRepo.AddForTest(new ChairRental
            {
                RentalId = 11,
                ChairId = 1,
                StartDate = new DateTime(2023, 1, 12),
                EndDate = new DateTime(2023, 1, 12),
                RentalType = RentalType.Daglig,
                Price = 500
            });

            var service = new RentalService(fakeRepo, null);

            var updateData = new RentalUpdateData
            {
                StartDate = new DateTime(2023, 1, 10),
                EndDate = new DateTime(2023, 1, 10)
            };

            var result = service.UpdateRental(11, updateData);

            if (result.Success) throw new Exception("Fejl! Systemet tillod overlap ved opdatering");
            if (!result.ErrorMessage.Contains("Overlap med eksisterende booking")) throw new Exception($"Forkert fejlbesked: {result.ErrorMessage}");

            Console.WriteLine("OK");
        }
    }
}