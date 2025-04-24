using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Text.Json;
using System.ComponentModel;
using ExpenditureManager.ObjectClasses;
using Microsoft.Win32;

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

        /// <summary>
        /// Is the program starting up?
        /// </summary>
        private bool isInitialStartup = true;

        /// <summary>
        /// The entire list to hold on the values.
        /// </summary>
        private List<DayEntry> DayEntryList;
        /// <summary>
        /// To bind with EntriesDataGrid.
        /// </summary>
        private DayEntry SelectedDayEntry;
        /// <summary>
        /// Selected datetime when viewing the budget breakdown.
        /// </summary>
        private DateTime SelectedDateTime;
        /// <summary>
        /// Selected week number when viewing the budget breakdown.
        /// </summary>
        private int SelectedWeekNum;

        /// <summary>
        /// The storage file's full path.
        /// </summary>
        private string SavedLocation;
        /// <summary>
        /// Used for prompting user if the file is not updated yet.
        /// </summary>
        private bool isSaved;

        #endregion

        public MainWindow()
        {
            if (DayEntryList == null)
            {
                DayEntryList = new List<DayEntry>();
            }
            isSaved = true;

            InitializeComponent();
            Closing += OnClosing;

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

        #region MainWindow Events

        /// <summary>
        /// Prompt to ask user to save progress before closing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="cea"></param>
        private void OnClosing(object sender, CancelEventArgs cea)
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
                    cea.Cancel = false;
                }
                else if (result == MessageBoxResult.No)
                {
                    cea.Cancel = false;
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    cea.Cancel = true;
                }
            }
            else
            {
                cea.Cancel = false;
            }
        }

        #endregion

        #region File Input and Output

        /// <summary>
        /// Check location path of the data. If the folder or file not exists, it will auto create the relevant folder and file. 
        /// </summary>
        /// <param name="doCreateNewFile">Create the relevant folder and file if such item does not exists.</param>
        /// <returns>Returns false value if it encounters errors when creating a new directory and file or does not exist at the moment.</returns>
        private bool CheckLocationExist(bool doCreateNewFile)
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

                if (!File.Exists(SavedLocation) && doCreateNewFile)
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

                    return true;
                }
                else
                {
                    return false;
                }
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
                if (CheckLocationExist(false))
                {
                    string buffer = "";

                    using (StreamReader sr = new StreamReader(SavedLocation))
                    {
                        buffer = sr.ReadToEnd();
                        sr.Close();
                    }

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
        /// <summary>
        /// Serialise the list to a JSON object, and then save it to a file.
        /// </summary>
        private void SaveToJsonFile()
        {
            try
            {
                CheckLocationExist(true);

                string buffer = JsonSerializer.Serialize(DayEntryList);

                using (StreamWriter sw = new StreamWriter(SavedLocation))
                {
                    sw.Write(buffer);
                    sw.Close();
                }

                isSaved = true;

                MessageBox.Show("Record saved successfully!", "Save success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "SaveToJsonFile error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Data Export

        /// <summary>
        /// Escape commas on string input for the CSV export.
        /// </summary>
        /// <param name="value">String input.</param>
        /// <returns>String values with trimmed whitespaces and escaped commas.</returns>
        private static string EscapeCommas(string value)
        {
            if (value == null)
            {
                return "";
            }
            else
            {
                return value.Trim().Replace("\"", "\"\"");
            }
        }

        /// <summary>
        /// Export the DayEntryList data to CSV form.
        /// </summary>
        private bool ExportEntriesToCsv()
        {
            bool status = false;
            if (DayEntryList == null)
            {
                DayEntryList = new List<DayEntry>();
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = $"My Budget Report as of {DateTime.Now:yyyy-MM-dd}";
            sfd.DefaultExt = ".csv";
            sfd.Filter = "CSV Files (*.csv)|*.csv";
            sfd.AddExtension = true;
            bool? dialogResult = sfd.ShowDialog();
            if (dialogResult == true)
            {
                string filePath = sfd.FileName;
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    using (StreamWriter writer = new StreamWriter(filePath))
                    {
                        writer.WriteLine("Date,Category,Name,Recipient,Amount,Comment");

                        foreach (DayEntry dayEntry in DayEntryList)
                        {
                            foreach (Entry entry in dayEntry.EntryList)
                            {
                                string line = $"{dayEntry.Date:dd-MM-yyyy},{EscapeCommas(entry.Category)},{EscapeCommas(entry.Name)},{EscapeCommas(entry.Recipient)},RM{entry.Amount:F2},{EscapeCommas(entry.Comment)}";
                                writer.WriteLine(line);
                            }
                        }
                    }
                }
                status = true;
            }

            return status;
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

        private void ExportCsvMenu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool exportStatus = ExportEntriesToCsv();
                if (exportStatus)
                {
                    MessageBox.Show("Export as CSV successful.", "Export status", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unable to export as CSV", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        
    }
}
