using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ExpenditureManager.ObjectClasses
{
    /// <summary>
    /// Collection of entries per day.
    /// </summary>
    public class DayEntry : INotifyPropertyChanged
    {
        private ObservableCollection<Entry> _entryList;
        /// <summary>
        /// Collection of entries today.
        /// </summary>
        [JsonPropertyName("entrylist")]
        public ObservableCollection<Entry> EntryList
        {
            get { return _entryList; }
            set { _entryList = value; }
        }
        private DateTime _date;
        /// <summary>
        /// The date these entries occurred. Hours, minutes, seconds will always be zero.
        /// </summary>
        [JsonPropertyName("date")]
        public DateTime Date 
        { 
            get { return _date; } 
            set
            {
                _date = value;
                OnPropertyChanged("Date");
            }
        }
        private decimal _totalAmount;
        /// <summary>
        /// Total amount of the spending occurred today.
        /// </summary>
        [JsonPropertyName("totalamount")]
        public decimal TotalAmount 
        { 
            get { return _totalAmount; }
            set 
            {
                _totalAmount = value;
            }
        }

        /// <summary>
        /// Create a new DayEntry object. EntryList will be initialised automatically.
        /// </summary>
        public DayEntry() 
        {
            if (_entryList == null)
            {
                _entryList = new ObservableCollection<Entry>();
            }
            _entryList.CollectionChanged += EntryList_CollectionChanged;
        }

        public void CalculateTotalAmount()
        {
            if (TotalAmount > decimal.Zero) { TotalAmount = decimal.Zero; }

            foreach (var entry in EntryList)
            {
                TotalAmount += entry.Amount;
            }

            OnPropertyChanged("TotalAmount");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private void EntryList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("EntryList");
        }
    }
}
