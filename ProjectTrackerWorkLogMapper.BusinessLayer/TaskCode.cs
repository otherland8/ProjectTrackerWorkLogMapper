using ProjectTrackerWorkLogMapper.DataLayer.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectTrackerWorkLogMapper.BusinessLayer
{
    public class TaskCode
    {
        public int GetTaskIDByJiraCode(string jiraCode)
        {
            return new TaskCodeRepository().GetTaskIDByJiraCode(jiraCode);
        }
    }
}
