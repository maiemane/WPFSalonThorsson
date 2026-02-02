using SalonT.Repositories;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using WPFSalonThorsson.Models;

namespace WPFSalonThorsson.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly IChairRepository _chairRepository;
        private readonly IRenterRepository _renterRepository;

        public event PropertyChangedEventHandler? PropertyChanged;

        // ===== Lists =====
        public ObservableCollection<ChairRental> UpcomingRentals { get; } = new();
        public ObservableCollection<ChairRental> CompletedRentals { get; } = new();

        private ChairRental? _selectedRental;
        public ChairRental? SelectedRental
        {
            get => _selectedRental;
            set
            {
                _selectedRental = value;
                OnPropertyChanged();
                UpdateCanExecute();
            }
        }

        // ===== Status/Search =====
        private string? _statusMessage;
        public string? StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private string? _searchRentalId;
        public string? SearchRentalId
        {
            get => _searchRentalId;
            set { _searchRentalId = value; OnPropertyChanged(); }
        }

        // ===== Form =====
        private string? _formTitle = "Opret Lejeaftale";
        public string? FormTitle
        {
            get => _formTitle;
            set { _formTitle = value; OnPropertyChanged(); }
        }

        private string? _renterSearchText;
        public string? RenterSearchText
        {
            get => _renterSearchText;
            set { _renterSearchText = value; OnPropertyChanged(); }
        }

        private string? _chairId;
        public string? ChairId
        {
            get => _chairId;
            set { _chairId = value; OnPropertyChanged(); UpdateCanExecute(); }
        }

        private Renter? _selectedRenter;
        public Renter? SelectedRenter
        {
            get => _selectedRenter;
            set
            {
                _selectedRenter = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedRenterDisplay));
                UpdateCanExecute();
            }
        }

        public string SelectedRenterDisplay =>
            SelectedRenter == null
                ? "(ingen valgt)"
                : $"ID {SelectedRenter.RenterId} - {SelectedRenter.Name} (Tlf: {SelectedRenter.PhoneNumber})";

        private RentalType _selectedRentalType = RentalType.Daglig;
        public RentalType SelectedRentalType
        {
            get => _selectedRentalType;
            set
            {
                _selectedRentalType = value;
                OnPropertyChanged();
                UpdateCanExecute(); // fordi månedlig kræver EndDate
            }
        }

        public ObservableCollection<RentalType> RentalTypes { get; } =
            new ObservableCollection<RentalType>(Enum.GetValues(typeof(RentalType)).Cast<RentalType>());

        private PaymentStatus _selectedPaymentStatus = PaymentStatus.Ubetalt;
        public PaymentStatus SelectedPaymentStatus
        {
            get => _selectedPaymentStatus;
            set { _selectedPaymentStatus = value; OnPropertyChanged(); }
        }

        // OBS: kald den PaymentStatuses (ikke PaymentStatus) for at undgå forvirring i XAML
        public ObservableCollection<PaymentStatus> PaymentStatuses { get; } =
            new ObservableCollection<PaymentStatus>(Enum.GetValues(typeof(PaymentStatus)).Cast<PaymentStatus>());

        private DateTime? _startDate;
        public DateTime? StartDate
        {
            get => _startDate;
            set { _startDate = value; OnPropertyChanged(); UpdateCanExecute(); }
        }

        private DateTime? _endDate;
        public DateTime? EndDate
        {
            get => _endDate;
            set { _endDate = value; OnPropertyChanged(); UpdateCanExecute(); }
        }

        private string? _price;
        public string? Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(); UpdateCanExecute(); }
        }

        // ===== Commands =====
        public RelayCommand RefreshCommand { get; }
        public RelayCommand SearchRentalCommand { get; }
        public RelayCommand StartCreateCommand { get; }
        public RelayCommand StartEditCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand FindRenterCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        // ===== Edit-state =====
        private bool _isEditing;
        private int? _editingRentalId;

        public MainWindowViewModel(IChairRepository chairRepository, IRenterRepository renterRepository)
        {
            _chairRepository = chairRepository;
            _renterRepository = renterRepository;

            RefreshCommand = new RelayCommand(Refresh);
            SearchRentalCommand = new RelayCommand(SearchRental);
            StartCreateCommand = new RelayCommand(StartCreate);

            // CanExecute så man ikke kan redigere/slette uden valgt række
            StartEditCommand = new RelayCommand(StartEdit, () => SelectedRental != null);
            DeleteCommand = new RelayCommand(DeleteSelected, () => SelectedRental != null);

            FindRenterCommand = new RelayCommand(FindRenter);
            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(StartCreate);

            Refresh();
            StartCreate();
        }

        private void Refresh()
        {
            try
            {
                UpcomingRentals.Clear();
                CompletedRentals.Clear();

                foreach (var r in _chairRepository.GetUpcomingRentals(DateTime.Now))
                    UpcomingRentals.Add(r);

                foreach (var r in _chairRepository.GetCompletedRentals(DateTime.Now))
                    CompletedRentals.Add(r);

                StatusMessage = "Lejeaftaler opdateret.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fejl ved opdatering af lejeaftaler: {ex.Message}";
            }
        }

        private void SearchRental()
        {
            if (!int.TryParse(SearchRentalId, out int id))
            {
                StatusMessage = "Ugyldigt ID.";
                return;
            }

            try
            {
                var rental = _chairRepository.GetRentalDetails(id);
                if (rental == null)
                {
                    StatusMessage = $"Lejeaftale med ID {id} ikke fundet.";
                    return;
                }

                LoadRentalToForm(rental);

                _isEditing = true;
                _editingRentalId = rental.RentalId;
                FormTitle = $"Rediger Lejeaftale (ID {rental.RentalId})";
                StatusMessage = "Lejeaftale indlæst til redigering.";

                UpdateCanExecute();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fejl ved søgning efter lejeaftale: {ex.Message}";
            }
        }

        private void StartCreate()
        {
            _isEditing = false;
            _editingRentalId = null;

            FormTitle = "Opret Lejeaftale";

            SelectedRenter = null;
            RenterSearchText = null;

            ChairId = null;
            SelectedRentalType = RentalType.Daglig;
            SelectedPaymentStatus = PaymentStatus.Ubetalt;

            StartDate = DateTime.Today;
            EndDate = DateTime.Today;

            Price = null;

            StatusMessage = "Udfyld formularen for at oprette en ny lejeaftale.";
            UpdateCanExecute();
        }

        private void StartEdit()
        {
            if (SelectedRental == null)
            {
                StatusMessage = "Vælg en lejeaftale først.";
                return;
            }

            try
            {
                var full = _chairRepository.GetRentalDetails(SelectedRental.RentalId);
                if (full == null)
                {
                    StatusMessage = "Kunne ikke finde lejeaftalen i databasen.";
                    return;
                }

                LoadRentalToForm(full);

                _isEditing = true;
                _editingRentalId = full.RentalId;
                FormTitle = $"Rediger Lejeaftale (ID {full.RentalId})";
                StatusMessage = "Redigér felter og tryk Gem.";

                UpdateCanExecute();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fejl ved redigering: {ex.Message}";
            }
        }

        private void LoadRentalToForm(ChairRental r)
        {
            ChairId = r.ChairId.ToString();
            SelectedRentalType = r.RentalType;
            SelectedPaymentStatus = r.PaymentStatus;

            StartDate = r.StartDate.Date;
            EndDate = r.EndDate.Date;

            Price = r.Price.ToString();

            try
            {
                SelectedRenter = _renterRepository.GetRenterById(r.RenterId);
            }
            catch
            {
                SelectedRenter = null;
            }
        }

        private void DeleteSelected()
        {
            if (SelectedRental == null) return;

            try
            {
                bool ok = _chairRepository.DeleteRental(SelectedRental.RentalId);
                StatusMessage = ok ? "Lejeaftale er slettet." : "Kunne ikke slette lejeaftale.";

                Refresh();
                StartCreate();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fejl ved sletning: {ex.Message}";
            }
        }

        private void FindRenter()
        {
            if (!int.TryParse(RenterSearchText, out int value))
            {
                StatusMessage = "Indtast venligst tlf nummer eller lejerID (kun tal).";
                return;
            }

            try
            {
                var renter = _renterRepository.GetRenterByPhone(value)
                             ?? _renterRepository.GetRenterById(value);

                if (renter == null)
                {
                    SelectedRenter = null;
                    StatusMessage = "Ingen lejer fundet.";
                    return;
                }

                SelectedRenter = renter;
                StatusMessage = $"Valgt lejer: {renter.Name}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fejl ved lejer-søgning: {ex.Message}";
            }
        }

        private bool CanSave()
        {
            if (SelectedRenter == null) return false;

            if (!int.TryParse(ChairId, out int chairId) || chairId <= 0) return false;

            if (StartDate == null) return false;

            if (!decimal.TryParse(Price, out var price) || price <= 0) return false;

            // månedlig kræver slutdato og slut>=start
            if (SelectedRentalType == RentalType.Maanedlig)
            {
                if (EndDate == null) return false;
                if (EndDate.Value.Date < StartDate.Value.Date) return false;
            }

            return true;
        }

        private void Save()
        {
            // Valider igen (CanSave hjælper, men vi laver stadig robust Save)
            if (SelectedRenter == null)
            {
                StatusMessage = "Vælg en lejer først.";
                return;
            }

            if (!int.TryParse(ChairId, out int chairId) || chairId <= 0)
            {
                StatusMessage = "Ugyldigt Stol ID.";
                return;
            }

            if (!decimal.TryParse(Price, out decimal price) || price <= 0)
            {
                StatusMessage = "Ugyldig pris.";
                return;
            }

            if (StartDate == null)
            {
                StatusMessage = "Vælg en startdato.";
                return;
            }

            DateTime start = StartDate.Value.Date;

            DateTime end;
            if (SelectedRentalType == RentalType.Daglig)
            {
                end = start;
            }
            else
            {
                if (EndDate == null)
                {
                    StatusMessage = "Vælg en slutdato for månedlig leje.";
                    return;
                }

                end = EndDate.Value.Date;

                if (end < start)
                {
                    StatusMessage = "Slutdato må ikke være før startdato.";
                    return;
                }
            }

            // TotalPrice: daglig = price, månedlig = ceil(dage/30)*price (min 1 måned)
            decimal totalPrice;
            if (SelectedRentalType == RentalType.Daglig)
            {
                totalPrice = price;
            }
            else
            {
                var days = (end - start).TotalDays;
                var months = (int)Math.Ceiling(days / 30.0);
                if (months < 1) months = 1;
                totalPrice = months * price;
            }

            try
            {
                if (_isEditing)
                {
                    if (_editingRentalId == null)
                    {
                        StatusMessage = "Ingen lejeaftale er valgt til redigering.";
                        return;
                    }

                    var existing = _chairRepository.GetRentalDetails(_editingRentalId.Value);
                    if (existing == null)
                    {
                        StatusMessage = "Lejeaftalen findes ikke længere i databasen.";
                        return;
                    }

                    existing.ChairId = chairId;
                    existing.RenterId = SelectedRenter.RenterId;
                    existing.RentalType = SelectedRentalType;
                    existing.PaymentStatus = SelectedPaymentStatus;
                    existing.Price = price;
                    existing.TotalPrice = totalPrice;
                    existing.StartDate = start;
                    existing.EndDate = end;

                    bool ok = _chairRepository.UpdateRental(existing);
                    StatusMessage = ok ? "Lejeaftale opdateret." : "Kunne ikke opdatere lejeaftale.";
                }
                else
                {
                    var rental = new ChairRental
                    {
                        ChairId = chairId,
                        RenterId = SelectedRenter.RenterId,
                        RentalType = SelectedRentalType,
                        PaymentStatus = SelectedPaymentStatus,
                        Price = price,
                        TotalPrice = totalPrice,
                        StartDate = start,
                        EndDate = end,
                        CreatedDate = DateTime.Now
                    };

                    int newId = _chairRepository.InsertRental(rental);
                    StatusMessage = $"Lejeaftale oprettet (ID {newId}).";
                }

                Refresh();
                StartCreate();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fejl ved gem: {ex.Message}";
            }
        }

        private void UpdateCanExecute()
        {
            StartEditCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}


