// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.PerformanceData;
using System.Globalization;
using System.Management.Automation.Tracing;

namespace System.Management.Automation.PerformanceData
{
    
    public abstract class CounterSetInstanceBase : IDisposable
    {
        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();
        #region Protected Members

        
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        protected CounterSetRegistrarBase _counterSetRegistrarBase;

        // NOTE: Check whether the following dictionaries need to be concurrent
        // because there would be only 1 thread creating the instance,
        // and that instance would then be shared by multiple threads for data access.
        // Those threads won't modify/manipulate the dictionary, but they would only access it.

        
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        protected ConcurrentDictionary<string, int> _counterNameToIdMapping;
        
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        protected ConcurrentDictionary<int, CounterType> _counterIdToTypeMapping;

        #region Constructors
        
        protected CounterSetInstanceBase(CounterSetRegistrarBase counterSetRegistrarInst)
        {
            this._counterSetRegistrarBase = counterSetRegistrarInst;
            _counterNameToIdMapping = new ConcurrentDictionary<string, int>();
            _counterIdToTypeMapping = new ConcurrentDictionary<int, CounterType>();

            CounterInfo[] counterInfoArray = this._counterSetRegistrarBase.CounterInfoArray;

            for (int i = 0; i < counterInfoArray.Length; i++)
            {
                this._counterIdToTypeMapping.TryAdd(counterInfoArray[i].Id, counterInfoArray[i].Type);
                if (!string.IsNullOrWhiteSpace(counterInfoArray[i].Name))
                {
                    this._counterNameToIdMapping.TryAdd(counterInfoArray[i].Name, counterInfoArray[i].Id);
                }
            }
        }

        #endregion

        
        protected bool RetrieveTargetCounterIdIfValid(int counterId, bool isNumerator, out int targetCounterId)
        {
            targetCounterId = counterId;
            if (isNumerator == false)
            {
                bool isDenominatorValid = false;
                CounterType counterType = this._counterIdToTypeMapping[counterId];
                switch (counterType)
                {
                    case CounterType.MultiTimerPercentageActive:
                    case CounterType.MultiTimerPercentageActive100Ns:
                    case CounterType.MultiTimerPercentageNotActive:
                    case CounterType.MultiTimerPercentageNotActive100Ns:
                    case CounterType.RawFraction32:
                    case CounterType.RawFraction64:
                    case CounterType.SampleFraction:
                    case CounterType.AverageCount64:
                    case CounterType.AverageTimer32:
                        isDenominatorValid = true;
                        break;
                }

                if (isDenominatorValid == false)
                {
                    InvalidOperationException invalidOperationException =
                        new InvalidOperationException(
                            string.Format(
                            CultureInfo.InvariantCulture,
                            "Denominator for update not valid for the given counter id {0}",
                            counterId));
                    _tracer.TraceException(invalidOperationException);
                    return false;
                }

                targetCounterId = counterId + 1;
            }

            return true;
        }

        #endregion

        #region Public Methods

        
        public abstract bool UpdateCounterByValue(
            int counterId,
            long stepAmount,
            bool isNumerator);

        
        public abstract bool UpdateCounterByValue(
            string counterName,
            long stepAmount,
            bool isNumerator);

        
        public abstract bool SetCounterValue(
            int counterId,
            long counterValue,
            bool isNumerator);

        
        public abstract bool SetCounterValue(
            string counterName,
            long counterValue,
            bool isNumerator);

        
        public abstract bool GetCounterValue(int counterId, bool isNumerator, out long counterValue);

        
        public abstract bool GetCounterValue(string counterName, bool isNumerator, out long counterValue);

        
        public abstract void Dispose();

        
        #endregion
    }

    
    public class PSCounterSetInstance : CounterSetInstanceBase
    {
        #region Private Members
        private bool _Disposed;
        private CounterSet _CounterSet;
        private CounterSetInstance _CounterSetInstance;
        private readonly PowerShellTraceSource _tracer = PowerShellTraceSourceFactory.GetTraceSource();

        #region Private Methods

        private void CreateCounterSetInstance()
        {
            _CounterSet =
                new CounterSet(
                    base._counterSetRegistrarBase.ProviderId,
                    base._counterSetRegistrarBase.CounterSetId,
                    base._counterSetRegistrarBase.CounterSetInstType);

            // Add the counters to the counter set definition.
            foreach (CounterInfo counterInfo in base._counterSetRegistrarBase.CounterInfoArray)
            {
                if (counterInfo.Name == null)
                {
                    _CounterSet.AddCounter(counterInfo.Id, counterInfo.Type);
                }
                else
                {
                    _CounterSet.AddCounter(counterInfo.Id, counterInfo.Type, counterInfo.Name);
                }
            }

            string instanceName = PSPerfCountersMgr.Instance.GetCounterSetInstanceName();
            // Create an instance of the counter set (contains the counter data).
            _CounterSetInstance = _CounterSet.CreateCounterSetInstance(instanceName);
        }

        private void UpdateCounterByValue(CounterData TargetCounterData, long stepAmount)
        {
            Debug.Assert(TargetCounterData != null);
            if (stepAmount == -1)
            {
                TargetCounterData.Decrement();
            }
            else if (stepAmount == 1)
            {
                TargetCounterData.Increment();
            }
            else
            {
                TargetCounterData.IncrementBy(stepAmount);
            }
        }

        #endregion

        #endregion

        #region Constructors
        
        public PSCounterSetInstance(CounterSetRegistrarBase counterSetRegBaseObj)
            : base(counterSetRegBaseObj)
        {
            CreateCounterSetInstance();
        }

        #endregion

        #region Destructor

        
        ~PSCounterSetInstance()
        {
            Dispose(false);
        }

        #endregion

        #region Protected Methods
        
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!_Disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    _CounterSetInstance.Dispose();
                    _CounterSet.Dispose();
                }
                // Note disposing has been done.
                _Disposed = true;
            }
        }

        #endregion

        #region IDisposable Overrides
        
        public override void Dispose()
        {
            this.Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SuppressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        #endregion

        #region CounterSetInstanceBase Overrides

        #region Public Methods

        
        public override bool UpdateCounterByValue(int counterId, long stepAmount, bool isNumerator)
        {
            if (_Disposed)
            {
                ObjectDisposedException objectDisposedException =
                    new ObjectDisposedException("PSCounterSetInstance");
                _tracer.TraceException(objectDisposedException);
                return false;
            }

            int targetCounterId;
            if (base.RetrieveTargetCounterIdIfValid(counterId, isNumerator, out targetCounterId))
            {
                CounterData targetCounterData = _CounterSetInstance.Counters[targetCounterId];
                if (targetCounterData != null)
                {
                    this.UpdateCounterByValue(targetCounterData, stepAmount);
                    return true;
                }
                else
                {
                    InvalidOperationException invalidOperationException =
                    new InvalidOperationException(
                        string.Format(
                        CultureInfo.InvariantCulture,
                        "Lookup for counter corresponding to counter id {0} failed",
                        counterId));
                    _tracer.TraceException(invalidOperationException);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        
        public override bool UpdateCounterByValue(string counterName, long stepAmount, bool isNumerator)
        {
            if (_Disposed)
            {
                ObjectDisposedException objectDisposedException =
                    new ObjectDisposedException("PSCounterSetInstance");
                _tracer.TraceException(objectDisposedException);
                return false;
            }
            // retrieve counter id associated with the counter name
            if (counterName == null)
            {
                ArgumentNullException argNullException = new ArgumentNullException(nameof(counterName));
                _tracer.TraceException(argNullException);
                return false;
            }

            try
            {
                int targetCounterId = this._counterNameToIdMapping[counterName];
                return this.UpdateCounterByValue(targetCounterId, stepAmount, isNumerator);
            }
            catch (KeyNotFoundException)
            {
                InvalidOperationException invalidOperationException =
                    new InvalidOperationException(
                    string.Format(
                    CultureInfo.InvariantCulture,
                    "Lookup for counter corresponding to counter name {0} failed",
                    counterName));
                _tracer.TraceException(invalidOperationException);
                return false;
            }
        }

        
        public override bool SetCounterValue(int counterId, long counterValue, bool isNumerator)
        {
            if (_Disposed)
            {
                ObjectDisposedException objectDisposedException =
                    new ObjectDisposedException("PSCounterSetInstance");
                _tracer.TraceException(objectDisposedException);
                return false;
            }

            int targetCounterId;
            if (base.RetrieveTargetCounterIdIfValid(counterId, isNumerator, out targetCounterId))
            {
                CounterData targetCounterData = _CounterSetInstance.Counters[targetCounterId];

                if (targetCounterData != null)
                {
                    targetCounterData.Value = counterValue;
                    return true;
                }
                else
                {
                    InvalidOperationException invalidOperationException =
                        new InvalidOperationException(
                            string.Format(
                            CultureInfo.InvariantCulture,
                            "Lookup for counter corresponding to counter id {0} failed",
                            counterId));
                    _tracer.TraceException(invalidOperationException);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        
        public override bool SetCounterValue(string counterName, long counterValue, bool isNumerator)
        {
            if (_Disposed)
            {
                ObjectDisposedException objectDisposedException =
                    new ObjectDisposedException("PSCounterSetInstance");
                _tracer.TraceException(objectDisposedException);
                return false;
            }

            // retrieve counter id associated with the counter name
            if (counterName == null)
            {
                ArgumentNullException argNullException = new ArgumentNullException(nameof(counterName));
                _tracer.TraceException(argNullException);
                return false;
            }

            try
            {
                int targetCounterId = this._counterNameToIdMapping[counterName];
                return this.SetCounterValue(targetCounterId, counterValue, isNumerator);
            }
            catch (KeyNotFoundException)
            {
                InvalidOperationException invalidOperationException =
                    new InvalidOperationException(
                    string.Format(
                    CultureInfo.InvariantCulture,
                    "Lookup for counter corresponding to counter name {0} failed",
                    counterName));
                _tracer.TraceException(invalidOperationException);
                return false;
            }
        }

        
        public override bool GetCounterValue(int counterId, bool isNumerator, out long counterValue)
        {
            counterValue = -1;
            if (_Disposed)
            {
                ObjectDisposedException objectDisposedException =
                    new ObjectDisposedException("PSCounterSetInstance");
                _tracer.TraceException(objectDisposedException);
                return false;
            }

            int targetCounterId;
            if (base.RetrieveTargetCounterIdIfValid(counterId, isNumerator, out targetCounterId))
            {
                CounterData targetCounterData = _CounterSetInstance.Counters[targetCounterId];

                if (targetCounterData != null)
                {
                    counterValue = targetCounterData.Value;
                    return true;
                }
                else
                {
                    InvalidOperationException invalidOperationException =
                        new InvalidOperationException(
                            string.Format(
                            CultureInfo.InvariantCulture,
                            "Lookup for counter corresponding to counter id {0} failed",
                            counterId));
                    _tracer.TraceException(invalidOperationException);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        
        public override bool GetCounterValue(string counterName, bool isNumerator, out long counterValue)
        {
            counterValue = -1;
            if (_Disposed)
            {
                ObjectDisposedException objectDisposedException =
                    new ObjectDisposedException("PSCounterSetInstance");
                _tracer.TraceException(objectDisposedException);
                return false;
            }

            // retrieve counter id associated with the counter name
            if (counterName == null)
            {
                ArgumentNullException argNullException = new ArgumentNullException(nameof(counterName));
                _tracer.TraceException(argNullException);
                return false;
            }

            try
            {
                int targetCounterId = this._counterNameToIdMapping[counterName];
                return this.GetCounterValue(targetCounterId, isNumerator, out counterValue);
            }
            catch (KeyNotFoundException)
            {
                InvalidOperationException invalidOperationException =
                    new InvalidOperationException(
                        string.Format(
                        CultureInfo.InvariantCulture,
                        "Lookup for counter corresponding to counter name {0} failed",
                        counterName));
                _tracer.TraceException(invalidOperationException);
                return false;
            }
        }

        #endregion
        #endregion
    }
}
