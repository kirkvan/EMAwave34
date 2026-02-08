//
// Copyright (C) 2024, NinjaTrader LLC <www.ninjatrader.com>.
//
#region Using declarations
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
    /// <summary>
    /// Defines the severity level of a log message.
    /// </summary>
    public enum LogLevel
    {
    	Trace,
    	Debug,
    	Info,
    	Warn,
    	Error
    }

    /// <summary>
    /// Controls how timestamps are emitted in log output.
    /// </summary>
    public enum LogTimestampMode
    {
        WallClock,
        BarTime,
        Dual
    }

    /// <summary>
    /// Represents an immutable log entry with timestamp, level, and message content.
    /// </summary>
    internal sealed class LogEntry
    {
        public readonly long Sequence;
        public readonly DateTime WallClockTimestamp;
        public readonly DateTime? BarTimestamp;
        public readonly LogTimestampMode TimestampMode;
        public readonly LogLevel Level;
        public readonly string Message;
        public readonly NinjaScriptBase StrategyInstance;

        public LogEntry(long sequence, DateTime wallClockTimestamp, DateTime? barTimestamp, LogTimestampMode timestampMode, LogLevel level, string message, NinjaScriptBase strategyInstance)
        {
            Sequence = sequence;
            WallClockTimestamp = wallClockTimestamp;
            BarTimestamp = barTimestamp;
            TimestampMode = timestampMode;
            Level = level;
            Message = message;
            StrategyInstance = strategyInstance;
        }
    }

/// <summary>
    /// Provides a centralized and standardized logging facility for the Leapfrog strategy.
    /// Thread-safe, chronological logging for real-time trading operations with conditional logging support.
    /// </summary>
    public static class ServiceLogger
    {
        private static readonly ConcurrentQueue<LogEntry> _logQueue = new ConcurrentQueue<LogEntry>();
        private static readonly ManualResetEventSlim _newEntryEvent = new ManualResetEventSlim(false);
        private static readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static readonly Task _writerTask;
        private static volatile bool _isDisposed = false;
        private static long _sequenceCounter = 0;

        /// <summary>
        /// Static constructor to initialize the writer task.
        /// </summary>
        static ServiceLogger()
        {
            _writerTask = Task.Run(() => ProcessLogEntriesAsync(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// Processes log entries from the queue in chronological order on a dedicated background thread.
        /// </summary>
        private static async Task ProcessLogEntriesAsync(CancellationToken cancellationToken)
        {
            var batch = new List<LogEntry>(256);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Wait for new entries or cancellation
                    if (!_newEntryEvent.Wait(100, cancellationToken))
                        continue;

                    // Reset the event before processing
                    _newEntryEvent.Reset();

                    // Drain all available entries into a batch
                    batch.Clear();
                    while (_logQueue.TryDequeue(out LogEntry entry))
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;
                        batch.Add(entry);
                    }

                    if (batch.Count == 0)
                        continue;

                    // Total-order the batch by sequence. This guarantees stable log ordering even
                    // under multi-threaded enqueue interleaving.
                    batch.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));

                    foreach (var entry in batch)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        try
                        {
                            WriteLogEntry(entry);
                        }
                        catch (Exception ex)
                        {
                            // Fallback logging to prevent infinite recursion
                            Console.WriteLine($"[ERROR] ServiceLogger writer task failed: {ex.Message}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                // Fallback logging
                Console.WriteLine($"[CRITICAL] ServiceLogger writer task crashed: {ex.Message}");
            }
        }

        /// <summary>
        /// Writes a single log entry to the NinjaScript Output window.
        /// Called from the dedicated writer thread to ensure chronological order.
        /// </summary>
        private static void WriteLogEntry(LogEntry entry)
        {
            if (entry.StrategyInstance == null || entry.StrategyInstance.State == State.Finalized)
                return;

            string timestamp = FormatTimestamp(entry);
            string level = entry.Level.ToString().ToUpperInvariant();
            string formattedMessage = $"[{timestamp}] [{level}] [SEQ={entry.Sequence:D10}] {entry.Message}";
            entry.StrategyInstance.Print(formattedMessage);
        }

        /// <summary>
        /// Checks if a log message at the specified level should be logged based on strategy configuration.
        /// </summary>
        private static bool ShouldLog(NinjaScriptBase strategyInstance, LogLevel level)
        {
            if (strategyInstance == null || strategyInstance.State == State.Finalized)
                return false;

            // Check if strategy implements logging configuration
            if (strategyInstance is ILoggingConfig loggingConfig)
            {
                return level >= loggingConfig.MinimumLogLevel;
            }

            // Safe default if not configured: only warn/error
            return level >= LogLevel.Warn;
        }

        /// <summary>
        /// Lazy evaluation helper for Debug level messages with instrument context. Only builds string if logging is enabled.
        /// </summary>
        public static void Debug(Func<string> messageFactory, NinjaScriptBase strategyInstance, string instrument)
        {
            if (ShouldLog(strategyInstance, LogLevel.Debug))
                Print(LogLevel.Debug, $"[{instrument}] {messageFactory()}", strategyInstance);
        }

        /// <summary>
        /// Lazy evaluation helper for Debug level messages. Only builds string if logging is enabled.
        /// </summary>
        public static void Debug(Func<string> messageFactory, NinjaScriptBase strategyInstance)
        {
            if (ShouldLog(strategyInstance, LogLevel.Debug))
                Print(LogLevel.Debug, messageFactory(), strategyInstance);
        }

        /// <summary>
        /// Lazy evaluation helper for Debug level messages with instrument context and explicit timestamp.
        /// </summary>
        public static void Debug(Func<string> messageFactory, NinjaScriptBase strategyInstance, string instrument, DateTime eventTime)
        {
            if (ShouldLog(strategyInstance, LogLevel.Debug))
                Print(LogLevel.Debug, $"[{instrument}] {messageFactory()}", strategyInstance, eventTime);
        }

        /// <summary>
        /// Lazy evaluation helper for Debug level messages with explicit timestamp.
        /// </summary>
        public static void Debug(Func<string> messageFactory, NinjaScriptBase strategyInstance, DateTime eventTime)
        {
            if (ShouldLog(strategyInstance, LogLevel.Debug))
                Print(LogLevel.Debug, messageFactory(), strategyInstance, eventTime);
        }

        /// <summary>
        /// Lazy evaluation helper for Info level messages with instrument context. Only builds string if logging is enabled.
        /// </summary>
        public static void Info(Func<string> messageFactory, NinjaScriptBase strategyInstance, string instrument)
        {
            if (ShouldLog(strategyInstance, LogLevel.Info))
                Print(LogLevel.Info, $"[{instrument}] {messageFactory()}", strategyInstance);
        }

        /// <summary>
        /// Lazy evaluation helper for Info level messages. Only builds string if logging is enabled.
        /// </summary>
        public static void Info(Func<string> messageFactory, NinjaScriptBase strategyInstance)
        {
            if (ShouldLog(strategyInstance, LogLevel.Info))
                Print(LogLevel.Info, messageFactory(), strategyInstance);
        }

        /// <summary>
        /// Lazy evaluation helper for Info level messages with instrument context and explicit timestamp.
        /// </summary>
        public static void Info(Func<string> messageFactory, NinjaScriptBase strategyInstance, string instrument, DateTime eventTime)
        {
            if (ShouldLog(strategyInstance, LogLevel.Info))
                Print(LogLevel.Info, $"[{instrument}] {messageFactory()}", strategyInstance, eventTime);
        }

        /// <summary>
        /// Lazy evaluation helper for Info level messages with explicit timestamp.
        /// </summary>
        public static void Info(Func<string> messageFactory, NinjaScriptBase strategyInstance, DateTime eventTime)
        {
            if (ShouldLog(strategyInstance, LogLevel.Info))
                Print(LogLevel.Info, messageFactory(), strategyInstance, eventTime);
        }

        /// <summary>
        /// Lazy evaluation helper for Warn level messages with instrument context. Only builds string if logging is enabled.
        /// </summary>
        public static void Warn(Func<string> messageFactory, NinjaScriptBase strategyInstance, string instrument)
        {
            if (ShouldLog(strategyInstance, LogLevel.Warn))
                Print(LogLevel.Warn, $"[{instrument}] {messageFactory()}", strategyInstance);
        }

        /// <summary>
        /// Lazy evaluation helper for Warn level messages. Only builds string if logging is enabled.
        /// </summary>
        public static void Warn(Func<string> messageFactory, NinjaScriptBase strategyInstance)
        {
            if (ShouldLog(strategyInstance, LogLevel.Warn))
                Print(LogLevel.Warn, messageFactory(), strategyInstance);
        }

        /// <summary>
        /// Lazy evaluation helper for Warn level messages with instrument context and explicit timestamp.
        /// </summary>
        public static void Warn(Func<string> messageFactory, NinjaScriptBase strategyInstance, string instrument, DateTime eventTime)
        {
            if (ShouldLog(strategyInstance, LogLevel.Warn))
                Print(LogLevel.Warn, $"[{instrument}] {messageFactory()}", strategyInstance, eventTime);
        }

        /// <summary>
        /// Lazy evaluation helper for Warn level messages with explicit timestamp.
        /// </summary>
        public static void Warn(Func<string> messageFactory, NinjaScriptBase strategyInstance, DateTime eventTime)
        {
            if (ShouldLog(strategyInstance, LogLevel.Warn))
                Print(LogLevel.Warn, messageFactory(), strategyInstance, eventTime);
        }

        /// <summary>
        /// Lazy evaluation helper for Error level messages with instrument context. Only builds string if logging is enabled.
        /// </summary>
        public static void Error(Func<string> messageFactory, NinjaScriptBase strategyInstance, string instrument)
        {
            if (ShouldLog(strategyInstance, LogLevel.Error))
                Print(LogLevel.Error, $"[{instrument}] {messageFactory()}", strategyInstance);
        }

        /// <summary>
        /// Lazy evaluation helper for Error level messages. Only builds string if logging is enabled.
        /// </summary>
        public static void Error(Func<string> messageFactory, NinjaScriptBase strategyInstance)
        {
            if (ShouldLog(strategyInstance, LogLevel.Error))
                Print(LogLevel.Error, messageFactory(), strategyInstance);
        }

        /// <summary>
        /// Lazy evaluation helper for Error level messages with instrument context and explicit timestamp.
        /// </summary>
        public static void Error(Func<string> messageFactory, NinjaScriptBase strategyInstance, string instrument, DateTime eventTime)
        {
            if (ShouldLog(strategyInstance, LogLevel.Error))
                Print(LogLevel.Error, $"[{instrument}] {messageFactory()}", strategyInstance, eventTime);
        }

        /// <summary>
        /// Lazy evaluation helper for Error level messages with explicit timestamp.
        /// </summary>
        public static void Error(Func<string> messageFactory, NinjaScriptBase strategyInstance, DateTime eventTime)
        {
            if (ShouldLog(strategyInstance, LogLevel.Error))
                Print(LogLevel.Error, messageFactory(), strategyInstance, eventTime);
        }

        /// <summary>
        /// Enqueues a log entry using an explicit timestamp provided by the event.
        /// </summary>
        public static void Print(LogLevel level, string message, NinjaScriptBase strategyInstance, DateTime eventTime)
        {
            if (!ShouldLog(strategyInstance, level) || _isDisposed)
                return;

            try
            {
                long seq = Interlocked.Increment(ref _sequenceCounter);
                var entry = CreateLogEntry(seq, level, message, strategyInstance, eventTime);
                _logQueue.Enqueue(entry);
                _newEntryEvent.Set();
            }
            catch (Exception ex)
            {
                try
                {
                    strategyInstance?.Print($"[ERROR] ServiceLogger.Print (eventTime) failed: {ex.Message}");
                }
                catch
                {
                    Console.WriteLine($"[CRITICAL] ServiceLogger completely failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Prints a formatted log message to the NinjaScript Output window.
        /// </summary>
        /// <param name="level">The severity level of the message (Debug, Info, Warn, Error).</param>
        /// <param name="message">The log message content.</param>
        /// <param name="strategyInstance">The NinjaScript instance from which the log is being called (usually 'this').</param>
        public static void Print(LogLevel level, string message, NinjaScriptBase strategyInstance)
        {
            // Check logging level BEFORE any processing
            if (!ShouldLog(strategyInstance, level) || _isDisposed)
                return;
            
            try
            {
                long seq = Interlocked.Increment(ref _sequenceCounter);
                
                // Create immutable log entry with captured timestamp + monotonic sequence.
                var entry = CreateLogEntry(seq, level, message, strategyInstance, null);
                
                // Enqueue for chronological processing by writer thread
                _logQueue.Enqueue(entry);
                
                // Signal the writer thread that new entries are available
                _newEntryEvent.Set();
            }
            catch (Exception ex)
            {
                // Fallback logging to prevent strategy crashes
                try
                {
                    strategyInstance?.Print($"[ERROR] ServiceLogger.Print failed: {ex.Message}");
                }
                catch
                {
                    // Last resort - prevent any exceptions from escaping
                    Console.WriteLine($"[CRITICAL] ServiceLogger completely failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Resolves the appropriate timestamp mode for the current execution context.
        /// </summary>
        private static LogTimestampMode ResolveTimestampMode(NinjaScriptBase strategyInstance)
        {
            try
            {
                if (strategyInstance is ILoggingConfig loggingConfig)
                {
                    // Historical/backtest always use bar time for deterministic sequencing
                    if (loggingConfig.IsBacktestContext)
                        return LogTimestampMode.BarTime;

                    // Debug mode defaults to dual timestamps
                    if (loggingConfig.EnableDebugLogging)
                        return LogTimestampMode.Dual;
                }
            }
            catch { /* best-effort */ }

            // Fallback: if we can detect Historical, prefer bar time
            if (strategyInstance is NinjaTrader.NinjaScript.Strategies.Strategy strategy && strategy.State == State.Historical)
                return LogTimestampMode.BarTime;

            // Default for live trading
            return LogTimestampMode.WallClock;
        }

        private static LogEntry CreateLogEntry(long sequence, LogLevel level, string message, NinjaScriptBase strategyInstance, DateTime? eventTime)
        {
            LogTimestampMode mode = ResolveTimestampMode(strategyInstance);
            DateTime wallClock = DateTime.Now;
            DateTime? barTime = null;

            if (mode == LogTimestampMode.BarTime || mode == LogTimestampMode.Dual)
            {
                barTime = eventTime ?? TryGetBarTimestamp(strategyInstance);
            }

            return new LogEntry(sequence, wallClock, barTime, mode, level, message, strategyInstance);
        }

        private static DateTime? TryGetBarTimestamp(NinjaScriptBase strategyInstance)
        {
            if (strategyInstance is NinjaTrader.NinjaScript.Strategies.Strategy strategy)
                return TryGetStrategyTime0(strategy);
            return null;
        }

        private static string FormatTimestamp(LogEntry entry)
        {
            const string format = "yyyy-MM-dd HH:mm:ss.fff";
            switch (entry.TimestampMode)
            {
                case LogTimestampMode.WallClock:
                    return entry.WallClockTimestamp.ToString(format);
                case LogTimestampMode.BarTime:
                    return entry.BarTimestamp.HasValue
                        ? entry.BarTimestamp.Value.ToString(format)
                        : entry.WallClockTimestamp.ToString(format);
                case LogTimestampMode.Dual:
                    string wall = entry.WallClockTimestamp.ToString(format);
                    string bar = entry.BarTimestamp.HasValue ? entry.BarTimestamp.Value.ToString(format) : "n/a";
                    return $"W={wall} | B={bar}";
                default:
                    return entry.WallClockTimestamp.ToString(format);
            }
        }

        private static DateTime? TryGetStrategyTime0(NinjaTrader.NinjaScript.Strategies.Strategy strategy)
        {
            try
            {
                if (strategy?.Time != null && strategy.Time.Count > 0)
                    return strategy.Time[0];
            }
            catch { /* best-effort */ }
            return null;
        }
        
        /// <summary>
        /// Cleanup method for proper resource management.
        /// </summary>
        public static void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            try
            {
                // Signal cancellation to writer task
                _cancellationTokenSource.Cancel();

                // Process any remaining entries before shutdown
                while (_logQueue.TryDequeue(out LogEntry entry))
                {
                    try
                    {
                        WriteLogEntry(entry);
                    }
                    catch
                    {
                        // Ignore errors during shutdown
                    }
                }

                // Wait for writer task to complete (with timeout)
                if (_writerTask != null && !_writerTask.IsCompleted)
                {
                    _writerTask.Wait(TimeSpan.FromSeconds(2));
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
            finally
            {
                // Dispose resources
                try
                {
                    _newEntryEvent?.Dispose();
                    _cancellationTokenSource?.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }
        }
    }
}
