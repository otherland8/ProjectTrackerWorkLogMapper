using Data.DataObjects;
using ICB.ObjectFactory;
using ICB.Shared;
using ICBCommon;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProjectTrackerWorkLogMapper.BusinessLayer
{
    public enum ActionStatus
    {
        Working = 2,
        Testing = 3,
        Done = 4,
        Checked = 5,
        Rejected = 6
    }


    public class ProjectTracker
    {
        private const int MINUTES_IN_AN_HOUR = 60;
        private const double WORK_DAY_START_HOUR = 9.0;
        private const int ERROR_VALUE = -1;


        private string CurrentUser { get; set; }
        private string TaskTitle { get; set; }
        private double TimeSpentInMinutes { get; set; }
        private string ActionText { get; set; }
        public int ProjectID { get; set; }
        public int TaskID { get; set; }

        public ProjectTracker(int taskID, int projectID)
        {
            ProjectID = projectID;
            TaskID = taskID;
        }

        public ProjectTracker(int projectID) : this(0, projectID) { }

        public ProjectTracker() : this(0, 0) { }

        public void SaveNewAction(WorkLog workLog)
        {
            TimeSpentInMinutes = workLog.TimeSpent * MINUTES_IN_AN_HOUR;
            CurrentUser = SystemInformation.UserName;
            ActionText = workLog.IssueID + (!String.IsNullOrEmpty(workLog.Title) ? String.Format(" - {0}", workLog.Title) : "");

            //Get token, containing UserID and credentials
            IToken currentToken = Factory.GetOperators().LogonAD(CurrentUser);
            if (currentToken == null)
            {
                throw new NullReferenceException(String.Format("No token for user with username: {0} was found.", CurrentUser));
            }

            //Get existing actions for period
            DataTable actionsForWorkLogPeriod = Factory.GetActionsView().GetDataForPeriod(currentToken, currentToken.UserID, workLog.Date, workLog.Date.AddDays(1));
            if (WorkLogExists(actionsForWorkLogPeriod, workLog))
            {
                throw new ArgumentException(String.Format("Action for task ID: {0} and already exists for {1}/{2}/{3}.", 
                    TaskID, workLog.Date.Month, workLog.Date.Day, workLog.Date.Year));
            }

            //Get current task for the given task ID
            DataSet currentTask = Factory.GetTask().Load(TaskID);
            if (currentTask == null)
            {
                throw new NullReferenceException(String.Format("No task with task ID: {0} was found.", TaskID));    
            }

            TaskTitle = currentTask.Tables["Tasks"].Rows[0]["Title"].ToString();

            //Set the minimal date to be used for the new action "to" date
            DateTime minimalFreeDate = GetMinimalFreeDate(actionsForWorkLogPeriod, workLog.Date);

            IAction Action = Factory.GetAction();

            DataSet newAction = Action.NewAction(currentToken, TaskID, ProjectID, minimalFreeDate, minimalFreeDate.AddHours(workLog.TimeSpent));

            AddActionData(newAction);
 
            AddExecutorData(newAction, currentToken);

            int result = Action.Save(currentToken, newAction);
            if (result == ERROR_VALUE)
            {
                throw new Exception(String.Format("Saving action with task ID: {0} for user with username: {1} failed.", TaskID, CurrentUser));
            }
        }

        private DateTime GetMinimalFreeDate(DataTable actions, DateTime workLogDate)
        {
            DateTime minimalFreeDate = workLogDate.AddHours(WORK_DAY_START_HOUR);
            if (actions.Rows.Count != 0)
            {   
                foreach (DataRow action in actions.Rows)
                {
                    string dateString = Converter.ToString(action["TimeTo"]);
                    DateTime actionToDate = Convert.ToDateTime(dateString);
                    if (minimalFreeDate <= actionToDate)
                    {
                        minimalFreeDate = actionToDate;
                    }
                }
            }
            return minimalFreeDate;
        }

        private bool WorkLogExists(DataTable actions, WorkLog workLog)
        {
            if (actions.Rows.Count != 0)
            {
                foreach (DataRow action in actions.Rows)
                {
                    if (String.Equals(action["Txt"].ToString(), ActionText))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void AddActionData(DataSet dsAction)
        {
            dsAction.Tables["Actions"].Rows[0]["Txt"] = ActionText;
            dsAction.Tables["Actions"].Rows[0]["StatusID"] = (int)ActionStatus.Working;
            dsAction.Tables["Actions"].Rows[0]["DurationMinutes"] = TimeSpentInMinutes;
            dsAction.Tables["Actions"].Rows[0]["Subject"] = TaskTitle;
            dsAction.Tables["Actions"].Rows[0]["ReportDuration"] = TimeSpentInMinutes;
            dsAction.Tables["Actions"].Rows[0]["Description"] = ActionText;
        }
        private void AddExecutorData(DataSet dsAction, IToken token)
        {
            DataRow drExecutorToAction = dsAction.Tables["ExecutorsToAction"].NewRow();

            drExecutorToAction["OperatorID"] = token.UserID;
            drExecutorToAction["DurationInMinutes"] = TimeSpentInMinutes;
            drExecutorToAction["ReportDuration"] = TimeSpentInMinutes;
            drExecutorToAction["FullName"] = token.FullName;

            dsAction.Tables["ExecutorsToAction"].Rows.Add(drExecutorToAction);
        }
    }
}
