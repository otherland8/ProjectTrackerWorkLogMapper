using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using ProjectTrackerWorkLogMapper.BusinessLayer;
using System.Globalization;
using System.Threading;

namespace ProjectTrackerWorkLogMapper
{
    class Program
    {
        static void Main(string[] args)
        {
            CultureInfo cultureInfo = new CultureInfo("en");
            Thread.CurrentThread.CurrentCulture = cultureInfo;

            const string QUIT_COMMAND_CHARACTER = "q";
            const string CHANGE_CREDENTIALS_CHARACTER = "c";
            const string IMPORT_FROM_FILE_CHARACTER = "i";

            string fromDateString = String.Empty;
            string toDateString = String.Empty;
            bool quit = false;
            string workLogHTMLString = String.Empty;
 

            Console.WriteLine("Project Tracker Worklog Mapper");
            while (!quit)
            {
                try
                {

                    if (String.IsNullOrEmpty(Settings.Default.Username) && String.IsNullOrEmpty(Settings.Default.Password))
                    {
                        Console.WriteLine("Jira credentials not entered. Please enter your credentials below: ");
                        ChangeCredentials();
                        Console.WriteLine("------------------------------");
                    }

                    List<WorkLog> list = new List<WorkLog>();
                    Console.WriteLine("------------------------------");

                    Console.WriteLine("Enter start date (mm/dd/yyyy), \"" + CHANGE_CREDENTIALS_CHARACTER + 
                        "\" to change jira credentials, \"" + IMPORT_FROM_FILE_CHARACTER + 
                        "\" or \"" + QUIT_COMMAND_CHARACTER + "\" to quit: ");
                    fromDateString = Console.ReadLine();
                    if (String.Equals(fromDateString, CHANGE_CREDENTIALS_CHARACTER))
                    {
                        ChangeCredentials();
                        continue;
                    }
                    if (String.Equals(fromDateString, IMPORT_FROM_FILE_CHARACTER))
                    {
                        string filePath = String.Empty;
                        Console.WriteLine("Enter file path: ");
                        filePath = Console.ReadLine();
                        string fileName = filePath.Split('\\').LastOrDefault();
                        Console.WriteLine(String.Format("Mapping jira report from file \"{0}\"...", fileName));
                        list = new WorkLogMapper().GetWorkLogObjectsFromHTML(workLogHTMLString, filePath);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Done!");
                        Console.ResetColor();
                        Console.WriteLine("------------------------------");
                        continue;
                    }
                    if (String.Equals(fromDateString, QUIT_COMMAND_CHARACTER))
                    {
                        quit = true;
                        continue;
                    }
                    DateTime fromDate = Convert.ToDateTime(fromDateString);

                    Console.WriteLine("------------------------------");
                    Console.WriteLine("Enter end date (mm/dd/yyyy), \"" + CHANGE_CREDENTIALS_CHARACTER +
                        "\" to change jira credentials, \"" + IMPORT_FROM_FILE_CHARACTER +
                        "\" to import from file or \"" + QUIT_COMMAND_CHARACTER + "\" to quit: ");

                    toDateString = Console.ReadLine();
                    if (String.Equals(toDateString, CHANGE_CREDENTIALS_CHARACTER))
                    {
                        ChangeCredentials();
                        continue;
                    }
                    if (String.Equals(toDateString, IMPORT_FROM_FILE_CHARACTER))
                    {
                        string filePath = String.Empty;
                        Console.WriteLine("Enter file path: ");
                        filePath = Console.ReadLine();
                        string fileName = filePath.Split('\\').LastOrDefault();
                        Console.WriteLine(String.Format("Mapping jira report from file {0}...", fileName));
                        list = new WorkLogMapper().GetWorkLogObjectsFromHTML(workLogHTMLString, filePath);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Done!");
                        Console.ResetColor();
                        Console.WriteLine("------------------------------");
                        continue;
                    }
                    if (String.Equals(toDateString, QUIT_COMMAND_CHARACTER))
                    {
                        quit = true;
                        continue;
                    }
                    DateTime toDate = Convert.ToDateTime(toDateString);
                    if (fromDate > toDate)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("The entered start date was bigger than the end date. Please enter correct dates.");
                        Console.ResetColor();
                        continue;
                    }

                    Console.WriteLine("------------------------------");
                    Console.WriteLine(String.Format("Downloading jira report for user {0} from {1} to {2}...", Settings.Default.Username, fromDateString, toDateString));

                    WorkLogDownloader downloader = new WorkLogDownloader(fromDate, toDate, Settings.Default.Username, Settings.Default.Password, Settings.Default.LoginUrl, Settings.Default.Url);
                    workLogHTMLString = downloader.GetWorkLogHTML().Result;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Done!");
                    Console.ResetColor();
                    Console.WriteLine("------------------------------");

                    Console.WriteLine(String.Format("Mapping jira report for user {0} from {1} to {2}...", Settings.Default.Username, fromDateString, toDateString));
                    list = new WorkLogMapper().GetWorkLogObjectsFromHTML(workLogHTMLString);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Done!");
                    Console.ResetColor();
                    Console.WriteLine("------------------------------");

                    SaveWorkLogs(list);
                    
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(String.Format("An error has occurred. Error message: {0}", e.Message));
                    Console.ResetColor();
                    continue;
                }
            }
        }

        private static void ChangeCredentials()
        {
            Console.Write("Enter username: ");
            Settings.Default.Username = Console.ReadLine();
            Console.Write("Enter password: ");
            Settings.Default.Password = GetNewPassword();
            if (String.IsNullOrEmpty(Settings.Default.Username) || String.IsNullOrEmpty(Settings.Default.Password))
            {
                throw new ArgumentException("Username and/or password cannot be empty.");
            }
            Settings.Default.Save();
        }

        private static string GetNewPassword()
        {
            string newPassword = String.Empty;
            ConsoleKeyInfo key = Console.ReadKey(true);
            while(key.Key != ConsoleKey.Enter)
            {
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    newPassword += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && newPassword.Length > 0)
                    {
                        newPassword = newPassword.Substring(0, (newPassword.Length - 1));
                        Console.Write("\b \b");
                    }
                }
                key = Console.ReadKey(true);
            }
            Console.WriteLine();
            return newPassword;
        }

        private static void SaveWorkLogs(List<WorkLog> list)
        {
            TaskCode taskCode = new TaskCode();
            ProjectTracker tracker = new ProjectTracker(Settings.Default.ProjectID);

            if (list != null && list.Count > 0)
            {
                foreach (WorkLog log in list)
                {
                    try
                    {
                        Console.WriteLine(String.Format("Fetching jira task ID for task with issue ID: {0}...", log.IssueID));
                        int currentJiraTaskID = taskCode.GetTaskIDByJiraCode(log.IssueID);
                        if (currentJiraTaskID == default(int))
                        {
                            throw new ArgumentException(String.Format("No task ID for worklog with issue ID: {0} was found.", log.IssueID));
                        }
                        tracker.TaskID = currentJiraTaskID;
                        Console.WriteLine(String.Format("Saving action for jira task with issue ID: {0}...", log.IssueID));
                        tracker.SaveNewAction(log);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Done!");
                        Console.ResetColor();
                        Console.WriteLine("------------------------------");

                    }
                    catch (ArgumentException e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(e.Message);
                        Console.ResetColor();
                        Console.WriteLine("------------------------------");
                        continue;
                    }
                }
            }
            else
            {
                throw new ArgumentException(String.Format("No actions were found for user {0} for the specified date interval...", Settings.Default.Username));
            }
        }
    }
}
