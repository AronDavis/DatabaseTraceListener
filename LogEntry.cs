using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTL
{
    public class LogEntry
    {
        public string DateTimeCreated;
        public string Category;
        public string Contents;
        public string StackTrace;
        public string ThreadId;
        public string ProcessName;
        public int ProcessId;

        public LogEntry(string dateTimeCreated, string category, string contents, string stackTrace, string threadId, string processName, int processId)
        {
            DateTimeCreated = dateTimeCreated;
            Category = category;
            Contents = contents;
            StackTrace = stackTrace;
            ThreadId = threadId;
            ProcessName = processName;
            ProcessId = processId;
        }
    }
}
