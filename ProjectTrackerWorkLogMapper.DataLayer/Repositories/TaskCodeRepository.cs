using ProjectTrackerWorkLogMapper.DataLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectTrackerWorkLogMapper.DataLayer.Repositories
{
    public class TaskCodeRepository : ITaskCodeRepository
    {
        public int GetTaskIDByJiraCode(string jiraCode)
        {
            using (var context = new TaskCodeDBContext())
            {
                return context.TaskCodes.Where(taskCode => jiraCode.Contains(taskCode.JiraCode)).Select(taskCode => taskCode.TaskID).FirstOrDefault();
            }
        }
    }
}
