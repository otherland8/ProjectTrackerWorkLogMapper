using Microsoft.Win32;
using ProjectTrackerWorkLogMapper.BusinessLayer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace ProjectTrackerWorkLogMapper.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<WorkLog> WorkLogsToMap { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            CultureInfo cultureInfo = new CultureInfo("en");
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            pbProgress.Visibility = Visibility.Hidden;
            if (String.IsNullOrEmpty(Settings.Default.Username) && String.IsNullOrEmpty(Settings.Default.Password))
            {
                pupCredentials.IsOpen = true;
                btnCancel.Visibility = Visibility.Hidden;
            }
        }

        private async void btnMapWorkLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                tbContent.Text = String.Empty;
                if (dtpEndDate.SelectedDate == null || dtpStartDate.SelectedDate == null)
                {
                    throw new ArgumentException("Please select valid start and end dates.");
                }
                if (dtpStartDate.SelectedDate.Value > dtpEndDate.SelectedDate.Value)
                {
                    throw new ArgumentException("The entered start date was bigger than the end date. Please enter correct dates.");
                }

                svContent.Visibility = Visibility.Visible;

                MapJiraReport(await GetJiraReport());

                SaveActions();
                pbProgress.Visibility = Visibility.Hidden;

            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                tbContent.Text = String.Empty;
                svContent.Visibility = Visibility.Hidden;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                tbContent.Text = String.Empty;
                svContent.Visibility = Visibility.Hidden;
            }
        }

        private async Task<string> GetJiraReport()
        {
            tbContent.Text += String.Format("Downloading jira report for user {0} from {1:MM/dd/yyyy} to {2:MM/dd/yyyy}...\n", Settings.Default.Username,
                    dtpStartDate.SelectedDate.Value, dtpEndDate.SelectedDate.Value);
            WorkLogDownloader downloader = new WorkLogDownloader(dtpStartDate.SelectedDate.Value, dtpEndDate.SelectedDate.Value,
                Settings.Default.Username, Settings.Default.Password, Settings.Default.LoginUrl, Settings.Default.Url);
            string outputHTML = await downloader.GetWorkLogHTML();
            tbContent.Text += "Done!\n";
            return outputHTML;
        }

        private void MapJiraReport(string reportHTML)
        {
            tbContent.Text += String.Format("Mapping jira report for user {0} from {1:MM/dd/yyyy} to {2:MM/dd/yyyy}...\n", Settings.Default.Username,
                    dtpStartDate.SelectedDate.Value, dtpEndDate.SelectedDate.Value);
            WorkLogsToMap = new WorkLogMapper().GetWorkLogObjectsFromHTML(reportHTML);
            tbContent.Text += "Done!\n";
        }

        private void SaveActions()
        {
            TaskCode taskCode = new TaskCode();
            ProjectTracker tracker = new ProjectTracker(Settings.Default.ProjectID);

            if (WorkLogsToMap != null && WorkLogsToMap.Count != 0)
            {
                pbProgress.Visibility = Visibility.Visible;
                pbProgress.Minimum = 0;
                pbProgress.Maximum = WorkLogsToMap.Count;
                pbProgress.Value = 0;
                foreach (WorkLog log in WorkLogsToMap)
                {
                    try
                    {
                        pbProgress.Value++;
                        tbContent.Text += String.Format("Fetching jira task ID for task with issue ID: {0}...\n", log.IssueID);
                        int currentJiraTaskID = taskCode.GetTaskIDByJiraCode(log.IssueID);
                        if (currentJiraTaskID == default(int))
                        {
                            throw new ArgumentException(String.Format("No task ID for worklog with issue ID: {0} was found.", log.IssueID));
                        }
                        tracker.TaskID = currentJiraTaskID;
                        tbContent.Text += String.Format("Saving action for jira task with issue ID: {0}...\n", log.IssueID);
                        tracker.SaveNewAction(log);
                        tbContent.Text += "Done!\n";
                    }
                    catch (ArgumentException ex)
                    {
                        MessageBox.Show(ex.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        tbContent.Text = String.Empty;
                        svContent.Visibility = Visibility.Hidden;
                    }
                }
            }
            else
            {
                MessageBox.Show(String.Format("No actions were logged for user {0} from {1:MM/dd/yyyy} to {2:MM/dd/yyyy}...", Settings.Default.Username,
                dtpStartDate.SelectedDate.Value, dtpEndDate.SelectedDate.Value), "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                tbContent.Text = String.Empty;
                svContent.Visibility = Visibility.Hidden;
            }
        }

        public void CancelCredentialsPopUp(object sender, EventArgs e)
        {
            pupCredentials.IsOpen = !pupCredentials.IsOpen;
            txtJiraUsername.Focus();
            txtJiraUsername.Text = String.Empty;
            pboxJiraPassword.Password = String.Empty;
        }

        public void SaveCredentials(object sender, EventArgs e)
        {
            btnCancel.Visibility = Visibility.Visible;
            if (String.IsNullOrEmpty(txtJiraUsername.Text) || String.IsNullOrEmpty(pboxJiraPassword.Password))
            {
                MessageBox.Show("Username and/or password cannot be empty", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                Settings.Default.Username = txtJiraUsername.Text;
                Settings.Default.Password = pboxJiraPassword.Password;
                Settings.Default.Save();
                txtJiraUsername.Text = String.Empty;
                pboxJiraPassword.Password = String.Empty;
                pupCredentials.IsOpen = !pupCredentials.IsOpen;
            }
        }

        public void ImportFromFile(object sender, EventArgs e)
        {
            OpenFileDialog importFromFileDialog = new OpenFileDialog();
            importFromFileDialog.DefaultExt = "*.jspa";
            importFromFileDialog.Filter = "Java servlet page alias (*.jspa)|*.jspa|Microsoft Excel files (*.xls)|*.xls|All files (*.*)|*.*";
            Nullable<bool> result = importFromFileDialog.ShowDialog();
            if (result == true)
            {
                try
                {
                    svContent.Visibility = Visibility.Visible;
                    string filePath = importFromFileDialog.FileName;
                    WorkLogsToMap = new WorkLogMapper().GetWorkLogObjectsFromHTML("", filePath);
                    if (WorkLogsToMap.Count > 0)
                    {
                        GetImportedFileDates();
                        SaveActions();
                        pbProgress.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        throw new ArgumentException(String.Format("No worklogs were found in file: {0}", filePath));
                    }
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show(ex.Message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    tbContent.Text = String.Empty;
                    svContent.Visibility = Visibility.Hidden;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format("An error has occured. Error message: {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    tbContent.Text = String.Empty;
                    svContent.Visibility = Visibility.Hidden;
                }
            }
        }

        private void GetImportedFileDates()
        {
            List<DateTime> result = new List<DateTime>();
            WorkLogsToMap.ForEach(wl => result.Add(wl.Date));
            result.Sort();
            dtpStartDate.SelectedDate = result.FirstOrDefault();
            dtpEndDate.SelectedDate = result.LastOrDefault();
        }
    }
}
