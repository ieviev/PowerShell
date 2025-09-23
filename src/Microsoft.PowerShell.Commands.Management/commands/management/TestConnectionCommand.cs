// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsDiagnostic.Test, "Connection", DefaultParameterSetName = DefaultPingParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097144")]
    [OutputType(typeof(PingStatus), ParameterSetName = new string[] { DefaultPingParameterSet })]
    [OutputType(typeof(PingStatus), ParameterSetName = new string[] { RepeatPingParameterSet, MtuSizeDetectParameterSet })]
    [OutputType(typeof(bool), ParameterSetName = new string[] { DefaultPingParameterSet, RepeatPingParameterSet, TcpPortParameterSet })]
    [OutputType(typeof(PingMtuStatus), ParameterSetName = new string[] { MtuSizeDetectParameterSet })]
    [OutputType(typeof(int), ParameterSetName = new string[] { MtuSizeDetectParameterSet })]
    [OutputType(typeof(TraceStatus), ParameterSetName = new string[] { TraceRouteParameterSet })]
    [OutputType(typeof(TcpPortStatus), ParameterSetName = new string[] { TcpPortParameterSet })]
    public class TestConnectionCommand : PSCmdlet, IDisposable
    {
        #region Parameter Set Names
        private const string DefaultPingParameterSet = "DefaultPing";
        private const string RepeatPingParameterSet = "RepeatPing";
        private const string TraceRouteParameterSet = "TraceRoute";
        private const string TcpPortParameterSet = "TcpPort";
        private const string MtuSizeDetectParameterSet = "MtuSizeDetect";

        #endregion

        #region Cmdlet Defaults

        // Count of pings sent to each trace route hop. Default mimics Windows' defaults.
        // If this value changes, we need to update 'ConsoleTraceRouteReply' resource string.
        private const uint DefaultTraceRoutePingCount = 3;

        // Default size for the send buffer.
        private const int DefaultSendBufferSize = 32;

        private const int DefaultMaxHops = 128;

        private const string TestConnectionExceptionId = "TestConnectionException";

        #endregion

        #region Private Fields

        private static readonly byte[] s_DefaultSendBuffer = Array.Empty<byte>();

        private readonly CancellationTokenSource _dnsLookupCancel = new();

        private bool _disposed;

        private Ping? _sender;

        #endregion

        #region Parameters

        
        [Parameter(ParameterSetName = DefaultPingParameterSet)]
        [Parameter(ParameterSetName = RepeatPingParameterSet)]
        public SwitchParameter Ping { get; set; } = true;

        
        [Parameter(ParameterSetName = DefaultPingParameterSet)]
        [Parameter(ParameterSetName = RepeatPingParameterSet)]
        [Parameter(ParameterSetName = TraceRouteParameterSet)]
        [Parameter(ParameterSetName = MtuSizeDetectParameterSet)]
        [Parameter(ParameterSetName = TcpPortParameterSet)]
        public SwitchParameter IPv4 { get; set; }

        
        [Parameter(ParameterSetName = DefaultPingParameterSet)]
        [Parameter(ParameterSetName = RepeatPingParameterSet)]
        [Parameter(ParameterSetName = TraceRouteParameterSet)]
        [Parameter(ParameterSetName = MtuSizeDetectParameterSet)]
        [Parameter(ParameterSetName = TcpPortParameterSet)]
        public SwitchParameter IPv6 { get; set; }

        
        [Parameter(ParameterSetName = DefaultPingParameterSet)]
        [Parameter(ParameterSetName = RepeatPingParameterSet)]
        [Parameter(ParameterSetName = TraceRouteParameterSet)]
        [Parameter(ParameterSetName = MtuSizeDetectParameterSet)]
        [Parameter(ParameterSetName = TcpPortParameterSet)]
        public SwitchParameter ResolveDestination { get; set; }

        
        [Parameter(ParameterSetName = DefaultPingParameterSet)]
        [Parameter(ParameterSetName = RepeatPingParameterSet)]
        [Parameter(ParameterSetName = TraceRouteParameterSet)]
        [Parameter(ParameterSetName = TcpPortParameterSet)]
        public string Source { get; } = Dns.GetHostName();

        
        [Parameter(ParameterSetName = DefaultPingParameterSet)]
        [Parameter(ParameterSetName = RepeatPingParameterSet)]
        [Parameter(ParameterSetName = TraceRouteParameterSet)]
        [ValidateRange(1, DefaultMaxHops)]
        [Alias("Ttl", "TimeToLive", "Hops")]
        public int MaxHops { get; set; } = DefaultMaxHops;

        
        [Parameter(ParameterSetName = DefaultPingParameterSet)]
        [Parameter(ParameterSetName = TcpPortParameterSet)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int Count { get; set; } = 4;

        
        [Parameter(ParameterSetName = DefaultPingParameterSet)]
        [Parameter(ParameterSetName = RepeatPingParameterSet)]
        [Parameter(ParameterSetName = TcpPortParameterSet)]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int Delay { get; set; } = 1;

        
        [Parameter(ParameterSetName = DefaultPingParameterSet)]
        [Parameter(ParameterSetName = RepeatPingParameterSet)]
        [Alias("Size", "Bytes", "BS")]
        [ValidateRange(0, 65500)]
        public int BufferSize { get; set; } = DefaultSendBufferSize;

        
        [Parameter(ParameterSetName = DefaultPingParameterSet)]
        [Parameter(ParameterSetName = RepeatPingParameterSet)]
        public SwitchParameter DontFragment { get; set; }

        
        [Parameter(Mandatory = true, ParameterSetName = RepeatPingParameterSet)]
        [Parameter(ParameterSetName = TcpPortParameterSet)]
        [Alias("Continuous")]
        public SwitchParameter Repeat { get; set; }

        
        [Parameter]
        public SwitchParameter Quiet { get; set; }

        
        [Parameter(ParameterSetName = TcpPortParameterSet)]
        public SwitchParameter Detailed;

        
        [Parameter]
        [ValidateRange(ValidateRangeKind.Positive)]
        public int TimeoutSeconds { get; set; } = 5;

        
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [Alias("ComputerName")]
        public string[]? TargetName { get; set; }

        
        [Parameter(Mandatory = true, ParameterSetName = MtuSizeDetectParameterSet)]
        [Alias("MtuSizeDetect")]
        public SwitchParameter MtuSize { get; set; }

        
        [Parameter(Mandatory = true, ParameterSetName = TraceRouteParameterSet)]
        public SwitchParameter Traceroute { get; set; }

        
        [ValidateRange(0, 65535)]
        [Parameter(Mandatory = true, ParameterSetName = TcpPortParameterSet)]
        public int TcpPort { get; set; }

        #endregion Parameters

        
        protected override void BeginProcessing()
        {
            switch (ParameterSetName)
            {
                case RepeatPingParameterSet:
                    Count = int.MaxValue;
                    break;
                case TcpPortParameterSet:
                    SetCountForTcpTest();
                    break;
            }
        }

        
        protected override void ProcessRecord()
        {
            if (TargetName == null)
            {
                return;
            }

            foreach (var targetName in TargetName)
            {
                switch (ParameterSetName)
                {
                    case DefaultPingParameterSet:
                    case RepeatPingParameterSet:
                        ProcessPing(targetName);
                        break;
                    case MtuSizeDetectParameterSet:
                        ProcessMTUSize(targetName);
                        break;
                    case TraceRouteParameterSet:
                        ProcessTraceroute(targetName);
                        break;
                    case TcpPortParameterSet:
                        ProcessConnectionByTCPPort(targetName);
                        break;
                }
            }
        }

        
        protected override void StopProcessing()
        {
            _sender?.SendAsyncCancel();
            _dnsLookupCancel.Cancel();
        }

        #region ConnectionTest

        private void SetCountForTcpTest()
        {
            if (Repeat.IsPresent)
            {
                Count = int.MaxValue;
            }
            else if (!MyInvocation.BoundParameters.ContainsKey(nameof(Count)))
            {
                Count = 1;
            }
        }

        private void ProcessConnectionByTCPPort(string targetNameOrAddress)
        {
            if (!TryResolveNameOrAddress(targetNameOrAddress, out _, out IPAddress? targetAddress))
            {
                if (Quiet.IsPresent)
                {
                    WriteObject(false);
                }

                return;
            }

            int timeoutMilliseconds = TimeoutSeconds * 1000;
            int delayMilliseconds = Delay * 1000;

            for (var i = 1; i <= Count; i++)
            {
                long latency = 0;
                SocketError status = SocketError.SocketError;

                Stopwatch stopwatch = new Stopwatch();

                using var client = new TcpClient();

                try
                {
                    stopwatch.Start();

                    if (client.ConnectAsync(targetAddress, TcpPort).Wait(timeoutMilliseconds, _dnsLookupCancel.Token))
                    {
                        latency = stopwatch.ElapsedMilliseconds;
                        status = SocketError.Success;
                    }
                    else
                    {
                        status = SocketError.TimedOut;
                    }
                }
                catch (AggregateException ae)
                {
                    ae.Handle((ex) =>
                    {
                        if (ex is TaskCanceledException)
                        {
                            throw new PipelineStoppedException();
                        }
                        if (ex is SocketException socketException)
                        {
                            status = socketException.SocketErrorCode;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    });
                }
                finally
                {
                    stopwatch.Reset();
                }

                if (!Detailed.IsPresent)
                {
                    WriteObject(status == SocketError.Success);
                    return;
                }
                else
                {
                    WriteObject(new TcpPortStatus(
                        i,
                        Source,
                        targetNameOrAddress,
                        targetAddress,
                        TcpPort,
                        latency,
                        status == SocketError.Success,
                        status
                    ));
                }

                if (i < Count)
                {
                    Task.Delay(delayMilliseconds).Wait(_dnsLookupCancel.Token);
                }
            }
        }

        #endregion ConnectionTest

        #region TracerouteTest

        private void ProcessTraceroute(string targetNameOrAddress)
        {
            byte[] buffer = GetSendBuffer(BufferSize);

            if (!TryResolveNameOrAddress(targetNameOrAddress, out string resolvedTargetName, out IPAddress? targetAddress))
            {
                if (!Quiet.IsPresent)
                {
                    WriteObject(false);
                }

                return;
            }

            int currentHop = 1;
            PingOptions pingOptions = new(currentHop, DontFragment.IsPresent);
            PingReply reply;
            PingReply discoveryReply;
            int timeout = TimeoutSeconds * 1000;
            Stopwatch timer = new();

            IPAddress hopAddress;
            do
            {
                pingOptions.Ttl = currentHop;

#if !UNIX
                // Get intermediate hop target. This needs to be done first, so that we can target it properly
                // and get useful responses.
                var discoveryAttempts = 0;
                bool addressIsValid = false;
                do
                {
                    discoveryReply = SendCancellablePing(targetAddress, timeout, buffer, pingOptions);
                    discoveryAttempts++;
                    addressIsValid = !(discoveryReply.Address.Equals(IPAddress.Any)
                        || discoveryReply.Address.Equals(IPAddress.IPv6Any));
                }
                while (discoveryAttempts <= DefaultTraceRoutePingCount && addressIsValid);

                // If we aren't able to get a valid address, just re-target the final destination of the trace.
                hopAddress = addressIsValid ? discoveryReply.Address : targetAddress;
#else
                // Unix Ping API returns nonsense "TimedOut" for ALL intermediate hops. No way around this
                // issue for traceroutes as we rely on information (intermediate addresses, etc.) that is
                // simply not returned to us by the API.
                // The only supported states on Unix seem to be Success and TimedOut. Workaround is to
                // keep targeting the final address; at the very least we will be able to tell the user
                // the required number of hops to reach the destination.
                hopAddress = targetAddress;
                discoveryReply = SendCancellablePing(targetAddress, timeout, buffer, pingOptions);
#endif
                var hopAddressString = discoveryReply.Address.ToString();

                string routerName = hopAddressString;
                try
                {
                    if (!TryResolveNameOrAddress(hopAddressString, out routerName, out _))
                    {
                        routerName = hopAddressString;
                    }
                }
                catch
                {
                    // Swallow hostname resolve exceptions and continue with traceroute
                }

                // In traceroutes we don't use 'Count' parameter.
                // If we change 'DefaultTraceRoutePingCount' we should change 'ConsoleTraceRouteReply' resource string.
                for (uint i = 1; i <= DefaultTraceRoutePingCount; i++)
                {
                    try
                    {
                        reply = SendCancellablePing(hopAddress, timeout, buffer, pingOptions, timer);

                        if (!Quiet.IsPresent)
                        {
                            var status = new PingStatus(
                                Source,
                                routerName,
                                reply,
                                reply.Status == IPStatus.Success
                                    ? reply.RoundtripTime
                                    : timer.ElapsedMilliseconds,

                                // If we use the empty buffer, then .NET actually uses a 32 byte buffer so we want to show
                                // as the result object the actual buffer size used instead of 0.
                                buffer.Length == 0 ? DefaultSendBufferSize : buffer.Length,
                                pingNum: i);
                            WriteObject(new TraceStatus(
                                currentHop,
                                status,
                                Source,
                                resolvedTargetName,
                                targetAddress));
                        }
                    }
                    catch (PingException ex)
                    {
                        string message = StringUtil.Format(
                            TestConnectionResources.NoPingResult,
                            resolvedTargetName,
                            ex.Message);
                        Exception pingException = new PingException(message, ex.InnerException);
                        ErrorRecord errorRecord = new(
                            pingException,
                            TestConnectionExceptionId,
                            ErrorCategory.ResourceUnavailable,
                            resolvedTargetName);
                        WriteError(errorRecord);

                        continue;
                    }

                    // We use short delay because it is impossible DoS with trace route.
                    Thread.Sleep(50);
                    timer.Reset();
                }

                currentHop++;
            } while (currentHop <= MaxHops
                && (discoveryReply.Status == IPStatus.TtlExpired
                    || discoveryReply.Status == IPStatus.TimedOut));

            if (Quiet.IsPresent)
            {
                WriteObject(currentHop <= MaxHops);
            }
            else if (currentHop > MaxHops)
            {
                var message = StringUtil.Format(
                    TestConnectionResources.MaxHopsExceeded,
                    resolvedTargetName,
                    MaxHops);
                var pingException = new PingException(message);
                WriteError(new ErrorRecord(
                    pingException,
                    TestConnectionExceptionId,
                    ErrorCategory.ConnectionError,
                    targetAddress));
            }
        }

        #endregion TracerouteTest

        #region MTUSizeTest
        private void ProcessMTUSize(string targetNameOrAddress)
        {
            PingReply? reply, replyResult = null;
            if (!TryResolveNameOrAddress(targetNameOrAddress, out string resolvedTargetName, out IPAddress? targetAddress))
            {
                if (Quiet.IsPresent)
                {
                    WriteObject(-1);
                }

                return;
            }

            // Caution! Algorithm is sensitive to changing boundary values.
            int HighMTUSize = 10000;
            int CurrentMTUSize = 1473;
            int LowMTUSize = targetAddress.AddressFamily == AddressFamily.InterNetworkV6 ? 1280 : 68;
            int timeout = TimeoutSeconds * 1000;

            PingReply? timeoutReply = null;

            try
            {
                PingOptions pingOptions = new(MaxHops, true);
                int retry = 1;

                while (LowMTUSize < (HighMTUSize - 1))
                {
                    byte[] buffer = GetSendBuffer(CurrentMTUSize);

                    WriteDebug(StringUtil.Format(
                        "LowMTUSize: {0}, CurrentMTUSize: {1}, HighMTUSize: {2}",
                        LowMTUSize,
                        CurrentMTUSize,
                        HighMTUSize));

                    reply = SendCancellablePing(targetAddress, timeout, buffer, pingOptions);

                    if (reply.Status == IPStatus.PacketTooBig || reply.Status == IPStatus.TimedOut)
                    {
                        HighMTUSize = CurrentMTUSize;
                        timeoutReply = reply;
                        retry = 1;
                    }
                    else if (reply.Status == IPStatus.Success)
                    {
                        LowMTUSize = CurrentMTUSize;
                        replyResult = reply;
                        retry = 1;
                    }
                    else
                    {
                        // If the host didn't reply, try again up to the 'Count' value.
                        if (retry >= Count)
                        {
                            string message = StringUtil.Format(
                                TestConnectionResources.NoPingResult,
                                targetAddress,
                                reply.Status.ToString());
                            Exception pingException = new PingException(message);
                            ErrorRecord errorRecord = new(
                                pingException,
                                TestConnectionExceptionId,
                                ErrorCategory.ResourceUnavailable,
                                targetAddress);
                            WriteError(errorRecord);
                            return;
                        }
                        else
                        {
                            retry++;
                            continue;
                        }
                    }

                    CurrentMTUSize = (LowMTUSize + HighMTUSize) / 2;

                    // Prevent DoS attack.
                    Thread.Sleep(100);
                }
            }
            catch (PingException ex)
            {
                string message = StringUtil.Format(TestConnectionResources.NoPingResult, targetAddress, ex.Message);
                Exception pingException = new PingException(message, ex.InnerException);
                ErrorRecord errorRecord = new(
                    pingException,
                    TestConnectionExceptionId,
                    ErrorCategory.ResourceUnavailable,
                    targetAddress);
                WriteError(errorRecord);
                return;
            }

            if (Quiet.IsPresent)
            {
                WriteObject(CurrentMTUSize);
            }
            else
            {
                if (replyResult is null)
                {
                    if (timeoutReply is not null)
                    {
                        Exception timeoutException = new TimeoutException(targetAddress.ToString());
                        ErrorRecord errorRecord = new(
                            timeoutException,
                            TestConnectionExceptionId,
                            ErrorCategory.ResourceUnavailable,
                            timeoutReply);
                        WriteError(errorRecord);
                    }
                    else
                    {
                        ArgumentNullException.ThrowIfNull(replyResult);
                    }
                }
                else
                {
                    WriteObject(new PingMtuStatus(
                        Source,
                        resolvedTargetName,
                        replyResult,
                        CurrentMTUSize));
                }

            }
        }

        #endregion MTUSizeTest

        #region PingTest

        private void ProcessPing(string targetNameOrAddress)
        {
            if (!TryResolveNameOrAddress(targetNameOrAddress, out string resolvedTargetName, out IPAddress? targetAddress))
            {
                if (Quiet.IsPresent)
                {
                    WriteObject(false);
                }

                return;
            }

            bool quietResult = true;
            byte[] buffer = GetSendBuffer(BufferSize);

            PingReply reply;
            PingOptions pingOptions = new(MaxHops, DontFragment.IsPresent);
            int timeout = TimeoutSeconds * 1000;
            int delay = Delay * 1000;

            for (int i = 1; i <= Count; i++)
            {
                try
                {
                    reply = SendCancellablePing(targetAddress, timeout, buffer, pingOptions);
                }
                catch (PingException ex)
                {
                    string message = StringUtil.Format(TestConnectionResources.NoPingResult, resolvedTargetName, ex.Message);
                    Exception pingException = new PingException(message, ex.InnerException);
                    ErrorRecord errorRecord = new(
                        pingException,
                        TestConnectionExceptionId,
                        ErrorCategory.ResourceUnavailable,
                        resolvedTargetName);
                    WriteError(errorRecord);

                    quietResult = false;
                    continue;
                }

                if (Quiet.IsPresent)
                {
                    // Return 'true' only if all pings have completed successfully.
                    quietResult &= reply.Status == IPStatus.Success;
                }
                else
                {
                    WriteObject(new PingStatus(
                        Source,
                        resolvedTargetName,
                        reply,
                        reply.RoundtripTime,
                        buffer.Length == 0 ? DefaultSendBufferSize : buffer.Length,
                        pingNum: (uint)i));
                }

                // Delay between pings, but not after last ping.
                if (i < Count && Delay > 0)
                {
                    Thread.Sleep(delay);
                }
            }

            if (Quiet.IsPresent)
            {
                WriteObject(quietResult);
            }
        }

        #endregion PingTest

        private bool TryResolveNameOrAddress(
            string targetNameOrAddress,
            out string resolvedTargetName,
            [NotNullWhen(true)]
            out IPAddress? targetAddress)
        {
            resolvedTargetName = targetNameOrAddress;

            IPHostEntry hostEntry;
            if (IPAddress.TryParse(targetNameOrAddress, out targetAddress))
            {
                if ((IPv4 && targetAddress.AddressFamily != AddressFamily.InterNetwork)
                    || (IPv6 && targetAddress.AddressFamily != AddressFamily.InterNetworkV6))
                {
                    string message = StringUtil.Format(
                        TestConnectionResources.NoPingResult,
                        resolvedTargetName,
                        TestConnectionResources.TargetAddressAbsent);
                    Exception pingException = new PingException(message, null);
                    ErrorRecord errorRecord = new(
                        pingException,
                        TestConnectionExceptionId,
                        ErrorCategory.ResourceUnavailable,
                        resolvedTargetName);
                    WriteError(errorRecord);
                    return false;
                }

                if (ResolveDestination)
                {
                    hostEntry = GetCancellableHostEntry(targetNameOrAddress);
                    resolvedTargetName = hostEntry.HostName;
                }
                else
                {
                    resolvedTargetName = targetAddress.ToString();
                }
            }
            else
            {
                try
                {
                    hostEntry = GetCancellableHostEntry(targetNameOrAddress);

                    if (ResolveDestination)
                    {
                        resolvedTargetName = hostEntry.HostName;
                        hostEntry = GetCancellableHostEntry(hostEntry.HostName);
                    }
                }
                catch (PipelineStoppedException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (!Quiet.IsPresent)
                    {
                        string message = StringUtil.Format(
                            TestConnectionResources.NoPingResult,
                            resolvedTargetName,
                            TestConnectionResources.CannotResolveTargetName);
                        Exception pingException = new PingException(message, ex);
                        ErrorRecord errorRecord = new(
                            pingException,
                            TestConnectionExceptionId,
                            ErrorCategory.ResourceUnavailable,
                            resolvedTargetName);
                        WriteError(errorRecord);
                    }

                    return false;
                }

                if (IPv6 || IPv4)
                {
                    targetAddress = GetHostAddress(hostEntry);

                    if (targetAddress == null)
                    {
                        string message = StringUtil.Format(
                            TestConnectionResources.NoPingResult,
                            resolvedTargetName,
                            TestConnectionResources.TargetAddressAbsent);
                        Exception pingException = new PingException(message, null);
                        ErrorRecord errorRecord = new(
                            pingException,
                            TestConnectionExceptionId,
                            ErrorCategory.ResourceUnavailable,
                            resolvedTargetName);
                        WriteError(errorRecord);
                        return false;
                    }
                }
                else
                {
                    targetAddress = hostEntry.AddressList[0];
                }
            }

            return true;
        }

        private IPHostEntry GetCancellableHostEntry(string targetNameOrAddress)
        {
            var task = Dns.GetHostEntryAsync(targetNameOrAddress);
            var waitHandles = new[] { ((IAsyncResult)task).AsyncWaitHandle, _dnsLookupCancel.Token.WaitHandle };

            // WaitAny() returns the index of the first signal it gets; 1 is our cancellation token.
            if (WaitHandle.WaitAny(waitHandles) == 1)
            {
                throw new PipelineStoppedException();
            }

            return task.GetAwaiter().GetResult();
        }

        private IPAddress? GetHostAddress(IPHostEntry hostEntry)
        {
            AddressFamily addressFamily = IPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;

            foreach (var address in hostEntry.AddressList)
            {
                if (address.AddressFamily == addressFamily)
                {
                    return address;
                }
            }

            return null;
        }

        // Users most often use the default buffer size so we cache the buffer.
        // Creates and fills a send buffer. This follows the ping.exe and CoreFX model.
        private static byte[] GetSendBuffer(int bufferSize)
        {
            if (bufferSize == DefaultSendBufferSize)
            {
                return s_DefaultSendBuffer;
            }

            byte[] sendBuffer = new byte[bufferSize];

            for (int i = 0; i < bufferSize; i++)
            {
                sendBuffer[i] = (byte)((int)'a' + i % 23);
            }

            return sendBuffer;
        }

        
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        
        /// <param name="disposing">
        /// Specified as true when Dispose() was called, false if this is called from the finalizer.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _sender?.Dispose();
                    _dnsLookupCancel.Dispose();
                }

                _disposed = true;
            }
        }

        // Uses the SendAsync() method to send pings, so that Ctrl+C can halt the request early if needed.
        private PingReply SendCancellablePing(
            IPAddress targetAddress,
            int timeout,
            byte[] buffer,
            PingOptions pingOptions,
            Stopwatch? timer = null)
        {
            try
            {
                _sender = new Ping();

                timer?.Start();
                // 'SendPingAsync' always uses the default synchronization context (threadpool).
                // This is what we want to avoid the deadlock resulted by async work being scheduled back to the
                // pipeline thread due to a change of the current synchronization context of the pipeline thread.
                return _sender.SendPingAsync(targetAddress, timeout, buffer, pingOptions).GetAwaiter().GetResult();
            }
            catch (PingException ex) when (ex.InnerException is TaskCanceledException)
            {
                // The only cancellation we have implemented is on pipeline stops via StopProcessing().
                throw new PipelineStoppedException();
            }
            finally
            {
                timer?.Stop();
                _sender?.Dispose();
                _sender = null;
            }
        }

        
        public class TcpPortStatus
        {
            
            /// <param name="id">The number of this test.</param>
            /// <param name="source">The source machine name or IP of the test.</param>
            /// <param name="target">The target machine name or IP of the test.</param>
            /// <param name="targetAddress">The resolved IP from the target.</param>
            /// <param name="port">The port used for the connection.</param>
            /// <param name="latency">The latency of the test.</param>
            /// <param name="connected">If the test connection succeeded.</param>
            /// <param name="status">Status of the underlying socket.</param>
            internal TcpPortStatus(int id, string source, string target, IPAddress targetAddress, int port, long latency, bool connected, SocketError status)
            {
                Id = id;
                Source = source;
                Target = target;
                TargetAddress = targetAddress;
                Port = port;
                Latency = latency;
                Connected = connected;
                Status = status;
            }

            
            public int Id { get; set; }

            
            public string Source { get; }

            
            public string Target { get; }

            
            public IPAddress TargetAddress { get; }

            
            public int Port { get; }

            
            public long Latency { get; set; }

            
            public bool Connected { get; set; }

            
            public SocketError Status { get; set; }
        }

        
        public class PingStatus
        {
            
            /// <param name="source">The source machine name or IP of the ping.</param>
            /// <param name="destination">The destination machine name of the ping.</param>
            /// <param name="reply">The response from the ping attempt.</param>
            /// <param name="latency">The latency of the ping.</param>
            /// <param name="bufferSize">The buffer size.</param>
            /// <param name="pingNum">The sequence number in the sequence of pings to the hop point.</param>
            internal PingStatus(
                string source,
                string destination,
                PingReply reply,
                long latency,
                int bufferSize,
                uint pingNum)
                : this(source, destination, reply, pingNum)
            {
                _bufferSize = bufferSize;
                _latency = latency;
            }

            
            /// <param name="source">The source machine name or IP of the ping.</param>
            /// <param name="destination">The destination machine name of the ping.</param>
            /// <param name="reply">The response from the ping attempt.</param>
            /// <param name="pingNum">The sequence number of the ping in the sequence of pings to the target.</param>
            internal PingStatus(string source, string destination, PingReply reply, uint pingNum)
            {
                Ping = pingNum;
                Reply = reply;
                Source = source;
                Destination = destination;
            }

            // These values can be set manually to skirt issues with the Ping API on Unix platforms
            // so that we can return meaningful known data that is discarded by the API.
            private readonly int _bufferSize = -1;

            private readonly long _latency = -1;

            
            public uint Ping { get; }

            
            public string Source { get; }

            
            public string Destination { get; }

            
            public IPAddress? Address { get => Reply.Status == IPStatus.Success ? Reply.Address : null; }

            
            public string DisplayAddress { get => Address?.ToString() ?? "*"; }

            
            public long Latency { get => _latency >= 0 ? _latency : Reply.RoundtripTime; }

            
            public IPStatus Status { get => Reply.Status; }

            
            public int BufferSize { get => _bufferSize >= 0 ? _bufferSize : Reply.Buffer.Length; }

            
            public PingReply Reply { get; }
        }

        
        public class PingMtuStatus : PingStatus
        {
            
            /// <param name="source">The source machine name or IP of the ping.</param>
            /// <param name="destination">The destination machine name of the ping.</param>
            /// <param name="reply">The response from the ping attempt.</param>
            /// <param name="bufferSize">The buffer size from the successful ping attempt.</param>
            internal PingMtuStatus(string source, string destination, PingReply reply, int bufferSize)
                : base(source, destination, reply, 1)
            {
                MtuSize = bufferSize;
            }

            
            public int MtuSize { get; }
        }

        
        public class TraceStatus
        {
            
            /// <param name="hop">The hop number of this trace hop.</param>
            /// <param name="status">The PingStatus response from this trace hop.</param>
            /// <param name="source">The source computer name or IP address of the traceroute.</param>
            /// <param name="destination">The target destination of the traceroute.</param>
            /// <param name="destinationAddress">The target IPAddress of the overall traceroute.</param>
            internal TraceStatus(
                int hop,
                PingStatus status,
                string source,
                string destination,
                IPAddress destinationAddress)
            {
                _status = status;
                Hop = hop;
                Source = source;
                Target = destination;
                TargetAddress = destinationAddress;

                if (_status.Address == IPAddress.Any
                    || _status.Address == IPAddress.IPv6Any)
                {
                    Hostname = null;
                }
                else
                {
                    Hostname = _status.Destination;
                }
            }

            private readonly PingStatus _status;

            
            public int Hop { get; }

            
            /// <value></value>
            public string? Hostname { get; }

            
            public uint Ping { get => _status.Ping; }

            
            public IPAddress? HopAddress { get => _status.Address; }

            
            public long Latency { get => _status.Latency; }

            
            public IPStatus Status { get => _status.Status; }

            
            public string Source { get; }

            
            public string Target { get; }

            
            public IPAddress TargetAddress { get; }

            
            public PingReply Reply { get => _status.Reply; }
        }

        
        ~TestConnectionCommand()
        {
            Dispose(disposing: false);
        }
    }
}
