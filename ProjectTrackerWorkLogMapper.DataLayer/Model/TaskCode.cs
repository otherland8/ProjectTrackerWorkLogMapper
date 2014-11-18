using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectTrackerWorkLogMapper.DataLayer.Model
{
    public class TaskCode
    {
        [Key]
        public int TaskID { get; set; }

        public string JiraCode { get; set; }

    }
}
