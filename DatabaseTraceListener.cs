﻿using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;

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
        public TraceSource TraceSource { get; set; }

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
            TraceSource = new TraceSource("DatabaseTraceListener", SourceLevels.Error);
        }

        private void writeToDatabase()
        {
            if (_logEntryQueue.Count == 0)
                return;

            string query = @"INSERT INTO LogEntry(dateTimeCreated, category, contents, stackTrace, threadId, processName, processId, eventId, source, machineName)
                             VALUES (@dateTimeCreated, @category, @contents, @stackTrace, @threadId, @processName, @processId, @eventId, @source, @machineName)";

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
                        command.Parameters.Add("@eventId", SqlDbType.Int);
                        command.Parameters.Add("@source", SqlDbType.VarChar);
                        command.Parameters.Add("@machineName", SqlDbType.VarChar);


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
                            command.Parameters["@eventId"].Value = logEntry.EventId;
                            command.Parameters["@source"].Value = logEntry.Source;
                            command.Parameters["@machineName"].Value = logEntry.MachineName;

                            try
                            {
                                command.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                TraceSource.TraceEvent(TraceEventType.Error, 0, ex.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TraceSource.TraceEvent(TraceEventType.Error, 0, ex.ToString());
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
                dateTimeCreated: eventCache.DateTime,
                category: eventType.ToString(),
                contents: data.ToString(),
                stackTrace: trace.ToString(),
                threadId: eventCache.ThreadId,
                processName: Process.GetProcessById(eventCache.ProcessId).ProcessName,
                processId: eventCache.ProcessId,
                eventId: id,
                source: source,
                machineName: Environment.MachineName
                ));
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            StackTrace trace = new StackTrace(3, true);
            addToQueue(new LogEntry(
                dateTimeCreated: eventCache.DateTime,
                category: eventType.ToString(),
                contents: message,
                stackTrace: trace.ToString(),
                threadId: eventCache.ThreadId,
                processName: Process.GetProcessById(eventCache.ProcessId).ProcessName,
                processId: eventCache.ProcessId,
                eventId: id,
                source: source,
                machineName: Environment.MachineName
                ));
        }

        public override void Write(string message)
        {
            StackTrace trace = new StackTrace(1, true);
            Process process = Process.GetCurrentProcess();
            addToQueue(new LogEntry(
                dateTimeCreated: DateTime.Now,
                category: "",
                contents: message,
                stackTrace: trace.ToString(),
                threadId: Thread.CurrentThread.ManagedThreadId.ToString(),
                processName: process.ProcessName,
                processId: process.Id,
                eventId: null,
                source: null,
                machineName: Environment.MachineName
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
                dateTimeCreated: DateTime.Now,
                category: category,
                contents: message,
                stackTrace: trace.ToString(),
                threadId: Thread.CurrentThread.ManagedThreadId.ToString(),
                processName: process.ProcessName,
                processId: process.Id,
                eventId: null,
                source: null,
                machineName: Environment.MachineName
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
