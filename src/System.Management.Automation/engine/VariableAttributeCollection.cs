// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    
    internal class PSVariableAttributeCollection : Collection<Attribute>
    {
        #region constructor

        
        /// <param name="variable">
        /// The variable that needs to be verified anytime an attribute
        /// changes.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="variable"/> is null.
        /// </exception>
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

        
        /// <param name="index">
        /// The zero-based index at which <paramref name="item"/> should be inserted.
        /// </param>
        /// <param name="item">
        /// The attribute being added to the collection.
        /// </param>
        /// <exception cref="ValidationMetadataException">
        /// If the new attribute causes the variable to be in an invalid state.
        /// </exception>
        /// <exception cref="ArgumentTransformationMetadataException">
        /// If the new attribute is an ArgumentTransformationAttribute and the transformation
        /// fails.
        /// </exception>
        protected override void InsertItem(int index, Attribute item)
        {
            object variableValue = VerifyNewAttribute(item);

            base.InsertItem(index, item);

            _variable.SetValueRaw(variableValue, true);
        }

        
        /// <param name="index">
        /// The zero-based index at which <paramref name="item"/> should be set.
        /// </param>
        /// <param name="item">
        /// The attribute being set in the collection.
        /// </param>
        /// <exception cref="ValidationMetadataException">
        /// If the new attribute causes the variable to be in an invalid state.
        /// </exception>
        protected override void SetItem(int index, Attribute item)
        {
            object variableValue = VerifyNewAttribute(item);

            base.SetItem(index, item);

            _variable.SetValueRaw(variableValue, true);
        }
        #endregion Collection overrides

        #region private data

        
        /// <param name="item">The attribute to add.</param>
        internal void AddAttributeNoCheck(Attribute item)
        {
            base.InsertItem(this.Count, item);
        }

        
        /// <param name="item">
        /// The new attribute to be added to the collection.
        /// </param>
        /// <returns>
        /// The new variable value. This may change from the original value if the
        /// new attribute is an ArgumentTransformationAttribute.
        /// </returns>
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
