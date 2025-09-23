// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.PowerShell;
using Microsoft.PowerShell.Commands;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

namespace System.Management.Automation.Language
{
    using KeyValuePair = Tuple<ExpressionAst, StatementAst>;
    using IfClause = Tuple<PipelineBaseAst, StatementBlockAst>;
    using SwitchClause = Tuple<ExpressionAst, StatementBlockAst>;
    using System.Runtime.CompilerServices;
    using System.Reflection.Emit;

#nullable enable

    internal interface ISupportsAssignment
    {
        IAssignableValue GetAssignableValue();
    }

    internal interface IAssignableValue
    {
        
        Expression? GetValue(Compiler compiler, List<Expression> exprs, List<ParameterExpression> temps);

        
        Expression SetValue(Compiler compiler, Expression rhs);
    }
#nullable restore

    internal interface IParameterMetadataProvider
    {
        bool HasAnyScriptBlockAttributes();

        RuntimeDefinedParameterDictionary GetParameterMetadata(bool automaticPositions, ref bool usesCmdletBinding);

        IEnumerable<Attribute> GetScriptBlockAttributes();

        IEnumerable<ExperimentalAttribute> GetExperimentalAttributes();

        bool UsesCmdletBinding();

        ReadOnlyCollection<ParameterAst> Parameters { get; }

        ScriptBlockAst Body { get; }

        #region Remoting/Invoke Command

        PowerShell GetPowerShell(ExecutionContext context, Dictionary<string, object> variables, bool isTrustedInput,
            bool filterNonUsingVariables, bool? createLocalScope, params object[] args);

        string GetWithInputHandlingForInvokeCommand();

        
        Tuple<string, string> GetWithInputHandlingForInvokeCommandWithUsingExpression(Tuple<List<VariableExpressionAst>, string> usingVariablesTuple);

        #endregion Remoting/Invoke Command
    }

    
    [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
    public abstract class Ast
    {
        
        protected Ast(IScriptExtent extent)
        {
            if (extent == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(extent));
            }

            this.Extent = extent;
        }

        
        public IScriptExtent Extent { get; }

        
        public Ast Parent { get; private set; }

        
        public object Visit(ICustomAstVisitor astVisitor)
        {
            if (astVisitor == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(astVisitor));
            }

            return this.Accept(astVisitor);
        }

        
        public void Visit(AstVisitor astVisitor)
        {
            if (astVisitor == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(astVisitor));
            }

            this.InternalVisit(astVisitor);
        }

        
        public IEnumerable<Ast> FindAll(Func<Ast, bool> predicate, bool searchNestedScriptBlocks)
        {
            if (predicate == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(predicate));
            }

            return AstSearcher.FindAll(this, predicate, searchNestedScriptBlocks);
        }

        
        public Ast Find(Func<Ast, bool> predicate, bool searchNestedScriptBlocks)
        {
            if (predicate == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(predicate));
            }

            return AstSearcher.FindFirst(this, predicate, searchNestedScriptBlocks);
        }

        
        public override string ToString()
        {
            return Extent.Text;
        }

        
        public abstract Ast Copy();

        
        public object SafeGetValue()
        {
            return SafeGetValue(skipHashtableSizeCheck: false);
        }

        
        public object SafeGetValue(bool skipHashtableSizeCheck)
        {
            try
            {
                ExecutionContext context = null;
                if (System.Management.Automation.Runspaces.Runspace.DefaultRunspace != null)
                {
                    context = System.Management.Automation.Runspaces.Runspace.DefaultRunspace.ExecutionContext;
                }

                return GetSafeValueVisitor.GetSafeValue(this, context, skipHashtableSizeCheck ? GetSafeValueVisitor.SafeValueContext.SkipHashtableSizeCheck : GetSafeValueVisitor.SafeValueContext.Default);
            }
            catch
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, AutomationExceptions.CantConvertWithDynamicExpression, this.Extent.Text));
            }
        }

        
        internal static T[] CopyElements<T>(ReadOnlyCollection<T> elements) where T : Ast
        {
            if (elements == null || elements.Count == 0) { return null; }

            var result = new T[elements.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (T)elements[i].Copy();
            }

            return result;
        }

        
        internal static T CopyElement<T>(T element) where T : Ast
        {
            if (element == null) { return null; }

            return (T)element.Copy();
        }

        // Should be protected AND internal, but C# doesn't support that.
        internal void SetParents<T>(ReadOnlyCollection<T> children)
            where T : Ast
        {
            for (int index = 0; index < children.Count; index++)
            {
                var child = children[index];
                SetParent(child);
            }
        }

        // Should be protected AND internal, but C# doesn't support that.
        internal void SetParents<T1, T2>(ReadOnlyCollection<Tuple<T1, T2>> children)
            where T1 : Ast
            where T2 : Ast
        {
            for (int index = 0; index < children.Count; index++)
            {
                var child = children[index];
                SetParent(child.Item1);
                SetParent(child.Item2);
            }
        }

        // Should be protected AND internal, but C# doesn't support that.
        internal void SetParent(Ast child)
        {
            if (child.Parent != null)
            {
                throw new InvalidOperationException(ParserStrings.AstIsReused);
            }

            Diagnostics.Assert(child.Parent == null, "Parent can only be set once");
            child.Parent = this;
        }

        internal void ClearParent()
        {
            this.Parent = null;
        }

        internal abstract object Accept(ICustomAstVisitor visitor);

        internal abstract AstVisitAction InternalVisit(AstVisitor visitor);

        internal static readonly PSTypeName[] EmptyPSTypeNameArray = Array.Empty<PSTypeName>();

        internal bool IsInWorkflow()
        {
            // Scan up the AST's parents, looking for a script block that is either
            // a workflow, or has a job definition attribute.
            // Stop scanning when we encounter a FunctionDefinitionAst
            Ast current = this;
            bool stopScanning = false;

            while (current != null && !stopScanning)
            {
                if (current is ScriptBlockAst scriptBlock)
                {
                    // See if this uses the workflow keyword
                    if (scriptBlock.Parent is FunctionDefinitionAst functionDefinition)
                    {
                        stopScanning = true;
                        if (functionDefinition.IsWorkflow)
                        {
                            return true;
                        }
                    }
                }

                if (current is CommandAst commandAst &&
                    string.Equals(TokenKind.InlineScript.Text(), commandAst.GetCommandName(), StringComparison.OrdinalIgnoreCase) &&
                    this != commandAst)
                {
                    return false;
                }

                current = current.Parent;
            }

            return false;
        }

        internal bool HasSuspiciousContent { get; set; }

        #region Search Ancestor Ast

        internal static ConfigurationDefinitionAst GetAncestorConfigurationDefinitionAstAndDynamicKeywordStatementAst(
            Ast ast,
            out DynamicKeywordStatementAst keywordAst)
        {
            ConfigurationDefinitionAst configAst = null;
            keywordAst = GetAncestorAst<DynamicKeywordStatementAst>(ast);
            configAst = (keywordAst != null) ? GetAncestorAst<ConfigurationDefinitionAst>(keywordAst) : GetAncestorAst<ConfigurationDefinitionAst>(ast);
            return configAst;
        }

        internal static HashtableAst GetAncestorHashtableAst(Ast ast, out Ast lastChildOfHashtable)
        {
            HashtableAst hashtableAst = null;
            lastChildOfHashtable = null;
            while (ast != null)
            {
                hashtableAst = ast as HashtableAst;
                if (hashtableAst != null)
                    break;
                lastChildOfHashtable = ast;
                ast = ast.Parent;
            }

            return hashtableAst;
        }

        internal static TypeDefinitionAst GetAncestorTypeDefinitionAst(Ast ast)
        {
            TypeDefinitionAst typeDefinitionAst = null;

            while (ast != null)
            {
                typeDefinitionAst = ast as TypeDefinitionAst;
                if (typeDefinitionAst != null)
                    break;

                // Nested function isn't really a member of the type so stop looking
                // Anonymous script blocks are though
                if (ast is FunctionDefinitionAst functionDefinitionAst && functionDefinitionAst.Parent is not FunctionMemberAst)
                    break;
                ast = ast.Parent;
            }

            return typeDefinitionAst;
        }

        
        internal static T GetAncestorAst<T>(Ast ast) where T : Ast
        {
            T targetAst = null;
            var parent = ast;
            while (parent != null)
            {
                targetAst = parent as T;
                if (targetAst != null)
                    break;
                parent = parent.Parent;
            }

            return targetAst;
        }

        #endregion
    }

    // A dummy class to hold an extent for open/close curlies so we can step in the debugger.
    // This Ast is never produced by the parser, only from the compiler.
    internal class SequencePointAst : Ast
    {
        public SequencePointAst(IScriptExtent extent)
            : base(extent)
        {
        }

        
        public override Ast Copy()
        {
            Diagnostics.Assert(false, "code should be unreachable");
            return null;
        }

        internal override object Accept(ICustomAstVisitor visitor)
        {
            Diagnostics.Assert(false, "code should be unreachable");
            return null;
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            Diagnostics.Assert(false, "code should be unreachable");
            return visitor.CheckForPostAction(this, AstVisitAction.Continue);
        }
    }

    
    public class ErrorStatementAst : PipelineBaseAst
    {
        internal ErrorStatementAst(IScriptExtent extent, IEnumerable<Ast> nestedAsts = null)
            : base(extent)
        {
            if (nestedAsts != null && nestedAsts.Any())
            {
                NestedAst = new ReadOnlyCollection<Ast>(nestedAsts.ToArray());
                SetParents(NestedAst);
            }
        }

        internal ErrorStatementAst(IScriptExtent extent, Token kind, IEnumerable<Ast> nestedAsts = null)
            : base(extent)
        {
            if (kind == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(kind));
            }

            Kind = kind;
            if (nestedAsts != null && nestedAsts.Any())
            {
                NestedAst = new ReadOnlyCollection<Ast>(nestedAsts.ToArray());
                SetParents(NestedAst);
            }
        }

        internal ErrorStatementAst(IScriptExtent extent, Token kind, IEnumerable<KeyValuePair<string, Tuple<Token, Ast>>> flags, IEnumerable<Ast> conditions, IEnumerable<Ast> bodies)
            : base(extent)
        {
            if (kind == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(kind));
            }

            Kind = kind;
            if (flags != null && flags.Any())
            {
                Flags = new Dictionary<string, Tuple<Token, Ast>>(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, Tuple<Token, Ast>> entry in flags)
                {
                    if (Flags.ContainsKey(entry.Key))
                        continue;

                    Flags.Add(entry.Key, entry.Value);
                    if (entry.Value.Item2 != null)
                    {
                        SetParent(entry.Value.Item2);
                    }
                }
            }

            if (conditions != null && conditions.Any())
            {
                Conditions = new ReadOnlyCollection<Ast>(conditions.ToArray());
                SetParents(Conditions);
            }

            if (bodies != null && bodies.Any())
            {
                Bodies = new ReadOnlyCollection<Ast>(bodies.ToArray());
                SetParents(Bodies);
            }
        }

        
        public Token Kind { get; }

        
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public Dictionary<string, Tuple<Token, Ast>> Flags { get; }

        
        public ReadOnlyCollection<Ast> Conditions { get; }

        
        public ReadOnlyCollection<Ast> Bodies { get; }

        
        public ReadOnlyCollection<Ast> NestedAst { get; }

        
        public override Ast Copy()
        {
            if (this.Kind == null)
            {
                var newNestedAst = CopyElements(this.NestedAst);
                return new ErrorStatementAst(this.Extent, newNestedAst);
            }
            else if (Flags != null || Conditions != null || Bodies != null)
            {
                var newConditions = CopyElements(this.Conditions);
                var newBodies = CopyElements(this.Bodies);
                Dictionary<string, Tuple<Token, Ast>> newFlags = null;

                if (this.Flags != null)
                {
                    newFlags = new Dictionary<string, Tuple<Token, Ast>>(StringComparer.OrdinalIgnoreCase);
                    foreach (KeyValuePair<string, Tuple<Token, Ast>> entry in this.Flags)
                    {
                        var newAst = CopyElement(entry.Value.Item2);
                        newFlags.Add(entry.Key, new Tuple<Token, Ast>(entry.Value.Item1, newAst));
                    }
                }

                return new ErrorStatementAst(this.Extent, this.Kind, newFlags, newConditions, newBodies);
            }
            else
            {
                var newNestedAst = CopyElements(this.NestedAst);
                return new ErrorStatementAst(this.Extent, this.Kind, newNestedAst);
            }
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitErrorStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitErrorStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue && NestedAst != null)
            {
                for (int index = 0; index < NestedAst.Count; index++)
                {
                    var ast = NestedAst[index];
                    action = ast.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            if (action == AstVisitAction.Continue && Flags != null)
            {
                foreach (var tuple in Flags.Values)
                {
                    if (tuple.Item2 == null)
                        continue;

                    action = tuple.Item2.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            if (action == AstVisitAction.Continue && Conditions != null)
            {
                for (int index = 0; index < Conditions.Count; index++)
                {
                    var ast = Conditions[index];
                    action = ast.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            if (action == AstVisitAction.Continue && Bodies != null)
            {
                for (int index = 0; index < Bodies.Count; index++)
                {
                    var ast = Bodies[index];
                    action = ast.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class ErrorExpressionAst : ExpressionAst
    {
        internal ErrorExpressionAst(IScriptExtent extent, IEnumerable<Ast> nestedAsts = null)
            : base(extent)
        {
            if (nestedAsts != null && nestedAsts.Any())
            {
                NestedAst = new ReadOnlyCollection<Ast>(nestedAsts.ToArray());
                SetParents(NestedAst);
            }
        }

        
        public ReadOnlyCollection<Ast> NestedAst { get; }

        
        public override Ast Copy()
        {
            var newNestedAst = CopyElements(this.NestedAst);
            return new ErrorExpressionAst(this.Extent, newNestedAst);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitErrorExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitErrorExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue && NestedAst != null)
            {
                for (int index = 0; index < NestedAst.Count; index++)
                {
                    var ast = NestedAst[index];
                    action = ast.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    #region Script Blocks

    
    public class ScriptRequirements
    {
        internal static readonly ReadOnlyCollection<PSSnapInSpecification> EmptySnapinCollection =
            Utils.EmptyReadOnlyCollection<PSSnapInSpecification>();

        internal static readonly ReadOnlyCollection<string> EmptyAssemblyCollection =
            Utils.EmptyReadOnlyCollection<string>();

        internal static readonly ReadOnlyCollection<ModuleSpecification> EmptyModuleCollection =
            Utils.EmptyReadOnlyCollection<ModuleSpecification>();

        internal static readonly ReadOnlyCollection<string> EmptyEditionCollection =
            Utils.EmptyReadOnlyCollection<string>();

        
        public string RequiredApplicationId { get; internal set; }

        
        public Version RequiredPSVersion { get; internal set; }

        
        public ReadOnlyCollection<string> RequiredPSEditions { get; internal set; }

        
        public ReadOnlyCollection<ModuleSpecification> RequiredModules { get; internal set; }

        
        public ReadOnlyCollection<string> RequiredAssemblies { get; internal set; }

        
        public bool IsElevationRequired { get; internal set; }
    }

    
    public class ScriptBlockAst : Ast, IParameterMetadataProvider
    {
        private static readonly ReadOnlyCollection<AttributeAst> s_emptyAttributeList =
            Utils.EmptyReadOnlyCollection<AttributeAst>();

        private static readonly ReadOnlyCollection<UsingStatementAst> s_emptyUsingStatementList =
            Utils.EmptyReadOnlyCollection<UsingStatementAst>();

        internal bool HadErrors { get; set; }

        internal bool IsConfiguration { get; private set; }

        internal bool PostParseChecksPerformed { get; set; }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public ScriptBlockAst(IScriptExtent extent,
                              IEnumerable<UsingStatementAst> usingStatements,
                              IEnumerable<AttributeAst> attributes,
                              ParamBlockAst paramBlock,
                              NamedBlockAst beginBlock,
                              NamedBlockAst processBlock,
                              NamedBlockAst endBlock,
                              NamedBlockAst dynamicParamBlock)
            : this(
                extent,
                usingStatements,
                attributes,
                paramBlock,
                beginBlock,
                processBlock,
                endBlock,
                cleanBlock: null,
                dynamicParamBlock)
        {
        }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public ScriptBlockAst(
            IScriptExtent extent,
            IEnumerable<UsingStatementAst> usingStatements,
            IEnumerable<AttributeAst> attributes,
            ParamBlockAst paramBlock,
            NamedBlockAst beginBlock,
            NamedBlockAst processBlock,
            NamedBlockAst endBlock,
            NamedBlockAst cleanBlock,
            NamedBlockAst dynamicParamBlock)
            : base(extent)
        {
            SetUsingStatements(usingStatements);

            if (attributes != null)
            {
                this.Attributes = new ReadOnlyCollection<AttributeAst>(attributes.ToArray());
                SetParents(Attributes);
            }
            else
            {
                this.Attributes = s_emptyAttributeList;
            }

            if (paramBlock != null)
            {
                this.ParamBlock = paramBlock;
                SetParent(paramBlock);
            }

            if (beginBlock != null)
            {
                this.BeginBlock = beginBlock;
                SetParent(beginBlock);
            }

            if (processBlock != null)
            {
                this.ProcessBlock = processBlock;
                SetParent(processBlock);
            }

            if (endBlock != null)
            {
                this.EndBlock = endBlock;
                SetParent(endBlock);
            }

            if (cleanBlock != null)
            {
                this.CleanBlock = cleanBlock;
                SetParent(cleanBlock);
            }

            if (dynamicParamBlock != null)
            {
                this.DynamicParamBlock = dynamicParamBlock;
                SetParent(dynamicParamBlock);
            }
        }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public ScriptBlockAst(IScriptExtent extent,
                              IEnumerable<UsingStatementAst> usingStatements,
                              ParamBlockAst paramBlock,
                              NamedBlockAst beginBlock,
                              NamedBlockAst processBlock,
                              NamedBlockAst endBlock,
                              NamedBlockAst dynamicParamBlock)
            : this(extent, usingStatements, null, paramBlock, beginBlock, processBlock, endBlock, dynamicParamBlock)
        {
        }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public ScriptBlockAst(
            IScriptExtent extent,
            IEnumerable<UsingStatementAst> usingStatements,
            ParamBlockAst paramBlock,
            NamedBlockAst beginBlock,
            NamedBlockAst processBlock,
            NamedBlockAst endBlock,
            NamedBlockAst cleanBlock,
            NamedBlockAst dynamicParamBlock)
            : this(extent, usingStatements, null, paramBlock, beginBlock, processBlock, endBlock, cleanBlock, dynamicParamBlock)
        {
        }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public ScriptBlockAst(IScriptExtent extent,
                              ParamBlockAst paramBlock,
                              NamedBlockAst beginBlock,
                              NamedBlockAst processBlock,
                              NamedBlockAst endBlock,
                              NamedBlockAst dynamicParamBlock)
            : this(extent, null, paramBlock, beginBlock, processBlock, endBlock, dynamicParamBlock)
        {
        }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public ScriptBlockAst(
            IScriptExtent extent,
            ParamBlockAst paramBlock,
            NamedBlockAst beginBlock,
            NamedBlockAst processBlock,
            NamedBlockAst endBlock,
            NamedBlockAst cleanBlock,
            NamedBlockAst dynamicParamBlock)
            : this(extent, null, paramBlock, beginBlock, processBlock, endBlock, cleanBlock, dynamicParamBlock)
        {
        }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public ScriptBlockAst(IScriptExtent extent, List<UsingStatementAst> usingStatements, ParamBlockAst paramBlock, StatementBlockAst statements, bool isFilter)
            : this(extent, usingStatements, null, paramBlock, statements, isFilter, false)
        {
        }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public ScriptBlockAst(IScriptExtent extent, ParamBlockAst paramBlock, StatementBlockAst statements, bool isFilter)
            : this(extent, null, null, paramBlock, statements, isFilter, false)
        {
        }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public ScriptBlockAst(IScriptExtent extent, ParamBlockAst paramBlock, StatementBlockAst statements, bool isFilter, bool isConfiguration)
            : this(extent, null, null, paramBlock, statements, isFilter, isConfiguration)
        {
        }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public ScriptBlockAst(IScriptExtent extent, IEnumerable<UsingStatementAst> usingStatements, ParamBlockAst paramBlock, StatementBlockAst statements, bool isFilter, bool isConfiguration)
            : this(extent, usingStatements, null, paramBlock, statements, isFilter, isConfiguration)
        {
        }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public ScriptBlockAst(IScriptExtent extent, IEnumerable<AttributeAst> attributes, ParamBlockAst paramBlock, StatementBlockAst statements, bool isFilter, bool isConfiguration)
            : this(extent, null, attributes, paramBlock, statements, isFilter, isConfiguration)
        {
        }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public ScriptBlockAst(IScriptExtent extent, IEnumerable<UsingStatementAst> usingStatements, IEnumerable<AttributeAst> attributes, ParamBlockAst paramBlock, StatementBlockAst statements, bool isFilter, bool isConfiguration)
            : base(extent)
        {
            SetUsingStatements(usingStatements);

            if (attributes != null)
            {
                this.Attributes = new ReadOnlyCollection<AttributeAst>(attributes.ToArray());
                SetParents(Attributes);
            }
            else
            {
                this.Attributes = s_emptyAttributeList;
            }

            if (statements == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(statements));
            }

            if (paramBlock != null)
            {
                this.ParamBlock = paramBlock;
                SetParent(paramBlock);
            }

            if (isFilter)
            {
                this.ProcessBlock = new NamedBlockAst(statements.Extent, TokenKind.Process, statements, true);
                SetParent(ProcessBlock);
            }
            else
            {
                this.EndBlock = new NamedBlockAst(statements.Extent, TokenKind.End, statements, true);
                this.IsConfiguration = isConfiguration;
                SetParent(EndBlock);
            }
        }

        private void SetUsingStatements(IEnumerable<UsingStatementAst> usingStatements)
        {
            if (usingStatements != null)
            {
                this.UsingStatements = new ReadOnlyCollection<UsingStatementAst>(usingStatements.ToArray());
                SetParents(UsingStatements);
            }
            else
            {
                this.UsingStatements = s_emptyUsingStatementList;
            }
        }

        
        public ReadOnlyCollection<AttributeAst> Attributes { get; }

        
        public ReadOnlyCollection<UsingStatementAst> UsingStatements { get; private set; }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Param")]
        public ParamBlockAst ParamBlock { get; }

        
        public NamedBlockAst BeginBlock { get; }

        
        public NamedBlockAst ProcessBlock { get; }

        
        public NamedBlockAst EndBlock { get; }

        
        public NamedBlockAst CleanBlock { get; }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Param")]
        public NamedBlockAst DynamicParamBlock { get; }

        
        public ScriptRequirements ScriptRequirements { get; internal set; }

        
        public CommentHelpInfo GetHelpContent()
        {
            Dictionary<Ast, Token[]> scriptBlockTokenCache = new Dictionary<Ast, Token[]>();
            var commentTokens = HelpCommentsParser.GetHelpCommentTokens(this, scriptBlockTokenCache);
            if (commentTokens != null)
            {
                return HelpCommentsParser.GetHelpContents(commentTokens.Item1, commentTokens.Item2);
            }

            return null;
        }

        
        public ScriptBlock GetScriptBlock()
        {
            if (!PostParseChecksPerformed)
            {
                Parser parser = new Parser();
                // we call PerformPostParseChecks on root ScriptBlockAst, to obey contract of SymbolResolver.
                // It needs to be run from the top of the tree.
                // It's ok to report an error from a different part of AST in this case.
                var root = GetRootScriptBlockAst();
                root.PerformPostParseChecks(parser);
                if (parser.ErrorList.Count > 0)
                {
                    throw new ParseException(parser.ErrorList.ToArray());
                }
            }

            if (HadErrors)
            {
                throw new PSInvalidOperationException();
            }

            return new ScriptBlock(this, isFilter: false);
        }

        private ScriptBlockAst GetRootScriptBlockAst()
        {
            ScriptBlockAst rootScriptBlockAst = this;
            ScriptBlockAst parent;
            while ((parent = Ast.GetAncestorAst<ScriptBlockAst>(rootScriptBlockAst.Parent)) != null)
            {
                rootScriptBlockAst = parent;
            }

            return rootScriptBlockAst;
        }

        
        public override Ast Copy()
        {
            var newParamBlock = CopyElement(this.ParamBlock);
            var newBeginBlock = CopyElement(this.BeginBlock);
            var newProcessBlock = CopyElement(this.ProcessBlock);
            var newEndBlock = CopyElement(this.EndBlock);
            var newCleanBlock = CopyElement(this.CleanBlock);
            var newDynamicParamBlock = CopyElement(this.DynamicParamBlock);
            var newAttributes = CopyElements(this.Attributes);
            var newUsingStatements = CopyElements(this.UsingStatements);

            return new ScriptBlockAst(
                this.Extent,
                newUsingStatements,
                newAttributes,
                newParamBlock,
                newBeginBlock,
                newProcessBlock,
                newEndBlock,
                newCleanBlock,
                newDynamicParamBlock)
            {
                IsConfiguration = this.IsConfiguration,
                ScriptRequirements = this.ScriptRequirements
            };
        }

        internal string ToStringForSerialization()
        {
            string result = this.ToString();
            if (Parent != null)
            {
                // Parent is FunctionDefinitionAst or ScriptBlockExpressionAst
                // The extent includes curlies which we want to exclude.
                Diagnostics.Assert(result[0] == '{' && result[result.Length - 1] == '}',
                    "There is an incorrect assumption about the extent.");
                result = result.Substring(1, result.Length - 2);
            }

            return result;
        }

        internal string ToStringForSerialization(Tuple<List<VariableExpressionAst>, string> usingVariablesTuple, int initialStartOffset, int initialEndOffset)
        {
            Diagnostics.Assert(usingVariablesTuple.Item1 != null && usingVariablesTuple.Item1.Count > 0 && !string.IsNullOrEmpty(usingVariablesTuple.Item2),
                               "Caller makes sure the value passed in is not null or empty");
            Diagnostics.Assert(initialStartOffset < initialEndOffset && initialStartOffset >= this.Extent.StartOffset && initialEndOffset <= this.Extent.EndOffset,
                               "Caller makes sure the section is within the ScriptBlockAst");

            List<VariableExpressionAst> usingVars = usingVariablesTuple.Item1; // A list of using variables
            string newParams = usingVariablesTuple.Item2; // The new parameters are separated by the comma

            // astElements contains
            //  -- UsingVariable
            //  -- ParamBlockAst
            var astElements = new List<Ast>(usingVars);
            if (ParamBlock != null)
            {
                astElements.Add(ParamBlock);
            }

            int indexOffset = this.Extent.StartOffset;
            int startOffset = initialStartOffset - indexOffset;
            int endOffset = initialEndOffset - indexOffset;

            string script = this.ToString();
            var newScript = new StringBuilder();

            foreach (var ast in astElements.OrderBy(static ast => ast.Extent.StartOffset))
            {
                int astStartOffset = ast.Extent.StartOffset - indexOffset;
                int astEndOffset = ast.Extent.EndOffset - indexOffset;

                // Skip the ast that is before the section that we care about
                if (astStartOffset < startOffset) { continue; }
                // We are done processing the section that we care about
                if (astStartOffset >= endOffset) { break; }

                if (ast is VariableExpressionAst varAst)
                {
                    VariablePath varPath = varAst.VariablePath;
                    string varName = varPath.IsDriveQualified ? $"{varPath.DriveName}_{varPath.UnqualifiedPath}" : $"{varPath.UnqualifiedPath}";
                    string varSign = varAst.Splatted ? "@" : "$";
                    string newVarName = varSign + UsingExpressionAst.UsingPrefix + varName;

                    newScript.Append(script.AsSpan(startOffset, astStartOffset - startOffset));
                    newScript.Append(newVarName);
                    startOffset = astEndOffset;
                }
                else
                {
                    var paramAst = ast as ParamBlockAst;
                    Diagnostics.Assert(paramAst != null, "The elements in astElements are either ParamBlockAst or VariableExpressionAst");

                    int currentOffset;
                    if (paramAst.Parameters.Count == 0)
                    {
                        currentOffset = astEndOffset - 1;
                    }
                    else
                    {
                        var firstParam = paramAst.Parameters[0];
                        currentOffset = firstParam.Attributes.Count == 0 ? firstParam.Name.Extent.StartOffset - indexOffset : firstParam.Attributes[0].Extent.StartOffset - indexOffset;
                        newParams += ",\n";
                    }

                    newScript.Append(script.AsSpan(startOffset, currentOffset - startOffset));
                    newScript.Append(newParams);
                    startOffset = currentOffset;
                }
            }

            newScript.Append(script.AsSpan(startOffset, endOffset - startOffset));
            string result = newScript.ToString();

            if (Parent != null && initialStartOffset == this.Extent.StartOffset && initialEndOffset == this.Extent.EndOffset)
            {
                // Parent is FunctionDefinitionAst or ScriptBlockExpressionAst
                // The extent includes curlies which we want to exclude.
                Diagnostics.Assert(result[0] == '{' && result[result.Length - 1] == '}',
                    "There is an incorrect assumption about the extent.");
                result = result.Substring(1, result.Length - 2);
            }

            return result;
        }

        internal void PerformPostParseChecks(Parser parser)
        {
            bool etwEnabled = ParserEventSource.Log.IsEnabled();
            if (etwEnabled) ParserEventSource.Log.ResolveSymbolsStart();

            SymbolResolver.ResolveSymbols(parser, this);
            if (etwEnabled)
            {
                ParserEventSource.Log.ResolveSymbolsStop();
                ParserEventSource.Log.SemanticChecksStart();
            }

            SemanticChecks.CheckAst(parser, this);
            if (etwEnabled) ParserEventSource.Log.SemanticChecksStop();

            Diagnostics.Assert(PostParseChecksPerformed, "Post parse checks not set during semantic checks");
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitScriptBlock(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitScriptBlock(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);

            if (visitor is AstVisitor2 visitor2)
            {
                if (action == AstVisitAction.Continue)
                {
                    foreach (var usingStatement in UsingStatements)
                    {
                        action = usingStatement.InternalVisit(visitor2);
                        if (action != AstVisitAction.Continue)
                        {
                            break;
                        }
                    }
                }

                if (action == AstVisitAction.Continue)
                {
                    foreach (var attr in Attributes)
                    {
                        action = attr.InternalVisit(visitor2);
                        if (action != AstVisitAction.Continue)
                        {
                            break;
                        }
                    }
                }
            }

            if (action == AstVisitAction.Continue)
            {
                _ = VisitAndShallContinue(ParamBlock) &&
                    VisitAndShallContinue(DynamicParamBlock) &&
                    VisitAndShallContinue(BeginBlock) &&
                    VisitAndShallContinue(ProcessBlock) &&
                    VisitAndShallContinue(EndBlock) &&
                    VisitAndShallContinue(CleanBlock);
            }

            return visitor.CheckForPostAction(this, action);

            bool VisitAndShallContinue(Ast ast)
            {
                if (ast is not null)
                {
                    action = ast.InternalVisit(visitor);
                }

                return action == AstVisitAction.Continue;
            }
        }

        #endregion Visitors

        #region IParameterMetadataProvider implementation

        bool IParameterMetadataProvider.HasAnyScriptBlockAttributes()
        {
            return Attributes.Count > 0 || ParamBlock != null && ParamBlock.Attributes.Count > 0;
        }

        RuntimeDefinedParameterDictionary IParameterMetadataProvider.GetParameterMetadata(bool automaticPositions, ref bool usesCmdletBinding)
        {
            if (ParamBlock != null)
            {
                return Compiler.GetParameterMetaData(ParamBlock.Parameters, automaticPositions, ref usesCmdletBinding);
            }

            return new RuntimeDefinedParameterDictionary { Data = RuntimeDefinedParameterDictionary.EmptyParameterArray };
        }

        IEnumerable<Attribute> IParameterMetadataProvider.GetScriptBlockAttributes()
        {
            for (int index = 0; index < Attributes.Count; index++)
            {
                var attributeAst = Attributes[index];
                yield return Compiler.GetAttribute(attributeAst);
            }

            if (ParamBlock != null)
            {
                for (int index = 0; index < ParamBlock.Attributes.Count; index++)
                {
                    var attributeAst = ParamBlock.Attributes[index];
                    yield return Compiler.GetAttribute(attributeAst);
                }
            }
        }

        IEnumerable<ExperimentalAttribute> IParameterMetadataProvider.GetExperimentalAttributes()
        {
            for (int index = 0; index < Attributes.Count; index++)
            {
                AttributeAst attributeAst = Attributes[index];
                ExperimentalAttribute expAttr = GetExpAttributeHelper(attributeAst);
                if (expAttr != null) { yield return expAttr; }
            }

            if (ParamBlock != null)
            {
                for (int index = 0; index < ParamBlock.Attributes.Count; index++)
                {
                    var attributeAst = ParamBlock.Attributes[index];
                    var expAttr = GetExpAttributeHelper(attributeAst);
                    if (expAttr != null) { yield return expAttr; }
                }
            }

            static ExperimentalAttribute GetExpAttributeHelper(AttributeAst attributeAst)
            {
                AttributeAst potentialExpAttr = null;
                string expAttrTypeName = typeof(ExperimentalAttribute).FullName;
                string attrAstTypeName = attributeAst.TypeName.Name;

                if (TypeAccelerators.Get.TryGetValue(attrAstTypeName, out Type attrType) && attrType == typeof(ExperimentalAttribute))
                {
                    potentialExpAttr = attributeAst;
                }
                else if (expAttrTypeName.EndsWith(attrAstTypeName, StringComparison.OrdinalIgnoreCase))
                {
                    // Handle two cases:
                    //   1. declare the attribute using full type name;
                    //   2. declare the attribute using partial type name due to 'using namespace'.
                    int expAttrLength = expAttrTypeName.Length;
                    int attrAstLength = attrAstTypeName.Length;
                    if (expAttrLength == attrAstLength || expAttrTypeName[expAttrLength - attrAstLength - 1] == '.')
                    {
                        potentialExpAttr = attributeAst;
                    }
                }

                if (potentialExpAttr != null)
                {
                    try
                    {
                        return Compiler.GetAttribute(potentialExpAttr) as ExperimentalAttribute;
                    }
                    catch (Exception)
                    {
                        // catch all and assume it's not a declaration of ExperimentalAttribute
                    }
                }

                return null;
            }
        }

        ReadOnlyCollection<ParameterAst> IParameterMetadataProvider.Parameters
        {
            get { return (ParamBlock != null) ? this.ParamBlock.Parameters : null; }
        }

        ScriptBlockAst IParameterMetadataProvider.Body { get { return this; } }

        #region PowerShell Conversion

        PowerShell IParameterMetadataProvider.GetPowerShell(ExecutionContext context, Dictionary<string, object> variables, bool isTrustedInput,
            bool filterNonUsingVariables, bool? createLocalScope, params object[] args)
        {
            ExecutionContext.CheckStackDepth();
            return ScriptBlockToPowerShellConverter.Convert(this, null, isTrustedInput, context, variables, filterNonUsingVariables, createLocalScope, args);
        }

        string IParameterMetadataProvider.GetWithInputHandlingForInvokeCommand()
        {
            return GetWithInputHandlingForInvokeCommandImpl(null);
        }

        Tuple<string, string> IParameterMetadataProvider.GetWithInputHandlingForInvokeCommandWithUsingExpression(
            Tuple<List<VariableExpressionAst>, string> usingVariablesTuple)
        {
            string additionalNewParams = usingVariablesTuple.Item2;
            string scriptBlockText = GetWithInputHandlingForInvokeCommandImpl(usingVariablesTuple);

            string paramText = null;
            if (ParamBlock == null)
            {
                paramText = "param(" + additionalNewParams + ")" + Environment.NewLine;
            }

            return new Tuple<string, string>(paramText, scriptBlockText);
        }

        private string GetWithInputHandlingForInvokeCommandImpl(Tuple<List<VariableExpressionAst>, string> usingVariablesTuple)
        {
            // do not add "$input |" to complex pipelines
            var pipelineAst = GetSimplePipeline(false, out _, out _);
            if (pipelineAst == null)
            {
                return (usingVariablesTuple == null)
                    ? this.ToStringForSerialization()
                    : this.ToStringForSerialization(usingVariablesTuple, this.Extent.StartOffset, this.Extent.EndOffset);
            }

            // do not add "$input |" to pipelines beginning with an expression
            if (pipelineAst.PipelineElements[0] is CommandExpressionAst)
            {
                return (usingVariablesTuple == null)
                    ? this.ToStringForSerialization()
                    : this.ToStringForSerialization(usingVariablesTuple, this.Extent.StartOffset, this.Extent.EndOffset);
            }

            // do not add "$input |" to commands that reference $input in their arguments
            if (AstSearcher.IsUsingDollarInput(this))
            {
                return (usingVariablesTuple == null)
                    ? this.ToStringForSerialization()
                    : this.ToStringForSerialization(usingVariablesTuple, this.Extent.StartOffset, this.Extent.EndOffset);
            }

            // all checks above failed - change script into "$input | <original script>"
            var sb = new StringBuilder();
            if (ParamBlock != null)
            {
                string paramText = (usingVariablesTuple == null)
                    ? ParamBlock.ToString()
                    : this.ToStringForSerialization(usingVariablesTuple, ParamBlock.Extent.StartOffset, ParamBlock.Extent.EndOffset);
                sb.Append(paramText);
            }

            sb.Append("$input |");
            string pipelineText = (usingVariablesTuple == null)
                ? pipelineAst.ToString()
                : this.ToStringForSerialization(usingVariablesTuple, pipelineAst.Extent.StartOffset, pipelineAst.Extent.EndOffset);
            sb.Append(pipelineText);

            return sb.ToString();
        }

        #endregion PowerShell Conversion

        bool IParameterMetadataProvider.UsesCmdletBinding()
        {
            bool usesCmdletBinding = false;

            if (ParamBlock != null)
            {
                usesCmdletBinding = this.ParamBlock.Attributes.Any(static attribute => typeof(CmdletBindingAttribute) == attribute.TypeName.GetReflectionAttributeType());
                if (!usesCmdletBinding)
                {
                    usesCmdletBinding = ParamBlockAst.UsesCmdletBinding(ParamBlock.Parameters);
                }
            }

            return usesCmdletBinding;
        }

        #endregion IParameterMetadataProvider implementation

        internal PipelineAst GetSimplePipeline(bool allowMultiplePipelines, out string errorId, out string errorMsg)
        {
            if (BeginBlock != null
                || ProcessBlock != null
                || CleanBlock != null
                || DynamicParamBlock != null)
            {
                errorId = nameof(AutomationExceptions.CanConvertOneClauseOnly);
                errorMsg = AutomationExceptions.CanConvertOneClauseOnly;
                return null;
            }

            if (EndBlock == null || EndBlock.Statements.Count < 1)
            {
                errorId = "CantConvertEmptyPipeline";
                errorMsg = AutomationExceptions.CantConvertEmptyPipeline;
                return null;
            }

            if (EndBlock.Traps != null && EndBlock.Traps.Count > 0)
            {
                errorId = "CantConvertScriptBlockWithTrap";
                errorMsg = AutomationExceptions.CantConvertScriptBlockWithTrap;
                return null;
            }

            // Make sure all statements are pipelines.
            if (EndBlock.Statements.Any(ast => ast is not PipelineAst))
            {
                errorId = "CanOnlyConvertOnePipeline";
                errorMsg = AutomationExceptions.CanOnlyConvertOnePipeline;
                return null;
            }

            if (EndBlock.Statements.Count != 1 && !allowMultiplePipelines)
            {
                errorId = "CanOnlyConvertOnePipeline";
                errorMsg = AutomationExceptions.CanOnlyConvertOnePipeline;
                return null;
            }

            errorId = null;
            errorMsg = null;
            return EndBlock.Statements[0] as PipelineAst;
        }
    }

    
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Param")]
    public class ParamBlockAst : Ast
    {
        private static readonly ReadOnlyCollection<AttributeAst> s_emptyAttributeList =
            Utils.EmptyReadOnlyCollection<AttributeAst>();

        private static readonly ReadOnlyCollection<ParameterAst> s_emptyParameterList =
            Utils.EmptyReadOnlyCollection<ParameterAst>();

        
        public ParamBlockAst(IScriptExtent extent, IEnumerable<AttributeAst> attributes, IEnumerable<ParameterAst> parameters)
            : base(extent)
        {
            if (attributes != null)
            {
                this.Attributes = new ReadOnlyCollection<AttributeAst>(attributes.ToArray());
                SetParents(Attributes);
            }
            else
            {
                this.Attributes = s_emptyAttributeList;
            }

            if (parameters != null)
            {
                this.Parameters = new ReadOnlyCollection<ParameterAst>(parameters.ToArray());
                SetParents(Parameters);
            }
            else
            {
                this.Parameters = s_emptyParameterList;
            }
        }

        
        public ReadOnlyCollection<AttributeAst> Attributes { get; }

        
        public ReadOnlyCollection<ParameterAst> Parameters { get; }

        
        public override Ast Copy()
        {
            var newAttributes = CopyElements(this.Attributes);
            var newParameters = CopyElements(this.Parameters);
            return new ParamBlockAst(this.Extent, newAttributes, newParameters);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitParamBlock(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitParamBlock(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < Attributes.Count; index++)
                {
                    var attributeAst = Attributes[index];
                    action = attributeAst.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < Parameters.Count; index++)
                {
                    var paramAst = Parameters[index];
                    action = paramAst.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        internal static bool UsesCmdletBinding(IEnumerable<ParameterAst> parameters)
        {
            bool usesCmdletBinding = false;
            foreach (var parameter in parameters)
            {
                usesCmdletBinding = parameter.Attributes.Any(attribute => (attribute.TypeName.GetReflectionAttributeType() != null) &&
                                                                          attribute.TypeName.GetReflectionAttributeType() == typeof(ParameterAttribute));
                if (usesCmdletBinding)
                {
                    break;
                }
            }

            return usesCmdletBinding;
        }
    }

    
    public class NamedBlockAst : Ast
    {
        
        public NamedBlockAst(IScriptExtent extent, TokenKind blockName, StatementBlockAst statementBlock, bool unnamed)
            : base(extent)
        {
            // Validate the block name.  If the block is unnamed, it must be an End block (for a function)
            // or Process block (for a filter).
            if (HasInvalidBlockName(blockName, unnamed))
            {
                throw PSTraceSource.NewArgumentException(nameof(blockName));
            }

            if (statementBlock == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(statementBlock));
            }

            this.Unnamed = unnamed;
            this.BlockKind = blockName;
            var statements = statementBlock.Statements;
            this.Statements = statements;
            for (int index = 0; index < statements.Count; index++)
            {
                var stmt = statements[index];
                stmt.ClearParent();
            }

            SetParents(statements);

            var traps = statementBlock.Traps;
            if (traps != null && traps.Count > 0)
            {
                this.Traps = traps;
                for (int index = 0; index < traps.Count; index++)
                {
                    var trap = traps[index];
                    trap.ClearParent();
                }

                SetParents(traps);
            }

            if (!unnamed)
            {
                if (statementBlock.Extent is InternalScriptExtent statementsExtent)
                {
                    this.OpenCurlyExtent = new InternalScriptExtent(statementsExtent.PositionHelper, statementsExtent.StartOffset, statementsExtent.StartOffset + 1);
                    this.CloseCurlyExtent = new InternalScriptExtent(statementsExtent.PositionHelper, statementsExtent.EndOffset - 1, statementsExtent.EndOffset);
                }
            }
        }

        
        public bool Unnamed { get; }

        
        public TokenKind BlockKind { get; }

        
        public ReadOnlyCollection<StatementAst> Statements { get; }

        
        public ReadOnlyCollection<TrapStatementAst> Traps { get; }

        
        public override Ast Copy()
        {
            var newTraps = CopyElements(this.Traps);
            var newStatements = CopyElements(this.Statements);
            var statementBlockExtent = this.Extent;

            if (this.OpenCurlyExtent != null && this.CloseCurlyExtent != null)
            {
                // For explicitly named block, the Extent of the StatementBlockAst is not
                // the same as the Extent of the NamedBlockAst. In this case, reconstruct
                // the Extent of the StatementBlockAst from openExtent/closeExtent.
                var openExtent = (InternalScriptExtent)this.OpenCurlyExtent;
                var closeExtent = (InternalScriptExtent)this.CloseCurlyExtent;
                statementBlockExtent = new InternalScriptExtent(openExtent.PositionHelper, openExtent.StartOffset, closeExtent.EndOffset);
            }

            var statementBlock = new StatementBlockAst(statementBlockExtent, newStatements, newTraps);
            return new NamedBlockAst(this.Extent, this.BlockKind, statementBlock, this.Unnamed);
        }

        private static bool HasInvalidBlockName(TokenKind blockName, bool unnamed)
        {
            return !blockName.HasTrait(TokenFlags.ScriptBlockBlockName)
                || (unnamed
                    && blockName != TokenKind.Process
                    && blockName != TokenKind.End);
        }

        // Used by the debugger for command breakpoints
        internal IScriptExtent OpenCurlyExtent { get; }

        internal IScriptExtent CloseCurlyExtent { get; }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitNamedBlock(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitNamedBlock(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = StatementBlockAst.InternalVisit(visitor, Traps, Statements, action);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class NamedAttributeArgumentAst : Ast
    {
        
        public NamedAttributeArgumentAst(IScriptExtent extent, string argumentName, ExpressionAst argument, bool expressionOmitted)
            : base(extent)
        {
            if (string.IsNullOrEmpty(argumentName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(argumentName));
            }

            if (argument == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(argument));
            }

            this.Argument = argument;
            SetParent(argument);
            this.ArgumentName = argumentName;
            this.ExpressionOmitted = expressionOmitted;
        }

        
        public string ArgumentName { get; }

        
        public ExpressionAst Argument { get; }

        
        public bool ExpressionOmitted { get; }

        
        public override Ast Copy()
        {
            var newArgument = CopyElement(this.Argument);
            return new NamedAttributeArgumentAst(this.Extent, this.ArgumentName, newArgument, this.ExpressionOmitted);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitNamedAttributeArgument(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitNamedAttributeArgument(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Argument.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public abstract class AttributeBaseAst : Ast
    {
        
        protected AttributeBaseAst(IScriptExtent extent, ITypeName typeName)
            : base(extent)
        {
            if (typeName == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(typeName));
            }

            this.TypeName = typeName;
        }

        
        public ITypeName TypeName { get; }

        internal abstract Attribute GetAttribute();
    }

    
    public class AttributeAst : AttributeBaseAst
    {
        private static readonly ReadOnlyCollection<ExpressionAst> s_emptyPositionalArguments =
            Utils.EmptyReadOnlyCollection<ExpressionAst>();

        private static readonly ReadOnlyCollection<NamedAttributeArgumentAst> s_emptyNamedAttributeArguments =
            Utils.EmptyReadOnlyCollection<NamedAttributeArgumentAst>();

        
        public AttributeAst(IScriptExtent extent,
                            ITypeName typeName,
                            IEnumerable<ExpressionAst> positionalArguments,
                            IEnumerable<NamedAttributeArgumentAst> namedArguments)
            : base(extent, typeName)
        {
            if (positionalArguments != null)
            {
                this.PositionalArguments = new ReadOnlyCollection<ExpressionAst>(positionalArguments.ToArray());
                SetParents(PositionalArguments);
            }
            else
            {
                this.PositionalArguments = s_emptyPositionalArguments;
            }

            if (namedArguments != null)
            {
                this.NamedArguments = new ReadOnlyCollection<NamedAttributeArgumentAst>(namedArguments.ToArray());
                SetParents(NamedArguments);
            }
            else
            {
                this.NamedArguments = s_emptyNamedAttributeArguments;
            }
        }

        
        public ReadOnlyCollection<ExpressionAst> PositionalArguments { get; }

        
        public ReadOnlyCollection<NamedAttributeArgumentAst> NamedArguments { get; }

        
        public override Ast Copy()
        {
            var newPositionalArguments = CopyElements(this.PositionalArguments);
            var newNamedArguments = CopyElements(this.NamedArguments);
            return new AttributeAst(this.Extent, this.TypeName, newPositionalArguments, newNamedArguments);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitAttribute(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitAttribute(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < PositionalArguments.Count; index++)
                {
                    var expressionAst = PositionalArguments[index];
                    action = expressionAst.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < NamedArguments.Count; index++)
                {
                    var namedAttributeArgumentAst = NamedArguments[index];
                    action = namedAttributeArgumentAst.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        internal override Attribute GetAttribute()
        {
            return Compiler.GetAttribute(this);
        }
    }

    
    public class TypeConstraintAst : AttributeBaseAst
    {
        
        public TypeConstraintAst(IScriptExtent extent, ITypeName typeName)
            : base(extent, typeName)
        {
        }

        
        public TypeConstraintAst(IScriptExtent extent, Type type)
            : base(extent, new ReflectionTypeName(type))
        {
        }

        
        public override Ast Copy()
        {
            return new TypeConstraintAst(this.Extent, this.TypeName);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitTypeConstraint(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitTypeConstraint(this);
            return visitor.CheckForPostAction(this, action == AstVisitAction.SkipChildren ? AstVisitAction.Continue : action);
        }

        #endregion Visitors

        internal override Attribute GetAttribute()
        {
            return Compiler.GetAttribute(this);
        }
    }

    
    public class ParameterAst : Ast
    {
        private static readonly ReadOnlyCollection<AttributeBaseAst> s_emptyAttributeList =
            Utils.EmptyReadOnlyCollection<AttributeBaseAst>();

        
        public ParameterAst(IScriptExtent extent,
                            VariableExpressionAst name,
                            IEnumerable<AttributeBaseAst> attributes,
                            ExpressionAst defaultValue)
            : base(extent)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            if (attributes != null)
            {
                this.Attributes = new ReadOnlyCollection<AttributeBaseAst>(attributes.ToArray());
                SetParents(Attributes);
            }
            else
            {
                this.Attributes = s_emptyAttributeList;
            }

            this.Name = name;
            SetParent(name);
            if (defaultValue != null)
            {
                this.DefaultValue = defaultValue;
                SetParent(defaultValue);
            }
        }

        
        public ReadOnlyCollection<AttributeBaseAst> Attributes { get; }

        
        public VariableExpressionAst Name { get; }

        
        public ExpressionAst DefaultValue { get; }

        
        public Type StaticType
        {
            get
            {
                Type type = null;
                var typeConstraint = Attributes.OfType<TypeConstraintAst>().FirstOrDefault();
                if (typeConstraint != null)
                {
                    type = typeConstraint.TypeName.GetReflectionType();
                }

                return type ?? typeof(object);
            }
        }

        
        public override Ast Copy()
        {
            var newName = CopyElement(this.Name);
            var newAttributes = CopyElements(this.Attributes);
            var newDefaultValue = CopyElement(this.DefaultValue);
            return new ParameterAst(this.Extent, newName, newAttributes, newDefaultValue);
        }

        internal string GetTooltip()
        {
            var typeConstraint = Attributes.OfType<TypeConstraintAst>().FirstOrDefault();
            var type = typeConstraint != null ? typeConstraint.TypeName.FullName : "object";
            return type + " " + Name.VariablePath.UserPath;
        }

        
        internal string GetParamTextWithDollarUsingHandling(IEnumerator<VariableExpressionAst> orderedUsingVar)
        {
            int indexOffset = Extent.StartOffset;
            int startOffset = 0;
            int endOffset = Extent.EndOffset - Extent.StartOffset;

            string paramText = ToString();
            if (orderedUsingVar.Current == null && !orderedUsingVar.MoveNext())
            {
                return paramText;
            }

            var newParamText = new StringBuilder();
            do
            {
                var varAst = orderedUsingVar.Current;
                int astStartOffset = varAst.Extent.StartOffset - indexOffset;
                int astEndOffset = varAst.Extent.EndOffset - indexOffset;

                // Skip the VariableAst that is before section we care about
                if (astStartOffset < startOffset) { continue; }
                // We are done processing the current ParameterAst
                if (astStartOffset >= endOffset) { break; }

                VariablePath varPath = varAst.VariablePath;
                string varName = varPath.IsDriveQualified ? $"{varPath.DriveName}_{varPath.UnqualifiedPath}" : $"{varPath.UnqualifiedPath}";
                string varSign = varAst.Splatted ? "@" : "$";
                string newVarName = varSign + UsingExpressionAst.UsingPrefix + varName;

                newParamText.Append(paramText.AsSpan(startOffset, astStartOffset - startOffset));
                newParamText.Append(newVarName);
                startOffset = astEndOffset;
            } while (orderedUsingVar.MoveNext());

            if (startOffset == 0)
            {
                // Nothing changed within the ParameterAst text
                return paramText;
            }

            newParamText.Append(paramText.AsSpan(startOffset, endOffset - startOffset));
            return newParamText.ToString();
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitParameter(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitParameter(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < Attributes.Count; index++)
                {
                    var attributeAst = Attributes[index];
                    action = attributeAst.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            if (action == AstVisitAction.Continue)
            {
                action = Name.InternalVisit(visitor);
            }

            if (action == AstVisitAction.Continue && DefaultValue != null)
            {
                action = DefaultValue.InternalVisit(visitor);
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    #endregion Script Blocks

    #region Statements

    
    public class StatementBlockAst : Ast
    {
        private static readonly ReadOnlyCollection<StatementAst> s_emptyStatementCollection = Utils.EmptyReadOnlyCollection<StatementAst>();

        
        public StatementBlockAst(IScriptExtent extent, IEnumerable<StatementAst> statements, IEnumerable<TrapStatementAst> traps)
            : base(extent)
        {
            if (statements != null)
            {
                this.Statements = new ReadOnlyCollection<StatementAst>(statements.ToArray());
                SetParents(Statements);
            }
            else
            {
                this.Statements = s_emptyStatementCollection;
            }

            if (traps != null && traps.Any())
            {
                this.Traps = new ReadOnlyCollection<TrapStatementAst>(traps.ToArray());
                SetParents(Traps);
            }
        }

        
        public ReadOnlyCollection<StatementAst> Statements { get; }

        
        public ReadOnlyCollection<TrapStatementAst> Traps { get; }

        
        public override Ast Copy()
        {
            var newStatements = CopyElements(this.Statements);
            var newTraps = CopyElements(this.Traps);

            return new StatementBlockAst(this.Extent, newStatements, newTraps);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitStatementBlock(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitStatementBlock(this);
            return visitor.CheckForPostAction(this, InternalVisit(visitor, Traps, Statements, action));
        }

        internal static AstVisitAction InternalVisit(AstVisitor visitor,
                                                     ReadOnlyCollection<TrapStatementAst> traps,
                                                     ReadOnlyCollection<StatementAst> statements,
                                                     AstVisitAction action)
        {
            if (action == AstVisitAction.SkipChildren)
                return AstVisitAction.Continue;

            if (action == AstVisitAction.Continue && traps != null)
            {
                for (int index = 0; index < traps.Count; index++)
                {
                    var trapAst = traps[index];
                    action = trapAst.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            if (action == AstVisitAction.Continue && statements != null)
            {
                for (int index = 0; index < statements.Count; index++)
                {
                    var statementAst = statements[index];
                    action = statementAst.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            return action;
        }

        #endregion Visitors
    }

    
    public abstract class StatementAst : Ast
    {
        
        protected StatementAst(IScriptExtent extent)
            : base(extent)
        {
        }
    }

    
    [Flags]
    public enum TypeAttributes
    {
        
        None = 0x00,

        
        Class = 0x01,

        
        Interface = 0x02,

        
        Enum = 0x04,
    }

    
    public class TypeDefinitionAst : StatementAst
    {
        private static readonly ReadOnlyCollection<AttributeAst> s_emptyAttributeList =
            Utils.EmptyReadOnlyCollection<AttributeAst>();

        private static readonly ReadOnlyCollection<MemberAst> s_emptyMembersCollection =
            Utils.EmptyReadOnlyCollection<MemberAst>();

        private static readonly ReadOnlyCollection<TypeConstraintAst> s_emptyBaseTypesCollection =
            Utils.EmptyReadOnlyCollection<TypeConstraintAst>();

        
        public TypeDefinitionAst(IScriptExtent extent, string name, IEnumerable<AttributeAst> attributes, IEnumerable<MemberAst> members, TypeAttributes typeAttributes, IEnumerable<TypeConstraintAst> baseTypes)
            : base(extent)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            if (attributes != null && attributes.Any())
            {
                Attributes = new ReadOnlyCollection<AttributeAst>(attributes.ToArray());
                SetParents(Attributes);
            }
            else
            {
                Attributes = s_emptyAttributeList;
            }

            if (members != null && members.Any())
            {
                Members = new ReadOnlyCollection<MemberAst>(members.ToArray());
                SetParents(Members);
            }
            else
            {
                Members = s_emptyMembersCollection;
            }

            if (baseTypes != null && baseTypes.Any())
            {
                BaseTypes = new ReadOnlyCollection<TypeConstraintAst>(baseTypes.ToArray());
                SetParents(BaseTypes);
            }
            else
            {
                BaseTypes = s_emptyBaseTypesCollection;
            }

            this.Name = name;
            this.TypeAttributes = typeAttributes;
        }

        
        public string Name { get; }

        
        public ReadOnlyCollection<AttributeAst> Attributes { get; }

        
        public ReadOnlyCollection<TypeConstraintAst> BaseTypes { get; }

        
        public ReadOnlyCollection<MemberAst> Members { get; }

        
        public TypeAttributes TypeAttributes { get; }

        
        public bool IsEnum { get { return (TypeAttributes & TypeAttributes.Enum) == TypeAttributes.Enum; } }

        
        public bool IsClass { get { return (TypeAttributes & TypeAttributes.Class) == TypeAttributes.Class; } }

        
        public bool IsInterface { get { return (TypeAttributes & TypeAttributes.Interface) == TypeAttributes.Interface; } }

        internal Type Type
        {
            get
            {
                return _type;
            }

            set
            {
                // The assert may seem a little bit confusing.
                // It's because RuntimeType is not a public class and I don't want to use Name string in assert.
                // The basic idea is that Type field should go thru 3 stages:
                // 1. null
                // 2. TypeBuilder
                // 3. RuntimeType
                // We also allow wipe type (assign to null), because there could be errors.
                Diagnostics.Assert(value == null || _type == null || _type is TypeBuilder, "Type must be assigned only once to RuntimeType");
                _type = value;
            }
        }

        
        public override Ast Copy()
        {
            return new TypeDefinitionAst(Extent, Name, CopyElements(Attributes), CopyElements(Members), TypeAttributes, CopyElements(BaseTypes));
        }

        private Type _type;

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            var visitor2 = visitor as ICustomAstVisitor2;
            return visitor2?.VisitTypeDefinition(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = AstVisitAction.Continue;
            if (visitor is AstVisitor2 visitor2)
            {
                action = visitor2.VisitTypeDefinition(this);
                if (action == AstVisitAction.SkipChildren)
                    return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            }

            // REVIEW: should old visitors completely skip the attributes and
            // bodies of methods, or should they get a chance to see them.  If
            // we want to skip them, the code below needs to move up into the
            // above test 'visitor2 != null'.
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < Attributes.Count; index++)
                {
                    var attribute = Attributes[index];
                    action = attribute.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue)
                    {
                        break;
                    }
                }
            }

            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < BaseTypes.Count; index++)
                {
                    var baseTypes = BaseTypes[index];
                    action = baseTypes.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue)
                    {
                        break;
                    }
                }
            }

            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < Members.Count; index++)
                {
                    var member = Members[index];
                    action = member.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue)
                    {
                        break;
                    }
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public enum UsingStatementKind
    {
        
        Assembly = 0,

        
        Command = 1,

        
        Module = 2,

        
        Namespace = 3,

        
        Type = 4,
    }

    
    public class UsingStatementAst : StatementAst
    {
        
        public UsingStatementAst(IScriptExtent extent, UsingStatementKind kind, StringConstantExpressionAst name)
            : base(extent)
        {
            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            if (kind == UsingStatementKind.Command || kind == UsingStatementKind.Type)
            {
                throw PSTraceSource.NewArgumentException(nameof(kind));
            }

            UsingStatementKind = kind;
            Name = name;

            SetParent(Name);
        }

        
        public UsingStatementAst(IScriptExtent extent, HashtableAst moduleSpecification)
            : base(extent)
        {
            if (moduleSpecification == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(moduleSpecification));
            }

            UsingStatementKind = UsingStatementKind.Module;
            ModuleSpecification = moduleSpecification;

            SetParent(moduleSpecification);
        }

        
        public UsingStatementAst(IScriptExtent extent, UsingStatementKind kind, StringConstantExpressionAst aliasName,
                                 StringConstantExpressionAst resolvedAliasAst)
            : base(extent)
        {
            if (aliasName == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(aliasName));
            }

            if (resolvedAliasAst == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(resolvedAliasAst));
            }

            if (kind == UsingStatementKind.Assembly)
            {
                throw PSTraceSource.NewArgumentException(nameof(kind));
            }

            UsingStatementKind = kind;
            Name = aliasName;
            Alias = resolvedAliasAst;

            SetParent(Name);
            SetParent(Alias);
        }

        
        public UsingStatementAst(IScriptExtent extent, StringConstantExpressionAst aliasName, HashtableAst moduleSpecification)
            : base(extent)
        {
            if (moduleSpecification == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(moduleSpecification));
            }

            UsingStatementKind = UsingStatementKind.Module;
            ModuleSpecification = moduleSpecification;
            Name = aliasName;

            SetParent(moduleSpecification);
        }

        
        public UsingStatementKind UsingStatementKind { get; }

        
        public StringConstantExpressionAst Name { get; }

        
        public StringConstantExpressionAst Alias { get; }

        
        public HashtableAst ModuleSpecification { get; }

        
        internal PSModuleInfo ModuleInfo { get; private set; }

        
        public override Ast Copy()
        {
            var copy = Alias != null
                ? new UsingStatementAst(Extent, UsingStatementKind, Name, Alias)
                : new UsingStatementAst(Extent, UsingStatementKind, Name);
            copy.ModuleInfo = ModuleInfo;

            return copy;
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            var visitor2 = visitor as ICustomAstVisitor2;
            return visitor2?.VisitUsingStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = AstVisitAction.Continue;
            if (visitor is AstVisitor2 visitor2)
            {
                action = visitor2.VisitUsingStatement(this);
                if (action != AstVisitAction.Continue)
                    return visitor.CheckForPostAction(this, action);
            }

            if (Name != null)
                action = Name.InternalVisit(visitor);

            if (action != AstVisitAction.Continue)
                return visitor.CheckForPostAction(this, action);

            if (ModuleSpecification != null)
                action = ModuleSpecification.InternalVisit(visitor);

            if (action != AstVisitAction.Continue)
                return visitor.CheckForPostAction(this, action);

            if (Alias != null)
                action = Alias.InternalVisit(visitor);

            return visitor.CheckForPostAction(this, action);
        }

        #endregion

        
        internal ReadOnlyDictionary<string, TypeDefinitionAst> DefineImportedModule(PSModuleInfo moduleInfo)
        {
            var types = moduleInfo.GetExportedTypeDefinitions();
            ModuleInfo = moduleInfo;
            return types;
        }

        
        internal bool IsUsingModuleOrAssembly()
        {
            return UsingStatementKind == UsingStatementKind.Assembly || UsingStatementKind == UsingStatementKind.Module;
        }
    }

    
    public abstract class MemberAst : Ast
    {
        
        protected MemberAst(IScriptExtent extent) : base(extent)
        {
        }

        
        public abstract string Name { get; }

        internal abstract string GetTooltip();
    }

    
    [Flags]
    public enum PropertyAttributes
    {
        
        None = 0x00,

        
        Public = 0x01,

        
        Private = 0x02,

        
        Static = 0x10,

        
        Literal = 0x20,

        
        Hidden = 0x40,
    }

    
    public class PropertyMemberAst : MemberAst
    {
        private static readonly ReadOnlyCollection<AttributeAst> s_emptyAttributeList =
            Utils.EmptyReadOnlyCollection<AttributeAst>();

        
        public PropertyMemberAst(IScriptExtent extent, string name, TypeConstraintAst propertyType, IEnumerable<AttributeAst> attributes, PropertyAttributes propertyAttributes, ExpressionAst initialValue)
            : base(extent)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            if ((propertyAttributes & (PropertyAttributes.Private | PropertyAttributes.Public)) ==
                (PropertyAttributes.Private | PropertyAttributes.Public))
            {
                throw PSTraceSource.NewArgumentException(nameof(propertyAttributes));
            }

            Name = name;
            if (propertyType != null)
            {
                PropertyType = propertyType;
                SetParent(PropertyType);
            }

            if (attributes != null)
            {
                this.Attributes = new ReadOnlyCollection<AttributeAst>(attributes.ToArray());
                SetParents(Attributes);
            }
            else
            {
                this.Attributes = s_emptyAttributeList;
            }

            PropertyAttributes = propertyAttributes;
            InitialValue = initialValue;

            if (InitialValue != null)
            {
                SetParent(InitialValue);
            }
        }

        
        public override string Name { get; }

        
        public TypeConstraintAst PropertyType { get; }

        
        public ReadOnlyCollection<AttributeAst> Attributes { get; }

        
        public PropertyAttributes PropertyAttributes { get; }

        
        public ExpressionAst InitialValue { get; }

        
        public bool IsPublic { get { return (PropertyAttributes & PropertyAttributes.Public) != 0; } }

        
        public bool IsPrivate { get { return (PropertyAttributes & PropertyAttributes.Private) != 0; } }

        
        public bool IsHidden { get { return (PropertyAttributes & PropertyAttributes.Hidden) != 0; } }

        
        public bool IsStatic { get { return (PropertyAttributes & PropertyAttributes.Static) != 0; } }

        
        public override Ast Copy()
        {
            var newPropertyType = CopyElement(PropertyType);
            var newAttributes = CopyElements(Attributes);
            var newInitialValue = CopyElement(InitialValue);
            return new PropertyMemberAst(Extent, Name, newPropertyType, newAttributes, PropertyAttributes, newInitialValue);
        }

        internal override string GetTooltip()
        {
            var type = PropertyType != null ? PropertyType.TypeName.FullName : "object";
            return IsStatic
                ? "static " + type + " " + Name
                : type + " " + Name;
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            var visitor2 = visitor as ICustomAstVisitor2;
            return visitor2?.VisitPropertyMember(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = AstVisitAction.Continue;
            if (visitor is AstVisitor2 visitor2)
            {
                action = visitor2.VisitPropertyMember(this);
                if (action == AstVisitAction.SkipChildren)
                    return visitor.CheckForPostAction(this, AstVisitAction.Continue);

                if (action == AstVisitAction.Continue && PropertyType != null)
                    action = PropertyType.InternalVisit(visitor);

                if (action == AstVisitAction.Continue)
                {
                    for (int index = 0; index < Attributes.Count; index++)
                    {
                        var attributeAst = Attributes[index];
                        action = attributeAst.InternalVisit(visitor);
                        if (action != AstVisitAction.Continue)
                        {
                            break;
                        }
                    }
                }

                if (action == AstVisitAction.Continue && InitialValue != null)
                    action = InitialValue.InternalVisit(visitor);
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    [Flags]
    public enum MethodAttributes
    {
        
        None = 0x00,

        
        Public = 0x01,

        
        Private = 0x02,

        
        Static = 0x10,

        
        Hidden = 0x40,
    }

    
    public class FunctionMemberAst : MemberAst, IParameterMetadataProvider
    {
        private static readonly ReadOnlyCollection<AttributeAst> s_emptyAttributeList =
            Utils.EmptyReadOnlyCollection<AttributeAst>();

        private static readonly ReadOnlyCollection<ParameterAst> s_emptyParameterList =
            Utils.EmptyReadOnlyCollection<ParameterAst>();

        private readonly FunctionDefinitionAst _functionDefinitionAst;

        
        public FunctionMemberAst(IScriptExtent extent, FunctionDefinitionAst functionDefinitionAst, TypeConstraintAst returnType, IEnumerable<AttributeAst> attributes, MethodAttributes methodAttributes)
            : base(extent)
        {
            if (functionDefinitionAst == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(functionDefinitionAst));
            }

            if ((methodAttributes & (MethodAttributes.Private | MethodAttributes.Public)) ==
                (MethodAttributes.Private | MethodAttributes.Public))
            {
                throw PSTraceSource.NewArgumentException(nameof(methodAttributes));
            }

            if (returnType != null)
            {
                ReturnType = returnType;
                SetParent(returnType);
            }

            if (attributes != null)
            {
                this.Attributes = new ReadOnlyCollection<AttributeAst>(attributes.ToArray());
                SetParents(Attributes);
            }
            else
            {
                this.Attributes = s_emptyAttributeList;
            }

            _functionDefinitionAst = functionDefinitionAst;
            SetParent(functionDefinitionAst);
            MethodAttributes = methodAttributes;
        }

        
        public override string Name { get { return _functionDefinitionAst.Name; } }

        
        public ReadOnlyCollection<AttributeAst> Attributes { get; }

        
        public TypeConstraintAst ReturnType { get; }

        
        public ReadOnlyCollection<ParameterAst> Parameters
        {
            get { return _functionDefinitionAst.Parameters ?? s_emptyParameterList; }
        }

        
        public ScriptBlockAst Body { get { return _functionDefinitionAst.Body; } }

        
        public MethodAttributes MethodAttributes { get; }

        
        public bool IsPublic { get { return (MethodAttributes & MethodAttributes.Public) != 0; } }

        
        public bool IsPrivate { get { return (MethodAttributes & MethodAttributes.Private) != 0; } }

        
        public bool IsHidden { get { return (MethodAttributes & MethodAttributes.Hidden) != 0; } }

        
        public bool IsStatic { get { return (MethodAttributes & MethodAttributes.Static) != 0; } }

        
        public bool IsConstructor
        {
            get { return Name.Equals(((TypeDefinitionAst)Parent).Name, StringComparison.OrdinalIgnoreCase); }
        }

        internal IScriptExtent NameExtent { get { return _functionDefinitionAst.NameExtent; } }

        private string _toolTip;

        
        public override Ast Copy()
        {
            var newDefn = CopyElement(_functionDefinitionAst);
            var newReturnType = CopyElement(ReturnType);
            var newAttributes = CopyElements(Attributes);

            return new FunctionMemberAst(Extent, newDefn, newReturnType, newAttributes, MethodAttributes);
        }

        internal override string GetTooltip()
        {
            if (!string.IsNullOrEmpty(_toolTip))
            {
                return _toolTip;
            }

            var sb = new StringBuilder();
            var classMembers = ((TypeDefinitionAst)Parent).Members;
            for (int i = 0; i < classMembers.Count; i++)
            {
                var methodMember = classMembers[i] as FunctionMemberAst;
                if (methodMember is null ||
                    !Name.Equals(methodMember.Name) ||
                    IsStatic != methodMember.IsStatic)
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }

                if (methodMember.IsStatic)
                {
                    sb.Append("static ");
                }

                if (!methodMember.IsConstructor)
                {
                    sb.Append(methodMember.IsReturnTypeVoid() ? "void" : methodMember.ReturnType.TypeName.FullName);
                    sb.Append(' ');
                }

                sb.Append(methodMember.Name);
                sb.Append('(');
                for (int j = 0; j < methodMember.Parameters.Count; j++)
                {
                    if (j > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(methodMember.Parameters[j].GetTooltip());
                }

                sb.Append(')');
            }

            _toolTip = sb.ToString();
            return _toolTip;
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            var visitor2 = visitor as ICustomAstVisitor2;
            return visitor2?.VisitFunctionMember(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = AstVisitAction.Continue;
            if (visitor is AstVisitor2 visitor2)
            {
                action = visitor2.VisitFunctionMember(this);
                if (action == AstVisitAction.SkipChildren)
                    return visitor.CheckForPostAction(this, AstVisitAction.Continue);
                if (action == AstVisitAction.Continue)
                {
                    for (int index = 0; index < Attributes.Count; index++)
                    {
                        var attributeAst = Attributes[index];
                        action = attributeAst.InternalVisit(visitor);
                        if (action != AstVisitAction.Continue)
                        {
                            break;
                        }
                    }
                }

                if (action == AstVisitAction.Continue && ReturnType != null)
                    action = ReturnType.InternalVisit(visitor);

                if (action == AstVisitAction.Continue)
                    action = _functionDefinitionAst.InternalVisit(visitor);
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        #region IParameterMetadataProvider implementation

        bool IParameterMetadataProvider.HasAnyScriptBlockAttributes()
        {
            return ((IParameterMetadataProvider)_functionDefinitionAst).HasAnyScriptBlockAttributes();
        }

        ReadOnlyCollection<ParameterAst> IParameterMetadataProvider.Parameters
        {
            get { return ((IParameterMetadataProvider)_functionDefinitionAst).Parameters; }
        }

        RuntimeDefinedParameterDictionary IParameterMetadataProvider.GetParameterMetadata(bool automaticPositions, ref bool usesCmdletBinding)
        {
            return ((IParameterMetadataProvider)_functionDefinitionAst).GetParameterMetadata(automaticPositions, ref usesCmdletBinding);
        }

        IEnumerable<Attribute> IParameterMetadataProvider.GetScriptBlockAttributes()
        {
            return ((IParameterMetadataProvider)_functionDefinitionAst).GetScriptBlockAttributes();
        }

        IEnumerable<ExperimentalAttribute> IParameterMetadataProvider.GetExperimentalAttributes()
        {
            return ((IParameterMetadataProvider)_functionDefinitionAst).GetExperimentalAttributes();
        }

        bool IParameterMetadataProvider.UsesCmdletBinding()
        {
            return ((IParameterMetadataProvider)_functionDefinitionAst).UsesCmdletBinding();
        }

        PowerShell IParameterMetadataProvider.GetPowerShell(ExecutionContext context, Dictionary<string, object> variables, bool isTrustedInput,
            bool filterNonUsingVariables, bool? createLocalScope, params object[] args)
        {
            // OK?  I think this isn't reachable
            throw new NotSupportedException();
        }

        string IParameterMetadataProvider.GetWithInputHandlingForInvokeCommand()
        {
            // OK?  I think this isn't reachable
            throw new NotSupportedException();
        }

        Tuple<string, string> IParameterMetadataProvider.GetWithInputHandlingForInvokeCommandWithUsingExpression(Tuple<List<VariableExpressionAst>, string> usingVariablesTuple)
        {
            throw new NotImplementedException();
        }

        #endregion IParameterMetadataProvider implementation

        #region Internal helpers
        internal bool IsReturnTypeVoid()
        {
            if (ReturnType == null)
                return true;
            var typeName = ReturnType.TypeName as TypeName;
            return typeName != null && typeName.IsType(typeof(void));
        }

        internal Type GetReturnType()
        {
            return ReturnType == null ? typeof(void) : ReturnType.TypeName.GetReflectionType();
        }
        #endregion
    }

    internal enum SpecialMemberFunctionType
    {
        None,
        DefaultConstructor,
        StaticConstructor,
    }

    internal class CompilerGeneratedMemberFunctionAst : MemberAst, IParameterMetadataProvider
    {
        internal CompilerGeneratedMemberFunctionAst(IScriptExtent extent, TypeDefinitionAst definingType, SpecialMemberFunctionType type)
            : base(extent)
        {
            StatementAst statement = null;
            if (type == SpecialMemberFunctionType.DefaultConstructor)
            {
                var invokeMemberAst = new BaseCtorInvokeMemberExpressionAst(extent, extent, Array.Empty<ExpressionAst>());
                statement = new CommandExpressionAst(extent, invokeMemberAst, null);
            }

            Body = new ScriptBlockAst(extent, null, new StatementBlockAst(extent, statement == null ? null : new[] { statement }, null), false);
            this.SetParent(Body);
            definingType.SetParent(this);
            DefiningType = definingType;
            Type = type;
        }

        public override string Name
        {
            // This is fine for now, but if we add non-constructors, the name will be wrong.
            get { return DefiningType.Name; }
        }

        internal TypeDefinitionAst DefiningType { get; }

        internal SpecialMemberFunctionType Type { get; }

        internal override string GetTooltip()
        {
            return DefiningType.Name + " new()";
        }

        public override Ast Copy()
        {
            return new CompilerGeneratedMemberFunctionAst(Extent, (TypeDefinitionAst)DefiningType.Copy(), Type);
        }

        internal override object Accept(ICustomAstVisitor visitor)
        {
            Diagnostics.Assert(false, "code should be unreachable");
            return null;
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            Diagnostics.Assert(false, "code should be unreachable");
            return AstVisitAction.Continue;
        }

        public bool HasAnyScriptBlockAttributes()
        {
            return ((IParameterMetadataProvider)Body).HasAnyScriptBlockAttributes();
        }

        public RuntimeDefinedParameterDictionary GetParameterMetadata(bool automaticPositions, ref bool usesCmdletBinding)
        {
            return new RuntimeDefinedParameterDictionary { Data = RuntimeDefinedParameterDictionary.EmptyParameterArray };
        }

        public IEnumerable<Attribute> GetScriptBlockAttributes()
        {
            return ((IParameterMetadataProvider)Body).GetScriptBlockAttributes();
        }

        public IEnumerable<ExperimentalAttribute> GetExperimentalAttributes()
        {
            return ((IParameterMetadataProvider)Body).GetExperimentalAttributes();
        }

        public bool UsesCmdletBinding()
        {
            return false;
        }

        public ReadOnlyCollection<ParameterAst> Parameters { get { return null; } }

        public ScriptBlockAst Body { get; }

        public PowerShell GetPowerShell(ExecutionContext context, Dictionary<string, object> variables, bool isTrustedInput,
            bool filterNonUsingVariables, bool? createLocalScope, params object[] args)
        {
            Diagnostics.Assert(false, "code should be unreachable");
            return null;
        }

        public string GetWithInputHandlingForInvokeCommand()
        {
            Diagnostics.Assert(false, "code should be unreachable");
            return null;
        }

        public Tuple<string, string> GetWithInputHandlingForInvokeCommandWithUsingExpression(Tuple<List<VariableExpressionAst>, string> usingVariablesTuple)
        {
            Diagnostics.Assert(false, "code should be unreachable");
            return null;
        }
    }

    
    public class FunctionDefinitionAst : StatementAst, IParameterMetadataProvider
    {
        
        public FunctionDefinitionAst(IScriptExtent extent,
                                     bool isFilter,
                                     bool isWorkflow,
                                     string name,
                                     IEnumerable<ParameterAst> parameters,
                                     ScriptBlockAst body)
            : base(extent)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            if (body == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(body));
            }

            if (isFilter && isWorkflow)
            {
                throw PSTraceSource.NewArgumentException(nameof(isFilter));
            }

            this.IsFilter = isFilter;
            this.IsWorkflow = isWorkflow;

            this.Name = name;
            if (parameters != null && parameters.Any())
            {
                this.Parameters = new ReadOnlyCollection<ParameterAst>(parameters.ToArray());
                SetParents(Parameters);
            }

            this.Body = body;
            SetParent(body);
        }

        internal FunctionDefinitionAst(IScriptExtent extent,
                                       bool isFilter,
                                       bool isWorkflow,
                                       Token functionNameToken,
                                       IEnumerable<ParameterAst> parameters,
                                       ScriptBlockAst body)
            : this(extent,
                   isFilter,
                   isWorkflow,
                   (functionNameToken.Kind == TokenKind.Generic) ? ((StringToken)functionNameToken).Value : functionNameToken.Text,
                   parameters,
                   body)
        {
            NameExtent = functionNameToken.Extent;
        }

        
        public bool IsFilter { get; }

        
        public bool IsWorkflow { get; }

        
        public string Name { get; }

        
        public ReadOnlyCollection<ParameterAst> Parameters { get; }

        
        public ScriptBlockAst Body { get; }

        internal IScriptExtent NameExtent { get; private set; }

        
        public CommentHelpInfo GetHelpContent()
        {
            Dictionary<Ast, Token[]> scriptBlockTokenCache = new Dictionary<Ast, Token[]>();
            var commentTokens = HelpCommentsParser.GetHelpCommentTokens(this, scriptBlockTokenCache);
            if (commentTokens != null)
            {
                return HelpCommentsParser.GetHelpContents(commentTokens.Item1, commentTokens.Item2);
            }

            return null;
        }

        
        public CommentHelpInfo GetHelpContent(Dictionary<Ast, Token[]> scriptBlockTokenCache)
        {
            ArgumentNullException.ThrowIfNull(scriptBlockTokenCache);

            var commentTokens = HelpCommentsParser.GetHelpCommentTokens(this, scriptBlockTokenCache);
            if (commentTokens != null)
            {
                return HelpCommentsParser.GetHelpContents(commentTokens.Item1, commentTokens.Item2);
            }

            return null;
        }

        
        public override Ast Copy()
        {
            var newParameters = CopyElements(this.Parameters);
            var newBody = CopyElement(this.Body);

            return new FunctionDefinitionAst(this.Extent, this.IsFilter, this.IsWorkflow, this.Name, newParameters, newBody) { NameExtent = this.NameExtent };
        }

        internal string GetParamTextFromParameterList(Tuple<List<VariableExpressionAst>, string> usingVariablesTuple = null)
        {
            Diagnostics.Assert(Parameters != null, "Caller makes sure that Parameters is not null before calling this method.");

            string additionalNewUsingParams = null;
            IEnumerator<VariableExpressionAst> orderedUsingVars = null;
            if (usingVariablesTuple != null)
            {
                Diagnostics.Assert(
                    usingVariablesTuple.Item1 != null && usingVariablesTuple.Item1.Count > 0 && !string.IsNullOrEmpty(usingVariablesTuple.Item2),
                    "Caller makes sure the value passed in is not null or empty.");
                orderedUsingVars = usingVariablesTuple.Item1.OrderBy(static varAst => varAst.Extent.StartOffset).GetEnumerator();
                additionalNewUsingParams = usingVariablesTuple.Item2;
            }

            var sb = new StringBuilder("param(");
            string separator = string.Empty;

            if (additionalNewUsingParams != null)
            {
                // Add the $using variable parameters if necessary
                sb.Append(additionalNewUsingParams);
                separator = ", ";
            }

            for (int i = 0; i < Parameters.Count; i++)
            {
                var param = Parameters[i];
                sb.Append(separator);
                sb.Append(orderedUsingVars != null
                              ? param.GetParamTextWithDollarUsingHandling(orderedUsingVars)
                              : param.ToString());
                separator = ", ";
            }

            sb.Append(')');
            sb.Append(Environment.NewLine);

            return sb.ToString();
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitFunctionDefinition(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitFunctionDefinition(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
            {
                if (Parameters != null)
                {
                    for (int index = 0; index < Parameters.Count; index++)
                    {
                        var param = Parameters[index];
                        action = param.InternalVisit(visitor);
                        if (action != AstVisitAction.Continue) break;
                    }
                }

                if (action == AstVisitAction.Continue)
                    action = Body.InternalVisit(visitor);
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        #region IParameterMetadataProvider implementation

        bool IParameterMetadataProvider.HasAnyScriptBlockAttributes()
        {
            return ((IParameterMetadataProvider)Body).HasAnyScriptBlockAttributes();
        }

        RuntimeDefinedParameterDictionary IParameterMetadataProvider.GetParameterMetadata(bool automaticPositions, ref bool usesCmdletBinding)
        {
            if (Parameters != null)
            {
                return Compiler.GetParameterMetaData(Parameters, automaticPositions, ref usesCmdletBinding);
            }

            if (Body.ParamBlock != null)
            {
                return Compiler.GetParameterMetaData(Body.ParamBlock.Parameters, automaticPositions, ref usesCmdletBinding);
            }

            return new RuntimeDefinedParameterDictionary { Data = RuntimeDefinedParameterDictionary.EmptyParameterArray };
        }

        IEnumerable<Attribute> IParameterMetadataProvider.GetScriptBlockAttributes()
        {
            return ((IParameterMetadataProvider)Body).GetScriptBlockAttributes();
        }

        IEnumerable<ExperimentalAttribute> IParameterMetadataProvider.GetExperimentalAttributes()
        {
            return ((IParameterMetadataProvider)Body).GetExperimentalAttributes();
        }

        ReadOnlyCollection<ParameterAst> IParameterMetadataProvider.Parameters
        {
            get { return Parameters ?? (Body.ParamBlock?.Parameters); }
        }

        PowerShell IParameterMetadataProvider.GetPowerShell(ExecutionContext context, Dictionary<string, object> variables, bool isTrustedInput,
            bool filterNonUsingVariables, bool? createLocalScope, params object[] args)
        {
            ExecutionContext.CheckStackDepth();
            return ScriptBlockToPowerShellConverter.Convert(this.Body, this.Parameters, isTrustedInput, context, variables, filterNonUsingVariables, createLocalScope, args);
        }

        string IParameterMetadataProvider.GetWithInputHandlingForInvokeCommand()
        {
            string result = ((IParameterMetadataProvider)Body).GetWithInputHandlingForInvokeCommand();
            return Parameters == null ? result : (GetParamTextFromParameterList() + result);
        }

        Tuple<string, string> IParameterMetadataProvider.GetWithInputHandlingForInvokeCommandWithUsingExpression(
            Tuple<List<VariableExpressionAst>, string> usingVariablesTuple)
        {
            Tuple<string, string> result =
                ((IParameterMetadataProvider)Body).GetWithInputHandlingForInvokeCommandWithUsingExpression(usingVariablesTuple);

            if (Parameters == null)
            {
                return result;
            }

            string paramText = GetParamTextFromParameterList(usingVariablesTuple);
            return new Tuple<string, string>(paramText, result.Item2);
        }

        bool IParameterMetadataProvider.UsesCmdletBinding()
        {
            bool usesCmdletBinding = false;
            if (Parameters != null)
            {
                usesCmdletBinding = ParamBlockAst.UsesCmdletBinding(Parameters);
            }
            else if (Body.ParamBlock != null)
            {
                usesCmdletBinding = ((IParameterMetadataProvider)Body).UsesCmdletBinding();
            }

            return usesCmdletBinding;
        }

        #endregion IParameterMetadataProvider implementation
    }

    
    public class IfStatementAst : StatementAst
    {
        
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public IfStatementAst(IScriptExtent extent, IEnumerable<IfClause> clauses, StatementBlockAst elseClause)
            : base(extent)
        {
            if (clauses == null || !clauses.Any())
            {
                throw PSTraceSource.NewArgumentException(nameof(clauses));
            }

            this.Clauses = new ReadOnlyCollection<IfClause>(clauses.ToArray());
            SetParents(Clauses);

            if (elseClause != null)
            {
                this.ElseClause = elseClause;
                SetParent(elseClause);
            }
        }

        
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public ReadOnlyCollection<IfClause> Clauses { get; }

        
        public StatementBlockAst ElseClause { get; }

        
        public override Ast Copy()
        {
            var newClauses = new List<IfClause>(this.Clauses.Count);
            for (int i = 0; i < this.Clauses.Count; i++)
            {
                var clause = this.Clauses[i];
                var newCondition = CopyElement(clause.Item1);
                var newStatementBlock = CopyElement(clause.Item2);

                newClauses.Add(Tuple.Create(newCondition, newStatementBlock));
            }

            var newElseClause = CopyElement(this.ElseClause);
            return new IfStatementAst(this.Extent, newClauses, newElseClause);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitIfStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitIfStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < Clauses.Count; index++)
                {
                    var ifClause = Clauses[index];
                    action = ifClause.Item1.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                    action = ifClause.Item2.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            if (action == AstVisitAction.Continue && ElseClause != null)
            {
                action = ElseClause.InternalVisit(visitor);
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class DataStatementAst : StatementAst
    {
        private static readonly ExpressionAst[] s_emptyCommandsAllowed = Array.Empty<ExpressionAst>();

        
        public DataStatementAst(IScriptExtent extent,
                                string variableName,
                                IEnumerable<ExpressionAst> commandsAllowed,
                                StatementBlockAst body)
            : base(extent)
        {
            if (body == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(body));
            }

            if (string.IsNullOrWhiteSpace(variableName))
            {
                variableName = null;
            }

            this.Variable = variableName;
            if (commandsAllowed != null && commandsAllowed.Any())
            {
                this.CommandsAllowed = new ReadOnlyCollection<ExpressionAst>(commandsAllowed.ToArray());
                SetParents(CommandsAllowed);
                this.HasNonConstantAllowedCommand = CommandsAllowed.Any(static ast => ast is not StringConstantExpressionAst);
            }
            else
            {
                this.CommandsAllowed = new ReadOnlyCollection<ExpressionAst>(s_emptyCommandsAllowed);
            }

            this.Body = body;
            SetParent(body);
        }

        
        public string Variable { get; }

        
        public ReadOnlyCollection<ExpressionAst> CommandsAllowed { get; }

        
        public StatementBlockAst Body { get; }

        
        public override Ast Copy()
        {
            var newCommandsAllowed = CopyElements(this.CommandsAllowed);
            var newBody = CopyElement(this.Body);
            return new DataStatementAst(this.Extent, this.Variable, newCommandsAllowed, newBody);
        }

        internal bool HasNonConstantAllowedCommand { get; }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitDataStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitDataStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Body.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        #region Code Generation Details

        internal int TupleIndex { get; set; } = VariableAnalysis.Unanalyzed;

        #endregion Code Generation Details
    }

    #region Looping Statements

    
    public abstract class LabeledStatementAst : StatementAst
    {
        
        protected LabeledStatementAst(IScriptExtent extent, string label, PipelineBaseAst condition)
            : base(extent)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                label = null;
            }

            this.Label = label;
            if (condition != null)
            {
                this.Condition = condition;
                SetParent(condition);
            }
        }

        
        public string Label { get; }

        
        public PipelineBaseAst Condition { get; }
    }

    
    public abstract class LoopStatementAst : LabeledStatementAst
    {
        
        protected LoopStatementAst(IScriptExtent extent, string label, PipelineBaseAst condition, StatementBlockAst body)
            : base(extent, label, condition)
        {
            if (body == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(body));
            }

            this.Body = body;
            SetParent(body);
        }

        
        public StatementBlockAst Body { get; }
    }

    
    [Flags]
    public enum ForEachFlags
    {
        
        None = 0x00,

        
        Parallel = 0x01,

        // If any flags are added that impact evaluation of items during the foreach statement, then
        // a binder (and caching strategy) needs to be added similar to SwitchClauseEvalBinder.
    }

    
    public class ForEachStatementAst : LoopStatementAst
    {
        
        public ForEachStatementAst(IScriptExtent extent,
                                   string label,
                                   ForEachFlags flags,
                                   VariableExpressionAst variable,
                                   PipelineBaseAst expression,
                                   StatementBlockAst body)
            : base(extent, label, expression, body)
        {
            if (expression == null || variable == null)
            {
                throw PSTraceSource.NewArgumentNullException(expression == null ? "expression" : "variablePath");
            }

            this.Flags = flags;
            this.Variable = variable;
            SetParent(variable);
        }

        
        public ForEachStatementAst(IScriptExtent extent,
                                   string label,
                                   ForEachFlags flags,
                                   ExpressionAst throttleLimit,
                                   VariableExpressionAst variable,
                                   PipelineBaseAst expression,
                                   StatementBlockAst body)
            : this(extent, label, flags, variable, expression, body)
        {
            this.ThrottleLimit = throttleLimit;
            if (throttleLimit != null)
            {
                SetParent(throttleLimit);
            }
        }

        
        public VariableExpressionAst Variable { get; }

        
        public ExpressionAst ThrottleLimit { get; }

        
        public ForEachFlags Flags { get; }

        
        public override Ast Copy()
        {
            var newVariable = CopyElement(this.Variable);
            var newExpression = CopyElement(this.Condition);
            var newBody = CopyElement(this.Body);

            if (this.ThrottleLimit != null)
            {
                var newThrottleLimit = CopyElement(this.ThrottleLimit);
                return new ForEachStatementAst(this.Extent, this.Label, this.Flags, newThrottleLimit,
                                               newVariable, newExpression, newBody);
            }

            return new ForEachStatementAst(this.Extent, this.Label, this.Flags, newVariable, newExpression, newBody);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitForEachStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitForEachStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Variable.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Condition.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Body.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class ForStatementAst : LoopStatementAst
    {
        
        public ForStatementAst(IScriptExtent extent,
                               string label,
                               PipelineBaseAst initializer,
                               PipelineBaseAst condition,
                               PipelineBaseAst iterator,
                               StatementBlockAst body)
            : base(extent, label, condition, body)
        {
            if (initializer != null)
            {
                this.Initializer = initializer;
                SetParent(initializer);
            }

            if (iterator != null)
            {
                this.Iterator = iterator;
                SetParent(iterator);
            }
        }

        
        public PipelineBaseAst Initializer { get; }

        
        public PipelineBaseAst Iterator { get; }

        
        public override Ast Copy()
        {
            var newInitializer = CopyElement(this.Initializer);
            var newCondition = CopyElement(this.Condition);
            var newIterator = CopyElement(this.Iterator);
            var newBody = CopyElement(this.Body);

            return new ForStatementAst(this.Extent, this.Label, newInitializer, newCondition, newIterator, newBody);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitForStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitForStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue && Initializer != null)
                action = Initializer.InternalVisit(visitor);
            if (action == AstVisitAction.Continue && Condition != null)
                action = Condition.InternalVisit(visitor);
            if (action == AstVisitAction.Continue && Iterator != null)
                action = Iterator.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Body.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class DoWhileStatementAst : LoopStatementAst
    {
        
        public DoWhileStatementAst(IScriptExtent extent, string label, PipelineBaseAst condition, StatementBlockAst body)
            : base(extent, label, condition, body)
        {
            if (condition == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(condition));
            }
        }

        
        public override Ast Copy()
        {
            var newCondition = CopyElement(this.Condition);
            var newBody = CopyElement(this.Body);
            return new DoWhileStatementAst(this.Extent, this.Label, newCondition, newBody);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitDoWhileStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitDoWhileStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Condition.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Body.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class DoUntilStatementAst : LoopStatementAst
    {
        
        public DoUntilStatementAst(IScriptExtent extent, string label, PipelineBaseAst condition, StatementBlockAst body)
            : base(extent, label, condition, body)
        {
            if (condition == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(condition));
            }
        }

        
        public override Ast Copy()
        {
            var newCondition = CopyElement(this.Condition);
            var newBody = CopyElement(this.Body);
            return new DoUntilStatementAst(this.Extent, this.Label, newCondition, newBody);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitDoUntilStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitDoUntilStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Condition.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Body.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class WhileStatementAst : LoopStatementAst
    {
        
        public WhileStatementAst(IScriptExtent extent, string label, PipelineBaseAst condition, StatementBlockAst body)
            : base(extent, label, condition, body)
        {
            if (condition == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(condition));
            }
        }

        
        public override Ast Copy()
        {
            var newCondition = CopyElement(this.Condition);
            var newBody = CopyElement(this.Body);
            return new WhileStatementAst(this.Extent, this.Label, newCondition, newBody);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitWhileStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitWhileStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Condition.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Body.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    [Flags]
    public enum SwitchFlags
    {
        
        None = 0x00,

        
        File = 0x01,

        
        Regex = 0x02,

        
        Wildcard = 0x04,

        
        Exact = 0x08,

        
        CaseSensitive = 0x10,

        
        Parallel = 0x20,

        // If any flags are added that influence evaluation of switch elements,
        // then the caching strategy in SwitchClauseEvalBinder needs to be updated,
        // and possibly its _binderCache.
    }

    
    public class SwitchStatementAst : LabeledStatementAst
    {
        private static readonly SwitchClause[] s_emptyClauseArray = Array.Empty<SwitchClause>();

        
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public SwitchStatementAst(IScriptExtent extent,
                                  string label,
                                  PipelineBaseAst condition,
                                  SwitchFlags flags,
                                  IEnumerable<SwitchClause> clauses,
                                  StatementBlockAst @default)
            : base(extent, label, condition)
        {
            if ((clauses == null || !clauses.Any()) && @default == null)
            {
                // Must specify either clauses or default.  If neither, just complain about clauses as that's the most likely
                // invalid argument.
                throw PSTraceSource.NewArgumentException(nameof(clauses));
            }

            this.Flags = flags;
            this.Clauses = new ReadOnlyCollection<SwitchClause>(
                (clauses != null && clauses.Any()) ? clauses.ToArray() : s_emptyClauseArray);
            SetParents(Clauses);
            if (@default != null)
            {
                this.Default = @default;
                SetParent(@default);
            }
        }

        
        public SwitchFlags Flags { get; }

        
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public ReadOnlyCollection<SwitchClause> Clauses { get; }

        
        public StatementBlockAst Default { get; }

        
        public override Ast Copy()
        {
            var newCondition = CopyElement(this.Condition);
            var newDefault = CopyElement(this.Default);

            List<SwitchClause> newClauses = null;
            if (this.Clauses.Count > 0)
            {
                newClauses = new List<SwitchClause>(this.Clauses.Count);
                for (int i = 0; i < this.Clauses.Count; i++)
                {
                    var clause = this.Clauses[i];
                    var newSwitchItem1 = CopyElement(clause.Item1);
                    var newSwitchItem2 = CopyElement(clause.Item2);

                    newClauses.Add(Tuple.Create(newSwitchItem1, newSwitchItem2));
                }
            }

            return new SwitchStatementAst(this.Extent, this.Label, newCondition, this.Flags, newClauses, newDefault);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitSwitchStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitSwitchStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Condition.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < Clauses.Count; index++)
                {
                    var switchClauseAst = Clauses[index];
                    action = switchClauseAst.Item1.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                    action = switchClauseAst.Item2.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            if (action == AstVisitAction.Continue && Default != null)
                action = Default.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    #endregion Looping Statements

    #region Exception Handling Statements

    
    public class CatchClauseAst : Ast
    {
        private static readonly ReadOnlyCollection<TypeConstraintAst> s_emptyCatchTypes =
            Utils.EmptyReadOnlyCollection<TypeConstraintAst>();

        
        public CatchClauseAst(IScriptExtent extent, IEnumerable<TypeConstraintAst> catchTypes, StatementBlockAst body)
            : base(extent)
        {
            if (body == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(body));
            }

            if (catchTypes != null)
            {
                this.CatchTypes = new ReadOnlyCollection<TypeConstraintAst>(catchTypes.ToArray());
                SetParents(CatchTypes);
            }
            else
            {
                this.CatchTypes = s_emptyCatchTypes;
            }

            this.Body = body;
            SetParent(body);
        }

        
        public ReadOnlyCollection<TypeConstraintAst> CatchTypes { get; }

        
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "CatchAll")]
        public bool IsCatchAll { get { return CatchTypes.Count == 0; } }

        
        public StatementBlockAst Body { get; }

        
        public override Ast Copy()
        {
            var newCatchTypes = CopyElements(this.CatchTypes);
            var newBody = CopyElement(this.Body);
            return new CatchClauseAst(this.Extent, newCatchTypes, newBody);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitCatchClause(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitCatchClause(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            for (int index = 0; index < CatchTypes.Count; index++)
            {
                var catchType = CatchTypes[index];
                if (action != AstVisitAction.Continue) break;
                action = catchType.InternalVisit(visitor);
            }

            if (action == AstVisitAction.Continue)
                action = Body.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class TryStatementAst : StatementAst
    {
        private static readonly ReadOnlyCollection<CatchClauseAst> s_emptyCatchClauses =
            Utils.EmptyReadOnlyCollection<CatchClauseAst>();

        
        public TryStatementAst(IScriptExtent extent,
                               StatementBlockAst body,
                               IEnumerable<CatchClauseAst> catchClauses,
                               StatementBlockAst @finally)
            : base(extent)
        {
            if (body == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(body));
            }

            if ((catchClauses == null || !catchClauses.Any()) && @finally == null)
            {
                // If no catches and no finally, just complain about catchClauses as that's the most likely invalid argument.
                throw PSTraceSource.NewArgumentException(nameof(catchClauses));
            }

            this.Body = body;
            SetParent(body);
            if (catchClauses != null)
            {
                this.CatchClauses = new ReadOnlyCollection<CatchClauseAst>(catchClauses.ToArray());
                SetParents(CatchClauses);
            }
            else
            {
                this.CatchClauses = s_emptyCatchClauses;
            }

            if (@finally != null)
            {
                this.Finally = @finally;
                SetParent(@finally);
            }
        }

        
        public StatementBlockAst Body { get; }

        
        public ReadOnlyCollection<CatchClauseAst> CatchClauses { get; }

        
        public StatementBlockAst Finally { get; }

        
        public override Ast Copy()
        {
            var newBody = CopyElement(this.Body);
            var newCatchClauses = CopyElements(this.CatchClauses);
            var newFinally = CopyElement(this.Finally);

            return new TryStatementAst(this.Extent, newBody, newCatchClauses, newFinally);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitTryStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitTryStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Body.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < CatchClauses.Count; index++)
                {
                    var catchClause = CatchClauses[index];
                    action = catchClause.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            if (action == AstVisitAction.Continue && Finally != null)
                action = Finally.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class TrapStatementAst : StatementAst
    {
        
        public TrapStatementAst(IScriptExtent extent, TypeConstraintAst trapType, StatementBlockAst body)
            : base(extent)
        {
            if (body == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(body));
            }

            if (trapType != null)
            {
                this.TrapType = trapType;
                SetParent(trapType);
            }

            this.Body = body;
            SetParent(body);
        }

        
        public TypeConstraintAst TrapType { get; }

        
        public StatementBlockAst Body { get; }

        
        public override Ast Copy()
        {
            var newTrapType = CopyElement(this.TrapType);
            var newBody = CopyElement(this.Body);
            return new TrapStatementAst(this.Extent, newTrapType, newBody);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitTrap(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitTrap(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue && TrapType != null)
                action = TrapType.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Body.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    #endregion Exception Handling Statements

    #region Flow Control Statements

    
    public class BreakStatementAst : StatementAst
    {
        
        public BreakStatementAst(IScriptExtent extent, ExpressionAst label)
            : base(extent)
        {
            if (label != null)
            {
                this.Label = label;
                SetParent(label);
            }
        }

        
        public ExpressionAst Label { get; }

        
        public override Ast Copy()
        {
            var newLabel = CopyElement(this.Label);
            return new BreakStatementAst(this.Extent, newLabel);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitBreakStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitBreakStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue && Label != null)
                action = Label.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class ContinueStatementAst : StatementAst
    {
        
        public ContinueStatementAst(IScriptExtent extent, ExpressionAst label)
            : base(extent)
        {
            if (label != null)
            {
                this.Label = label;
                SetParent(label);
            }
        }

        
        public ExpressionAst Label { get; }

        
        public override Ast Copy()
        {
            var newLabel = CopyElement(this.Label);
            return new ContinueStatementAst(this.Extent, newLabel);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitContinueStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitContinueStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue && Label != null)
                action = Label.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class ReturnStatementAst : StatementAst
    {
        
        public ReturnStatementAst(IScriptExtent extent, PipelineBaseAst pipeline)
            : base(extent)
        {
            if (pipeline != null)
            {
                this.Pipeline = pipeline;
                SetParent(pipeline);
            }
        }

        
        public PipelineBaseAst Pipeline { get; }

        
        public override Ast Copy()
        {
            var newPipeline = CopyElement(this.Pipeline);
            return new ReturnStatementAst(this.Extent, newPipeline);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitReturnStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitReturnStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue && Pipeline != null)
                action = Pipeline.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class ExitStatementAst : StatementAst
    {
        
        public ExitStatementAst(IScriptExtent extent, PipelineBaseAst pipeline)
            : base(extent)
        {
            if (pipeline != null)
            {
                this.Pipeline = pipeline;
                SetParent(pipeline);
            }
        }

        
        public PipelineBaseAst Pipeline { get; }

        
        public override Ast Copy()
        {
            var newPipeline = CopyElement(this.Pipeline);
            return new ExitStatementAst(this.Extent, newPipeline);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitExitStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitExitStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue && Pipeline != null)
                action = Pipeline.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class ThrowStatementAst : StatementAst
    {
        
        public ThrowStatementAst(IScriptExtent extent, PipelineBaseAst pipeline)
            : base(extent)
        {
            if (pipeline != null)
            {
                this.Pipeline = pipeline;
                SetParent(pipeline);
            }
        }

        
        public PipelineBaseAst Pipeline { get; }

        
        public bool IsRethrow
        {
            get
            {
                if (Pipeline != null)
                    return false;

                var parent = Parent;
                while (parent != null)
                {
                    if (parent is CatchClauseAst)
                    {
                        return true;
                    }

                    parent = parent.Parent;
                }

                return false;
            }
        }

        
        public override Ast Copy()
        {
            var newPipeline = CopyElement(this.Pipeline);
            return new ThrowStatementAst(this.Extent, newPipeline);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitThrowStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitThrowStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue && Pipeline != null)
                action = Pipeline.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public abstract class ChainableAst : PipelineBaseAst
    {
        
        protected ChainableAst(IScriptExtent extent) : base(extent)
        {
        }
    }

    
    public class PipelineChainAst : ChainableAst
    {
        
        public PipelineChainAst(
            IScriptExtent extent,
            ChainableAst lhsChain,
            PipelineAst rhsPipeline,
            TokenKind chainOperator,
            bool background = false)
            : base(extent)
        {
            ArgumentNullException.ThrowIfNull(lhsChain);

            ArgumentNullException.ThrowIfNull(rhsPipeline);

            if (chainOperator != TokenKind.AndAnd && chainOperator != TokenKind.OrOr)
            {
                throw new ArgumentException(nameof(chainOperator));
            }

            LhsPipelineChain = lhsChain;
            RhsPipeline = rhsPipeline;
            Operator = chainOperator;
            Background = background;

            SetParent(LhsPipelineChain);
            SetParent(RhsPipeline);
        }

        
        public ChainableAst LhsPipelineChain { get; }

        
        public PipelineAst RhsPipeline { get; }

        
        public TokenKind Operator { get; }

        
        public bool Background { get; }

        
        public override Ast Copy()
        {
            return new PipelineChainAst(Extent, CopyElement(LhsPipelineChain), CopyElement(RhsPipeline), Operator, Background);
        }

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return (visitor as ICustomAstVisitor2)?.VisitPipelineChain(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            AstVisitAction action = AstVisitAction.Continue;

            // Can only visit new AST type if using AstVisitor2
            if (visitor is AstVisitor2 visitor2)
            {
                action = visitor2.VisitPipelineChain(this);
                if (action == AstVisitAction.SkipChildren)
                {
                    return visitor.CheckForPostAction(this, AstVisitAction.Continue);
                }
            }

            if (action == AstVisitAction.Continue)
            {
                action = LhsPipelineChain.InternalVisit(visitor);
            }

            if (action == AstVisitAction.Continue)
            {
                action = RhsPipeline.InternalVisit(visitor);
            }

            return visitor.CheckForPostAction(this, action);
        }
    }

    #endregion Flow Control Statements

    #region Pipelines

    
    public abstract class PipelineBaseAst : StatementAst
    {
        
        protected PipelineBaseAst(IScriptExtent extent)
            : base(extent)
        {
        }

        
        public virtual ExpressionAst GetPureExpression()
        {
            return null;
        }
    }

    
    public class PipelineAst : ChainableAst
    {
        
        public PipelineAst(IScriptExtent extent, IEnumerable<CommandBaseAst> pipelineElements, bool background)
            : base(extent)
        {
            if (pipelineElements == null || !pipelineElements.Any())
            {
                throw PSTraceSource.NewArgumentException(nameof(pipelineElements));
            }

            this.Background = background;
            this.PipelineElements = new ReadOnlyCollection<CommandBaseAst>(pipelineElements.ToArray());
            SetParents(PipelineElements);
        }

        
        public PipelineAst(IScriptExtent extent, IEnumerable<CommandBaseAst> pipelineElements) : this(extent, pipelineElements, background: false)
        {
        }

        
        public PipelineAst(IScriptExtent extent, CommandBaseAst commandAst, bool background)
            : base(extent)
        {
            if (commandAst == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(commandAst));
            }

            this.Background = background;
            this.PipelineElements = new ReadOnlyCollection<CommandBaseAst>(new CommandBaseAst[] { commandAst });
            SetParent(commandAst);
        }

        
        public PipelineAst(IScriptExtent extent, CommandBaseAst commandAst) : this(extent, commandAst, background: false)
        {
        }

        
        public ReadOnlyCollection<CommandBaseAst> PipelineElements { get; }

        
        public bool Background { get; internal set; }

        
        public override ExpressionAst GetPureExpression()
        {
            if (PipelineElements.Count != 1)
            {
                return null;
            }

            CommandExpressionAst expr = PipelineElements[0] as CommandExpressionAst;
            if (expr != null && expr.Redirections.Count == 0)
            {
                return expr.Expression;
            }

            return null;
        }

        
        public override Ast Copy()
        {
            var newPipelineElements = CopyElements(this.PipelineElements);
            return new PipelineAst(this.Extent, newPipelineElements, this.Background);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitPipeline(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitPipeline(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < PipelineElements.Count; index++)
                {
                    var commandAst = PipelineElements[index];
                    action = commandAst.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public abstract class CommandElementAst : Ast
    {
        
        protected CommandElementAst(IScriptExtent extent)
            : base(extent)
        {
        }
    }

    
    public class CommandParameterAst : CommandElementAst
    {
        
        public CommandParameterAst(IScriptExtent extent, string parameterName, ExpressionAst argument, IScriptExtent errorPosition)
            : base(extent)
        {
            if (errorPosition == null || string.IsNullOrEmpty(parameterName))
            {
                throw PSTraceSource.NewArgumentNullException(errorPosition == null ? "errorPosition" : "parameterName");
            }

            this.ParameterName = parameterName;
            if (argument != null)
            {
                this.Argument = argument;
                SetParent(argument);
            }

            this.ErrorPosition = errorPosition;
        }

        
        public string ParameterName { get; }

        
        public ExpressionAst Argument { get; }

        
        public IScriptExtent ErrorPosition { get; }

        
        public override Ast Copy()
        {
            var newArgument = CopyElement(this.Argument);
            return new CommandParameterAst(this.Extent, this.ParameterName, newArgument, this.ErrorPosition);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitCommandParameter(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitCommandParameter(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (Argument != null && action == AstVisitAction.Continue)
            {
                action = Argument.InternalVisit(visitor);
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public abstract class CommandBaseAst : StatementAst
    {
        private static readonly ReadOnlyCollection<RedirectionAst> s_emptyRedirections =
            Utils.EmptyReadOnlyCollection<RedirectionAst>();

        internal const int MaxRedirections = (int)RedirectionStream.Information + 1;

        
        protected CommandBaseAst(IScriptExtent extent, IEnumerable<RedirectionAst> redirections)
            : base(extent)
        {
            if (redirections != null)
            {
                this.Redirections = new ReadOnlyCollection<RedirectionAst>(redirections.ToArray());
                SetParents(Redirections);
            }
            else
            {
                this.Redirections = s_emptyRedirections;
            }
        }

        
        public ReadOnlyCollection<RedirectionAst> Redirections { get; }
    }

    
    public class CommandAst : CommandBaseAst
    {
        
        public CommandAst(IScriptExtent extent,
                          IEnumerable<CommandElementAst> commandElements,
                          TokenKind invocationOperator,
                          IEnumerable<RedirectionAst> redirections)
            : base(extent, redirections)
        {
            if (commandElements == null || !commandElements.Any())
            {
                throw PSTraceSource.NewArgumentException(nameof(commandElements));
            }

            if (invocationOperator != TokenKind.Dot && invocationOperator != TokenKind.Ampersand && invocationOperator != TokenKind.Unknown)
            {
                throw PSTraceSource.NewArgumentException(nameof(invocationOperator));
            }

            this.CommandElements = new ReadOnlyCollection<CommandElementAst>(commandElements.ToArray());
            SetParents(CommandElements);
            this.InvocationOperator = invocationOperator;
        }

        
        public ReadOnlyCollection<CommandElementAst> CommandElements { get; }

        
        public TokenKind InvocationOperator { get; }

        
        public string GetCommandName()
        {
            var name = CommandElements[0] as StringConstantExpressionAst;
            return name?.Value;
        }

        
        public DynamicKeyword DefiningKeyword { get; set; }

        
        public override Ast Copy()
        {
            var newCommandElements = CopyElements(this.CommandElements);
            var newRedirections = CopyElements(this.Redirections);
            return new CommandAst(this.Extent, newCommandElements, this.InvocationOperator, newRedirections)
            {
                DefiningKeyword = this.DefiningKeyword
            };
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitCommand(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitCommand(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < CommandElements.Count; index++)
                {
                    var commandElementAst = CommandElements[index];
                    action = commandElementAst.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < Redirections.Count; index++)
                {
                    var redirection = Redirections[index];
                    if (action == AstVisitAction.Continue)
                    {
                        action = redirection.InternalVisit(visitor);
                    }
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class CommandExpressionAst : CommandBaseAst
    {
        
        public CommandExpressionAst(IScriptExtent extent,
                                    ExpressionAst expression,
                                    IEnumerable<RedirectionAst> redirections)
            : base(extent, redirections)
        {
            if (expression == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(expression));
            }

            this.Expression = expression;
            SetParent(expression);
        }

        
        public ExpressionAst Expression { get; }

        
        public override Ast Copy()
        {
            var newExpression = CopyElement(this.Expression);
            var newRedirections = CopyElements(this.Redirections);
            return new CommandExpressionAst(this.Extent, newExpression, newRedirections);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitCommandExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitCommandExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Expression.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < Redirections.Count; index++)
                {
                    var redirection = Redirections[index];
                    if (action == AstVisitAction.Continue)
                    {
                        action = redirection.InternalVisit(visitor);
                    }
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public abstract class RedirectionAst : Ast
    {
        
        protected RedirectionAst(IScriptExtent extent, RedirectionStream from)
            : base(extent)
        {
            this.FromStream = from;
        }

        
        public RedirectionStream FromStream { get; }
    }

    
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public enum RedirectionStream
    {
        
        All = 0,

        
        Output = 1,

        
        Error = 2,

        
        Warning = 3,

        
        Verbose = 4,

        
        Debug = 5,

        
        Information = 6
    }

    
    public class MergingRedirectionAst : RedirectionAst
    {
        
        public MergingRedirectionAst(IScriptExtent extent, RedirectionStream from, RedirectionStream to)
            : base(extent, from)
        {
            this.ToStream = to;
        }

        
        public RedirectionStream ToStream { get; }

        
        public override Ast Copy()
        {
            return new MergingRedirectionAst(this.Extent, this.FromStream, this.ToStream);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitMergingRedirection(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitMergingRedirection(this);
            return visitor.CheckForPostAction(this, action == AstVisitAction.SkipChildren ? AstVisitAction.Continue : action);
        }

        #endregion Visitors
    }

    
    public class FileRedirectionAst : RedirectionAst
    {
        
        public FileRedirectionAst(IScriptExtent extent, RedirectionStream stream, ExpressionAst file, bool append)
            : base(extent, stream)
        {
            if (file == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(file));
            }

            this.Location = file;
            SetParent(file);
            this.Append = append;
        }

        
        public ExpressionAst Location { get; }

        
        public bool Append { get; }

        
        public override Ast Copy()
        {
            var newFile = CopyElement(this.Location);
            return new FileRedirectionAst(this.Extent, this.FromStream, newFile, this.Append);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitFileRedirection(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitFileRedirection(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Location.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    #endregion Pipelines

    
    public class AssignmentStatementAst : PipelineBaseAst
    {
        
        public AssignmentStatementAst(IScriptExtent extent, ExpressionAst left, TokenKind @operator, StatementAst right, IScriptExtent errorPosition)
            : base(extent)
        {
            if (left == null || right == null || errorPosition == null)
            {
                throw PSTraceSource.NewArgumentNullException(left == null ? "left" : right == null ? "right" : "errorPosition");
            }

            if ((@operator.GetTraits() & TokenFlags.AssignmentOperator) == 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(@operator));
            }

            // If the assignment is just an expression and the expression is not backgrounded then
            // remove the pipeline wrapping the expression.
            if (right is PipelineAst pipelineAst
                && !pipelineAst.Background
                && pipelineAst.PipelineElements.Count == 1
                && pipelineAst.PipelineElements[0] is CommandExpressionAst commandExpressionAst)
            {
                right = commandExpressionAst;
                right.ClearParent();
            }

            this.Operator = @operator;
            this.Left = left;
            SetParent(left);
            this.Right = right;
            SetParent(right);
            this.ErrorPosition = errorPosition;
        }

        
        public ExpressionAst Left { get; }

        
        public TokenKind Operator { get; }

        
        public StatementAst Right { get; }

        
        public IScriptExtent ErrorPosition { get; }

        
        public override Ast Copy()
        {
            var newLeft = CopyElement(this.Left);
            var newRight = CopyElement(this.Right);
            return new AssignmentStatementAst(this.Extent, newLeft, this.Operator, newRight, this.ErrorPosition);
        }

        
        public IEnumerable<ExpressionAst> GetAssignmentTargets()
        {
            if (Left is ArrayLiteralAst arrayExpression)
            {
                foreach (var element in arrayExpression.Elements)
                {
                    yield return element;
                }

                yield break;
            }

            yield return Left;
        }
        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitAssignmentStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitAssignmentStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Left.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Right.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public enum ConfigurationType
    {
        
        Resource = 0,

        
        Meta = 1
    }

    
    public class ConfigurationDefinitionAst : StatementAst
    {
        
        public ConfigurationDefinitionAst(IScriptExtent extent,
            ScriptBlockExpressionAst body,
            ConfigurationType type,
            ExpressionAst instanceName) : base(extent)
        {
            if (extent == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(extent));
            }

            if (body == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(body));
            }

            if (instanceName == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(instanceName));
            }

            this.Body = body;
            SetParent(body);
            this.ConfigurationType = type;
            this.InstanceName = instanceName;
            SetParent(instanceName);
        }

        
        public ScriptBlockExpressionAst Body { get; }

        
        public ConfigurationType ConfigurationType { get; }

        
        public ExpressionAst InstanceName { get; }

        
        public override Ast Copy()
        {
            ScriptBlockExpressionAst body = CopyElement(Body);
            ExpressionAst instanceName = CopyElement(InstanceName);
            return new ConfigurationDefinitionAst(Extent, body, ConfigurationType, instanceName)
            {
                LCurlyToken = this.LCurlyToken,
                ConfigurationToken = this.ConfigurationToken,
                CustomAttributes = this.CustomAttributes?.Select(static e => (AttributeAst)e.Copy())
            };
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            var visitor2 = visitor as ICustomAstVisitor2;
            return visitor2?.VisitConfigurationDefinition(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = AstVisitAction.Continue;
            if (visitor is AstVisitor2 visitor2)
            {
                action = visitor2.VisitConfigurationDefinition(this);
                if (action == AstVisitAction.SkipChildren)
                    return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            }

            if (action == AstVisitAction.Continue)
            {
                action = InstanceName.InternalVisit(visitor);
            }

            if (action == AstVisitAction.Continue)
            {
                Body.InternalVisit(visitor);
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        #region Internal methods/properties

        internal Token LCurlyToken { get; set; }

        internal Token ConfigurationToken { get; set; }

        internal IEnumerable<AttributeAst> CustomAttributes { get; set; }

        
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        internal List<DynamicKeyword> DefinedKeywords { get; set; }

        
        internal PipelineAst GenerateSetItemPipelineAst()
        {
            // **************************
            // Now construct the AST to call the function that will build the actual object.
            // This is composed of a call to command with the signature
            //    function PSDesiredStateConfiguration\\Configuration
            //    {
            //      param (
            //          ResourceModuleTuplesToImport,      # list of required resource-module tuples
            //          $OutputPath = ".",      # output path where the MOF will be placed
            //          $Name,                  # name of the configuration == name of the wrapper function
            //          [scriptblock]
            //              $Body,              # the body of the configuration
            //          [hashtable]
            //              $ArgsToBody,        # the argument values to pass to the body scriptblock
            //          [hashtable]
            //              $ConfigurationData, # a collection of property bags describe the configuration environment
            //          [string]
            //              $InstanceName = ""  # THe name of the configuration instance being created.
            //          [boolean]
            //               $IsMetaConfig = $false # the configuration to generated is a meta configuration
            //      )
            //   }

            var cea = new Collection<CommandElementAst>
            {
                new StringConstantExpressionAst(this.Extent,
                                                @"PSDesiredStateConfiguration\Configuration",
                                                StringConstantType.BareWord),
                new CommandParameterAst(LCurlyToken.Extent, "ArgsToBody", new VariableExpressionAst(LCurlyToken.Extent, "toBody", false), LCurlyToken.Extent),
                new CommandParameterAst(LCurlyToken.Extent, "Name", (ExpressionAst)InstanceName.Copy(), LCurlyToken.Extent)
            };

            // get import parameters
            var bodyStatements = Body.ScriptBlock.EndBlock.Statements;
            var resourceModulePairsToImport = new List<Tuple<string[], ModuleSpecification[], Version>>();
            var resourceBody = (from stm in bodyStatements where !IsImportCommand(stm, resourceModulePairsToImport) select (StatementAst)stm.Copy()).ToList();

            cea.Add(new CommandParameterAst(PositionUtilities.EmptyExtent, "ResourceModuleTuplesToImport", new ConstantExpressionAst(PositionUtilities.EmptyExtent, resourceModulePairsToImport), PositionUtilities.EmptyExtent));

            var scriptBlockBody = new ScriptBlockAst(Body.Extent,
                CustomAttributes?.Select(static att => (AttributeAst)att.Copy()).ToList(),
                null,
                new StatementBlockAst(Body.Extent, resourceBody, null),
                false, false);

            var scriptBlockExp = new ScriptBlockExpressionAst(Body.Extent, scriptBlockBody);

            // then the configuration scriptblock as -Body
            cea.Add(new CommandParameterAst(LCurlyToken.Extent, "Body", scriptBlockExp, LCurlyToken.Extent));

            cea.Add(new CommandParameterAst(LCurlyToken.Extent, "Outputpath", new VariableExpressionAst(LCurlyToken.Extent, "OutputPath", false), LCurlyToken.Extent));

            cea.Add(new CommandParameterAst(LCurlyToken.Extent, "ConfigurationData", new VariableExpressionAst(LCurlyToken.Extent, "ConfigurationData", false), LCurlyToken.Extent));

            cea.Add(new CommandParameterAst(LCurlyToken.Extent, "InstanceName", new VariableExpressionAst(LCurlyToken.Extent, "InstanceName", false), LCurlyToken.Extent));

            //
            // copy the configuration parameter to the new function parameter
            // the new set-item created function will have below parameters
            //    [cmdletbinding()]
            //    param(
            //            [string]
            //                $InstanceName,
            //            [string[]]
            //                $DependsOn,
            //            [string]
            //                $OutputPath,
            //            [hashtable]
            //            [Microsoft.PowerShell.DesiredStateConfiguration.ArgumentToConfigurationDataTransformation()]
            //               $ConfigurationData
            //        )
            //
            var attribAsts =
                ConfigurationBuildInParameterAttribAsts.Select(static attribAst => (AttributeAst)attribAst.Copy()).ToList();

            var paramAsts = ConfigurationBuildInParameters.Select(static paramAst => (ParameterAst)paramAst.Copy()).ToList();

            // the parameters defined in the configuration keyword will be combined with above parameters
            // it will be used to construct $ArgsToBody in the set-item created function boby using below statement
            //         $toBody = @{}+$PSBoundParameters
            //         $toBody.Remove(""OutputPath"")
            //         $toBody.Remove(""ConfigurationData"")
            //         $ConfigurationData = $psboundparameters[""ConfigurationData""]
            //         $Outputpath = $psboundparameters[""Outputpath""]
            if (Body.ScriptBlock.ParamBlock != null)
            {
                paramAsts.AddRange(Body.ScriptBlock.ParamBlock.Parameters.Select(static parameterAst => (ParameterAst)parameterAst.Copy()));
            }

            var paramBlockAst = new ParamBlockAst(this.Extent, attribAsts, paramAsts);

            var cmdAst = new CommandAst(this.Extent, cea, TokenKind.Unknown, null);

            var pipeLineAst = new PipelineAst(this.Extent, cmdAst, background: false);
            var funcStatements = ConfigurationExtraParameterStatements.Select(static statement => (StatementAst)statement.Copy()).ToList();
            funcStatements.Add(pipeLineAst);
            var statmentBlockAst = new StatementBlockAst(this.Extent, funcStatements, null);

            var funcBody = new ScriptBlockAst(Body.Extent,
                CustomAttributes?.Select(static att => (AttributeAst)att.Copy()).ToList(),
                paramBlockAst, statmentBlockAst, false, true);
            var funcBodyExp = new ScriptBlockExpressionAst(this.Extent, funcBody);

            #region "Construct Set-Item pipeline"

            // **************************
            // Now construct the AST to call the set-item cmdlet that will create the function
            // it will do set-item -Path function:\ConfigurationNameExpr -Value funcBody

            // create function:\confignameexpression
            var funcDriveStrExpr = new StringConstantExpressionAst(this.Extent, @"function:\",
                                                                   StringConstantType.BareWord);
            var funcPathStrExpr = new BinaryExpressionAst(this.Extent, funcDriveStrExpr,
                                                          TokenKind.Plus,
                                                          (ExpressionAst)InstanceName.Copy(),
                                                          this.Extent);

            var setItemCmdElements = new Collection<CommandElementAst>
            {
                new StringConstantExpressionAst(this.Extent, @"set-item",
                                                StringConstantType.BareWord),
                new CommandParameterAst(this.Extent, "Path",
                                        funcPathStrExpr,
                                        this.Extent),
                new CommandParameterAst(this.Extent, "Value", funcBodyExp,
                                        this.Extent)
            };

            // then the configuration scriptblock as function body

            var setItemCmdlet = new CommandAst(this.Extent, setItemCmdElements, TokenKind.Unknown, null);
            #endregion

            var returnPipelineAst = new PipelineAst(this.Extent, setItemCmdlet, background: false);

            SetParent(returnPipelineAst);

            return returnPipelineAst;
        }

        #endregion

        #region static fields/methods

        
        private static bool IsImportCommand(StatementAst stmt, List<Tuple<string[], ModuleSpecification[], Version>> resourceModulePairsToImport)
        {
            var dkwsAst = stmt as DynamicKeywordStatementAst;
            if (dkwsAst == null || !dkwsAst.Keyword.Keyword.Equals("Import-DscResource", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var commandAst = new CommandAst(dkwsAst.Extent, CopyElements(dkwsAst.CommandElements), TokenKind.Unknown, null);

            StaticBindingResult bindingResult = StaticParameterBinder.BindCommand(commandAst, false);
            ParameterBindingResult moduleNames = null;
            ParameterBindingResult resourceNames = null;
            ParameterBindingResult moduleVersion = null;
            const string nameParam = "Name";
            const string moduleNameParam = "ModuleName";
            const string moduleVersionParam = "ModuleVersion";

            // we are only interested in Name and ModuleName parameter
            foreach (var entry in bindingResult.BoundParameters)
            {
                string paramName = entry.Key;
                var paramValue = entry.Value;

                if ((paramName.Length <= nameParam.Length) && (paramName.AsSpan().Equals(nameParam.AsSpan(0, paramName.Length), StringComparison.OrdinalIgnoreCase)))
                {
                    resourceNames = paramValue;
                }
                // Since both parameters -ModuleName and -ModuleVersion has same start string i.e. Module so we will try to resolve it to -ModuleName
                // if user specifies like -Module
                if ((paramName.Length <= moduleNameParam.Length) && (paramName.AsSpan().Equals(moduleNameParam.AsSpan(0, paramName.Length), StringComparison.OrdinalIgnoreCase)))
                {
                    moduleNames = paramValue;
                }
                else if ((paramName.Length <= moduleVersionParam.Length) && (paramName.AsSpan().Equals(moduleVersionParam.AsSpan(0, paramName.Length), StringComparison.OrdinalIgnoreCase)))
                {
                    moduleVersion = paramValue;
                }
            }

            string[] resourceNamesTyped = new[] { "*" };
            ModuleSpecification[] moduleNamesTyped = null;
            Version moduleVersionTyped = null;

            if (resourceNames != null)
            {
                object resourceNameEvaluated;
                IsConstantValueVisitor.IsConstant(resourceNames.Value, out resourceNameEvaluated, true, true);
                resourceNamesTyped = LanguagePrimitives.ConvertTo<string[]>(resourceNameEvaluated);
            }

            if (moduleNames != null)
            {
                object moduleNameEvaluated;
                IsConstantValueVisitor.IsConstant(moduleNames.Value, out moduleNameEvaluated, true, true);
                moduleNamesTyped = LanguagePrimitives.ConvertTo<ModuleSpecification[]>(moduleNameEvaluated);
            }

            if (moduleVersion != null)
            {
                object moduleVersionEvaluated;
                IsConstantValueVisitor.IsConstant(moduleVersion.Value, out moduleVersionEvaluated, true, true);
                if (moduleVersionEvaluated is double)
                {
                    // this happens in case -ModuleVersion 1.0, then use extent text for that.
                    // The better way to do it would be define static binding API against CommandInfo, that holds information about parameter types.
                    // This way, we can avoid this ugly special-casing and say that -ModuleVersion has type [System.Version].
                    moduleVersionEvaluated = moduleVersion.Value.Extent.Text;
                }

                moduleVersionTyped = LanguagePrimitives.ConvertTo<Version>(moduleVersionEvaluated);

                // Use -ModuleVersion <Version> only in the case, if -ModuleName specified.
                // Override ModuleName versions.
                if (moduleNamesTyped != null && moduleNamesTyped.Length == 1)
                {
                    for (int i = 0; i < moduleNamesTyped.Length; i++)
                    {
                        moduleNamesTyped[i] = new ModuleSpecification(new Hashtable()
                        {
                            {"ModuleName", moduleNamesTyped[i].Name},
                            {"ModuleVersion", moduleVersionTyped}
                        });
                    }
                }
            }

            resourceModulePairsToImport.Add(new Tuple<string[], ModuleSpecification[], Version>(resourceNamesTyped, moduleNamesTyped, moduleVersionTyped));
            return true;
        }

        private const string ConfigurationBuildInParametersStr = @"
                        [cmdletbinding()]
                        param(
                            [string]
                                $InstanceName,
                            [string[]]
                                $DependsOn,
                            [PSCredential]
                                $PsDscRunAsCredential,
                            [string]
                                $OutputPath,
                            [hashtable]
                            [Microsoft.PowerShell.DesiredStateConfiguration.ArgumentToConfigurationDataTransformation()]
                               $ConfigurationData
                        )";

        private static IEnumerable<ParameterAst> ConfigurationBuildInParameters
        {
            get
            {
                if (s_configurationBuildInParameters == null)
                {
                    s_configurationBuildInParameters = new List<ParameterAst>();

                    Token[] tokens;
                    ParseError[] errors;
                    var sba = Parser.ParseInput(ConfigurationBuildInParametersStr, out tokens, out errors);
                    if (sba != null)
                    {
                        foreach (var parameterAst in sba.ParamBlock.Parameters)
                        {
                            s_configurationBuildInParameters.Add((ParameterAst)parameterAst.Copy());
                        }
                    }
                }

                return s_configurationBuildInParameters;
            }
        }

        private static List<ParameterAst> s_configurationBuildInParameters;

        private static IEnumerable<AttributeAst> ConfigurationBuildInParameterAttribAsts
        {
            get
            {
                if (s_configurationBuildInParameterAttrAsts == null)
                {
                    s_configurationBuildInParameterAttrAsts = new List<AttributeAst>();

                    Token[] tokens;
                    ParseError[] errors;
                    var sba = Parser.ParseInput(ConfigurationBuildInParametersStr, out tokens, out errors);
                    if (sba != null)
                    {
                        if (s_configurationBuildInParameters == null)
                        {
                            s_configurationBuildInParameters = new List<ParameterAst>();

                            foreach (var parameterAst in sba.ParamBlock.Parameters)
                            {
                                s_configurationBuildInParameters.Add((ParameterAst)parameterAst.Copy());
                            }
                        }

                        foreach (var attribAst in sba.ParamBlock.Attributes)
                        {
                            s_configurationBuildInParameterAttrAsts.Add((AttributeAst)attribAst.Copy());
                        }
                    }
                }

                return s_configurationBuildInParameterAttrAsts;
            }
        }

        private static List<AttributeAst> s_configurationBuildInParameterAttrAsts;

        private static IEnumerable<StatementAst> ConfigurationExtraParameterStatements
        {
            get
            {
                if (s_configurationExtraParameterStatements == null)
                {
                    s_configurationExtraParameterStatements = new List<StatementAst>();
                    Token[] tokens;
                    ParseError[] errors;
                    var sba = Parser.ParseInput(@"
                        Import-Module Microsoft.PowerShell.Management -Verbose:$false
                        Import-Module PSDesiredStateConfiguration -Verbose:$false
                        $toBody = @{}+$PSBoundParameters
                        $toBody.Remove(""OutputPath"")
                        $toBody.Remove(""ConfigurationData"")
                        $ConfigurationData = $psboundparameters[""ConfigurationData""]
                        $Outputpath = $psboundparameters[""Outputpath""]", out tokens, out errors);
                    if (sba != null)
                    {
                        foreach (var statementAst in sba.EndBlock.Statements)
                        {
                            s_configurationExtraParameterStatements.Add((StatementAst)statementAst.Copy());
                        }
                    }
                }

                return s_configurationExtraParameterStatements;
            }
        }

        private static List<StatementAst> s_configurationExtraParameterStatements;
        #endregion

    }

    
    public class DynamicKeywordStatementAst : StatementAst
    {
        
        public DynamicKeywordStatementAst(IScriptExtent extent,
            IEnumerable<CommandElementAst> commandElements) : base(extent)
        {
            if (commandElements == null || !commandElements.Any())
            {
                throw PSTraceSource.NewArgumentException(nameof(commandElements));
            }

            this.CommandElements = new ReadOnlyCollection<CommandElementAst>(commandElements.ToArray());
            SetParents(CommandElements);
        }

        
        public ReadOnlyCollection<CommandElementAst> CommandElements { get; }

        
        public override Ast Copy()
        {
            IEnumerable<CommandElementAst> commandElements = CopyElements(CommandElements);
            return new DynamicKeywordStatementAst(Extent, commandElements)
            {
                Keyword = this.Keyword,
                LCurly = this.LCurly,
                FunctionName = this.FunctionName,
                InstanceName = CopyElement(this.InstanceName),
                OriginalInstanceName = CopyElement(this.OriginalInstanceName),
                BodyExpression = CopyElement(this.BodyExpression),
                ElementName = this.ElementName,
            };
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            var visitor2 = visitor as ICustomAstVisitor2;
            return visitor2?.VisitDynamicKeywordStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = AstVisitAction.Continue;
            if (visitor is AstVisitor2 visitor2)
            {
                action = visitor2.VisitDynamicKeywordStatement(this);
                if (action == AstVisitAction.SkipChildren)
                    return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            }

            if (action == AstVisitAction.Continue)
            {
                foreach (CommandElementAst elementAst in CommandElements)
                {
                    action = elementAst.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue)
                        break;
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        #region Internal Properties/Methods

        internal DynamicKeyword Keyword
        {
            get
            {
                return _keyword;
            }

            set
            {
                _keyword = value.Copy();
            }
        }

        private DynamicKeyword _keyword;

        internal Token LCurly { get; set; }

        internal Token FunctionName { get; set; }

        internal ExpressionAst InstanceName { get; set; }

        internal ExpressionAst OriginalInstanceName { get; set; }

        internal ExpressionAst BodyExpression { get; set; }

        internal string ElementName { get; set; }

        private PipelineAst _commandCallPipelineAst;

        internal PipelineAst GenerateCommandCallPipelineAst()
        {
            if (_commandCallPipelineAst != null)
                return _commandCallPipelineAst;

            //
            // Now construct the AST to call the function that defines implements the keywords logic. There are
            // two different types of ASTs that may be generated, depending on the settings in the DynamicKeyword object.
            // The first type uses a fixed set of 4 arguments and has the command with the signature
            //  function moduleName\keyword
            //  {
            //      param (
            //          $KeywordData,       # the DynamicKeyword object processed by this rule.
            //          $Name,              # the value of the name expression syntax element. This may be null for keywords that don't take a name
            //          $Value,             # the body of the keyword - either a hashtable or a scriptblock
            //          $SourceMetadata     # a string containing the original source line information so that errors
            //      )
            //      # function logic...
            //  }
            //
            // The second type, where the DirectCall flag is set to true, simply calls the command directly.
            // In the original source, the keyword body will be a property collection where the allowed properties
            // in the collection correspond to the parameters on the actual called function.
            //
            var cea = new Collection<CommandElementAst>();

            //
            // First add the name of the command to call. If a module name has been provided
            // then use the module-qualified form of the command name..
            //
            if (string.IsNullOrEmpty(Keyword.ImplementingModule))
            {
                cea.Add(
                    new StringConstantExpressionAst(
                        FunctionName.Extent,
                        FunctionName.Text,
                        StringConstantType.BareWord));
            }
            else
            {
                cea.Add(
                    new StringConstantExpressionAst(
                        FunctionName.Extent,
                        Keyword.ImplementingModule + '\\' + FunctionName.Text,
                        StringConstantType.BareWord));
            }

            ExpressionAst expr = BodyExpression;
            if (Keyword.DirectCall)
            {
                // If this keyword takes a name, then add it as the parameter -InstanceName
                if (Keyword.NameMode != DynamicKeywordNameMode.NoName)
                {
                    cea.Add(
                        new CommandParameterAst(
                            FunctionName.Extent,
                            "InstanceName",
                            InstanceName,
                            FunctionName.Extent));
                }

                //
                // If it's a direct call keyword, then we just unravel the properties
                // in the hash literal expression and map them to parameters.
                // We've already checked to make sure that they're all valid names.
                //
                if (expr is HashtableAst hashtable)
                {
                    bool isHashtableValid = true;
                    //
                    // If it's a hash table, validate that only valid members have been specified.
                    //
                    foreach (var keyValueTuple in hashtable.KeyValuePairs)
                    {
                        var propName = keyValueTuple.Item1 as StringConstantExpressionAst;
                        if (propName == null)
                        {
                            isHashtableValid = false;
                            break;
                        }
                        else
                        {
                            if (!Keyword.Properties.ContainsKey(propName.Value))
                            {
                                isHashtableValid = false;
                                break;
                            }
                        }

                        if (keyValueTuple.Item2 is ErrorStatementAst)
                        {
                            isHashtableValid = false;
                            break;
                        }
                    }

                    if (isHashtableValid)
                    {
                        // Construct the real parameters if Hashtable is valid
                        foreach (var keyValueTuple in hashtable.KeyValuePairs)
                        {
                            var propName = (StringConstantExpressionAst)keyValueTuple.Item1;
                            ExpressionAst propValue = new SubExpressionAst(
                                FunctionName.Extent,
                                new StatementBlockAst(
                                    FunctionName.Extent,
                                    new StatementAst[] { (StatementAst)keyValueTuple.Item2.Copy() }, null));

                            cea.Add(
                                new CommandParameterAst(
                                    FunctionName.Extent,
                                    propName.Value,
                                    propValue,
                                    LCurly.Extent));
                        }
                    }
                    else
                    {
                        // Construct a fake parameter with the HashtableAst to be its value, so that
                        // tab completion on the property names would work.
                        cea.Add(
                            new CommandParameterAst(
                                FunctionName.Extent,
                                "InvalidPropertyHashtable",
                                hashtable,
                                LCurly.Extent));
                    }
                }
            }
            else
            {
                //
                // Add the -KeywordData parameter using the expression
                //  ([type]("System.Management.Automation.Language.DynamicKeyword"))::GetKeyword(name)
                // To invoke the method to get the keyword data object for that keyword.
                //
                var indexExpr = new InvokeMemberExpressionAst(
                    FunctionName.Extent,
                    new TypeExpressionAst(
                        FunctionName.Extent,
                        new TypeName(
                            FunctionName.Extent,
                            typeof(DynamicKeyword).FullName)),
                    new StringConstantExpressionAst(
                        FunctionName.Extent,
                        "GetKeyword",
                        StringConstantType.BareWord),
                    new List<ExpressionAst>
                        {
                            new StringConstantExpressionAst(
                                FunctionName.Extent,
                                FunctionName.Text,
                                StringConstantType.BareWord)
                        },
                    true);

                cea.Add(
                    new CommandParameterAst(
                        FunctionName.Extent,
                        "KeywordData",
                        indexExpr,
                        LCurly.Extent));

                //
                // Add the -Name parameter
                //
                cea.Add(
                    new CommandParameterAst(
                        FunctionName.Extent,
                        "Name",
                        InstanceName,
                        LCurly.Extent));

                //
                // Add the -Value parameter
                //
                cea.Add(
                    new CommandParameterAst(
                        LCurly.Extent,
                        "Value",
                        expr,
                        LCurly.Extent));

                //
                // Add the -SourceMetadata parameter
                //
                string sourceMetadata = FunctionName.Extent.File
                                        + "::" + FunctionName.Extent.StartLineNumber
                                        + "::" + FunctionName.Extent.StartColumnNumber
                                        + "::" + FunctionName.Extent.Text;
                cea.Add(
                    new CommandParameterAst(
                        LCurly.Extent, "SourceMetadata",
                        new StringConstantExpressionAst(
                            FunctionName.Extent,
                            sourceMetadata,
                            StringConstantType.BareWord),
                        LCurly.Extent));
            }

            //
            // Build the final statement - a pipeline containing a single element with is a CommandAst
            // containing the command we've built up.
            //
            var cmdAst = new CommandAst(FunctionName.Extent, cea, TokenKind.Unknown, null);
            cmdAst.DefiningKeyword = Keyword;
            _commandCallPipelineAst = new PipelineAst(FunctionName.Extent, cmdAst, background: false);
            return _commandCallPipelineAst;
        }

        #endregion
    }

    #endregion Statements

    #region Expressions

    
    public abstract class ExpressionAst : CommandElementAst
    {
        
        protected ExpressionAst(IScriptExtent extent)
            : base(extent)
        {
        }

        
        public virtual Type StaticType { get { return typeof(object); } }

        
        internal virtual bool ShouldPreserveOutputInCaseOfException()
        {
            if (this is not ParenExpressionAst and not SubExpressionAst)
            {
                PSTraceSource.NewInvalidOperationException();
            }

            if (!(this.Parent is CommandExpressionAst commandExpr))
            {
                return false;
            }

            var pipelineAst = commandExpr.Parent as PipelineAst;
            if (pipelineAst == null || pipelineAst.PipelineElements.Count > 1)
            {
                return false;
            }

            if (pipelineAst.Parent is ParenExpressionAst parenExpressionAst)
            {
                return parenExpressionAst.ShouldPreserveOutputInCaseOfException();
            }
            else
            {
                return (pipelineAst.Parent is StatementBlockAst || pipelineAst.Parent is NamedBlockAst);
            }
        }
    }

    
    public class TernaryExpressionAst : ExpressionAst
    {
        
        public TernaryExpressionAst(IScriptExtent extent, ExpressionAst condition, ExpressionAst ifTrue, ExpressionAst ifFalse)
            : base(extent)
        {
            Condition = condition ?? throw PSTraceSource.NewArgumentNullException(nameof(condition));
            IfTrue = ifTrue ?? throw PSTraceSource.NewArgumentNullException(nameof(ifTrue));
            IfFalse = ifFalse ?? throw PSTraceSource.NewArgumentNullException(nameof(ifFalse));

            SetParent(Condition);
            SetParent(IfTrue);
            SetParent(IfFalse);
        }

        
        public ExpressionAst Condition { get; }

        
        public ExpressionAst IfTrue { get; }

        
        public ExpressionAst IfFalse { get; }

        
        public override Ast Copy()
        {
            ExpressionAst newCondition = CopyElement(this.Condition);
            ExpressionAst newIfTrue = CopyElement(this.IfTrue);
            ExpressionAst newIfFalse = CopyElement(this.IfFalse);
            return new TernaryExpressionAst(this.Extent, newCondition, newIfTrue, newIfFalse);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            if (visitor is ICustomAstVisitor2 visitor2)
            {
                return visitor2.VisitTernaryExpression(this);
            }

            return null;
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = AstVisitAction.Continue;
            if (visitor is AstVisitor2 visitor2)
            {
                action = visitor2.VisitTernaryExpression(this);
                if (action == AstVisitAction.SkipChildren)
                {
                    return visitor.CheckForPostAction(this, AstVisitAction.Continue);
                }
            }

            if (action == AstVisitAction.Continue)
            {
                action = Condition.InternalVisit(visitor);
            }

            if (action == AstVisitAction.Continue)
            {
                action = IfTrue.InternalVisit(visitor);
            }

            if (action == AstVisitAction.Continue)
            {
                action = IfFalse.InternalVisit(visitor);
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class BinaryExpressionAst : ExpressionAst
    {
        
        public BinaryExpressionAst(IScriptExtent extent, ExpressionAst left, TokenKind @operator, ExpressionAst right, IScriptExtent errorPosition)
            : base(extent)
        {
            if ((@operator.GetTraits() & TokenFlags.BinaryOperator) == 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(@operator));
            }

            if (left == null || right == null || errorPosition == null)
            {
                throw PSTraceSource.NewArgumentNullException(left == null ? "left" : right == null ? "right" : "errorPosition");
            }

            this.Left = left;
            SetParent(left);
            this.Operator = @operator;
            this.Right = right;
            SetParent(right);
            this.ErrorPosition = errorPosition;
        }

        
        public TokenKind Operator { get; }

        
        public ExpressionAst Left { get; }

        
        public ExpressionAst Right { get; }

        
        public IScriptExtent ErrorPosition { get; }

        
        public override Ast Copy()
        {
            var newLeft = CopyElement(this.Left);
            var newRight = CopyElement(this.Right);
            return new BinaryExpressionAst(this.Extent, newLeft, this.Operator, newRight, this.ErrorPosition);
        }

        
        public override Type StaticType
        {
            get
            {
                switch (Operator)
                {
                    case TokenKind.Xor:
                    case TokenKind.And:
                    case TokenKind.Or:
                    case TokenKind.Is:
                        return typeof(bool);
                }

                return typeof(object);
            }
        }

        internal static readonly PSTypeName[] BoolTypeNameArray = new PSTypeName[] { new PSTypeName(typeof(bool)) };
        internal static readonly PSTypeName[] StringTypeNameArray = new PSTypeName[] { new PSTypeName(typeof(string)) };
        internal static readonly PSTypeName[] StringArrayTypeNameArray = new PSTypeName[] { new PSTypeName(typeof(string[])) };

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitBinaryExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitBinaryExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Left.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Right.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class UnaryExpressionAst : ExpressionAst
    {
        
        public UnaryExpressionAst(IScriptExtent extent, TokenKind tokenKind, ExpressionAst child)
            : base(extent)
        {
            if ((tokenKind.GetTraits() & TokenFlags.UnaryOperator) == 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(tokenKind));
            }

            if (child == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(child));
            }

            this.TokenKind = tokenKind;
            this.Child = child;
            SetParent(child);
        }

        
        public TokenKind TokenKind { get; }

        
        public ExpressionAst Child { get; }

        
        public override Ast Copy()
        {
            var newChild = CopyElement(this.Child);
            return new UnaryExpressionAst(this.Extent, this.TokenKind, newChild);
        }

        
        public override Type StaticType
        {
            get
            {
                return (TokenKind == TokenKind.Not || TokenKind == TokenKind.Exclaim)
                    ? typeof(bool)
                    : typeof(object);
            }
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitUnaryExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitUnaryExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Child.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class BlockStatementAst : StatementAst
    {
        
        public BlockStatementAst(IScriptExtent extent, Token kind, StatementBlockAst body)
            : base(extent)
        {
            if (kind == null || body == null)
            {
                throw PSTraceSource.NewArgumentNullException(kind == null ? "kind" : "body");
            }

            if (kind.Kind != TokenKind.Sequence && kind.Kind != TokenKind.Parallel)
            {
                throw PSTraceSource.NewArgumentException(nameof(kind));
            }

            this.Kind = kind;
            this.Body = body;
            SetParent(body);
        }

        
        public StatementBlockAst Body { get; }

        
        public Token Kind { get; }

        
        public override Ast Copy()
        {
            var newBody = CopyElement(this.Body);
            return new BlockStatementAst(this.Extent, this.Kind, newBody);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitBlockStatement(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitBlockStatement(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Body.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class AttributedExpressionAst : ExpressionAst, ISupportsAssignment, IAssignableValue
    {
        
        public AttributedExpressionAst(IScriptExtent extent, AttributeBaseAst attribute, ExpressionAst child)
            : base(extent)
        {
            if (attribute == null || child == null)
            {
                throw PSTraceSource.NewArgumentNullException(attribute == null ? "attribute" : "child");
            }

            this.Attribute = attribute;
            SetParent(attribute);
            this.Child = child;
            SetParent(child);
        }

        
        public ExpressionAst Child { get; }

        
        public AttributeBaseAst Attribute { get; }

        
        public override Ast Copy()
        {
            var newAttribute = CopyElement(this.Attribute);
            var newChild = CopyElement(this.Child);
            return new AttributedExpressionAst(this.Extent, newAttribute, newChild);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitAttributedExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitAttributedExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Attribute.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Child.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        #region Code Generation Details

        private ISupportsAssignment GetActualAssignableAst()
        {
            ExpressionAst child = this;
            var childAttributeAst = child as AttributedExpressionAst;
            while (childAttributeAst != null)
            {
                child = childAttributeAst.Child;
                childAttributeAst = child as AttributedExpressionAst;
            }

            // Semantic checks ensure this cast succeeds
            return (ISupportsAssignment)child;
        }

        private List<AttributeBaseAst> GetAttributes()
        {
            var attributes = new List<AttributeBaseAst>();
            var childAttributeAst = this;
            while (childAttributeAst != null)
            {
                attributes.Add(childAttributeAst.Attribute);
                childAttributeAst = childAttributeAst.Child as AttributedExpressionAst;
            }

            attributes.Reverse();
            return attributes;
        }

        IAssignableValue ISupportsAssignment.GetAssignableValue()
        {
            return this;
        }

        Expression IAssignableValue.GetValue(Compiler compiler, List<Expression> exprs, List<ParameterExpression> temps)
        {
            return (Expression)this.Accept(compiler);
        }

        Expression IAssignableValue.SetValue(Compiler compiler, Expression rhs)
        {
            var attributes = GetAttributes();
            var assignableValue = GetActualAssignableAst().GetAssignableValue();

            if (!(assignableValue is VariableExpressionAst variableExpr))
            {
                return assignableValue.SetValue(compiler, Compiler.ConvertValue(rhs, attributes));
            }

            return Compiler.CallSetVariable(Expression.Constant(variableExpr.VariablePath), rhs, Expression.Constant(attributes.ToArray()));
        }

        #endregion Code Generation Details
    }

    
    public class ConvertExpressionAst : AttributedExpressionAst, ISupportsAssignment
    {
        
        public ConvertExpressionAst(IScriptExtent extent, TypeConstraintAst typeConstraint, ExpressionAst child)
            : base(extent, typeConstraint, child)
        {
        }

        
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public TypeConstraintAst Type { get { return (TypeConstraintAst)Attribute; } }

        
        public override Ast Copy()
        {
            var newTypeConstraint = CopyElement(this.Type);
            var newChild = CopyElement(this.Child);
            return new ConvertExpressionAst(this.Extent, newTypeConstraint, newChild);
        }

        
        public override Type StaticType
        {
            get { return this.Type.TypeName.GetReflectionType() ?? typeof(object); }
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitConvertExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitConvertExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Type.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Child.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        #region Code Generation Details

        IAssignableValue ISupportsAssignment.GetAssignableValue()
        {
            if (Child is VariableExpressionAst varExpr && varExpr.TupleIndex >= 0)
            {
                // In the common case of a single cast on the lhs of an assignment, we may have saved the type of the
                // variable in the mutable tuple, so conversions will get generated elsewhere, and we can just use
                // the child as the assignable value.
                return varExpr;
            }

            return this;
        }

        internal bool IsRef()
        {
            return Type.TypeName.Name.Equals("ref", StringComparison.OrdinalIgnoreCase);
        }
        #endregion Code Generation Details
    }

    
    public class MemberExpressionAst : ExpressionAst, ISupportsAssignment
    {
        
        public MemberExpressionAst(IScriptExtent extent, ExpressionAst expression, CommandElementAst member, bool @static)
            : base(extent)
        {
            if (expression == null || member == null)
            {
                throw PSTraceSource.NewArgumentNullException(expression == null ? "expression" : "member");
            }

            this.Expression = expression;
            SetParent(expression);
            this.Member = member;
            SetParent(member);
            this.Static = @static;
        }

        
        public MemberExpressionAst(IScriptExtent extent, ExpressionAst expression, CommandElementAst member, bool @static, bool nullConditional)
            : this(extent, expression, member, @static)
        {
            this.NullConditional = nullConditional;
        }

        
        public ExpressionAst Expression { get; }

        
        public CommandElementAst Member { get; }

        
        public bool Static { get; }

        
        public bool NullConditional { get; protected set; }

        
        public override Ast Copy()
        {
            var newExpression = CopyElement(this.Expression);
            var newMember = CopyElement(this.Member);

            return new MemberExpressionAst(
                this.Extent,
                newExpression,
                newMember,
                this.Static,
                this.NullConditional);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitMemberExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitMemberExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Expression.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Member.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        IAssignableValue ISupportsAssignment.GetAssignableValue()
        {
            return new MemberAssignableValue { MemberExpression = this };
        }
    }

    
    public class InvokeMemberExpressionAst : MemberExpressionAst, ISupportsAssignment
    {
        
        public InvokeMemberExpressionAst(
            IScriptExtent extent,
            ExpressionAst expression,
            CommandElementAst method,
            IEnumerable<ExpressionAst> arguments,
            bool @static,
            IList<ITypeName> genericTypes)
            : base(extent, expression, method, @static)
        {
            if (arguments != null && arguments.Any())
            {
                this.Arguments = new ReadOnlyCollection<ExpressionAst>(arguments.ToArray());
                SetParents(Arguments);
            }

            if (genericTypes != null && genericTypes.Count > 0)
            {
                this.GenericTypeArguments = new ReadOnlyCollection<ITypeName>(genericTypes);
            }
        }

        
        public InvokeMemberExpressionAst(
            IScriptExtent extent,
            ExpressionAst expression,
            CommandElementAst method,
            IEnumerable<ExpressionAst> arguments,
            bool @static)
            : this(extent, expression, method, arguments, @static, genericTypes: null)
        {
        }

        
        public InvokeMemberExpressionAst(
            IScriptExtent extent,
            ExpressionAst expression,
            CommandElementAst method,
            IEnumerable<ExpressionAst> arguments,
            bool @static,
            bool nullConditional,
            IList<ITypeName> genericTypes)
            : this(extent, expression, method, arguments, @static, genericTypes)
        {
            this.NullConditional = nullConditional;
        }

        
        public InvokeMemberExpressionAst(
            IScriptExtent extent,
            ExpressionAst expression,
            CommandElementAst method,
            IEnumerable<ExpressionAst> arguments,
            bool @static,
            bool nullConditional)
            : this(extent, expression, method, arguments, @static, nullConditional, genericTypes: null)
        {
        }

        
        public ReadOnlyCollection<ITypeName> GenericTypeArguments { get; }

        
        public ReadOnlyCollection<ExpressionAst> Arguments { get; }

        
        public override Ast Copy()
        {
            var newExpression = CopyElement(this.Expression);
            var newMethod = CopyElement(this.Member);
            var newArguments = CopyElements(this.Arguments);

            return new InvokeMemberExpressionAst(
                this.Extent,
                newExpression,
                newMethod,
                newArguments,
                this.Static,
                this.NullConditional,
                this.GenericTypeArguments);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitInvokeMemberExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitInvokeMemberExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = this.InternalVisitChildren(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        internal AstVisitAction InternalVisitChildren(AstVisitor visitor)
        {
            var action = Expression.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Member.InternalVisit(visitor);
            if (action == AstVisitAction.Continue && Arguments != null)
            {
                for (int index = 0; index < Arguments.Count; index++)
                {
                    var arg = Arguments[index];
                    action = arg.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            return action;
        }

        #endregion Visitors

        IAssignableValue ISupportsAssignment.GetAssignableValue()
        {
            return new InvokeMemberAssignableValue { InvokeMemberExpressionAst = this };
        }
    }

    
    public class BaseCtorInvokeMemberExpressionAst : InvokeMemberExpressionAst
    {
        
        public BaseCtorInvokeMemberExpressionAst(IScriptExtent baseKeywordExtent, IScriptExtent baseCallExtent, IEnumerable<ExpressionAst> arguments)
            : base(
                baseCallExtent,
                new VariableExpressionAst(baseKeywordExtent, "this", false),
                new StringConstantExpressionAst(baseKeywordExtent, ".ctor", StringConstantType.BareWord),
                arguments,
                @static: false)
        {
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            AstVisitAction action = AstVisitAction.Continue;
            if (visitor is AstVisitor2 visitor2)
            {
                action = visitor2.VisitBaseCtorInvokeMemberExpression(this);
                if (action == AstVisitAction.SkipChildren)
                    return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            }

            if (action == AstVisitAction.Continue)
                action = this.InternalVisitChildren(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        internal override object Accept(ICustomAstVisitor visitor)
        {
            var visitor2 = visitor as ICustomAstVisitor2;
            return visitor2?.VisitBaseCtorInvokeMemberExpression(this);
        }
    }

    
#nullable enable
    public interface ITypeName
    {
        
        string FullName { get; }

        
        string Name { get; }

        
        string? AssemblyName { get; }

        
        bool IsArray { get; }

        
        bool IsGeneric { get; }

        
        Type? GetReflectionType();

        
        Type? GetReflectionAttributeType();

        
        IScriptExtent Extent { get; }
    }
#nullable restore

#nullable enable
    internal interface ISupportsTypeCaching
    {
        Type? CachedType { get; set; }
    }
#nullable restore

    
    public sealed class TypeName : ITypeName, ISupportsTypeCaching
    {
        private readonly string _name;
        private readonly IScriptExtent _extent;
        private readonly int _genericArgumentCount;
        private Type _type;

        internal TypeDefinitionAst _typeDefinitionAst;

        
        public TypeName(IScriptExtent extent, string name)
        {
            if (extent == null || string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentNullException(extent == null ? "extent" : "name");
            }

            var c = name[0];
            if (c == '[' || c == ']' || c == ',')
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            if (name.Contains('`'))
            {
                name = name.Replace("``", "`");
            }

            this._extent = extent;
            this._name = name;
        }

        
        public TypeName(IScriptExtent extent, string name, string assembly)
            : this(extent, name)
        {
            if (string.IsNullOrEmpty(assembly))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(assembly));
            }

            AssemblyName = assembly;
        }

        
        internal TypeName(IScriptExtent extent, string name, int genericArgumentCount)
            : this(extent, name)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(genericArgumentCount, 0);

            if (genericArgumentCount > 0 && !_name.Contains('`'))
            {
                _genericArgumentCount = genericArgumentCount;
            }
        }

        
        public string FullName { get { return AssemblyName != null ? _name + "," + AssemblyName : _name; } }

        
        public string Name { get { return _name; } }

        
        public string AssemblyName { get; internal set; }

        
        public bool IsArray { get { return false; } }

        
        public bool IsGeneric { get { return false; } }

        
        public IScriptExtent Extent { get { return _extent; } }

        internal bool HasDefaultCtor()
        {
            if (_typeDefinitionAst == null)
            {
                Type reflectionType = GetReflectionType();
                if (reflectionType == null)
                {
                    // we are pessimistic about default ctor presence.
                    return false;
                }

                return reflectionType.HasDefaultCtor();
            }

            bool hasExplicitCtor = false;
            foreach (var member in _typeDefinitionAst.Members)
            {
                if (member is FunctionMemberAst function)
                {
                    if (function.IsConstructor)
                    {
                        // TODO: add check for default values, once default values for parameters supported
                        if (function.Parameters.Count == 0)
                        {
                            return true;
                        }

                        hasExplicitCtor = true;
                    }
                }
            }
            // implicit ctor is default as well
            return !hasExplicitCtor;
        }

        
        public Type GetReflectionType()
        {
            if (_type == null)
            {
                Type type = _typeDefinitionAst != null ? _typeDefinitionAst.Type : TypeResolver.ResolveTypeName(this, out _);

                if (type is null && _genericArgumentCount > 0)
                {
                    // We try an alternate name only if it failed to resolve with the original name.
                    // This is because for a generic type like `System.Tuple<type1, type2>`, the original name `System.Tuple`
                    // can be resolved and hence `genericTypeName.TypeName.GetReflectionType()` in that case has always been
                    // returning the type `System.Tuple`. If we change to directly use the alternate name for resolution, the
                    // return value will become 'System.Tuple`1' in that case, and that's a breaking change.
                    TypeName newTypeName = new(
                        _extent,
                        string.Create(CultureInfo.InvariantCulture, $"{_name}`{_genericArgumentCount}"))
                    {
                        AssemblyName = AssemblyName
                    };

                    type = TypeResolver.ResolveTypeName(newTypeName, out _);
                }

                if (type != null)
                {
                    try
                    {
                        var unused = type.TypeHandle;
                    }
                    catch (NotSupportedException)
                    {
                        // If the value of 'type' comes from typeBuilder.AsType(), then the value of 'type.GetTypeInfo()'
                        // is actually the TypeBuilder instance itself. This is the same on both FullCLR and CoreCLR.
                        Diagnostics.Assert(_typeDefinitionAst != null, "_typeDefinitionAst can never be null");
                        return type;
                    }

                    Interlocked.CompareExchange(ref _type, type, null);
                }
            }

            return _type;
        }

        
        public Type GetReflectionAttributeType()
        {
            var result = GetReflectionType();
            if (result == null || !typeof(Attribute).IsAssignableFrom(result))
            {
                TypeName attrTypeName = new(_extent, $"{_name}Attribute", _genericArgumentCount)
                {
                    AssemblyName = AssemblyName
                };

                result = attrTypeName.GetReflectionType();
                if (result != null && !typeof(Attribute).IsAssignableFrom(result))
                {
                    result = null;
                }
            }

            return result;
        }

        internal void SetTypeDefinition(TypeDefinitionAst typeDefinitionAst)
        {
            Diagnostics.Assert(_typeDefinitionAst == null, "Class definition is already set and cannot be changed");
            Diagnostics.Assert(_type == null, "Cannot set class definition if type is already resolved");
            _typeDefinitionAst = typeDefinitionAst;
        }

        
        public override string ToString()
        {
            return FullName;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TypeName other))
                return false;

            if (!_name.Equals(other._name, StringComparison.OrdinalIgnoreCase))
                return false;

            if (AssemblyName == null)
                return other.AssemblyName == null;

            if (other.AssemblyName == null)
                return false;

            return AssemblyName.Equals(other.AssemblyName, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            var stringComparer = StringComparer.OrdinalIgnoreCase;
            var nameHashCode = stringComparer.GetHashCode(_name);
            if (AssemblyName == null)
                return nameHashCode;

            return Utils.CombineHashCodes(nameHashCode, stringComparer.GetHashCode(AssemblyName));
        }

        
        internal bool IsType(Type type)
        {
            string fullTypeName = type.FullName;
            if (fullTypeName.Equals(Name, StringComparison.OrdinalIgnoreCase))
                return true;
            int lastDotIndex = fullTypeName.LastIndexOf('.');
            if (lastDotIndex >= 0)
            {
                return fullTypeName.AsSpan(lastDotIndex + 1).Equals(Name, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        Type ISupportsTypeCaching.CachedType
        {
            get { return _type; }

            set { _type = value; }
        }
    }

    
    public sealed class GenericTypeName : ITypeName, ISupportsTypeCaching
    {
        private string _cachedFullName;
        private Type _cachedType;

        
        public GenericTypeName(IScriptExtent extent, ITypeName genericTypeName, IEnumerable<ITypeName> genericArguments)
        {
            if (genericTypeName == null || extent == null)
            {
                throw PSTraceSource.NewArgumentNullException(extent == null ? "extent" : "genericTypeName");
            }

            if (genericArguments == null)
            {
                throw PSTraceSource.NewArgumentException(nameof(genericArguments));
            }

            Extent = extent;
            this.TypeName = genericTypeName;
            this.GenericArguments = new ReadOnlyCollection<ITypeName>(genericArguments.ToArray());

            if (this.GenericArguments.Count == 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(genericArguments));
            }
        }

        
        public string FullName
        {
            get
            {
                if (_cachedFullName == null)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(TypeName.Name);
                    sb.Append('[');
                    bool first = true;
                    for (int index = 0; index < GenericArguments.Count; index++)
                    {
                        ITypeName typename = GenericArguments[index];
                        if (!first)
                        {
                            sb.Append(',');
                        }

                        first = false;
                        sb.Append(typename.FullName);
                    }

                    sb.Append(']');
                    var assemblyName = TypeName.AssemblyName;
                    if (assemblyName != null)
                    {
                        sb.Append(',');
                        sb.Append(assemblyName);
                    }

                    Interlocked.CompareExchange(ref _cachedFullName, sb.ToString(), null);
                }

                return _cachedFullName;
            }
        }

        
        public string Name
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(TypeName.Name);
                sb.Append('[');
                bool first = true;
                for (int index = 0; index < GenericArguments.Count; index++)
                {
                    ITypeName typename = GenericArguments[index];
                    if (!first)
                    {
                        sb.Append(',');
                    }

                    first = false;
                    sb.Append(typename.Name);
                }

                sb.Append(']');
                return sb.ToString();
            }
        }

        
        public string AssemblyName { get { return TypeName.AssemblyName; } }

        
        public bool IsArray { get { return false; } }

        
        public bool IsGeneric { get { return true; } }

        
        public ITypeName TypeName { get; }

        
        public ReadOnlyCollection<ITypeName> GenericArguments { get; }

        
        public IScriptExtent Extent { get; }

        
        public Type GetReflectionType()
        {
            if (_cachedType == null)
            {
                Type generic = GetGenericType(TypeName.GetReflectionType());

                if (generic != null && generic.ContainsGenericParameters)
                {
                    var argumentList = new List<Type>();
                    foreach (var arg in GenericArguments)
                    {
                        var type = arg.GetReflectionType();
                        if (type == null)
                            return null;
                        argumentList.Add(type);
                    }

                    try
                    {
                        var type = generic.MakeGenericType(argumentList.ToArray());
                        try
                        {
                            // We don't need the TypeHandle, but it's a good indication that a type doesn't
                            // support reflection which we need for many things.  So if this throws, we
                            // won't cache the type.  This situation arises while defining a type that
                            // refers to itself somehow (e.g. returns an instance of this type, array of
                            // this type, or this type as a generic parameter.)
                            // By not caching, we'll eventually get the real reflection capable type after
                            // the type is fully defined.
                            var unused = type.TypeHandle;
                        }
                        catch (NotSupportedException)
                        {
                            return type;
                        }

                        Interlocked.CompareExchange(ref _cachedType, type, null);
                    }
                    catch (Exception)
                    {
                        // We don't want to throw exception from GetReflectionType
                    }
                }
            }

            return _cachedType;
        }

        
        internal Type GetGenericType(Type generic)
        {
            if (generic == null || !generic.ContainsGenericParameters)
            {
                if (!TypeName.FullName.Contains('`'))
                {
                    TypeName newTypeName = new(
                        Extent,
                        string.Create(CultureInfo.InvariantCulture, $"{TypeName.Name}`{GenericArguments.Count}"))
                    {
                        AssemblyName = TypeName.AssemblyName
                    };

                    generic = newTypeName.GetReflectionType();
                }
            }

            return generic;
        }

        
        public Type GetReflectionAttributeType()
        {
            Type type = GetReflectionType();
            if (type == null)
            {
                Type generic = TypeName.GetReflectionAttributeType();
                if (generic == null || !generic.ContainsGenericParameters)
                {
                    if (!TypeName.FullName.Contains('`'))
                    {
                        TypeName newTypeName = new(
                            Extent,
                            string.Create(CultureInfo.InvariantCulture, $"{TypeName.Name}Attribute`{GenericArguments.Count}"))
                        {
                            AssemblyName = TypeName.AssemblyName
                        };

                        generic = newTypeName.GetReflectionType();
                    }
                }

                if (generic != null && generic.ContainsGenericParameters)
                {
                    type = generic.MakeGenericType((from arg in GenericArguments select arg.GetReflectionType()).ToArray());
                    Interlocked.CompareExchange(ref _cachedType, type, null);
                }
            }

            return type;
        }

        
        public override string ToString()
        {
            return FullName;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GenericTypeName other))
                return false;

            if (!TypeName.Equals(other.TypeName))
                return false;

            if (GenericArguments.Count != other.GenericArguments.Count)
                return false;

            var count = GenericArguments.Count;
            for (int i = 0; i < count; i++)
            {
                if (!GenericArguments[i].Equals(other.GenericArguments[i]))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hash = TypeName.GetHashCode();
            var count = GenericArguments.Count;
            for (int i = 0; i < count; i++)
            {
                hash = Utils.CombineHashCodes(hash, GenericArguments[i].GetHashCode());
            }

            return hash;
        }

        Type ISupportsTypeCaching.CachedType
        {
            get { return _cachedType; }

            set { _cachedType = value; }
        }
    }

    
    public sealed class ArrayTypeName : ITypeName, ISupportsTypeCaching
    {
        private string _cachedFullName;
        private Type _cachedType;

        
        public ArrayTypeName(IScriptExtent extent, ITypeName elementType, int rank)
        {
            if (extent == null || elementType == null)
            {
                throw PSTraceSource.NewArgumentNullException(extent == null ? "extent" : "name");
            }

            if (rank <= 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(rank));
            }

            Extent = extent;
            this.Rank = rank;
            this.ElementType = elementType;
        }

        private string GetName(bool includeAssemblyName)
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
                sb.Append(ElementType.Name);
                sb.Append('[');
                if (Rank > 1)
                {
                    sb.Append(',', Rank - 1);
                }

                sb.Append(']');
                if (includeAssemblyName)
                {
                    var assemblyName = ElementType.AssemblyName;
                    if (assemblyName != null)
                    {
                        sb.Append(',');
                        sb.Append(assemblyName);
                    }
                }
            }
            catch (InsufficientExecutionStackException)
            {
                throw new ScriptCallDepthException();
            }

            return sb.ToString();
        }

        
        public string FullName
        {
            get
            {
                if (_cachedFullName == null)
                {
                    Interlocked.CompareExchange(ref _cachedFullName, GetName(includeAssemblyName: true), null);
                }

                return _cachedFullName;
            }
        }

        
        public string Name
        {
            get { return GetName(includeAssemblyName: false); }
        }

        
        public string AssemblyName { get { return ElementType.AssemblyName; } }

        
        public bool IsArray { get { return true; } }

        
        public bool IsGeneric { get { return false; } }

        
        public ITypeName ElementType { get; }

        
        public int Rank { get; }

        
        public IScriptExtent Extent { get; }

        
        public Type GetReflectionType()
        {
            try
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
                if (_cachedType == null)
                {
                    Type elementType = ElementType.GetReflectionType();
                    if (elementType != null)
                    {
                        Type type = Rank == 1 ? elementType.MakeArrayType() : elementType.MakeArrayType(Rank);
                        try
                        {
                            // We don't need the TypeHandle, but it's a good indication that a type doesn't
                            // support reflection which we need for many things.  So if this throws, we
                            // won't cache the type.  This situation arises while defining a type that
                            // refers to itself somehow (e.g. returns an instance of this type, array of
                            // this type, or this type as a generic parameter.)
                            // By not caching, we'll eventually get the real reflection capable type after
                            // the type is fully defined.
                            var unused = type.TypeHandle;
                        }
                        catch (NotSupportedException)
                        {
                            return type;
                        }

                        Interlocked.CompareExchange(ref _cachedType, type, null);
                    }
                }
            }
            catch (InsufficientExecutionStackException)
            {
                throw new ScriptCallDepthException();
            }

            return _cachedType;
        }

        
        public Type GetReflectionAttributeType()
        {
            return null;
        }

        
        public override string ToString()
        {
            return FullName;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ArrayTypeName other))
                return false;

            return ElementType.Equals(other.ElementType) && Rank == other.Rank;
        }

        public override int GetHashCode()
        {
            return Utils.CombineHashCodes(ElementType.GetHashCode(), Rank.GetHashCode());
        }

        Type ISupportsTypeCaching.CachedType
        {
            get { return _cachedType; }

            set { _cachedType = value; }
        }
    }

    
    public sealed class ReflectionTypeName : ITypeName, ISupportsTypeCaching
    {
        private readonly Type _type;

        
        public ReflectionTypeName(Type type)
        {
            if (type == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(type));
            }

            _type = type;
        }

        
        public string FullName { get { return ToStringCodeMethods.Type(_type); } }

        
        public string Name { get { return FullName; } }

        
        public string AssemblyName { get { return _type.Assembly.FullName; } }

        
        public bool IsArray { get { return _type.IsArray; } }

        
        public bool IsGeneric { get { return _type.IsGenericType; } }

        
        public IScriptExtent Extent { get { return PositionUtilities.EmptyExtent; } }

        
        public Type GetReflectionType()
        {
            return _type;
        }

        
        public Type GetReflectionAttributeType()
        {
            return _type;
        }

        
        public override string ToString()
        {
            return FullName;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ReflectionTypeName other))
                return false;
            return _type == other._type;
        }

        public override int GetHashCode()
        {
            return _type.GetHashCode();
        }

        Type ISupportsTypeCaching.CachedType
        {
            get { return _type; }

            set { throw new InvalidOperationException(); }
        }
    }

    
    public class TypeExpressionAst : ExpressionAst
    {
        
        public TypeExpressionAst(IScriptExtent extent, ITypeName typeName)
            : base(extent)
        {
            if (typeName == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(typeName));
            }

            this.TypeName = typeName;
        }

        
        public ITypeName TypeName { get; }

        
        public override Ast Copy()
        {
            return new TypeExpressionAst(this.Extent, this.TypeName);
        }

        
        public override Type StaticType { get { return typeof(Type); } }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitTypeExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitTypeExpression(this);
            return visitor.CheckForPostAction(this, (action == AstVisitAction.SkipChildren ? AstVisitAction.Continue : action));
        }

        #endregion Visitors
    }

    
    public class VariableExpressionAst : ExpressionAst, ISupportsAssignment, IAssignableValue
    {
        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public VariableExpressionAst(IScriptExtent extent, string variableName, bool splatted)
            : base(extent)
        {
            if (string.IsNullOrEmpty(variableName))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(variableName));
            }

            this.VariablePath = new VariablePath(variableName);
            this.Splatted = splatted;
        }

        
        internal VariableExpressionAst(VariableToken token)
            : this(token.Extent, token.VariablePath, (token.Kind == TokenKind.SplattedVariable))
        {
        }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public VariableExpressionAst(IScriptExtent extent, VariablePath variablePath, bool splatted)
            : base(extent)
        {
            if (variablePath == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(variablePath));
            }

            this.VariablePath = variablePath;
            this.Splatted = splatted;
        }

        
        public VariablePath VariablePath { get; }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public bool Splatted { get; }

        
        public bool IsConstantVariable()
        {
            if (this.VariablePath.IsVariable)
            {
                string name = this.VariablePath.UnqualifiedPath;
                if (name.Equals(SpecialVariables.True, StringComparison.OrdinalIgnoreCase) ||
                    name.Equals(SpecialVariables.False, StringComparison.OrdinalIgnoreCase) ||
                    name.Equals(SpecialVariables.Null, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        
        public override Ast Copy()
        {
            return new VariableExpressionAst(this.Extent, this.VariablePath, this.Splatted);
        }

        internal bool IsSafeVariableReference(HashSet<string> validVariables, ref bool usesParameter)
        {
            bool ok = false;

            if (this.VariablePath.IsAnyLocal())
            {
                var varName = this.VariablePath.UnqualifiedPath;
                if (((validVariables != null) && validVariables.Contains(varName)) ||
                    varName.Equals(SpecialVariables.Args, StringComparison.OrdinalIgnoreCase))
                {
                    ok = true;
                    usesParameter = true;
                }
                else
                {
                    ok = !this.Splatted
                         && this.IsConstantVariable();
                }
            }

            return ok;
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitVariableExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitVariableExpression(this);
            return visitor.CheckForPostAction(this, (action == AstVisitAction.SkipChildren ? AstVisitAction.Continue : action));
        }

        #endregion Visitors

        #region Code Generation Details

        internal int TupleIndex { get; set; } = VariableAnalysis.Unanalyzed;

        internal bool Automatic { get; set; }

        internal bool Assigned { get; set; }

        IAssignableValue ISupportsAssignment.GetAssignableValue()
        {
            return this;
        }

        Expression IAssignableValue.GetValue(Compiler compiler, List<Expression> exprs, List<ParameterExpression> temps)
        {
            return (Expression)compiler.VisitVariableExpression(this);
        }

        Expression IAssignableValue.SetValue(Compiler compiler, Expression rhs)
        {
            if (this.VariablePath.IsVariable && this.VariablePath.UnqualifiedPath.Equals(SpecialVariables.Null, StringComparison.OrdinalIgnoreCase))
            {
                return rhs;
            }

            IEnumerable<PropertyInfo> tupleAccessPath;
            bool localInTuple;
            Type targetType = GetVariableType(compiler, out tupleAccessPath, out localInTuple);

            // Value types must be copied on assignment (if they are mutable), boxed or not.  This preserves language
            // semantics from V1/V2, and might be slightly more natural for a dynamic language.
            // To generate good code, we assume any object or PSObject could be a boxed value type, and generate a dynamic
            // site to handle copying as necessary.  Given this assumption, here are the possibilities:
            //
            //     - rhs is reference type
            //         * just convert to lhs type
            //     - rhs is value type
            //         * lhs type is value type, copy made by simple assignment
            //         * lhs type is object, copy is made by boxing, also handled in simple assignment
            //     - rhs is boxed value type
            //         * lhs type is value type, copy is made by unboxing conversion
            //         * lhs type is object/psobject, copy must be made dynamically (boxed value type can't be known statically)
            var rhsType = rhs.Type;
            if (localInTuple &&
                (targetType == typeof(object) || targetType == typeof(PSObject)) &&
                (rhsType == typeof(object) || rhsType == typeof(PSObject)))
            {
                rhs = DynamicExpression.Dynamic(PSVariableAssignmentBinder.Get(), typeof(object), rhs);
            }

            rhs = rhs.Convert(targetType);

            if (!localInTuple)
            {
                return Compiler.CallSetVariable(Expression.Constant(VariablePath), rhs);
            }

            Expression lhs = compiler.LocalVariablesParameter;
            foreach (var property in tupleAccessPath)
            {
                lhs = Expression.Property(lhs, property);
            }

            return Expression.Assign(lhs, rhs);
        }

        internal Type GetVariableType(Compiler compiler, out IEnumerable<PropertyInfo> tupleAccessPath, out bool localInTuple)
        {
            Type targetType = null;
            localInTuple = TupleIndex >= 0 &&
                                (compiler.Optimize || TupleIndex < (int)AutomaticVariable.NumberOfAutomaticVariables);
            tupleAccessPath = null;
            if (localInTuple)
            {
                tupleAccessPath = MutableTuple.GetAccessPath(compiler.LocalVariablesTupleType, TupleIndex);
                targetType = tupleAccessPath.Last().PropertyType;
            }
            else
            {
                targetType = typeof(object);
            }

            return targetType;
        }

        #endregion Code Generation Details
    }

    
    public class ConstantExpressionAst : ExpressionAst
    {
        
        public ConstantExpressionAst(IScriptExtent extent, object value)
            : base(extent)
        {
            this.Value = value;
        }

        internal ConstantExpressionAst(NumberToken token)
            : base(token.Extent)
        {
            this.Value = token.Value;
        }

        
        public object Value { get; }

        
        public override Ast Copy()
        {
            return new ConstantExpressionAst(this.Extent, this.Value);
        }

        
        public override Type StaticType
        {
            get { return Value != null ? Value.GetType() : typeof(object); }
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitConstantExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitConstantExpression(this);
            return visitor.CheckForPostAction(this, (action == AstVisitAction.SkipChildren ? AstVisitAction.Continue : action));
        }

        #endregion Visitors
    }

    
    public enum StringConstantType
    {
        
        SingleQuoted,

        
        SingleQuotedHereString,

        
        DoubleQuoted,

        
        DoubleQuotedHereString,

        
        BareWord
    }

    
    public class StringConstantExpressionAst : ConstantExpressionAst
    {
        
        public StringConstantExpressionAst(IScriptExtent extent, string value, StringConstantType stringConstantType)
            : base(extent, value)
        {
            if (value == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(value));
            }

            this.StringConstantType = stringConstantType;
        }

        internal StringConstantExpressionAst(StringToken token)
            : base(token.Extent, token.Value)
        {
            this.StringConstantType = MapTokenKindToStringConstantKind(token);
        }

        
        public StringConstantType StringConstantType { get; }

        
        public new string Value { get { return (string)base.Value; } }

        
        public override Ast Copy()
        {
            return new StringConstantExpressionAst(this.Extent, this.Value, this.StringConstantType);
        }

        
        public override Type StaticType
        {
            get { return typeof(string); }
        }

        internal static StringConstantType MapTokenKindToStringConstantKind(Token token)
        {
            switch (token.Kind)
            {
                case TokenKind.StringExpandable:
                    return StringConstantType.DoubleQuoted;
                case TokenKind.HereStringLiteral:
                    return StringConstantType.SingleQuotedHereString;
                case TokenKind.HereStringExpandable:
                    return StringConstantType.DoubleQuotedHereString;
                case TokenKind.StringLiteral:
                    return StringConstantType.SingleQuoted;
                case TokenKind.Generic:
                    return StringConstantType.BareWord;
            }

            throw PSTraceSource.NewInvalidOperationException();
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitStringConstantExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitStringConstantExpression(this);
            return visitor.CheckForPostAction(this, (action == AstVisitAction.SkipChildren ? AstVisitAction.Continue : action));
        }

        #endregion Visitors
    }

    
    public class ExpandableStringExpressionAst : ExpressionAst
    {
        
        public ExpandableStringExpressionAst(IScriptExtent extent,
                                             string value,
                                             StringConstantType type)
            : base(extent)
        {
            if (value == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(value));
            }

            if (type != StringConstantType.DoubleQuoted && type != StringConstantType.DoubleQuotedHereString
                && type != StringConstantType.BareWord)
            {
                throw PSTraceSource.NewArgumentException(nameof(type));
            }

            var ast = Language.Parser.ScanString(value);
            if (ast is ExpandableStringExpressionAst expandableStringAst)
            {
                this.FormatExpression = expandableStringAst.FormatExpression;
                this.NestedExpressions = expandableStringAst.NestedExpressions;
            }
            else
            {
                // We always compile to a format expression.  In the rare case that some external code (this can't happen
                // internally) passes in a string that doesn't require any expansion, we still need to generate code that
                // works.  This is slow as the ast is a string constant, but it should be rare.
                this.FormatExpression = "{0}";
                this.NestedExpressions = new ReadOnlyCollection<ExpressionAst>(new[] { ast });
            }

            // Set parents properly
            for (int i = 0; i < this.NestedExpressions.Count; i++)
            {
                this.NestedExpressions[i].ClearParent();
            }

            SetParents(this.NestedExpressions);

            this.Value = value;
            this.StringConstantType = type;
        }

        
        internal ExpandableStringExpressionAst(Token token, string value, string formatString, IEnumerable<ExpressionAst> nestedExpressions)
            : this(token.Extent, value, formatString,
                   StringConstantExpressionAst
                        .MapTokenKindToStringConstantKind(token),
                   nestedExpressions)
        {
        }

        private ExpandableStringExpressionAst(IScriptExtent extent, string value, string formatString,
                                              StringConstantType type, IEnumerable<ExpressionAst> nestedExpressions)
            : base(extent)
        {
            Diagnostics.Assert(nestedExpressions != null && nestedExpressions.Any(), "Must specify non-empty expressions.");

            this.FormatExpression = formatString;
            this.Value = value;
            this.StringConstantType = type;
            this.NestedExpressions = new ReadOnlyCollection<ExpressionAst>(nestedExpressions.ToArray());
            SetParents(NestedExpressions);
        }

        
        public string Value { get; }

        
        public StringConstantType StringConstantType { get; }

        
        public ReadOnlyCollection<ExpressionAst> NestedExpressions { get; }

        
        public override Ast Copy()
        {
            var newNestedExpressions = CopyElements(this.NestedExpressions);
            return new ExpandableStringExpressionAst(this.Extent, this.Value, this.FormatExpression, this.StringConstantType, newNestedExpressions);
        }

        
        public override Type StaticType
        {
            get { return typeof(string); }
        }

        
        internal string FormatExpression { get; }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitExpandableStringExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitExpandableStringExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue && NestedExpressions != null)
            {
                for (int index = 0; index < NestedExpressions.Count; index++)
                {
                    var exprAst = NestedExpressions[index];
                    action = exprAst.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class ScriptBlockExpressionAst : ExpressionAst
    {
        
        public ScriptBlockExpressionAst(IScriptExtent extent, ScriptBlockAst scriptBlock)
            : base(extent)
        {
            if (scriptBlock == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(scriptBlock));
            }

            this.ScriptBlock = scriptBlock;
            SetParent(scriptBlock);
        }

        
        public ScriptBlockAst ScriptBlock { get; }

        
        public override Ast Copy()
        {
            var newScriptBlock = CopyElement(this.ScriptBlock);
            return new ScriptBlockExpressionAst(this.Extent, newScriptBlock);
        }

        
        public override Type StaticType
        {
            get { return typeof(ScriptBlock); }
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitScriptBlockExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitScriptBlockExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = ScriptBlock.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class ArrayLiteralAst : ExpressionAst, ISupportsAssignment
    {
        
        public ArrayLiteralAst(IScriptExtent extent, IList<ExpressionAst> elements)
            : base(extent)
        {
            if (elements == null || elements.Count == 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(elements));
            }

            this.Elements = new ReadOnlyCollection<ExpressionAst>(elements);
            SetParents(Elements);
        }

        
        public ReadOnlyCollection<ExpressionAst> Elements { get; }

        
        public override Ast Copy()
        {
            var newElements = CopyElements(this.Elements);
            return new ArrayLiteralAst(this.Extent, newElements);
        }

        
        public override Type StaticType { get { return typeof(object[]); } }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitArrayLiteral(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitArrayLiteral(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < Elements.Count; index++)
                {
                    var element = Elements[index];
                    action = element.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        IAssignableValue ISupportsAssignment.GetAssignableValue()
        {
            return new ArrayAssignableValue { ArrayLiteral = this };
        }
    }

    
    public class HashtableAst : ExpressionAst
    {
        private static readonly ReadOnlyCollection<KeyValuePair> s_emptyKeyValuePairs = Utils.EmptyReadOnlyCollection<KeyValuePair>();

        
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public HashtableAst(IScriptExtent extent, IEnumerable<KeyValuePair> keyValuePairs)
            : base(extent)
        {
            if (keyValuePairs != null)
            {
                this.KeyValuePairs = new ReadOnlyCollection<KeyValuePair>(keyValuePairs.ToArray());
                SetParents(KeyValuePairs);
            }
            else
            {
                this.KeyValuePairs = s_emptyKeyValuePairs;
            }
        }

        
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public ReadOnlyCollection<KeyValuePair> KeyValuePairs { get; }

        
        public override Ast Copy()
        {
            List<KeyValuePair> newKeyValuePairs = null;
            if (this.KeyValuePairs.Count > 0)
            {
                newKeyValuePairs = new List<KeyValuePair>(this.KeyValuePairs.Count);
                for (int i = 0; i < this.KeyValuePairs.Count; i++)
                {
                    var keyValuePair = this.KeyValuePairs[i];
                    var newKey = CopyElement(keyValuePair.Item1);
                    var newValue = CopyElement(keyValuePair.Item2);
                    newKeyValuePairs.Add(Tuple.Create(newKey, newValue));
                }
            }

            return new HashtableAst(this.Extent, newKeyValuePairs);
        }

        
        public override Type StaticType { get { return typeof(Hashtable); } }

        // Indicates that this ast was constructed as part of a schematized object instead of just a plain hash literal.
        internal bool IsSchemaElement { get; set; }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitHashtable(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitHashtable(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
            {
                for (int index = 0; index < KeyValuePairs.Count; index++)
                {
                    var keyValuePairAst = KeyValuePairs[index];
                    action = keyValuePairAst.Item1.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                    action = keyValuePairAst.Item2.InternalVisit(visitor);
                    if (action != AstVisitAction.Continue) break;
                }
            }

            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class ArrayExpressionAst : ExpressionAst
    {
        
        public ArrayExpressionAst(IScriptExtent extent, StatementBlockAst statementBlock)
            : base(extent)
        {
            if (statementBlock == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(statementBlock));
            }

            this.SubExpression = statementBlock;
            SetParent(statementBlock);
        }

        
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public StatementBlockAst SubExpression { get; }

        
        public override Ast Copy()
        {
            var newStatementBlock = CopyElement(this.SubExpression);
            return new ArrayExpressionAst(this.Extent, newStatementBlock);
        }

        
        public override Type StaticType { get { return typeof(object[]); } }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitArrayExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitArrayExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = SubExpression.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Paren")]
    public class ParenExpressionAst : ExpressionAst, ISupportsAssignment
    {
        
        public ParenExpressionAst(IScriptExtent extent, PipelineBaseAst pipeline)
            : base(extent)
        {
            if (pipeline == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(pipeline));
            }

            this.Pipeline = pipeline;
            SetParent(pipeline);
        }

        
        public PipelineBaseAst Pipeline { get; }

        
        public override Ast Copy()
        {
            var newPipeline = CopyElement(this.Pipeline);
            return new ParenExpressionAst(this.Extent, newPipeline);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitParenExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitParenExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Pipeline.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        IAssignableValue ISupportsAssignment.GetAssignableValue()
        {
            return ((ISupportsAssignment)Pipeline.GetPureExpression()).GetAssignableValue();
        }
    }

    
    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
    public class SubExpressionAst : ExpressionAst
    {
        
        public SubExpressionAst(IScriptExtent extent, StatementBlockAst statementBlock)
            : base(extent)
        {
            if (statementBlock == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(statementBlock));
            }

            this.SubExpression = statementBlock;
            SetParent(statementBlock);
        }

        
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public StatementBlockAst SubExpression { get; }

        
        public override Ast Copy()
        {
            var newStatementBlock = CopyElement(this.SubExpression);
            return new SubExpressionAst(this.Extent, newStatementBlock);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitSubExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitSubExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = SubExpression.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class UsingExpressionAst : ExpressionAst
    {
        
        public UsingExpressionAst(IScriptExtent extent, ExpressionAst expressionAst)
            : base(extent)
        {
            if (expressionAst == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(expressionAst));
            }

            RuntimeUsingIndex = -1;
            this.SubExpression = expressionAst;
            SetParent(SubExpression);
        }

        
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public ExpressionAst SubExpression { get; }

        // Used from code gen to get the value from a well known location.
        internal int RuntimeUsingIndex
        {
            get;
            set;
        }

        
        public override Ast Copy()
        {
            var newExpression = CopyElement(this.SubExpression);
            var newUsingExpression = new UsingExpressionAst(this.Extent, newExpression);
            newUsingExpression.RuntimeUsingIndex = this.RuntimeUsingIndex;
            return newUsingExpression;
        }

        #region UsingExpression Utilities

        internal const string UsingPrefix = "__using_";

        
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "We want to get the underlying variable only for the UsingExpressionAst.")]
        public static VariableExpressionAst ExtractUsingVariable(UsingExpressionAst usingExpressionAst)
        {
            ArgumentNullException.ThrowIfNull(usingExpressionAst);

            return ExtractUsingVariableImpl(usingExpressionAst);
        }

        
        private static VariableExpressionAst ExtractUsingVariableImpl(ExpressionAst expression)
        {
            VariableExpressionAst variableExpr;

            if (expression is UsingExpressionAst usingExpr)
            {
                variableExpr = usingExpr.SubExpression as VariableExpressionAst;
                if (variableExpr != null)
                {
                    return variableExpr;
                }

                return ExtractUsingVariableImpl(usingExpr.SubExpression);
            }

            if (expression is IndexExpressionAst indexExpr)
            {
                variableExpr = indexExpr.Target as VariableExpressionAst;
                if (variableExpr != null)
                {
                    return variableExpr;
                }

                return ExtractUsingVariableImpl(indexExpr.Target);
            }

            if (expression is MemberExpressionAst memberExpr)
            {
                variableExpr = memberExpr.Expression as VariableExpressionAst;
                if (variableExpr != null)
                {
                    return variableExpr;
                }

                return ExtractUsingVariableImpl(memberExpr.Expression);
            }

            Diagnostics.Assert(false, "We should always be able to get a VariableExpressionAst from a UsingExpressionAst");
            return null;
        }

        #endregion UsingExpression Utilities

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitUsingExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitUsingExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = SubExpression.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors
    }

    
    public class IndexExpressionAst : ExpressionAst, ISupportsAssignment
    {
        
        public IndexExpressionAst(IScriptExtent extent, ExpressionAst target, ExpressionAst index)
            : base(extent)
        {
            if (target == null || index == null)
            {
                throw PSTraceSource.NewArgumentNullException(target == null ? "target" : "index");
            }

            this.Target = target;
            SetParent(target);
            this.Index = index;
            SetParent(index);
        }

        
        public IndexExpressionAst(IScriptExtent extent, ExpressionAst target, ExpressionAst index, bool nullConditional)
            : this(extent, target, index)
        {
            this.NullConditional = nullConditional;
        }

        
        public ExpressionAst Target { get; }

        
        public ExpressionAst Index { get; }

        
        public bool NullConditional { get; }

        
        public override Ast Copy()
        {
            var newTarget = CopyElement(this.Target);
            var newIndex = CopyElement(this.Index);
            return new IndexExpressionAst(this.Extent, newTarget, newIndex, this.NullConditional);
        }

        #region Visitors

        internal override object Accept(ICustomAstVisitor visitor)
        {
            return visitor.VisitIndexExpression(this);
        }

        internal override AstVisitAction InternalVisit(AstVisitor visitor)
        {
            var action = visitor.VisitIndexExpression(this);
            if (action == AstVisitAction.SkipChildren)
                return visitor.CheckForPostAction(this, AstVisitAction.Continue);
            if (action == AstVisitAction.Continue)
                action = Target.InternalVisit(visitor);
            if (action == AstVisitAction.Continue)
                action = Index.InternalVisit(visitor);
            return visitor.CheckForPostAction(this, action);
        }

        #endregion Visitors

        IAssignableValue ISupportsAssignment.GetAssignableValue()
        {
            return new IndexAssignableValue { IndexExpressionAst = this };
        }
    }

    #endregion Expressions

    #region Help

    
    public sealed class CommentHelpInfo
    {
        
        public string Synopsis { get; internal set; }

        
        public string Description { get; internal set; }

        
        public string Notes { get; internal set; }

        
        public IDictionary<string, string> Parameters { get; internal set; }

        
        public ReadOnlyCollection<string> Links { get; internal set; }

        
        public ReadOnlyCollection<string> Examples { get; internal set; }

        
        public ReadOnlyCollection<string> Inputs { get; internal set; }

        
        public ReadOnlyCollection<string> Outputs { get; internal set; }

        
        public string Component { get; internal set; }

        
        public string Role { get; internal set; }

        
        public string Functionality { get; internal set; }

        
        public string ForwardHelpTargetName { get; internal set; }

        
        public string ForwardHelpCategory { get; internal set; }

        
        public string RemoteHelpRunspace { get; internal set; }

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Maml")]
        public string MamlHelpFile { get; internal set; }

        
        public string GetCommentBlock()
        {
            var sb = new StringBuilder();

            sb.AppendLine("<#");
            if (!string.IsNullOrEmpty(Synopsis))
            {
                sb.AppendLine(".SYNOPSIS");
                sb.AppendLine(Synopsis);
            }

            if (!string.IsNullOrEmpty(Description))
            {
                sb.AppendLine(".DESCRIPTION");
                sb.AppendLine(Description);
            }

            foreach (var parameter in Parameters)
            {
                sb.Append(".PARAMETER ");
                sb.AppendLine(parameter.Key);
                sb.AppendLine(parameter.Value);
            }

            for (int index = 0; index < Inputs.Count; index++)
            {
                var input = Inputs[index];
                sb.AppendLine(".INPUTS");
                sb.AppendLine(input);
            }

            for (int index = 0; index < Outputs.Count; index++)
            {
                var output = Outputs[index];
                sb.AppendLine(".OUTPUTS");
                sb.AppendLine(output);
            }

            if (!string.IsNullOrEmpty(Notes))
            {
                sb.AppendLine(".NOTES");
                sb.AppendLine(Notes);
            }

            for (int index = 0; index < Examples.Count; index++)
            {
                var example = Examples[index];
                sb.AppendLine(".EXAMPLE");
                sb.AppendLine(example);
            }

            for (int index = 0; index < Links.Count; index++)
            {
                var link = Links[index];
                sb.AppendLine(".LINK");
                sb.AppendLine(link);
            }

            if (!string.IsNullOrEmpty(ForwardHelpTargetName))
            {
                sb.Append(".FORWARDHELPTARGETNAME ");
                sb.AppendLine(ForwardHelpTargetName);
            }

            if (!string.IsNullOrEmpty(ForwardHelpCategory))
            {
                sb.Append(".FORWARDHELPCATEGORY ");
                sb.AppendLine(ForwardHelpCategory);
            }

            if (!string.IsNullOrEmpty(RemoteHelpRunspace))
            {
                sb.Append(".REMOTEHELPRUNSPACE ");
                sb.AppendLine(RemoteHelpRunspace);
            }

            if (!string.IsNullOrEmpty(Component))
            {
                sb.AppendLine(".COMPONENT");
                sb.AppendLine(Component);
            }

            if (!string.IsNullOrEmpty(Role))
            {
                sb.AppendLine(".ROLE");
                sb.AppendLine(Role);
            }

            if (!string.IsNullOrEmpty(Functionality))
            {
                sb.AppendLine(".FUNCTIONALITY");
                sb.AppendLine(Functionality);
            }

            if (!string.IsNullOrEmpty(MamlHelpFile))
            {
                sb.Append(".EXTERNALHELP ");
                sb.AppendLine(MamlHelpFile);
            }

            sb.AppendLine("#>");
            return sb.ToString();
        }
    }

    #endregion Help
}
