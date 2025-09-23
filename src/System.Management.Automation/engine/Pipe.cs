// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Management.Automation.Runspaces;

namespace System.Management.Automation.Internal
{
    
    internal enum VariableStreamKind
    {
        Output,
        Error,
        Warning,
        Information
    }

    
    internal class Pipe
    {
        private readonly ExecutionContext _context;

        // If a pipeline object has been added, then
        // write objects to it, stepping one at a time...
        internal PipelineProcessor PipelineProcessor { get; }

        
        internal CommandProcessorBase DownstreamCmdlet
        {
            get
            {
                return _downstreamCmdlet;
            }

            set
            {
                Diagnostics.Assert(_resultList == null, "Tried to set downstream cmdlet when _resultList not null");
                _downstreamCmdlet = value;
            }
        }

        private CommandProcessorBase _downstreamCmdlet;

        
        internal PipelineReader<object> ExternalReader { get; set; }

        
        internal PipelineWriter ExternalWriter
        {
            get
            {
                return _externalWriter;
            }

            set
            {
                Diagnostics.Assert(_resultList == null, "Tried to set Pipe ExternalWriter when resultList not null");
                _externalWriter = value;
            }
        }

        private PipelineWriter _externalWriter;

        
        public override string ToString()
        {
            if (_downstreamCmdlet != null)
                return _downstreamCmdlet.ToString();
            return base.ToString();
        }

        
        internal int OutBufferCount { get; set; } = 0;

        
        internal bool IgnoreOutVariableList { get; set; }

        
        internal bool NullPipe
        {
            get
            {
                return _nullPipe;
            }

            set
            {
                _isRedirected = true;
                _nullPipe = value;
            }
        }

        private bool _nullPipe;

        
        internal Queue<object> ObjectQueue { get; }

        
        internal bool Empty
        {
            get
            {
                if (_enumeratorToProcess != null)
                    return _enumeratorToProcessIsEmpty;

                if (ObjectQueue != null)
                    return ObjectQueue.Count == 0;
                return true;
            }
        }

        
        internal bool IsRedirected
        {
            get { return _downstreamCmdlet != null || _isRedirected; }
        }

        private bool _isRedirected;

        
        private List<IList> _outVariableList;

        
        private List<IList> _errorVariableList;

        
        private List<IList> _warningVariableList;

        
        private List<IList> _informationVariableList;

        
        private PSVariable _pipelineVariableObject;

        private static void AddToVarList(List<IList> varList, object obj)
        {
            if (varList != null && varList.Count > 0)
            {
                for (int i = 0; i < varList.Count; i++)
                {
                    varList[i].Add(obj);
                }
            }
        }

        internal void AppendVariableList(VariableStreamKind kind, object obj)
        {
            switch (kind)
            {
                case VariableStreamKind.Error:
                    AddToVarList(_errorVariableList, obj);
                    break;
                case VariableStreamKind.Warning:
                    AddToVarList(_warningVariableList, obj);
                    break;
                case VariableStreamKind.Output:
                    AddToVarList(_outVariableList, obj);
                    break;
                case VariableStreamKind.Information:
                    AddToVarList(_informationVariableList, obj);
                    break;
            }
        }

        internal void AddVariableList(VariableStreamKind kind, IList list)
        {
            switch (kind)
            {
                case VariableStreamKind.Error:
                    _errorVariableList ??= new List<IList>();

                    _errorVariableList.Add(list);
                    break;
                case VariableStreamKind.Warning:
                    _warningVariableList ??= new List<IList>();

                    _warningVariableList.Add(list);
                    break;
                case VariableStreamKind.Output:
                    _outVariableList ??= new List<IList>();

                    _outVariableList.Add(list);
                    break;
                case VariableStreamKind.Information:
                    _informationVariableList ??= new List<IList>();

                    _informationVariableList.Add(list);
                    break;
            }
        }

        internal void SetPipelineVariable(PSVariable pipelineVariable)
        {
            _pipelineVariableObject = pipelineVariable;
        }

        internal void RemoveVariableList(VariableStreamKind kind, IList list)
        {
            switch (kind)
            {
                case VariableStreamKind.Error:
                    _errorVariableList.Remove(list);
                    break;
                case VariableStreamKind.Warning:
                    _warningVariableList.Remove(list);
                    break;
                case VariableStreamKind.Output:
                    _outVariableList.Remove(list);
                    break;
                case VariableStreamKind.Information:
                    _informationVariableList.Remove(list);
                    break;
            }
        }

        internal void RemovePipelineVariable()
        {
            if (_pipelineVariableObject != null)
            {
                _pipelineVariableObject.Value = null;
                _pipelineVariableObject = null;
            }
        }

        
        internal void SetVariableListForTemporaryPipe(Pipe tempPipe)
        {
            CopyVariableToTempPipe(VariableStreamKind.Error, _errorVariableList, tempPipe);
            CopyVariableToTempPipe(VariableStreamKind.Warning, _warningVariableList, tempPipe);
            CopyVariableToTempPipe(VariableStreamKind.Information, _informationVariableList, tempPipe);
        }

        private static void CopyVariableToTempPipe(VariableStreamKind streamKind, List<IList> variableList, Pipe tempPipe)
        {
            if (variableList != null && variableList.Count > 0)
            {
                for (int i = 0; i < variableList.Count; i++)
                {
                    tempPipe.AddVariableList(streamKind, variableList[i]);
                }
            }
        }

        #region ctor

        
        internal Pipe()
        {
            ObjectQueue = new Queue<object>();
        }

        
        internal Pipe(List<object> resultList)
        {
            Diagnostics.Assert(resultList != null, "resultList cannot be null");
            _isRedirected = true;
            _resultList = resultList;
        }

        private readonly List<object> _resultList;

        
        internal Pipe(System.Collections.ObjectModel.Collection<PSObject> resultCollection)
        {
            Diagnostics.Assert(resultCollection != null, "resultCollection cannot be null");
            _isRedirected = true;
            _resultCollection = resultCollection;
        }

        private readonly System.Collections.ObjectModel.Collection<PSObject> _resultCollection;

        
        internal Pipe(ExecutionContext context, PipelineProcessor outputPipeline)
        {
            Diagnostics.Assert(outputPipeline != null, "outputPipeline cannot be null");
            Diagnostics.Assert(outputPipeline != null, "context cannot be null");
            _isRedirected = true;
            _context = context;
            PipelineProcessor = outputPipeline;
        }

        
        internal Pipe(IEnumerator enumeratorToProcess)
        {
            Diagnostics.Assert(enumeratorToProcess != null, "enumeratorToProcess cannot be null");
            _enumeratorToProcess = enumeratorToProcess;

            // since there is an enumerator specified, we
            // assume that there is some stuff to read
            _enumeratorToProcessIsEmpty = false;
        }

        private readonly IEnumerator _enumeratorToProcess;
        private bool _enumeratorToProcessIsEmpty;

        #endregion ctor

        
        internal void Add(object obj)
        {
            if (obj == AutomationNull.Value)
                return;

            // OutVariable is appended for null pipes so that the following works:
            //     foo -OutVariable bar > $null
            AddToVarList(_outVariableList, obj);

            if (_nullPipe)
                return;

            // Store the current pipeline variable
            if (_pipelineVariableObject != null)
            {
                _pipelineVariableObject.Value = obj;
            }

            AddToPipe(obj);
        }

        internal void AddWithoutAppendingOutVarList(object obj)
        {
            if (obj == AutomationNull.Value || _nullPipe)
                return;

            AddToPipe(obj);
        }

        private void AddToPipe(object obj)
        {
            if (PipelineProcessor != null)
            {
                // Put the pipeline on the notification stack for stop.
                _context.PushPipelineProcessor(PipelineProcessor);
                PipelineProcessor.Step(obj);
                _context.PopPipelineProcessor(false);
            }
            else if (_resultCollection != null)
            {
                _resultCollection.Add(obj != null ? PSObject.AsPSObject(obj) : null);
            }
            else if (_resultList != null)
            {
                _resultList.Add(obj);
            }
            else if (_externalWriter != null)
            {
                _externalWriter.Write(obj);
            }
            else if (ObjectQueue != null)
            {
                ObjectQueue.Enqueue(obj);

                // This is the "streamlet" recursive call
                if (_downstreamCmdlet != null && ObjectQueue.Count > OutBufferCount)
                {
                    _downstreamCmdlet.DoExecute();
                }
            }
        }

        
        internal void AddItems(object objects)
        {
            // Use the extended type system to try and get an enumerator for the object being added.
            // If we get an enumerator, then add the individual elements. If the object isn't
            // enumerable (i.e. the call returned null) then add the object to the pipe
            // as a single element.
            IEnumerator ie = LanguagePrimitives.GetEnumerator(objects);
            try
            {
                if (ie == null)
                {
                    Add(objects);
                }
                else
                {
                    while (ParserOps.MoveNext(_context, null, ie))
                    {
                        object o = ParserOps.Current(null, ie);

                        // Slip over any instance of AutomationNull.Value in the pipeline...
                        if (o == AutomationNull.Value)
                        {
                            continue;
                        }

                        Add(o);
                    }
                }
            }
            finally
            {
                // If our object came from GetEnumerator (and hence is not IEnumerator), then we need to dispose
                // Otherwise, we don't own the object, so don't dispose.
                var disposable = ie as IDisposable;
                if (disposable != null && objects is not IEnumerator)
                {
                    disposable.Dispose();
                }
            }

            if (_externalWriter != null)
                return;

            // If there are objects waiting for the downstream command
            // call it now
            if (_downstreamCmdlet != null && ObjectQueue != null && ObjectQueue.Count > OutBufferCount)
            {
                _downstreamCmdlet.DoExecute();
            }
        }

        
        internal object Retrieve()
        {
            if (ObjectQueue != null && ObjectQueue.Count != 0)
            {
                return ObjectQueue.Dequeue();
            }
            else if (_enumeratorToProcess != null)
            {
                if (_enumeratorToProcessIsEmpty)
                {
                    return AutomationNull.Value;
                }

                while (true)
                {
                    if (!ParserOps.MoveNext(_context, errorPosition: null, _enumeratorToProcess))
                    {
                        _enumeratorToProcessIsEmpty = true;
                        return AutomationNull.Value;
                    }

                    object retValue = ParserOps.Current(errorPosition: null, _enumeratorToProcess);
                    if (retValue == AutomationNull.Value)
                    {
                        // 'AutomationNull.Value' from the enumerator won't be sent to the pipeline.
                        // We try to get the next value in this case.
                        continue;
                    }

                    return retValue;
                }
            }
            else if (ExternalReader != null)
            {
                try
                {
                    object o = ExternalReader.Read();
                    if (AutomationNull.Value == o)
                    {
                        // NOTICE-2004/06/08-JonN 963367
                        // The fix to this bug involves making one last
                        // attempt to read from the pipeline in DoComplete.
                        // We should be sure to not hit the ExternalReader
                        // again if it already reported completion.
                        ExternalReader = null;
                    }

                    return o;
                }
                catch (PipelineClosedException)
                {
                    return AutomationNull.Value;
                }
                catch (ObjectDisposedException)
                {
                    return AutomationNull.Value;
                }
            }
            else
                return AutomationNull.Value;
        }

        
        internal void Clear() => ObjectQueue?.Clear();

        
        internal object[] ToArray()
        {
            if (ObjectQueue == null || ObjectQueue.Count == 0)
                return MshCommandRuntime.StaticEmptyArray;

            return ObjectQueue.ToArray();
        }
    }
}
