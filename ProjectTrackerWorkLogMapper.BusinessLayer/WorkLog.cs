using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectTrackerWorkLogMapper.BusinessLayer
{
    public class WorkLog
    {
        public string IssueID { get; set; }

        public DateTime Date { get; set; }

        public double TimeSpent { get; set; }

        public string Comment { get; set; }

        public string Title { get; set; }
    }
}
