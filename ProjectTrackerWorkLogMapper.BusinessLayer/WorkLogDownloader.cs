using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Configuration;
using System.Collections.Specialized;

namespace ProjectTrackerWorkLogMapper.BusinessLayer
{
    public enum Months
    {
        Jan = 1,
        Feb = 2,
        Mar = 3,
        Apr = 4,
        May = 5,
        Jun = 6,
        Jul = 7,
        Aug = 8,
        Sep = 9,
        Oct = 10,
        Nov = 11,
        Dec = 12
    }

    public class WorkLogDownloader
    {
        private const string CONTENT_TYPE = "application/x-www-form-urlencoded";
        private const string HTTP_POST_BODY_LOGIN_TEMPLATE = "os_username={0}&os_password={1}";
        private const string HTTP_HEADER_COOKIE = "Set-Cookie";

        public WorkLogDownloader(DateTime? from, DateTime? to, string username, string password, string loginUrl, string jiraReportUrl)
        {
            FromDate = from;
            ToDate = to;
            Username = username;
            Password = password;
            LoginUrl = loginUrl;
            JIRAReportUrl = jiraReportUrl;
        }

        // Input
        private DateTime? FromDate { get; set; }
        private DateTime? ToDate { get; set; }

        // Output
        private string Url { get; set; }
        private string SessionID { get; set; }
        private string OutputHTML { get; set; }
        private string LoginUrl { get; set; }
        private string Username { get; set; }
        private string Password { get; set; }
        private string JIRAReportUrl { get; set; }


        public async Task<string> GetWorkLogHTML()
        {
            InitUrl();

            await Login();
            await DownloadWorkLogHTML();

            return OutputHTML; 
        }

        private async Task DownloadWorkLogHTML()
        {
            WebClient client = new WebClient();
            client.Headers.Add(HttpRequestHeader.Cookie, SessionID);

            OutputHTML = await client.DownloadStringTaskAsync(Url);
        }

        private async Task Login()
        {
            var loginRequest = (HttpWebRequest)HttpWebRequest.Create(LoginUrl);
            loginRequest.ContentType = CONTENT_TYPE;
            loginRequest.Method = "POST";

            string formData = string.Format(HTTP_POST_BODY_LOGIN_TEMPLATE, Username, Password);
            byte[] formDataEncoded = Encoding.ASCII.GetBytes(formData);

            using (var requestStream = await loginRequest.GetRequestStreamAsync())
            {
                await requestStream.WriteAsync(formDataEncoded, 0, formDataEncoded.Length);
            }

            var response = await loginRequest.GetResponseAsync();

            SessionID = response.Headers[HTTP_HEADER_COOKIE];
            if (string.IsNullOrWhiteSpace(SessionID))
            {
                throw new UnauthorizedAccessException("Could not login. Check user and pass in settings !");
            }
        }

        private void InitUrl()
        {
            string fromMonthShort = ((Months)FromDate.Value.Month).ToString();
            string toMonthShort = ((Months)ToDate.Value.Month).ToString();
            string fromYearShort = FromDate.Value.Year.ToString().Remove(0, 2);
            string toYearShort = ToDate.Value.Year.ToString().Remove(0, 2);

            Url = String.Format("{0}ConfigureReport!excelView.jspa?endDate={1}/{2}/{3}&showUsers=true&startDate={4}/{5}/{6}&weekends=true&reportKey=jira-timesheet-plugin%3Areport",
                JIRAReportUrl, ToDate.Value.Day, toMonthShort, toYearShort, FromDate.Value.Day, fromMonthShort, fromYearShort);
        }

    }
}
