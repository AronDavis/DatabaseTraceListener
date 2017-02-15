using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTL
{
    public class LogEntry
    {
        public int Id { get; private set; }
        public DateTime DateTimeCreated;
        public string Category;
        public string Contents;
        public string StackTrace;
        public string ThreadId;
        public string ProcessName;
        public int ProcessId;
        public int? EventId;
        public string Source;
        public string MachineName;

        public LogEntry(DateTime dateTimeCreated, string category, string contents, string stackTrace, string threadId, string processName, int processId, int? eventId, string source, string machineName)
        {
            DateTimeCreated = dateTimeCreated;
            Category = category;
            Contents = contents;
            StackTrace = stackTrace;
            ThreadId = threadId;
            ProcessName = processName;
            ProcessId = processId;
            EventId = eventId;
            Source = source;
            MachineName = machineName;
        }

        public LogEntry(IDataReader dr)
        {
            Id = (int)dr["id"];
            DateTimeCreated = (DateTime)dr["DateTimeCreated"];

            if (dr["Category"] != DBNull.Value)
                Category = (string)dr["Category"];

            if (dr["Contents"] != DBNull.Value)
                Contents = (string)dr["Contents"];

            if (dr["StackTrace"] != DBNull.Value)
                StackTrace = (string)dr["StackTrace"];

            ThreadId = (string)dr["ThreadId"];
            ProcessName = (string)dr["ProcessName"];
            ProcessId = (int)dr["ProcessId"];

            if (dr["EventId"] != DBNull.Value)
                EventId = (int)dr["EventId"];

            if (dr["Source"] != DBNull.Value)
                Source = (string)dr["Source"];

            MachineName = (string)dr["MachineName"];
        }
    }
}
