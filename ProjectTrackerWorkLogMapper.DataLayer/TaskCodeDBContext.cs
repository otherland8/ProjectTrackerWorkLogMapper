using ProjectTrackerWorkLogMapper.DataLayer.Model;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectTrackerWorkLogMapper.DataLayer
{
    class TaskCodeDBContext : DbContext
    {
        public TaskCodeDBContext() : base("JiraMapDB") { }

        public DbSet<TaskCode> TaskCodes { get; set; }
    }
}
