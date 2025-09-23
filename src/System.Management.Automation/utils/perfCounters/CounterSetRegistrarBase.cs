// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.PerformanceData;

namespace System.Management.Automation.PerformanceData
{
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
    public struct CounterInfo
    {
        #region Private Members

        #endregion

        #region Constructors
        
        public CounterInfo(int id, CounterType type, string name)
        {
            Id = id;
            Type = type;
            Name = name;
        }

        
        public CounterInfo(int id, CounterType type)
        {
            Id = id;
            Type = type;
            Name = null;
        }
        #endregion

        #region Properties
        
        public string Name { get; }

        
        public int Id { get; }

        
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public CounterType Type { get; }

        #endregion
    }

    
    public abstract class CounterSetRegistrarBase
    {
        #region Private Members

        #endregion

        #region Protected Members
        
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        protected CounterSetInstanceBase _counterSetInstanceBase;

        
        protected abstract CounterSetInstanceBase CreateCounterSetInstance();

        #endregion

        #region Constructors
        
        protected CounterSetRegistrarBase(
            Guid providerId,
            Guid counterSetId,
            CounterSetInstanceType counterSetInstType,
            CounterInfo[] counterInfoArray,
            string counterSetName = null)
        {
            ProviderId = providerId;
            CounterSetId = counterSetId;
            CounterSetInstType = counterSetInstType;
            CounterSetName = counterSetName;

            if (counterInfoArray is null || counterInfoArray.Length == 0)
            {
                throw new ArgumentNullException(nameof(counterInfoArray));
            }

            CounterInfoArray = new CounterInfo[counterInfoArray.Length];

            for (int i = 0; i < counterInfoArray.Length; i++)
            {
                CounterInfoArray[i] =
                    new CounterInfo(
                        counterInfoArray[i].Id,
                        counterInfoArray[i].Type,
                        counterInfoArray[i].Name
                        );
            }

            this._counterSetInstanceBase = null;
        }

        
        protected CounterSetRegistrarBase(
            CounterSetRegistrarBase srcCounterSetRegistrarBase)
        {
            ArgumentNullException.ThrowIfNull(srcCounterSetRegistrarBase);

            ProviderId = srcCounterSetRegistrarBase.ProviderId;
            CounterSetId = srcCounterSetRegistrarBase.CounterSetId;
            CounterSetInstType = srcCounterSetRegistrarBase.CounterSetInstType;
            CounterSetName = srcCounterSetRegistrarBase.CounterSetName;

            CounterInfo[] counterInfoArrayRef = srcCounterSetRegistrarBase.CounterInfoArray;
            CounterInfoArray = new CounterInfo[counterInfoArrayRef.Length];

            for (int i = 0; i < counterInfoArrayRef.Length; i++)
            {
                CounterInfoArray[i] =
                    new CounterInfo(
                        counterInfoArrayRef[i].Id,
                        counterInfoArrayRef[i].Type,
                        counterInfoArrayRef[i].Name);
            }
        }
        #endregion

        #region Properties

        
        public Guid ProviderId { get; }

        
        public Guid CounterSetId { get; }

        
        public string CounterSetName { get; }

        
        public CounterSetInstanceType CounterSetInstType { get; }

        
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public CounterInfo[] CounterInfoArray { get; }

        
        public CounterSetInstanceBase CounterSetInstance
        {
            get { return _counterSetInstanceBase ?? (_counterSetInstanceBase = CreateCounterSetInstance()); }
        }

        #endregion

        #region Public Methods

        
        public abstract void DisposeCounterSetInstance();

        #endregion
    }

    
    public class PSCounterSetRegistrar : CounterSetRegistrarBase
    {
        #region Constructors
        
        public PSCounterSetRegistrar(
            Guid providerId,
            Guid counterSetId,
            CounterSetInstanceType counterSetInstType,
            CounterInfo[] counterInfoArray,
            string counterSetName = null)
            : base(providerId, counterSetId, counterSetInstType, counterInfoArray, counterSetName)
        {
        }

        
        public PSCounterSetRegistrar(
            PSCounterSetRegistrar srcPSCounterSetRegistrar)
            : base(srcPSCounterSetRegistrar)
        {
            ArgumentNullException.ThrowIfNull(srcPSCounterSetRegistrar);
        }

        #endregion

        #region CounterSetRegistrarBase Overrides

        #region Protected Methods

        
        protected override CounterSetInstanceBase CreateCounterSetInstance()
        {
            return new PSCounterSetInstance(this);
        }

        #endregion

        #region Public Methods
        
        public override void DisposeCounterSetInstance()
        {
            base._counterSetInstanceBase.Dispose();
        }

        #endregion

        #endregion
    }
}
