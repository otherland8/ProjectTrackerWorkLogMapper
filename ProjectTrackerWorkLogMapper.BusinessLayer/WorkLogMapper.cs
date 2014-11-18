using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Data;
using System.Web;

namespace ProjectTrackerWorkLogMapper.BusinessLayer
{
    public class WorkLogMapper
    {
        private const int ISSUEID_INDEX = 5;
        private const int TITLE_INDEX = 7;
        private const int DATE_INDEX = 9;
        private const int TIMESPENT_INDEX = 13;
        private const int COMMENT_INDEX = 15;


        public List<WorkLog> GetWorkLogObjectsFromHTML(string workLogHTML, string filePath = "")
        {
            List<WorkLog> result = new List<WorkLog>();
            HtmlDocument document = new HtmlDocument();
            if (!String.IsNullOrEmpty(filePath))
            {
                document.Load(filePath);
            }
            else
            {
                document.LoadHtml(workLogHTML);
            }
            

            if (document.DocumentNode != null)
            {
                HtmlNode workLogTable = document.DocumentNode.SelectSingleNode("//table");
                if (workLogTable != null)
                {
                    IEnumerable<HtmlNode> workLogTableRows = workLogTable.Descendants("tr").Where(tr => tr.Attributes.Contains("class") && 
                        (tr.Attributes["class"].Value.Contains("rowNormal") || tr.Attributes["class"].Value.Contains("rowAlternate")));
                    if (workLogTableRows != null)
                    {
                        foreach (var row in workLogTableRows)
                        {
                            if (WorkLogExists(result, row))
                            {
                                continue;
                            }

                            result.Add (new WorkLog() 
                            { 
                                IssueID = row.ChildNodes[ISSUEID_INDEX].Descendants("a").FirstOrDefault().InnerHtml,
                                Title = HttpUtility.HtmlDecode(row.ChildNodes[TITLE_INDEX].InnerHtml),
                                Date = Convert.ToDateTime(row.ChildNodes[DATE_INDEX].InnerHtml),
                                TimeSpent = Double.Parse(row.ChildNodes[TIMESPENT_INDEX].InnerHtml),
                                Comment = HttpUtility.HtmlDecode(row.ChildNodes[COMMENT_INDEX].InnerHtml)
                            });
                        }
                    }
                }
            }


            return result;
        }


        private bool WorkLogExists(List<WorkLog> list, HtmlNode row)
        {
            foreach (WorkLog log in list)
            {
                if (String.Equals(log.IssueID, row.ChildNodes[ISSUEID_INDEX].Descendants("a").FirstOrDefault().InnerHtml) &&
                    String.Equals(log.Comment, row.ChildNodes[COMMENT_INDEX].InnerHtml) &&
                    log.Date == Convert.ToDateTime(row.ChildNodes[DATE_INDEX].InnerHtml) &&
                    String.Equals(log.Title, row.ChildNodes[TITLE_INDEX].InnerHtml))
                {
                    log.TimeSpent += Double.Parse(row.ChildNodes[TIMESPENT_INDEX].InnerHtml);
                    return true;
                }
            }
            return false;
        }
    }
}
