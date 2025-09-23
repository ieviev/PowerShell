// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Management.Automation.Language;

namespace System.Management.Automation
{
    
    [DebuggerDisplay("{ParameterName}")]
    internal sealed class CommandParameterInternal
    {
        private sealed class Parameter
        {
            internal Ast ast;
            internal string parameterName;
            internal string parameterText;
        }

        private sealed class Argument
        {
            internal Ast ast;
            internal object value;
            internal bool splatted;
        }

        private Parameter _parameter;
        private Argument _argument;
        private bool _spaceAfterParameter;
        private bool _fromHashtableSplatting;

        internal bool SpaceAfterParameter => _spaceAfterParameter;

        internal bool ParameterNameSpecified => _parameter != null;

        internal bool ArgumentSpecified => _argument != null;

        internal bool ParameterAndArgumentSpecified => ParameterNameSpecified && ArgumentSpecified;

        internal bool FromHashtableSplatting => _fromHashtableSplatting;

        
        internal string ParameterName
        {
            get
            {
                Diagnostics.Assert(ParameterNameSpecified, "Caller must verify parameter name was specified");
                return _parameter.parameterName;
            }

            set
            {
                Diagnostics.Assert(ParameterNameSpecified, "Caller must verify parameter name was specified");
                _parameter.parameterName = value;
            }
        }

        
        internal string ParameterText
        {
            get
            {
                Diagnostics.Assert(ParameterNameSpecified, "Caller must verify parameter name was specified");
                return _parameter.parameterText;
            }
        }

        
        internal Ast ParameterAst
        {
            get => _parameter?.ast;
        }

        
        internal IScriptExtent ParameterExtent
        {
            get => ParameterAst?.Extent ?? PositionUtilities.EmptyExtent;
        }

        
        internal Ast ArgumentAst
        {
            get => _argument?.ast;
        }

        
        internal IScriptExtent ArgumentExtent
        {
            get => ArgumentAst?.Extent ?? PositionUtilities.EmptyExtent;
        }

        
        internal object ArgumentValue
        {
            get { return _argument != null ? _argument.value : UnboundParameter.Value; }
        }

        
        internal bool ArgumentToBeSplatted
        {
            get { return _argument != null && _argument.splatted; }
        }

        
        internal void SetArgumentValue(Ast ast, object value)
        {
            _argument ??= new Argument();

            _argument.value = value;
            _argument.ast = ast;
        }

        
        internal IScriptExtent ErrorExtent
        {
            get
            {
                var argExtent = ArgumentExtent;
                return argExtent != PositionUtilities.EmptyExtent ? argExtent : ParameterExtent;
            }
        }

        #region ctor

        
        /// <param name="ast">The ast in script of the parameter.</param>
        /// <param name="parameterName">The parameter name (with no leading dash).</param>
        /// <param name="parameterText">The text of the parameter, as it did, or would, appear in script.</param>
        internal static CommandParameterInternal CreateParameter(
            string parameterName,
            string parameterText,
            Ast ast = null)
        {
            return new CommandParameterInternal
            {
                _parameter =
                           new Parameter { ast = ast, parameterName = parameterName, parameterText = parameterText }
            };
        }

        
        /// <param name="value">The argument value.</param>
        /// <param name="ast">The ast of the argument value in the script.</param>
        /// <param name="splatted">True if the argument value is to be splatted, false otherwise.</param>
        internal static CommandParameterInternal CreateArgument(
            object value,
            Ast ast = null,
            bool splatted = false)
        {
            return new CommandParameterInternal
            {
                _argument = new Argument
                {
                    value = value,
                    ast = ast,
                    splatted = splatted,
                }
            };
        }

        
        /// <param name="parameterAst">The ast in script of the parameter.</param>
        /// <param name="parameterName">The parameter name (with no leading dash).</param>
        /// <param name="parameterText">The text of the parameter, as it did, or would, appear in script.</param>
        /// <param name="argumentAst">The ast of the argument value in the script.</param>
        /// <param name="value">The argument value.</param>
        /// <param name="spaceAfterParameter">Used in native commands to correctly handle -foo:bar vs. -foo: bar.</param>
        /// <param name="fromSplatting">Indicate if this parameter-argument pair comes from splatting.</param>
        internal static CommandParameterInternal CreateParameterWithArgument(
            Ast parameterAst,
            string parameterName,
            string parameterText,
            Ast argumentAst,
            object value,
            bool spaceAfterParameter,
            bool fromSplatting = false)
        {
            return new CommandParameterInternal
            {
                _parameter = new Parameter { ast = parameterAst, parameterName = parameterName, parameterText = parameterText },
                _argument = new Argument { ast = argumentAst, value = value },
                _spaceAfterParameter = spaceAfterParameter,
                _fromHashtableSplatting = fromSplatting,
            };
        }

        #endregion ctor

        internal bool IsDashQuestion()
        {
            return ParameterNameSpecified && (ParameterName.Equals("?", StringComparison.OrdinalIgnoreCase));
        }
    }
}
