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
    /// Interface for strategy-level logging configuration.
    /// Allows EMAwave34ServiceLogger to read per-strategy logging policy without direct coupling.
    /// </summary>
    public interface IEMAwave34LoggingConfig
    {
        bool EnableDebugLogging { get; }
        EMAwave34LogLevel MinimumLogLevel { get; }
        bool IsBacktestContext { get; }
    }

    /// <summary>
    /// Defines the severity level of a log message.
    /// </summary>
    public enum EMAwave34LogLevel
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
    public enum EMAwave34LogTimestampMode
    {
        WallClock,
        BarTime,
        Dual
    }

    /// <summary>
    /// Represents an immutable log entry with timestamp, level, and message content.
    /// </summary>
    internal sealed class EMAwave34LogEntry
    {
        public readonly long Sequence;
        public readonly DateTime WallClockTimestamp;
        public readonly DateTime? BarTimestamp;
        public readonly EMAwave34LogTimestampMode TimestampMode;
        public readonly EMAwave34LogLevel Level;
        public readonly string Message;
        public readonly NinjaScriptBase StrategyInstance;

        public EMAwave34LogEntry(long sequence, DateTime wallClockTimestamp, DateTime? barTimestamp, EMAwave34LogTimestampMode timestampMode, EMAwave34LogLevel level, string message, NinjaScriptBase strategyInstance)
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
    /// Provides a centralized and standardized logging facility for the EMAwave34 strategy.
    /// Thread-safe, chronological logging for real-time trading operations with conditional logging support.
    /// </summary>
    public static class EMAwave34ServiceLogger
    {
        private static readonly ConcurrentQueue<EMAwave34LogEntry> _logQueue = new ConcurrentQueue<EMAwave34LogEntry>();
        private static readonly ManualResetEventSlim _newEntryEvent = new ManualResetEventSlim(false);
        private static readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static readonly Task _writerTask;
        private static volatile bool _isDisposed = false;
        private static long _sequenceCounter = 0;

        static EMAwave34ServiceLogger()
        {
            _writerTask = Task.Run(() => ProcessLogEntriesAsync(_cancellationTokenSource.Token));
        }

        private static async Task ProcessLogEntriesAsync(CancellationToken cancellationToken)
        {
            var batch = new List<EMAwave34LogEntry>(256);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!_newEntryEvent.Wait(100, cancellationToken))
                        continue;

                    _newEntryEvent.Reset();

                    batch.Clear();
                    while (_logQueue.TryDequeue(out EMAwave34LogEntry entry))
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;
                        batch.Add(entry);
                    }

                    if (batch.Count == 0)
                        continue;

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
                            Console.WriteLine($"[ERROR] EMAwave34ServiceLogger writer task failed: {ex.Message}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL] EMAwave34ServiceLogger writer task crashed: {ex.Message}");
            }
        }

        private static void WriteLogEntry(EMAwave34LogEntry entry)
        {
            if (entry.StrategyInstance == null || entry.StrategyInstance.State == State.Finalized)
                return;

            string timestamp = FormatTimestamp(entry);
            string level = entry.Level.ToString().ToUpperInvariant();
            string formattedMessage = $"[{timestamp}] [{level}] [SEQ={entry.Sequence:D10}] {entry.Message}";
            entry.StrategyInstance.Print(formattedMessage);
        }

        private static bool ShouldLog(NinjaScriptBase strategyInstance, EMAwave34LogLevel level)
        {
            if (strategyInstance == null || strategyInstance.State == State.Finalized)
                return false;

            if (strategyInstance is IEMAwave34LoggingConfig loggingConfig)
            {
                return level >= loggingConfig.MinimumLogLevel;
            }

            return level >= EMAwave34LogLevel.Warn;
        }

        public static void Debug(Func<string> messageFactory, NinjaScriptBase strategyInstance, string instrument)
        {
            if (ShouldLog(strategyInstance, EMAwave34LogLevel.Debug))
                Print(EMAwave34LogLevel.Debug, $"[{instrument}] {messageFactory()}", strategyInstance);
        }

        public static void Debug(Func<string> messageFactory, NinjaScriptBase strategyInstance)
        {
            if (ShouldLog(strategyInstance, EMAwave34LogLevel.Debug))
                Print(EMAwave34LogLevel.Debug, messageFactory(), strategyInstance);
        }

        public static void Debug(Func<string> messageFactory, NinjaScriptBase strategyInstance, string instrument, DateTime eventTime)
        {
            if (ShouldLog(strategyInstance, EMAwave34LogLevel.Debug))
                Print(EMAwave34LogLevel.Debug, $"[{instrument}] {messageFactory()}", strategyInstance, eventTime);
        }

        public static void Debug(Func<string> messageFactory, NinjaScriptBase strategyInstance, DateTime eventTime)
        {
            if (ShouldLog(strategyInstance, EMAwave34LogLevel.Debug))
                Print(EMAwave34LogLevel.Debug, messageFactory(), strategyInstance, eventTime);
        }

        public static void Info(Func<string> messageFactory, NinjaScriptBase strategyInstance, string instrument)
        {
            if (ShouldLog(strategyInstance, EMAwave34LogLevel.Info))
                Print(EMAwave34LogLevel.Info, $"[{instrument}] {messageFactory()}", strategyInstance);
        }

        public static void Info(Func<string> messageFactory, NinjaScriptBase strategyInstance)
        {
            if (ShouldLog(strategyInstance, EMAwave34LogLevel.Info))
                Print(EMAwave34LogLevel.Info, messageFactory(), strategyInstance);
        }

        public static void Info(Func<string> messageFactory, NinjaScriptBase strategyInstance, string instrument, DateTime eventTime)
        {
            if (ShouldLog(strategyInstance, EMAwave34LogLevel.Info))
                Print(EMAwave34LogLevel.Info, $"[{instrument}] {messageFactory()}", strategyInstance, eventTime);
        }

        public static void Info(Func<string> messageFactory, NinjaScriptBase strategyInstance, DateTime eventTime)
        {
            if (ShouldLog(strategyInstance, EMAwave34LogLevel.Info))
                Print(EMAwave34LogLevel.Info, messageFactory(), strategyInstance, eventTime);
        }

        public static void Warn(Func<string> messageFactory, NinjaScriptBase strategyInstance, string instrument)
        {
            if (ShouldLog(strategyInstance, EMAwave34LogLevel.Warn))
                Print(EMAwave34LogLevel.Warn, $"[{instrument}] {messageFactory()}", strategyInstance);
        }

        public static void Warn(Func<string> messageFactory, NinjaScriptBase strategyInstance)
        {
            if (ShouldLog(strategyInstance, EMAwave34LogLevel.Warn))
                Print(EMAwave34LogLevel.Warn, messageFactory(), strategyInstance);
        }

        public static void Warn(Func<string> messageFactory, NinjaScriptBase strategyInstance, string instrument, DateTime eventTime)
        {
            if (ShouldLog(strategyInstance, EMAwave34LogLevel.Warn))
                Print(EMAwave34LogLevel.Warn, $"[{instrument}] {messageFactory()}", strategyInstance, eventTime);
        }

        public static void Warn(Func<string> messageFactory, NinjaScriptBase strategyInstance, DateTime eventTime)
        {
            if (ShouldLog(strategyInstance, EMAwave34LogLevel.Warn))
                Print(EMAwave34LogLevel.Warn, messageFactory(), strategyInstance, eventTime);
        }

        public static void Error(Func<string> messageFactory, NinjaScriptBase strategyInstance, string instrument)
        {
            if (ShouldLog(strategyInstance, EMAwave34LogLevel.Error))
                Print(EMAwave34LogLevel.Error, $"[{instrument}] {messageFactory()}", strategyInstance);
        }

        public static void Error(Func<string> messageFactory, NinjaScriptBase strategyInstance)
        {
            if (ShouldLog(strategyInstance, EMAwave34LogLevel.Error))
                Print(EMAwave34LogLevel.Error, messageFactory(), strategyInstance);
        }

        public static void Error(Func<string> messageFactory, NinjaScriptBase strategyInstance, string instrument, DateTime eventTime)
        {
            if (ShouldLog(strategyInstance, EMAwave34LogLevel.Error))
                Print(EMAwave34LogLevel.Error, $"[{instrument}] {messageFactory()}", strategyInstance, eventTime);
        }

        public static void Error(Func<string> messageFactory, NinjaScriptBase strategyInstance, DateTime eventTime)
        {
            if (ShouldLog(strategyInstance, EMAwave34LogLevel.Error))
                Print(EMAwave34LogLevel.Error, messageFactory(), strategyInstance, eventTime);
        }

        public static void Print(EMAwave34LogLevel level, string message, NinjaScriptBase strategyInstance, DateTime eventTime)
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
                    strategyInstance?.Print($"[ERROR] EMAwave34ServiceLogger.Print (eventTime) failed: {ex.Message}");
                }
                catch
                {
                    Console.WriteLine($"[CRITICAL] EMAwave34ServiceLogger completely failed: {ex.Message}");
                }
            }
        }

        public static void Print(EMAwave34LogLevel level, string message, NinjaScriptBase strategyInstance)
        {
            if (!ShouldLog(strategyInstance, level) || _isDisposed)
                return;
            
            try
            {
                long seq = Interlocked.Increment(ref _sequenceCounter);
                var entry = CreateLogEntry(seq, level, message, strategyInstance, null);
                _logQueue.Enqueue(entry);
                _newEntryEvent.Set();
            }
            catch (Exception ex)
            {
                try
                {
                    strategyInstance?.Print($"[ERROR] EMAwave34ServiceLogger.Print failed: {ex.Message}");
                }
                catch
                {
                    Console.WriteLine($"[CRITICAL] EMAwave34ServiceLogger completely failed: {ex.Message}");
                }
            }
        }

        private static EMAwave34LogTimestampMode ResolveTimestampMode(NinjaScriptBase strategyInstance)
        {
            try
            {
                if (strategyInstance is IEMAwave34LoggingConfig loggingConfig)
                {
                    if (loggingConfig.IsBacktestContext)
                        return EMAwave34LogTimestampMode.BarTime;

                    if (loggingConfig.EnableDebugLogging)
                        return EMAwave34LogTimestampMode.Dual;
                }
            }
            catch { }

            if (strategyInstance is NinjaTrader.NinjaScript.Strategies.Strategy strategy && strategy.State == State.Historical)
                return EMAwave34LogTimestampMode.BarTime;

            return EMAwave34LogTimestampMode.WallClock;
        }

        private static EMAwave34LogEntry CreateLogEntry(long sequence, EMAwave34LogLevel level, string message, NinjaScriptBase strategyInstance, DateTime? eventTime)
        {
            EMAwave34LogTimestampMode mode = ResolveTimestampMode(strategyInstance);
            DateTime wallClock = DateTime.Now;
            DateTime? barTime = null;

            if (mode == EMAwave34LogTimestampMode.BarTime || mode == EMAwave34LogTimestampMode.Dual)
            {
                barTime = eventTime ?? TryGetBarTimestamp(strategyInstance);
            }

            return new EMAwave34LogEntry(sequence, wallClock, barTime, mode, level, message, strategyInstance);
        }

        private static DateTime? TryGetBarTimestamp(NinjaScriptBase strategyInstance)
        {
            if (strategyInstance is NinjaTrader.NinjaScript.Strategies.Strategy strategy)
                return TryGetStrategyTime0(strategy);
            return null;
        }

        private static string FormatTimestamp(EMAwave34LogEntry entry)
        {
            const string format = "yyyy-MM-dd HH:mm:ss.fff";
            switch (entry.TimestampMode)
            {
                case EMAwave34LogTimestampMode.WallClock:
                    return entry.WallClockTimestamp.ToString(format);
                case EMAwave34LogTimestampMode.BarTime:
                    return entry.BarTimestamp.HasValue
                        ? entry.BarTimestamp.Value.ToString(format)
                        : entry.WallClockTimestamp.ToString(format);
                case EMAwave34LogTimestampMode.Dual:
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
            catch { }
            return null;
        }

        public static void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            try
            {
                _cancellationTokenSource.Cancel();

                while (_logQueue.TryDequeue(out EMAwave34LogEntry entry))
                {
                    try
                    {
                        WriteLogEntry(entry);
                    }
                    catch
                    {
                    }
                }

                if (_writerTask != null && !_writerTask.IsCompleted)
                {
                    _writerTask.Wait(TimeSpan.FromSeconds(2));
                }
            }
            catch
            {
            }
            finally
            {
                try
                {
                    _newEntryEvent?.Dispose();
                    _cancellationTokenSource?.Dispose();
                }
                catch
                {
                }
            }
        }
    }
}
