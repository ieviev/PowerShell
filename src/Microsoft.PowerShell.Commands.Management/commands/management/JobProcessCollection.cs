// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable
#if !UNIX
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.PowerShell.Commands;


internal sealed class JobProcessCollection : IDisposable
{
    
    private bool? _initStatus;

    
    private Interop.Windows.SafeJobHandle? _jobObject;

    
    private Interop.Windows.SafeIoCompletionPort? _completionPort;

    
    public JobProcessCollection()
    { }

    
    public bool AssignProcessToJobObject(SafeProcessHandle process)
        => InitializeJob() && Interop.Windows.AssignProcessToJobObject(_jobObject, process);

    
    public void WaitForExit(CancellationToken cancellationToken)
    {
        if (_completionPort is null)
        {
            return;
        }

        using var cancellationRegistration = cancellationToken.Register(() =>
        {
            Interop.Windows.PostQueuedCompletionStatus(
                _completionPort,
                Interop.Windows.JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO);
        });

        int completionCode = 0;
        do
        {
            Interop.Windows.GetQueuedCompletionStatus(
                _completionPort,
                Interop.Windows.INFINITE,
                out completionCode);
        }
        while (completionCode != Interop.Windows.JOB_OBJECT_MSG_ACTIVE_PROCESS_ZERO);
        cancellationToken.ThrowIfCancellationRequested();
    }

    [MemberNotNullWhen(true, [nameof(_jobObject), nameof(_completionPort)])]
    private bool InitializeJob()
    {
        if (_initStatus.HasValue)
        {
            return _initStatus.Value;
        }

        if (_jobObject is null)
        {
            _jobObject = Interop.Windows.CreateJobObject();
            if (_jobObject.IsInvalid)
            {
                _initStatus = false;
                _jobObject.Dispose();
                _jobObject = null;
                return false;
            }
        }

        if (_completionPort is null)
        {
            _completionPort = Interop.Windows.CreateIoCompletionPort();
            if (_completionPort.IsInvalid)
            {
                _initStatus = false;
                _completionPort.Dispose();
                _completionPort = null;
                return false;
            }
        }

        _initStatus = Interop.Windows.SetInformationJobObject(
            _jobObject,
            _completionPort);

        return _initStatus.Value;
    }

    public void Dispose()
    {
        _jobObject?.Dispose();
        _completionPort?.Dispose();
    }
}
#endif
