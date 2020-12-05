using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ExpenditureManager.ObjectClasses
{
    /// <summary>
    /// Transaction entry.
    /// </summary>
    public class Entry : INotifyPropertyChanged
    {
        private string _name;
        /// <summary>
        /// Identifiable name of this entry.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name 
        { 
            get { return _name; } 
            set
            {
                _name = value;
                OnPropertyChanged("Name");
            }
        }
        private string _category;
        /// <summary>
        /// Identifiable group in which this transaction fits into.
        /// </summary>
        [JsonPropertyName("category")]
        public string Category 
        { 
            get { return _category; }
            set
            {
                _category = value;
                OnPropertyChanged("Category");
            } 
        }
        private string _recipient;
        /// <summary>
        /// The receiver of the transaction.
        /// </summary>
        [JsonPropertyName("recipient")]
        public string Recipient
        {
            get { return _recipient; }
            set
            {
                _recipient = value;
                OnPropertyChanged("Recipient");
            }
        }
        private decimal _amount;
        /// <summary>
        /// Amount of value transferred or payed.
        /// </summary>
        [JsonPropertyName("amount")]
        public decimal Amount
        {
            get { return _amount; }
            set
            {
                _amount = value;
                OnPropertyChanged("Amount");
            }
        }
        private string _comment;
        /// <summary>
        /// Additional message on an entry.
        /// </summary>
        [JsonPropertyName("comment")]
        public string Comment
        {
            get { return _comment; }
            set
            {
                _comment = value;
                OnPropertyChanged("Comment");
            }
        }

        /// <summary>
        /// Create a new Entry object.
        /// </summary>
        public Entry() { }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
