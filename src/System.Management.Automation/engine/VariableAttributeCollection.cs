// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    
    internal class PSVariableAttributeCollection : Collection<Attribute>
    {
        #region constructor

        
        internal PSVariableAttributeCollection(PSVariable variable)
        {
            if (variable == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(variable));
            }

            _variable = variable;
        }
        #endregion constructor

        #region Collection overrides

        
        protected override void InsertItem(int index, Attribute item)
        {
            object variableValue = VerifyNewAttribute(item);

            base.InsertItem(index, item);

            _variable.SetValueRaw(variableValue, true);
        }

        
        protected override void SetItem(int index, Attribute item)
        {
            object variableValue = VerifyNewAttribute(item);

            base.SetItem(index, item);

            _variable.SetValueRaw(variableValue, true);
        }
        #endregion Collection overrides

        #region private data

        
        internal void AddAttributeNoCheck(Attribute item)
        {
            base.InsertItem(this.Count, item);
        }

        
        private object VerifyNewAttribute(Attribute item)
        {
            object variableValue = _variable.Value;

            // Perform transformation before validating
            ArgumentTransformationAttribute argumentTransformation = item as ArgumentTransformationAttribute;
            if (argumentTransformation != null)
            {
                // Get an EngineIntrinsics instance using the context of the thread.

                ExecutionContext context = Runspaces.LocalPipeline.GetExecutionContextFromTLS();
                EngineIntrinsics engine = null;

                if (context != null)
                {
                    engine = context.EngineIntrinsics;
                }

                variableValue = argumentTransformation.TransformInternal(engine, variableValue);
            }

            if (!PSVariable.IsValidValue(variableValue, item))
            {
                ValidationMetadataException e = new ValidationMetadataException(
                    "ValidateSetFailure",
                    null,
                    Metadata.InvalidMetadataForCurrentValue,
                    _variable.Name,
                    ((_variable.Value != null) ? _variable.Value.ToString() : string.Empty));

                throw e;
            }

            return variableValue;
        }

        
        private readonly PSVariable _variable;
        #endregion private data
    }
}
