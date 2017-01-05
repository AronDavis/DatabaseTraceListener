using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace DTL
{
    /// <summary>
    /// Used for logging messages to a database
    /// </summary>
    public class DatabaseTraceListener : TraceListener
    {
        private string _connectionString;
        private int _flushAfter;
        private ConcurrentQueue<LogEntry> _logEntryQueue;

        public DatabaseTraceListener(string connectionString, int flushAfter)
        {
            initialize(connectionString, flushAfter);
        }

        public DatabaseTraceListener(string listenerName, string connectionString, int flushAfter) : base(listenerName)
        {
            initialize(connectionString, flushAfter);
        }

        private void initialize(string connectionString, int flushAfter)
        {
            _connectionString = connectionString;
            _flushAfter = flushAfter;
            _logEntryQueue = new ConcurrentQueue<LogEntry>();
        }

        private void writeToDatabase()
        {
            if (_logEntryQueue.Count == 0)
                return;

            string query = @"INSERT INTO AppLog(dateTimeCreated, category, contents, stackTrace, threadId, processName, processId)
                             VALUES (@dateTimeCreated, @category, @contents, @stackTrace, @threadId, @processName, @processId)";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Add("@dateTimeCreated", SqlDbType.DateTime);
                        command.Parameters.Add("@category", SqlDbType.VarChar);
                        command.Parameters.Add("@contents", SqlDbType.VarChar);
                        command.Parameters.Add("@stackTrace", SqlDbType.VarChar);
                        command.Parameters.Add("@threadId", SqlDbType.VarChar);
                        command.Parameters.Add("@processName", SqlDbType.VarChar);
                        command.Parameters.Add("@processId", SqlDbType.Int);

                        LogEntry logEntry;
                        while (_logEntryQueue.Count > 0)
                        {
                            _logEntryQueue.TryDequeue(out logEntry);
                            command.Parameters["@dateTimeCreated"].Value = logEntry.DateTimeCreated;
                            command.Parameters["@category"].Value = logEntry.Category;
                            command.Parameters["@contents"].Value = logEntry.Contents;
                            command.Parameters["@stackTrace"].Value = logEntry.StackTrace;
                            command.Parameters["@threadId"].Value = logEntry.ThreadId;
                            command.Parameters["@processName"].Value = logEntry.ProcessName;
                            command.Parameters["@processId"].Value = logEntry.ProcessId;

                            try
                            {
                                command.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void addToQueue(LogEntry logEntry)
        {
            _logEntryQueue.Enqueue(logEntry);

            if (_logEntryQueue.Count >= _flushAfter)
                writeToDatabase();
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {
            StackTrace trace = new StackTrace(3, true);
            addToQueue(new LogEntry(
                dateTimeCreated: eventCache.DateTime.ToString("o"),
                category: eventType.ToString(),
                contents: data.ToString(),
                stackTrace: trace.ToString(),
                threadId: eventCache.ThreadId,
                processName: Process.GetProcessById(eventCache.ProcessId).ProcessName,
                processId: eventCache.ProcessId
                ));
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            StackTrace trace = new StackTrace(3, true);
            addToQueue(new LogEntry(
                dateTimeCreated: eventCache.DateTime.ToString("o"),
                category: eventType.ToString(),
                contents: message,
                stackTrace: trace.ToString(),
                threadId: eventCache.ThreadId,
                processName: Process.GetProcessById(eventCache.ProcessId).ProcessName,
                processId: eventCache.ProcessId
                ));
        }

        public override void Write(string message)
        {
            StackTrace trace = new StackTrace(1, true);
            Process process = Process.GetCurrentProcess();
            addToQueue(new LogEntry(
                dateTimeCreated: DateTime.Now.ToString("o"),
                category: "",
                contents: message,
                stackTrace: trace.ToString(),
                threadId: Thread.CurrentThread.ManagedThreadId.ToString(),
                processName: process.ProcessName,
                processId: process.Id
                ));
        }

        public override void Write(object o)
        {
            Write(o.ToString());
        }

        public override void Write(string message, string category)
        {
            StackTrace trace = new StackTrace(1, true);
            Process process = Process.GetCurrentProcess();
            addToQueue(new LogEntry(
                dateTimeCreated: DateTime.Now.ToString("o"),
                category: category,
                contents: message,
                stackTrace: trace.ToString(),
                threadId: Thread.CurrentThread.ManagedThreadId.ToString(),
                processName: process.ProcessName,
                processId: process.Id
                ));
        }

        public override void Write(object o, string category)
        {
            Write(o.ToString(), category);
        }

        public override void WriteLine(string message)
        {
            Write(message);
        }

        public override void WriteLine(object o)
        {
            Write(o);
        }

        public override void WriteLine(string message, string category)
        {
            Write(message, category);
        }

        public override void WriteLine(object o, string category)
        {
            Write(o, category);
        }

        public override void Close()
        {
            writeToDatabase();
        }

        public override void Flush()
        {
            writeToDatabase();
        }
    }
}
