using SalonT.Repositories;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WPFSalonThorsson.Models;

namespace WPFSalonThorsson.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly IChairRepository _chairRepository;
        private readonly IRenterRepository _renterRepository;
        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<ChairRental> UpcomingRentals { get; } = new();
        public ObservableCollection<ChairRental> CompletedRentals { get; } = new();

        private ChairRental? _selectedRental;
        public ChairRental? SelectedRental
        {
            get => _selectedRental;
            set { _selectedRental = value; OnPropertyChanged(); UpdateCanExecute(); }
        }

        private string? _statusMessage;
        public string? StatusMessage {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private string? _searchRentalId;
        public string? SearchRentalId
        {
            get => _searchRentalId;
            set { _searchRentalId = value; OnPropertyChanged(); }
        }

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
            set { _chairId = value; OnPropertyChanged(); }
        }

        private Renter? _selectedRenter;
        public Renter? SelectedRenter
        {
            get => _selectedRenter;
            set { _selectedRenter = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedRenterDisplay)); }
        }

        public string SelectedRenterDisplay =>
        SelectedRenter == null
        ? "(ingen valgt)"
        : $"ID {SelectedRenter.RenterId} - {SelectedRenter.Name} (Tlf: {SelectedRenter.PhoneNumber})";

        private RentalType _selectedRentalType = RentalType.Daglig;
        public RentalType SelectedRentalType
        {
            get => _selectedRentalType;
            set { _selectedRentalType = value; OnPropertyChanged(); }
        }

        public ObservableCollection<RentalType> RentalTypes { get; } = new ObservableCollection<RentalType>(Enum.GetValues(typeof(RentalType)).Cast<RentalType>());

        private WPFSalonThorsson.Models.PaymentStatus _selectedPaymentStatus
        = WPFSalonThorsson.Models.PaymentStatus.Ubetalt;

        public WPFSalonThorsson.Models.PaymentStatus SelectedPaymentStatus
        {
            get => _selectedPaymentStatus;
            set { _selectedPaymentStatus = value; OnPropertyChanged(); }
        }

        public ObservableCollection<PaymentStatus> PaymentStatus { get; } = new ObservableCollection<PaymentStatus>(Enum.GetValues(typeof(PaymentStatus)).Cast<PaymentStatus>());

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

        public RelayCommand RefreshCommand { get; }
        public RelayCommand SearchRentalCommand { get; }
        public RelayCommand StartCreateCommand { get; }
        public RelayCommand StartEditCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand FindRentalCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        private bool _isEditing;
        private int? _editingRentalId;


        public MainWindowViewModel(IChairRepository chairRepository, IRenterRepository renterRepository)
        {
            _chairRepository = chairRepository;
            _renterRepository = renterRepository;

            RefreshCommand = new RelayCommand(Refresh);
            SearchRentalCommand = new RelayCommand(SearchRental);
            StartCreateCommand = new RelayCommand(StartCreate);
            StartEditCommand = new RelayCommand(StartEdit);
            DeleteCommand = new RelayCommand(DeleteSelected);
            FindRentalCommand = new RelayCommand(FindRenter);
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
                {
                    UpcomingRentals.Add(r);
                }
                foreach (var r in _chairRepository.GetCompletedRentals(DateTime.Now))
                {
                    CompletedRentals.Add(r);
                }

                StatusMessage = "Lejeaftaler opdateret.";
            }

            catch (Exception ex)
            {
                StatusMessage = $"Fejl ved opdatering af lejeaftaler: {ex.Message}";
            }
        }

        private void SearchRental()
        {
            if (!int.TryParse(SearchRentalId, out int Id))
            {
                StatusMessage = "Ugyldigt ID.";
                return;
            }

            try
            {
                var rental = _chairRepository.GetRentalDetails(Id);
                if (rental == null)
                {
                    StatusMessage = $"Lejeaftale med ID {Id} ikke fundet.";
                    return;
                }

                LoadRentalToForm(rental);
                _isEditing = true;
                _editingRentalId = rental.RentalId;
                FormTitle = $"Rediger Lejeaftale (ID {_editingRentalId})";
                StatusMessage = $"Lejeaftale med ID {Id} indlæst til redigering.";
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
            SelectedPaymentStatus = WPFSalonThorsson.Models.PaymentStatus.Ubetalt;
            StartDate = null;
            EndDate = null;
            Price = null;
            StatusMessage = "Udfyld formularen for at oprette en ny lejeaftale.";
            UpdateCanExecute();
        }

        private void StartEdit()
        {
            if (SelectedRental == null) return;

            try
            {
                var full = _chairRepository.GetRentalDetails(SelectedRental.RentalId);
                if (full == null)
                {
                    StatusMessage = "Ingen detaljer fundet på lejer.";
                    return;
                }

                LoadRentalToForm(full);
                _isEditing = true;
                _editingRentalId = null;
                FormTitle = $"Rediger Lejeaftale {full.RentalId})";
                StatusMessage = $"Lejeaftale fundet til redigering.";
                UpdateCanExecute();
            }

            catch (Exception ex)
            {

                StatusMessage = "Fejl ved redigering.";
            }
        }

        private void LoadRentalToForm(ChairRental r)
        {
            ChairId = r.ChairId.ToString();
            SelectedRentalType = r.RentalType;
            SelectedPaymentStatus = r.PaymentStatus;
            StartDate = r.StartDate;
            EndDate = r.EndDate;
            Price = r.Price.ToString();

            try
            {
                SelectedRenter = _renterRepository.GetRenterById(r.RenterId);
            }

            catch (Exception ex)
            {
                SelectedRenter = null;
            }
        }

        private void DeleteSelected()
        {
            if (SelectedRental == null) return;

            try
            {
                bool Ok = _chairRepository.DeleteRental(SelectedRental.RentalId);
                StatusMessage = Ok ? "Lejeaftale er slettet." : "Kunne ikke slette lejeaftale.";
                Refresh();
            }

            catch (Exception ex)
            {
                StatusMessage = "Fejl ved sletning.";
            }

        }

        private void FindRenter()
        {
            if (!int.TryParse(RenterSearchText, out int value))
            {
                StatusMessage = "Indtast venligst tlf nummer eller lejerID";
                return;
            }

            try
            {
                var renter = _renterRepository.GetRenterByPhone(value) ?? _renterRepository.GetRenterById(value);
                if (renter == null)
                {
                    SelectedRenter = null;
                    StatusMessage = "Ingen Lejer fundet.";
                    return;
                }

                SelectedRenter = renter;
                StatusMessage = $"Valgt lejer: {renter.Name}";
            }

            catch (Exception ex)
            {
                StatusMessage = "Fejl ved søgning.";
            }
        }

        private bool CanSave()
        {
            if (SelectedRenter == null) return false;
            if (!int.TryParse(ChairId, out int chairId) || chairId <= 0) return false;
            if (StartDate == null) return false;
            if (!decimal.TryParse(Price, out var price) || price <= 0) return false;
            return true;
        }

        private void Save()
        {
            StatusMessage = _isEditing
            ? "Gem ændringer demo." : "Opret lejeaftale demo.";
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
