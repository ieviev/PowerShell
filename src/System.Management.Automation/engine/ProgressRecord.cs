// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    
    [DataContract]
    public
    class ProgressRecord
    {
        #region Public API

        
        public
        ProgressRecord(int activityId, string activity, string statusDescription)
        {
            if (activityId < 0)
            {
                // negative Ids are reserved to indicate "no id" for parent Ids.

                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(activityId), activityId, ProgressRecordStrings.ArgMayNotBeNegative, "activityId");
            }

            if (string.IsNullOrEmpty(activity))
            {
                throw PSTraceSource.NewArgumentException(nameof(activity), ProgressRecordStrings.ArgMayNotBeNullOrEmpty, "activity");
            }

            if (string.IsNullOrEmpty(statusDescription))
            {
                throw PSTraceSource.NewArgumentException(nameof(activity), ProgressRecordStrings.ArgMayNotBeNullOrEmpty, "statusDescription");
            }

            this.id = activityId;
            this.activity = activity;
            this.status = statusDescription;
        }

        
        public
        ProgressRecord(int activityId)
        {
            if (activityId < 0)
            {
                // negative Ids are reserved to indicate "no id" for parent Ids.

                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(activityId), activityId, ProgressRecordStrings.ArgMayNotBeNegative, "activityId");
            }

            this.id = activityId;
        }

        
        internal ProgressRecord(ProgressRecord other)
        {
            this.activity = other.activity;
            this.currentOperation = other.currentOperation;
            this.id = other.id;
            this.parentId = other.parentId;
            this.percent = other.percent;
            this.secondsRemaining = other.secondsRemaining;
            this.status = other.status;
            this.type = other.type;
        }

        
        public
        int
        ActivityId
        {
            get
            {
                return id;
            }
        }

        
        public
        int
        ParentActivityId
        {
            get
            {
                return parentId;
            }

            set
            {
                if (value == ActivityId)
                {
                    throw PSTraceSource.NewArgumentException("value", ProgressRecordStrings.ParentActivityIdCantBeActivityId);
                }

                parentId = value;
            }
        }

        
        public
        string
        Activity
        {
            get
            {
                return activity;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw PSTraceSource.NewArgumentException("value", ProgressRecordStrings.ArgMayNotBeNullOrEmpty, "value");
                }

                activity = value;
            }
        }

        
        public
        string
        StatusDescription
        {
            get
            {
                return status;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw PSTraceSource.NewArgumentException("value", ProgressRecordStrings.ArgMayNotBeNullOrEmpty, "value");
                }

                status = value;
            }
        }

        
        public
        string
        CurrentOperation
        {
            get
            {
                return currentOperation;
            }

            set
            {
                // null or empty string is allowed

                currentOperation = value;
            }
        }

        
        public
        int
        PercentComplete
        {
            get
            {
                return percent;
            }

            set
            {
                // negative values are allowed

                if (value > 100)
                {
                    throw
                        PSTraceSource.NewArgumentOutOfRangeException(
                            "value", value, ProgressRecordStrings.PercentMayNotBeMoreThan100, "PercentComplete");
                }

                percent = value;
            }
        }

        
        public
        int
        SecondsRemaining
        {
            get
            {
                return secondsRemaining;
            }

            set
            {
                // negative values are allowed

                secondsRemaining = value;
            }
        }

        
        public
        ProgressRecordType
        RecordType
        {
            get
            {
                return type;
            }

            set
            {
                if (value != ProgressRecordType.Completed && value != ProgressRecordType.Processing)
                {
                    throw PSTraceSource.NewArgumentException("value");
                }

                type = value;
            }
        }

        
        public override
        string
        ToString()
        {
            return
                string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    "parent = {0} id = {1} act = {2} stat = {3} cur = {4} pct = {5} sec = {6} type = {7}",
                    parentId,
                    id,
                    activity,
                    status,
                    currentOperation,
                    percent,
                    secondsRemaining,
                    type);
        }

        #endregion

        #region Helper methods

        internal static int? GetSecondsRemaining(DateTime startTime, double percentageComplete)
        {
            Dbg.Assert(percentageComplete >= 0.0, "Caller should verify percentageComplete >= 0.0");
            Dbg.Assert(percentageComplete <= 1.0, "Caller should verify percentageComplete <= 1.0");
            Dbg.Assert(
                startTime.Kind == DateTimeKind.Utc,
                "DateTime arithmetic should always be done in utc mode [to avoid problems when some operands are calculated right before and right after switching to /from a daylight saving time");

            if ((percentageComplete < 0.00001) || double.IsNaN(percentageComplete))
            {
                return null;
            }

            DateTime now = DateTime.UtcNow;
            Dbg.Assert(startTime <= now, "Caller should pass a valid startTime");
            TimeSpan elapsedTime = now - startTime;

            TimeSpan totalTime;
            try
            {
                totalTime = TimeSpan.FromMilliseconds(elapsedTime.TotalMilliseconds / percentageComplete);
            }
            catch (OverflowException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }

            TimeSpan remainingTime = totalTime - elapsedTime;

            return (int)(remainingTime.TotalSeconds);
        }

        
        internal static int GetPercentageComplete(DateTime startTime, TimeSpan expectedDuration)
        {
            DateTime now = DateTime.UtcNow;

            Dbg.Assert(
                startTime.Kind == DateTimeKind.Utc,
                "DateTime arithmetic should always be done in utc mode [to avoid problems when some operands are calculated right before and right after switching to /from a daylight saving time");

            ArgumentOutOfRangeException.ThrowIfGreaterThan(startTime, now);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expectedDuration, TimeSpan.Zero);

            
            TimeSpan timeElapsed = now - startTime;
            double b = expectedDuration.TotalSeconds / 9.0;
            double a = 100.0 * b;
            double percentageRemaining = a / (timeElapsed.TotalSeconds + b);
            double percentageCompleted = 100.0 - percentageRemaining;

            return (int)Math.Floor(percentageCompleted);
        }

        #endregion

        #region DO NOT REMOVE OR RENAME THESE FIELDS - it will break remoting compatibility with Windows PowerShell

        [DataMember]
        private readonly int id;

        [DataMember]
        private int parentId = -1;

        [DataMember]
        private string activity;

        [DataMember]
        private string status;

        [DataMember]
        private string currentOperation;

        [DataMember]
        private int percent = -1;

        [DataMember]
        private int secondsRemaining = -1;

        [DataMember]
        private ProgressRecordType type = ProgressRecordType.Processing;

        #endregion

        #region Serialization / deserialization for remoting

        
        internal static ProgressRecord FromPSObjectForRemoting(PSObject progressAsPSObject)
        {
            if (progressAsPSObject == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(progressAsPSObject));
            }

            string activity = RemotingDecoder.GetPropertyValue<string>(progressAsPSObject, RemoteDataNameStrings.ProgressRecord_Activity);
            int activityId = RemotingDecoder.GetPropertyValue<int>(progressAsPSObject, RemoteDataNameStrings.ProgressRecord_ActivityId);
            string statusDescription = RemotingDecoder.GetPropertyValue<string>(progressAsPSObject, RemoteDataNameStrings.ProgressRecord_StatusDescription);

            ProgressRecord result = new ProgressRecord(activityId, activity, statusDescription);

            result.CurrentOperation = RemotingDecoder.GetPropertyValue<string>(progressAsPSObject, RemoteDataNameStrings.ProgressRecord_CurrentOperation);
            result.ParentActivityId = RemotingDecoder.GetPropertyValue<int>(progressAsPSObject, RemoteDataNameStrings.ProgressRecord_ParentActivityId);
            result.PercentComplete = RemotingDecoder.GetPropertyValue<int>(progressAsPSObject, RemoteDataNameStrings.ProgressRecord_PercentComplete);
            result.RecordType = RemotingDecoder.GetPropertyValue<ProgressRecordType>(progressAsPSObject, RemoteDataNameStrings.ProgressRecord_Type);
            result.SecondsRemaining = RemotingDecoder.GetPropertyValue<int>(progressAsPSObject, RemoteDataNameStrings.ProgressRecord_SecondsRemaining);

            return result;
        }

        
        internal PSObject ToPSObjectForRemoting()
        {
            // Activity used to be mandatory but that's no longer the case.
            // We ensure the string has a value to maintain compatibility with older versions.
            string activity = string.IsNullOrEmpty(Activity) ? " " : Activity;

            PSObject progressAsPSObject = RemotingEncoder.CreateEmptyPSObject();

            progressAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ProgressRecord_Activity, activity));
            progressAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ProgressRecord_ActivityId, this.ActivityId));
            progressAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ProgressRecord_StatusDescription, this.StatusDescription));

            progressAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ProgressRecord_CurrentOperation, this.CurrentOperation));
            progressAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ProgressRecord_ParentActivityId, this.ParentActivityId));
            progressAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ProgressRecord_PercentComplete, this.PercentComplete));
            progressAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ProgressRecord_Type, this.RecordType));
            progressAsPSObject.Properties.Add(new PSNoteProperty(RemoteDataNameStrings.ProgressRecord_SecondsRemaining, this.SecondsRemaining));

            return progressAsPSObject;
        }

        #endregion
    }

    
    public
    enum ProgressRecordType
    {
        
        Processing,

        
        Completed
    }
}
