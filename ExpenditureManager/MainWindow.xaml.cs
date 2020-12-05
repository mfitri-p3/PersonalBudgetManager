﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExpenditureManager.ObjectClasses;

namespace ExpenditureManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Constants

        private const string _dayNameString = "dddd"; //Name of the day, used for DayOfWeek.ToString().
        private const string _toTwoDigit = "N2"; //Numeric - 2 points, used for ToString() on decimal-based values.

        #endregion

        #region Objects and Variables

        private bool isInitialStartup = true; //Is the program starting up?

        private List<DayEntry> DayEntryList; //The entire list to hold on the values.
        private DayEntry SelectedDayEntry; //To bind with EntriesDataGrid
        private DateTime SelectedDateTime; 
        private int SelectedWeekNum;

        private string SavedLocation; //The storage file's full path.
        private bool isSaved; //Used for prompting user if the file is not updated yet.

        #endregion

        public MainWindow()
        {
            if (DayEntryList == null)
            {
                DayEntryList = new List<DayEntry>();
            }
            isSaved = true;

            InitializeComponent();

            DateSelectionCalendar.SelectedDate = DateTime.Now;
            DateSelectionCalendar.Focus();
            int thatYear = DateTime.Now.Year;
            //Display the calendar range for the current year only.
            DateSelectionCalendar.DisplayDateStart = new DateTime(thatYear, 1, 1);
            DateSelectionCalendar.DisplayDateEnd = new DateTime(thatYear, 12, 31);

            OpenJsonFile(); //Load up data.
            isInitialStartup = false; //Set it to false once done initialising.
        }

        #region Setup

        

        #endregion

        #region File Input and Output

        /// <summary>
        /// Returns false value if it encounters errors when creating a new directory and file. 
        /// </summary>
        /// <returns></returns>
        private bool CheckLocationExist()
        {
            try
            {
                if (string.IsNullOrEmpty(SavedLocation))
                {
                    string theFileName = string.Format("{0}_{1}.json", "MyExpenses", SelectedDateTime.Year);

                    SavedLocation = string.Format("{0}/{1}/{2}",
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "ExpenditureManager",
                        theFileName);
                }

                if (!File.Exists(SavedLocation))
                {
                    if (!Directory.Exists(SavedLocation))
                    {
                        Directory.CreateDirectory(string.Format("{0}/{1}",
                            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                            "ExpenditureManager"));
                    }

                    var objBuffer = File.Create(SavedLocation);
                    //Close FileStream after creating the file,
                    //Else, OpenJsonFile and SaveToJsonFile will return error due to "existing process using the file".
                    objBuffer.Close();
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        /// <summary>
        /// Opens a JSON file, deserialises it, and refresh the changes.
        /// </summary>
        private void OpenJsonFile()
        {
            try
            {
                if (CheckLocationExist())
                {
                    string buffer = "";

                    StreamReader sr = new StreamReader(SavedLocation);
                    buffer = sr.ReadToEnd();
                    sr.Close();

                    //Deserialise it only when there is JSON content from the file.
                    if (!string.IsNullOrWhiteSpace(buffer))
                    {
                        DayEntryList = JsonSerializer.Deserialize<List<DayEntry>>(buffer);
                    }

                    RefreshChange();

                    if (isInitialStartup == false)
                    {
                        MessageBox.Show("Record open successfully!", "Open success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "OpenJsonFile error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveToJsonFile()
        {
            try
            {
                CheckLocationExist();

                string buffer = JsonSerializer.Serialize(DayEntryList);

                StreamWriter sw = new StreamWriter(SavedLocation);
                sw.Write(buffer);
                sw.Close();

                isSaved = true;

                MessageBox.Show("Record saved successfully!", "Save success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "SaveToJsonFile error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Calendar and Calculation

        /// <summary>
        /// Set the SelectedDateTime variable based on DateSelectionCalendar.SelectedDate.
        /// </summary>
        private void GetGlobalDate()
        {
            SelectedDateTime = (DateTime)DateSelectionCalendar.SelectedDate;
        }
        /// <summary>
        /// Get the selected week number and assign it to the SelectedWeekNum variable.
        /// </summary>
        private void GetWeekNum()
        {
            System.Globalization.CultureInfo culInfo = System.Globalization.CultureInfo.CurrentCulture;
            SelectedWeekNum = culInfo.Calendar.GetWeekOfYear(
                SelectedDateTime,
                System.Globalization.CalendarWeekRule.FirstFullWeek,
                DayOfWeek.Sunday);
        }
        /// <summary>
        /// Update the DayWeekTextBlock element.
        /// </summary>
        private void UpdateDayWeekNum()
        {
            DayWeekTextBlock.Text = string.Format("{0}, Week Number {1}",
                SelectedDateTime.ToString(_dayNameString),
                SelectedWeekNum);
        }
        /// <summary>
        /// Calculate the total amount spent per month, per week, and per day.
        /// </summary>
        private void GetEveryAmount()
        {
            List<DayEntry> dayEntryBufferMonth = DayEntryList.FindAll(x => x.Date.Month == SelectedDateTime.Month).ToList();
            if (dayEntryBufferMonth.Count > 0)
            {
                decimal totalAmMonth = decimal.Zero;
                foreach (var dayEntry in dayEntryBufferMonth)
                {
                    totalAmMonth += dayEntry.TotalAmount;
                }
                TotalAmountMonthBlock.Text = string.Format("RM{0}", totalAmMonth.ToString(_toTwoDigit));
            }
            else
            {
                TotalAmountMonthBlock.Text = string.Format("RM{0}", "0.00");
            }

            if (SelectedDayEntry != null)
            {
                SelectedDayEntry.CalculateTotalAmount();
                TotalAmountDayBlock.Text = string.Format("RM{0}", SelectedDayEntry.TotalAmount.ToString(_toTwoDigit));
            }
            else
            {
                TotalAmountDayBlock.Text = string.Format("RM{0}", "0.00");
            }

            int dayOfWeek = (int)SelectedDateTime.DayOfWeek;
            var startDate = SelectedDateTime.AddDays(-dayOfWeek); //From Sunday
            var endDate = SelectedDateTime.AddDays(6 - dayOfWeek); //To Saturday
            decimal totalAmWeek = decimal.Zero;
            DayEntry buffer = new DayEntry();
            do
            {
                buffer = DayEntryList.Find(x => x.Date == startDate.Date);
                if (buffer != null)
                {
                    totalAmWeek += buffer.TotalAmount;
                }
                startDate = startDate.AddDays(1);
            } while (startDate <= endDate);
            TotalAmountWeekBlock.Text = string.Format("RM{0}", totalAmWeek.ToString(_toTwoDigit));
        }
        /// <summary>
        /// Refresh everything for any change.
        /// </summary>
        private void RefreshChange()
        {
            GetGlobalDate();
            GetWeekNum();
            UpdateDayWeekNum();

            if (isInitialStartup == false)
            {
                SelectedDayEntry = DayEntryList.Find(x => x.Date == SelectedDateTime.Date);
                if (SelectedDayEntry == null)
                {
                    SelectedDayEntry = new DayEntry();
                    SelectedDayEntry.Date = SelectedDateTime;
                }

                GetEveryAmount();

                //Because SelectedDayEntry will be kept changing multiple times,
                //This is the only solution so far to keep updating the EntriesDataGrid.
                //EntriesDataGrid.ItemsSource = null;
                //EntriesDataGrid.ItemsSource = SelectedDayEntry.EntryList;
                EntriesDataGrid.ItemsSource = SelectedDayEntry.EntryList;
            }
        }

        #endregion

        #region DataGrid Events

        private void DeleteRowMenu_Click(object sender, RoutedEventArgs e)
        {
            List<Entry> bufferList = EntriesDataGrid.SelectedItems.Cast<Entry>().ToList();
            foreach (Entry item in bufferList)
            {
                SelectedDayEntry.EntryList.Remove(item);
            }
            
            SelectedDayEntry.CalculateTotalAmount();
            RefreshChange();
        }

        private void EntriesDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                isSaved = false;

                if (!DayEntryList.Exists(x => x.Date == SelectedDayEntry.Date))
                {
                    DayEntryList.Add(SelectedDayEntry);
                }
            }
        }

        #endregion

        #region Calendar Events

        private void DateSelectionCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshChange();
        }

        #endregion

        #region Menu Events

        private void OpenRecordMenu_Click(object sender, RoutedEventArgs e)
        {
            OpenJsonFile();
        }

        private void SaveRecordMenu_Click(object sender, RoutedEventArgs e)
        {
            SaveToJsonFile();
        }

        private void ExitMenu_Click(object sender, RoutedEventArgs e)
        {
            if (isSaved == false)
            {
                var result = MessageBox.Show("You have not saved your changes into the file yet. Do you want to save them now?",
                    "Modification update to the file",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning,
                    MessageBoxResult.Yes);

                if (result == MessageBoxResult.Yes)
                {
                    SaveToJsonFile();
                    Application.Current.Shutdown(0);
                }
                else if (result == MessageBoxResult.No)
                {
                    Application.Current.Shutdown(0);
                }
            }
            else
            {
                Application.Current.Shutdown(0);
            }
        }


        #endregion

        
    }
}
