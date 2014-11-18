using ProjectTrackerWorkLogMapper.DataLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectTrackerWorkLogMapper.DataLayer.Repositories
{
    public interface ITaskCodeRepository
    {
        int GetTaskIDByJiraCode(string jiraCode);
    }
}
