using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.ViewModels
{
    public class ActivityLogViewModel
    {
        public List<ActivityLogRow> Rows { get; set; }
        public List<string> UserList { get; set; }
        public List<string> ActivityTypeList { get; set; }
        public string SelectedUser { get; set; }
        public string SelectedActivityType { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int TotalLogs { get; set; }
        public string Search { get; internal set; }
    }

    public class ActivityLogRow
    {
        public string Username { get; set; }
        public string ActivityType { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
    }
}