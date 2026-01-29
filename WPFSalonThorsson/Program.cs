using WPFSalonThorsson.Models;
using WPFSalonThorsson.Services;
using Salon.Repositories;
using System;
using System.Text;
using System.Collections.Generic;

namespace WPFSalonThorsson
{
    internal class Program
    {
        static void Main1()
        {
            Console.OutputEncoding = Encoding.Unicode;
            var chairRepo = new ChairRepository();
            var rentalService = new RentalService(chairRepo);

            while (true)
            {
                Console.Clear();
                Console.WriteLine("✂️ === SALON THORSSON DATABASE === ✂️");
                Console.WriteLine("1. 📝 Opret Rental");
                Console.WriteLine("2. ✏️ Opdater Rental");
                Console.WriteLine("3. 👤 Opdater Lejer Info");
                Console.WriteLine("4. 🗑️ Slet Rental");
                Console.WriteLine("5. 🔍 Søg Rental (ID)");
                Console.WriteLine("6. 📅 Vis Rentals (Historik/Kommende)");
                Console.WriteLine("0. 🚪 Afslut");
                Console.Write("\nVælg funktion: ");

                string? choice = Console.ReadLine();

                switch (choice)
                {
                    case "1": CreateRental(rentalService); break;
                    case "2": UpdateRental(rentalService, chairRepo); break;
                    case "3": UpdateRenter(rentalService); break;
                    case "4": DeleteRental(chairRepo); break;
                    case "5": SearchRental(chairRepo); break;
                    case "6": ShowRentals(chairRepo); break;
                    case "0": return;
                    default: Console.WriteLine("Ugyldigt valg"); break;
                }
                Console.WriteLine("\nTryk på en tast for at fortsætte...");
                Console.ReadKey();
            }
        }

        static void CreateRental(RentalService service)
        {
            Console.WriteLine("\n=== OPRET NY LEJEAFTALE ===");

            int renterId = HandleRenterSelection(service);
            if (renterId == 0) return;

            int chairId = 0;
            while (true)
            {
                chairId = ReadInt("Indtast Stol ID (eks. 1 eller 2): ");
                if (service.CheckChairExists(chairId))
                {
                    Console.WriteLine("✅ Stol fundet");
                    break;
                }
                Console.WriteLine($"❌ Stol med ID {chairId} findes ikke i databasen. Prøv igen");
            }

            Console.WriteLine("\nVælg Lejetype:");
            Console.WriteLine("1. Daglig");
            Console.WriteLine("2. Månedlig");
            string typeChoice = Console.ReadLine() ?? "";

            RentalResult result;

            if (typeChoice == "1")
            {
                DateTime date = ReadDate("Indtast dato (yyyy-MM-dd): ");
                decimal price = ReadDecimal("Indtast dagspris (DKK): ");
                Console.WriteLine("Betalingsstatus (1=Ubetalt, 2=Betalt, 3=Afventer): ");
                PaymentStatus status = (PaymentStatus)ReadInt("");

                result = service.CreateDailyRental(chairId, renterId, date, price, status);
            }
            else if (typeChoice == "2")
            {
                DateTime start = ReadDate("Startdato (yyyy-MM-dd): ");
                DateTime end = ReadDate("Slutdato (yyyy-MM-dd): ");
                decimal price = ReadDecimal("Indtast månedspris (DKK): ");
                Console.WriteLine("Betalingsstatus (1=Ubetalt, 2=Betalt, 3=Afventer): ");
                PaymentStatus status = (PaymentStatus)ReadInt("");

                result = service.CreateMonthlyRental(chairId, renterId, start, end, price, status);
            }
            else
            {
                Console.WriteLine("❌ Ugyldigt valg");
                return;
            }

            PrintResult(result);
        }

        static int HandleRenterSelection(RentalService service)
        {
            while (true)
            {
                Console.WriteLine("\n--- Vælg Lejer ---");
                Console.WriteLine("1. Find eksisterende (Telefon)");
                Console.WriteLine("2. Find eksisterende (ID)");
                Console.WriteLine("3. Opret Ny Lejer");
                string c = Console.ReadLine() ?? "";

                if (c == "1")
                {
                    int phone = ReadInt("Indtast telefonnummer: ");
                    var renter = service.GetRenterByPhone(phone);
                    if (renter != null)
                    {
                        Console.WriteLine($"Fundet: ID {renter.RenterId} - {renter.Name} (Tlf: {renter.PhoneNumber})");
                        Console.WriteLine("Er dette den korrekte lejer? (j/n)");
                        if (Console.ReadLine()?.ToLower() == "j") return renter.RenterId;
                    }
                    else Console.WriteLine("❌ Ingen lejer fundet med det nummer");
                }
                else if (c == "2")
                {
                    int id = ReadInt("Indtast Lejer ID: ");
                    var renter = service.GetRenterById(id);
                    if (renter != null)
                    {
                        Console.WriteLine($"Fundet: ID {renter.RenterId} - {renter.Name} (Tlf: {renter.PhoneNumber})");
                        return renter.RenterId;
                    }
                    else Console.WriteLine("❌ Ingen lejer fundet med det ID");
                }
                else if (c == "3")
                {
                    Console.Write("Navn: ");
                    string name = Console.ReadLine() ?? "";
                    int phone = ReadInt("Telefon: ");
                    try
                    {
                        int newId = service.CreateNewRenter(name, phone);
                        Console.WriteLine($"✅ Lejer oprettet med ID {newId}");
                        return newId;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Fejl: {ex.Message}");
                    }
                }
                else return 0;
            }
        }

        static void UpdateRenter(RentalService service)
        {
            Console.WriteLine("\n=== OPDATER LEJER INFO ===");

            Renter? renter = null;

            Console.WriteLine("Søg efter lejer via:");
            Console.WriteLine("1. Telefonnummer");
            Console.WriteLine("2. Lejer ID");
            string s = Console.ReadLine() ?? "";

            if (s == "1")
            {
                int p = ReadInt("Indtast telefon: ");
                renter = service.GetRenterByPhone(p);
            }
            else
            {
                int i = ReadInt("Indtast ID: ");
                renter = service.GetRenterById(i);
            }

            if (renter == null)
            {
                Console.WriteLine("❌ Lejer ikke fundet");
                return;
            }

            Console.WriteLine($"Valgt lejer: {renter.Name} (ID: {renter.RenterId}, Tlf: {renter.PhoneNumber})");
            Console.WriteLine("Indtast nye oplysninger (Tryk Enter for at beholde nuværende)");

            var updateData = new RentalService.RenterUpdateData();

            Console.Write($"Navn ({renter.Name}): ");
            string nameInput = Console.ReadLine() ?? "";
            if (!string.IsNullOrWhiteSpace(nameInput)) updateData.Name = nameInput;

            Console.Write($"Telefon ({renter.PhoneNumber}): ");
            string phoneInput = Console.ReadLine() ?? "";
            if (int.TryParse(phoneInput, out int pVal)) updateData.PhoneNumber = pVal;

            var result = service.UpdateRenterInfo(renter.RenterId, updateData);
            PrintResult(result);
        }

        static void UpdateRental(RentalService service, ChairRepository repo)
        {
            Console.WriteLine("\n=== OPDATER RENTAL ===");
            int id = ReadInt("Indtast Rental ID: ");

            var current = repo.GetRentalDetails(id);
            if (current == null)
            {
                Console.WriteLine("❌ Rental ikke fundet");
                return;
            }

            Console.WriteLine($"Nuværende: Stol {current.ChairId}, {current.StartDate:dd/MM}-{current.EndDate:dd/MM}, Pris: {current.Price}, Status: {current.PaymentStatus}");

            var updateData = new RentalService.RentalUpdateData();

            while (true)
            {
                Console.Write($"Stol ID ({current.ChairId}): ");
                string cInput = Console.ReadLine() ?? "";
                if (string.IsNullOrWhiteSpace(cInput)) break;

                if (int.TryParse(cInput, out int cVal))
                {
                    if (service.CheckChairExists(cVal))
                    {
                        updateData.ChairId = cVal;
                        break;
                    }
                    Console.WriteLine("❌ Den stol findes ikke. Prøv igen eller tryk Enter for at beholde");
                }
                else Console.WriteLine("⚠️ Ugyldigt tal");
            }

            Console.Write($"Start ({current.StartDate:yyyy-MM-dd}): ");
            if (DateTime.TryParse(Console.ReadLine(), out DateTime sVal)) updateData.StartDate = sVal;

            Console.Write($"Slut ({current.EndDate:yyyy-MM-dd}): ");
            if (DateTime.TryParse(Console.ReadLine(), out DateTime eVal)) updateData.EndDate = eVal;

            Console.Write($"Pris ({current.Price}): ");
            if (decimal.TryParse(Console.ReadLine(), out decimal pVal)) updateData.Price = pVal;

            Console.Write($"Status ({(int)current.PaymentStatus}): ");
            if (int.TryParse(Console.ReadLine(), out int stVal)) updateData.PaymentStatus = (PaymentStatus)stVal;

            var result = service.UpdateRental(id, updateData);
            PrintResult(result);
        }

        static void SearchRental(ChairRepository repo)
        {
            int id = ReadInt("Indtast Rental ID: ");
            var r = repo.GetRentalDetails(id);

            if (r != null)
            {
                string icon = r.PaymentStatus == PaymentStatus.Betalt ? "✅" : (r.PaymentStatus == PaymentStatus.Afventer ? "⏳" : "⚠️");
                Console.WriteLine("\n--- DETALJER ---");
                Console.WriteLine($"ID: {r.RentalId} (Oprettet: {r.CreatedDate:dd/MM/yyyy})");
                Console.WriteLine($"Lejer: {r.RenterName} (Tlf: {r.PhoneNumber})");
                Console.WriteLine($"Stol: {r.ChairId} - {r.RentalType}");
                Console.WriteLine($"Periode: {r.StartDate:dd/MM/yyyy} til {r.EndDate:dd/MM/yyyy}");
                Console.WriteLine($"Økonomi: {r.TotalPrice:C} - Status: {r.PaymentStatus} {icon}");
            }
            else
            {
                Console.WriteLine("❌ Rental ikke fundet");
            }
        }

        static void ShowRentals(ChairRepository repo)
        {
            Console.WriteLine("\n1. Kommende/Aktive lejeaftaler");
            Console.WriteLine("2. Historik (Afsluttede)");
            string choice = Console.ReadLine() ?? "";

            List<ChairRental> list = (choice == "1")
                ? repo.GetUpcomingRentals(DateTime.Now)
                : repo.GetCompletedRentals(DateTime.Now);

            Console.WriteLine($"\nFandt {list.Count} aftaler:");

            foreach (var r in list)
            {
                string icon = r.PaymentStatus == PaymentStatus.Betalt ? "✅" : (r.PaymentStatus == PaymentStatus.Afventer ? "⏳" : "⚠️");
                Console.WriteLine($"#{r.RentalId} - {r.RenterName} - {r.StartDate:dd/MM} til {r.EndDate:dd/MM} - {r.TotalPrice:N0} kr {icon}");
            }
        }

        static void DeleteRental(ChairRepository repo)
        {
            Console.WriteLine("\n=== 🗑️ SLET RENTAL ===");
            int id = ReadInt("Indtast ID på rental der skal slettes: ");

            var rental = repo.GetRentalDetails(id);

            if (rental != null)
            {
                Console.WriteLine($"Fundet aftale: ID {rental.RentalId}, {rental.RenterName}, {rental.StartDate:dd/MM}-{rental.EndDate:dd/MM}");
                Console.WriteLine($"⚠️ ADVARSEL: Er du sikker på at du vil slette denne aftale? Skriv 'SLET'");

                string confirm = Console.ReadLine()?.ToLower() ?? "";

                if (confirm == "slet")
                {
                    bool deleted = repo.DeleteRental(id);
                    if (deleted)
                        Console.WriteLine($"✅ Rental {id} er blevet slettet permanent.");
                    else
                        Console.WriteLine("❌ Fejl: Kunne ikke slette aftalen fra databasen.");
                }
                else
                {
                    Console.WriteLine("⚠️ Sletning annulleret.");
                }
            }
            else
            {
                Console.WriteLine($"❌ Ingen rental fundet med ID {id}.");
            }
        }

        static void PrintResult(RentalResult r)
        {
            if (r.Success)
                Console.WriteLine($"\n✅ Handling gennemført succesfuldt! (ID: {r.RentalId})");
            else
                Console.WriteLine($"\n❌ FEJL: {r.ErrorMessage}");
        }

        // INPUT HELPERS

        static int ReadInt(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string input = Console.ReadLine() ?? "";
                if (int.TryParse(input, out int result)) return result;
                Console.WriteLine("⚠️ Indtast venligst et heltal");
            }
        }

        static decimal ReadDecimal(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string input = Console.ReadLine() ?? "";
                if (decimal.TryParse(input, out decimal result)) return result;
                Console.WriteLine("⚠️ Indtast venligst et tal");
            }
        }

        static DateTime ReadDate(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string input = Console.ReadLine() ?? "";
                if (DateTime.TryParse(input, out DateTime result)) return result;
                Console.WriteLine("⚠️ Ugyldig dato - format: yyyy-MM-dd");
            }
        }
    }
}