// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    
    internal sealed class OutputGroupQueue
    {
        
        internal OutputGroupQueue(FormattedObjectsCache.ProcessCachedGroupNotification callBack, int objectCount)
        {
            _notificationCallBack = callBack;
            _objectCount = objectCount;
        }

        
        internal OutputGroupQueue(FormattedObjectsCache.ProcessCachedGroupNotification callBack, TimeSpan groupingDuration)
        {
            _notificationCallBack = callBack;
            _groupingDuration = groupingDuration;
        }

        
        internal List<PacketInfoData> Add(PacketInfoData o)
        {
            if (o is FormatStartData fsd)
            {
                // just cache the reference (used during the notification call)
                _formatStartData = fsd;
            }

            UpdateObjectCount(o);

            // STATE TRANSITION: we are not processing and we start
            if (!_processingGroup && (o is GroupStartData))
            {
                // just set the flag and start caching
                _processingGroup = true;
                _currentObjectCount = 0;

                if (_groupingDuration > TimeSpan.MinValue)
                {
                    _groupingTimer = Stopwatch.StartNew();
                }

                _queue.Enqueue(o);
                return null;
            }

            // STATE TRANSITION: we are processing and we stop
            if (_processingGroup &&
                ((o is GroupEndData) ||
                (_objectCount > 0) && (_currentObjectCount >= _objectCount)) ||
                ((_groupingTimer != null) && (_groupingTimer.Elapsed > _groupingDuration))
                )
            {
                // reset the object count
                _currentObjectCount = 0;

                if (_groupingTimer != null)
                {
                    _groupingTimer.Stop();
                    _groupingTimer = null;
                }

                // add object to queue, to be picked up
                _queue.Enqueue(o);

                // we are at the end of a group, drain the queue
                Notify();
                _processingGroup = false;

                List<PacketInfoData> retVal = new List<PacketInfoData>();

                while (_queue.Count > 0)
                {
                    retVal.Add(_queue.Dequeue());
                }

                return retVal;
            }

            // NO STATE TRANSITION: check the state we are in
            if (_processingGroup)
            {
                // we are in the caching state
                _queue.Enqueue(o);
                return null;
            }

            // we are not processing, so just return it
            List<PacketInfoData> ret = new List<PacketInfoData>();

            ret.Add(o);
            return ret;
        }

        private void UpdateObjectCount(PacketInfoData o)
        {
            // add only of it's not a control message
            // and it's not out of band
            if (o is FormatEntryData fed && !fed.outOfBand)
            {
                _currentObjectCount++;
            }
        }

        private void Notify()
        {
            if (_notificationCallBack == null)
                return;

            // filter out the out of band data, since they do not participate in the
            // auto resize algorithm
            List<PacketInfoData> validObjects = new List<PacketInfoData>();

            foreach (PacketInfoData x in _queue)
            {
                if (x is FormatEntryData fed && fed.outOfBand)
                    continue;

                validObjects.Add(x);
            }

            _notificationCallBack(_formatStartData, validObjects);
        }

        
        internal PacketInfoData Dequeue()
        {
            if (_queue.Count == 0)
                return null;

            return _queue.Dequeue();
        }

        
        private readonly Queue<PacketInfoData> _queue = new Queue<PacketInfoData>();

        
        private readonly int _objectCount = 0;

        
        private readonly TimeSpan _groupingDuration = TimeSpan.MinValue;
        private Stopwatch _groupingTimer = null;

        
        private readonly FormattedObjectsCache.ProcessCachedGroupNotification _notificationCallBack = null;

        
        private FormatStartData _formatStartData = null;

        
        private bool _processingGroup = false;

        
        private int _currentObjectCount = 0;
    }

    
    internal sealed class FormattedObjectsCache
    {
        
        internal delegate void ProcessCachedGroupNotification(FormatStartData formatStartData, List<PacketInfoData> objects);

        
        internal FormattedObjectsCache(bool cacheFrontEnd)
        {
            if (cacheFrontEnd)
                _frontEndQueue = new Queue<PacketInfoData>();
        }

        
        internal void EnableGroupCaching(ProcessCachedGroupNotification callBack, int objectCount)
        {
            if (callBack != null)
                _groupQueue = new OutputGroupQueue(callBack, objectCount);
        }

        
        internal void EnableGroupCaching(ProcessCachedGroupNotification callBack, TimeSpan groupingDuration)
        {
            if (callBack != null)
                _groupQueue = new OutputGroupQueue(callBack, groupingDuration);
        }

        
        internal List<PacketInfoData> Add(PacketInfoData o)
        {
            // if neither there, pass thru
            if (_frontEndQueue == null && _groupQueue == null)
            {
                List<PacketInfoData> retVal = new List<PacketInfoData>();
                retVal.Add(o);
                return retVal;
            }

            // if front present, add to front
            if (_frontEndQueue != null)
            {
                _frontEndQueue.Enqueue(o);
                return null;
            }

            // if back only, add to back
            return _groupQueue.Add(o);
        }

        
        internal List<PacketInfoData> Drain()
        {
            // if neither there,we did not cache at all
            if (_frontEndQueue == null && _groupQueue == null)
            {
                return null;
            }

            List<PacketInfoData> retVal = new List<PacketInfoData>();

            if (_frontEndQueue != null)
            {
                if (_groupQueue == null)
                {
                    // drain the front queue and return the data
                    while (_frontEndQueue.Count > 0)
                        retVal.Add(_frontEndQueue.Dequeue());

                    return retVal;
                }

                // move from the front to the back queue
                while (_frontEndQueue.Count > 0)
                {
                    List<PacketInfoData> groupQueueOut = _groupQueue.Add(_frontEndQueue.Dequeue());

                    if (groupQueueOut != null)
                        foreach (PacketInfoData x in groupQueueOut)
                            retVal.Add(x);
                }
            }

            // drain the back queue
            while (true)
            {
                PacketInfoData obj = _groupQueue.Dequeue();

                if (obj == null)
                    break;

                retVal.Add(obj);
            }

            return retVal;
        }

        
        private readonly Queue<PacketInfoData> _frontEndQueue;

        
        private OutputGroupQueue _groupQueue = null;
    }
}
