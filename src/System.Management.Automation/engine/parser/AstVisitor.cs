// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;

namespace System.Management.Automation.Language
{
    
#nullable enable
    public interface ICustomAstVisitor
    {
        object? DefaultVisit(Ast ast) => null;

        object? VisitErrorStatement(ErrorStatementAst errorStatementAst) => DefaultVisit(errorStatementAst);

        object? VisitErrorExpression(ErrorExpressionAst errorExpressionAst) => DefaultVisit(errorExpressionAst);

        #region Script Blocks

        object? VisitScriptBlock(ScriptBlockAst scriptBlockAst) => DefaultVisit(scriptBlockAst);

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Param")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        object? VisitParamBlock(ParamBlockAst paramBlockAst) => DefaultVisit(paramBlockAst);

        object? VisitNamedBlock(NamedBlockAst namedBlockAst) => DefaultVisit(namedBlockAst);

        object? VisitTypeConstraint(TypeConstraintAst typeConstraintAst) => DefaultVisit(typeConstraintAst);

        object? VisitAttribute(AttributeAst attributeAst) => DefaultVisit(attributeAst);

        object? VisitNamedAttributeArgument(NamedAttributeArgumentAst namedAttributeArgumentAst) => DefaultVisit(namedAttributeArgumentAst);

        object? VisitParameter(ParameterAst parameterAst) => DefaultVisit(parameterAst);

        #endregion Script Blocks

        #region Statements

        object? VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst) => DefaultVisit(functionDefinitionAst);

        object? VisitStatementBlock(StatementBlockAst statementBlockAst) => DefaultVisit(statementBlockAst);

        object? VisitIfStatement(IfStatementAst ifStmtAst) => DefaultVisit(ifStmtAst);

        object? VisitTrap(TrapStatementAst trapStatementAst) => DefaultVisit(trapStatementAst);

        object? VisitSwitchStatement(SwitchStatementAst switchStatementAst) => DefaultVisit(switchStatementAst);

        object? VisitDataStatement(DataStatementAst dataStatementAst) => DefaultVisit(dataStatementAst);

        object? VisitForEachStatement(ForEachStatementAst forEachStatementAst) => DefaultVisit(forEachStatementAst);

        object? VisitDoWhileStatement(DoWhileStatementAst doWhileStatementAst) => DefaultVisit(doWhileStatementAst);

        object? VisitForStatement(ForStatementAst forStatementAst) => DefaultVisit(forStatementAst);

        object? VisitWhileStatement(WhileStatementAst whileStatementAst) => DefaultVisit(whileStatementAst);

        object? VisitCatchClause(CatchClauseAst catchClauseAst) => DefaultVisit(catchClauseAst);

        object? VisitTryStatement(TryStatementAst tryStatementAst) => DefaultVisit(tryStatementAst);

        object? VisitBreakStatement(BreakStatementAst breakStatementAst) => DefaultVisit(breakStatementAst);

        object? VisitContinueStatement(ContinueStatementAst continueStatementAst) => DefaultVisit(continueStatementAst);

        object? VisitReturnStatement(ReturnStatementAst returnStatementAst) => DefaultVisit(returnStatementAst);

        object? VisitExitStatement(ExitStatementAst exitStatementAst) => DefaultVisit(exitStatementAst);

        object? VisitThrowStatement(ThrowStatementAst throwStatementAst) => DefaultVisit(throwStatementAst);

        object? VisitDoUntilStatement(DoUntilStatementAst doUntilStatementAst) => DefaultVisit(doUntilStatementAst);

        object? VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst) => DefaultVisit(assignmentStatementAst);

        #endregion Statements

        #region Pipelines

        object? VisitPipeline(PipelineAst pipelineAst) => DefaultVisit(pipelineAst);

        object? VisitCommand(CommandAst commandAst) => DefaultVisit(commandAst);

        object? VisitCommandExpression(CommandExpressionAst commandExpressionAst) => DefaultVisit(commandExpressionAst);

        object? VisitCommandParameter(CommandParameterAst commandParameterAst) => DefaultVisit(commandParameterAst);

        object? VisitFileRedirection(FileRedirectionAst fileRedirectionAst) => DefaultVisit(fileRedirectionAst);

        object? VisitMergingRedirection(MergingRedirectionAst mergingRedirectionAst) => DefaultVisit(mergingRedirectionAst);

        #endregion Pipelines

        #region Expressions

        object? VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst) => DefaultVisit(binaryExpressionAst);

        object? VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst) => DefaultVisit(unaryExpressionAst);

        object? VisitConvertExpression(ConvertExpressionAst convertExpressionAst) => DefaultVisit(convertExpressionAst);

        object? VisitConstantExpression(ConstantExpressionAst constantExpressionAst) => DefaultVisit(constantExpressionAst);

        object? VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst) => DefaultVisit(stringConstantExpressionAst);

        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "SubExpression")]
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "subExpression")]
        object? VisitSubExpression(SubExpressionAst subExpressionAst) => DefaultVisit(subExpressionAst);

        object? VisitUsingExpression(UsingExpressionAst usingExpressionAst) => DefaultVisit(usingExpressionAst);

        object? VisitVariableExpression(VariableExpressionAst variableExpressionAst) => DefaultVisit(variableExpressionAst);

        object? VisitTypeExpression(TypeExpressionAst typeExpressionAst) => DefaultVisit(typeExpressionAst);

        object? VisitMemberExpression(MemberExpressionAst memberExpressionAst) => DefaultVisit(memberExpressionAst);

        object? VisitInvokeMemberExpression(InvokeMemberExpressionAst invokeMemberExpressionAst) => DefaultVisit(invokeMemberExpressionAst);

        object? VisitArrayExpression(ArrayExpressionAst arrayExpressionAst) => DefaultVisit(arrayExpressionAst);

        object? VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst) => DefaultVisit(arrayLiteralAst);

        object? VisitHashtable(HashtableAst hashtableAst) => DefaultVisit(hashtableAst);

        object? VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst) => DefaultVisit(scriptBlockExpressionAst);

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Paren")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "paren")]
        object? VisitParenExpression(ParenExpressionAst parenExpressionAst) => DefaultVisit(parenExpressionAst);

        object? VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst) => DefaultVisit(expandableStringExpressionAst);

        object? VisitIndexExpression(IndexExpressionAst indexExpressionAst) => DefaultVisit(indexExpressionAst);

        object? VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst) => DefaultVisit(attributedExpressionAst);

        object? VisitBlockStatement(BlockStatementAst blockStatementAst) => DefaultVisit(blockStatementAst);

        #endregion Expressions
    }
#nullable restore

#nullable enable
    public interface ICustomAstVisitor2 : ICustomAstVisitor
    {
        object? VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst) => DefaultVisit(typeDefinitionAst);

        object? VisitPropertyMember(PropertyMemberAst propertyMemberAst) => DefaultVisit(propertyMemberAst);

        object? VisitFunctionMember(FunctionMemberAst functionMemberAst) => DefaultVisit(functionMemberAst);

        object? VisitBaseCtorInvokeMemberExpression(BaseCtorInvokeMemberExpressionAst baseCtorInvokeMemberExpressionAst) => DefaultVisit(baseCtorInvokeMemberExpressionAst);

        object? VisitUsingStatement(UsingStatementAst usingStatement) => DefaultVisit(usingStatement);

        object? VisitConfigurationDefinition(ConfigurationDefinitionAst configurationDefinitionAst) => DefaultVisit(configurationDefinitionAst);

        object? VisitDynamicKeywordStatement(DynamicKeywordStatementAst dynamicKeywordAst) => DefaultVisit(dynamicKeywordAst);

        object? VisitTernaryExpression(TernaryExpressionAst ternaryExpressionAst) => DefaultVisit(ternaryExpressionAst);

        object? VisitPipelineChain(PipelineChainAst statementChainAst) => DefaultVisit(statementChainAst);
    }
#nullable restore

#if DEBUG
    internal class CheckAllParentsSet : AstVisitor2
    {
        internal CheckAllParentsSet(Ast root)
        {
            this.Root = root;
        }

        private Ast Root { get; }

        internal AstVisitAction CheckParent(Ast ast)
        {
            if (ast != Root)
            {
                Diagnostics.Assert(ast.Parent != null, "Parent not set");
            }

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitErrorStatement(ErrorStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitErrorExpression(ErrorExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitScriptBlock(ScriptBlockAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitParamBlock(ParamBlockAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitNamedBlock(NamedBlockAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitTypeConstraint(TypeConstraintAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitAttribute(AttributeAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitParameter(ParameterAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitTypeExpression(TypeExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitFunctionDefinition(FunctionDefinitionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitStatementBlock(StatementBlockAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitIfStatement(IfStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitTrap(TrapStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitSwitchStatement(SwitchStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitDataStatement(DataStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitForEachStatement(ForEachStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitDoWhileStatement(DoWhileStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitForStatement(ForStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitWhileStatement(WhileStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitCatchClause(CatchClauseAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitTryStatement(TryStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitBreakStatement(BreakStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitContinueStatement(ContinueStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitReturnStatement(ReturnStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitExitStatement(ExitStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitThrowStatement(ThrowStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitDoUntilStatement(DoUntilStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitAssignmentStatement(AssignmentStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitPipeline(PipelineAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitCommand(CommandAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitCommandExpression(CommandExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitCommandParameter(CommandParameterAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitMergingRedirection(MergingRedirectionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitFileRedirection(FileRedirectionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitBinaryExpression(BinaryExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitUnaryExpression(UnaryExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitConvertExpression(ConvertExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitConstantExpression(ConstantExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitStringConstantExpression(StringConstantExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitSubExpression(SubExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitUsingExpression(UsingExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitVariableExpression(VariableExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitMemberExpression(MemberExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitInvokeMemberExpression(InvokeMemberExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitArrayExpression(ArrayExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitArrayLiteral(ArrayLiteralAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitHashtable(HashtableAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitScriptBlockExpression(ScriptBlockExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitParenExpression(ParenExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitExpandableStringExpression(ExpandableStringExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitIndexExpression(IndexExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitAttributedExpression(AttributedExpressionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitBlockStatement(BlockStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitNamedAttributeArgument(NamedAttributeArgumentAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitTypeDefinition(TypeDefinitionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitFunctionMember(FunctionMemberAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitPropertyMember(PropertyMemberAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitUsingStatement(UsingStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitConfigurationDefinition(ConfigurationDefinitionAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitDynamicKeywordStatement(DynamicKeywordStatementAst ast) { return CheckParent(ast); }

        public override AstVisitAction VisitTernaryExpression(TernaryExpressionAst ast) => CheckParent(ast);

        public override AstVisitAction VisitPipelineChain(PipelineChainAst ast) => CheckParent(ast);
    }

    
    internal class CheckTypeBuilder : AstVisitor2
    {
        public override AstVisitAction VisitTypeConstraint(TypeConstraintAst ast)
        {
            Type type = ast.TypeName.GetReflectionType();
            if (type != null)
            {
                Diagnostics.Assert(type is not TypeBuilder, "ReflectionType can never be TypeBuilder");
            }

            return AstVisitAction.Continue;
        }
    }
#endif

    
    internal class AstSearcher : AstVisitor2
    {
        #region External interface

        internal static IEnumerable<Ast> FindAll(Ast ast, Func<Ast, bool> predicate, bool searchNestedScriptBlocks)
        {
            Diagnostics.Assert(ast != null && predicate != null, "caller to verify arguments");

            var searcher = new AstSearcher(predicate, stopOnFirst: false, searchNestedScriptBlocks: searchNestedScriptBlocks);
            ast.InternalVisit(searcher);
            return searcher.Results;
        }

        internal static Ast FindFirst(Ast ast, Func<Ast, bool> predicate, bool searchNestedScriptBlocks)
        {
            Diagnostics.Assert(ast != null && predicate != null, "caller to verify arguments");

            var searcher = new AstSearcher(predicate, stopOnFirst: true, searchNestedScriptBlocks: searchNestedScriptBlocks);
            ast.InternalVisit(searcher);
            return searcher.Results.FirstOrDefault();
        }

        internal static bool Contains(Ast ast, Func<Ast, bool> predicate, bool searchNestedScriptBlocks)
        {
            Diagnostics.Assert(ast != null && predicate != null, "caller to verify arguments");

            var searcher = new AstSearcher(predicate, stopOnFirst: true, searchNestedScriptBlocks: searchNestedScriptBlocks);
            ast.InternalVisit(searcher);
            return searcher.Results.Count > 0;
        }

        internal static bool IsUsingDollarInput(Ast ast)
        {
            return (AstSearcher.Contains(
                ast,
                ast_ =>
                {
                    var varAst = ast_ as VariableExpressionAst;
                    if (varAst != null)
                    {
                        return varAst.VariablePath.IsVariable &&
                               varAst.VariablePath.UnqualifiedPath.Equals(SpecialVariables.Input,
                                                                          StringComparison.OrdinalIgnoreCase);
                    }

                    return false;
                },
                searchNestedScriptBlocks: false));
        }

        #endregion External interface

        protected AstSearcher(Func<Ast, bool> callback, bool stopOnFirst, bool searchNestedScriptBlocks)
        {
            _callback = callback;
            _stopOnFirst = stopOnFirst;
            _searchNestedScriptBlocks = searchNestedScriptBlocks;
            this.Results = new List<Ast>();
        }

        private readonly Func<Ast, bool> _callback;
        private readonly bool _stopOnFirst;
        private readonly bool _searchNestedScriptBlocks;
        protected readonly List<Ast> Results;

        protected AstVisitAction Check(Ast ast)
        {
            if (_callback(ast))
            {
                Results.Add(ast);
                if (_stopOnFirst)
                {
                    return AstVisitAction.StopVisit;
                }
            }

            return AstVisitAction.Continue;
        }

        protected AstVisitAction CheckScriptBlock(Ast ast)
        {
            var action = Check(ast);
            if (action == AstVisitAction.Continue && !_searchNestedScriptBlocks)
            {
                action = AstVisitAction.SkipChildren;
            }

            return action;
        }

        public override AstVisitAction VisitErrorStatement(ErrorStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitErrorExpression(ErrorExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitScriptBlock(ScriptBlockAst ast) { return Check(ast); }

        public override AstVisitAction VisitParamBlock(ParamBlockAst ast) { return Check(ast); }

        public override AstVisitAction VisitNamedBlock(NamedBlockAst ast) { return Check(ast); }

        public override AstVisitAction VisitTypeConstraint(TypeConstraintAst ast) { return Check(ast); }

        public override AstVisitAction VisitAttribute(AttributeAst ast) { return Check(ast); }

        public override AstVisitAction VisitParameter(ParameterAst ast) { return Check(ast); }

        public override AstVisitAction VisitTypeExpression(TypeExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitFunctionDefinition(FunctionDefinitionAst ast) { return CheckScriptBlock(ast); }

        public override AstVisitAction VisitStatementBlock(StatementBlockAst ast) { return Check(ast); }

        public override AstVisitAction VisitIfStatement(IfStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitTrap(TrapStatementAst ast) { return CheckScriptBlock(ast); }

        public override AstVisitAction VisitSwitchStatement(SwitchStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitDataStatement(DataStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitForEachStatement(ForEachStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitDoWhileStatement(DoWhileStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitForStatement(ForStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitWhileStatement(WhileStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitCatchClause(CatchClauseAst ast) { return Check(ast); }

        public override AstVisitAction VisitTryStatement(TryStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitBreakStatement(BreakStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitContinueStatement(ContinueStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitReturnStatement(ReturnStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitExitStatement(ExitStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitThrowStatement(ThrowStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitDoUntilStatement(DoUntilStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitAssignmentStatement(AssignmentStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitPipeline(PipelineAst ast) { return Check(ast); }

        public override AstVisitAction VisitCommand(CommandAst ast) { return Check(ast); }

        public override AstVisitAction VisitCommandExpression(CommandExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitCommandParameter(CommandParameterAst ast) { return Check(ast); }

        public override AstVisitAction VisitMergingRedirection(MergingRedirectionAst ast) { return Check(ast); }

        public override AstVisitAction VisitFileRedirection(FileRedirectionAst ast) { return Check(ast); }

        public override AstVisitAction VisitBinaryExpression(BinaryExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitUnaryExpression(UnaryExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitConvertExpression(ConvertExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitConstantExpression(ConstantExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitStringConstantExpression(StringConstantExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitSubExpression(SubExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitUsingExpression(UsingExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitVariableExpression(VariableExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitMemberExpression(MemberExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitInvokeMemberExpression(InvokeMemberExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitArrayExpression(ArrayExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitArrayLiteral(ArrayLiteralAst ast) { return Check(ast); }

        public override AstVisitAction VisitHashtable(HashtableAst ast) { return Check(ast); }

        public override AstVisitAction VisitScriptBlockExpression(ScriptBlockExpressionAst ast) { return CheckScriptBlock(ast); }

        public override AstVisitAction VisitParenExpression(ParenExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitExpandableStringExpression(ExpandableStringExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitIndexExpression(IndexExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitAttributedExpression(AttributedExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitNamedAttributeArgument(NamedAttributeArgumentAst ast) { return Check(ast); }

        public override AstVisitAction VisitTypeDefinition(TypeDefinitionAst ast) { return Check(ast); }

        public override AstVisitAction VisitPropertyMember(PropertyMemberAst ast) { return Check(ast); }

        public override AstVisitAction VisitFunctionMember(FunctionMemberAst ast) { return Check(ast); }

        public override AstVisitAction VisitUsingStatement(UsingStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitBlockStatement(BlockStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitConfigurationDefinition(ConfigurationDefinitionAst ast) { return Check(ast); }

        public override AstVisitAction VisitDynamicKeywordStatement(DynamicKeywordStatementAst ast) { return Check(ast); }

        public override AstVisitAction VisitTernaryExpression(TernaryExpressionAst ast) { return Check(ast); }

        public override AstVisitAction VisitPipelineChain(PipelineChainAst ast) { return Check(ast); }
    }

    
    public abstract class DefaultCustomAstVisitor : ICustomAstVisitor
    {
        public virtual object DefaultVisit(Ast ast) => null;

        public virtual object VisitErrorStatement(ErrorStatementAst errorStatementAst) => DefaultVisit(errorStatementAst);

        public virtual object VisitErrorExpression(ErrorExpressionAst errorExpressionAst) => DefaultVisit(errorExpressionAst);

        public virtual object VisitScriptBlock(ScriptBlockAst scriptBlockAst) => DefaultVisit(scriptBlockAst);

        public virtual object VisitParamBlock(ParamBlockAst paramBlockAst) => DefaultVisit(paramBlockAst);

        public virtual object VisitNamedBlock(NamedBlockAst namedBlockAst) => DefaultVisit(namedBlockAst);

        public virtual object VisitTypeConstraint(TypeConstraintAst typeConstraintAst) => DefaultVisit(typeConstraintAst);

        public virtual object VisitAttribute(AttributeAst attributeAst) => DefaultVisit(attributeAst);

        public virtual object VisitNamedAttributeArgument(NamedAttributeArgumentAst namedAttributeArgumentAst) => DefaultVisit(namedAttributeArgumentAst);

        public virtual object VisitParameter(ParameterAst parameterAst) => DefaultVisit(parameterAst);

        public virtual object VisitStatementBlock(StatementBlockAst statementBlockAst) => DefaultVisit(statementBlockAst);

        public virtual object VisitIfStatement(IfStatementAst ifStmtAst) => DefaultVisit(ifStmtAst);

        public virtual object VisitTrap(TrapStatementAst trapStatementAst) => DefaultVisit(trapStatementAst);

        public virtual object VisitSwitchStatement(SwitchStatementAst switchStatementAst) => DefaultVisit(switchStatementAst);

        public virtual object VisitDataStatement(DataStatementAst dataStatementAst) => DefaultVisit(dataStatementAst);

        public virtual object VisitForEachStatement(ForEachStatementAst forEachStatementAst) => DefaultVisit(forEachStatementAst);

        public virtual object VisitDoWhileStatement(DoWhileStatementAst doWhileStatementAst) => DefaultVisit(doWhileStatementAst);

        public virtual object VisitForStatement(ForStatementAst forStatementAst) => DefaultVisit(forStatementAst);

        public virtual object VisitWhileStatement(WhileStatementAst whileStatementAst) => DefaultVisit(whileStatementAst);

        public virtual object VisitCatchClause(CatchClauseAst catchClauseAst) => DefaultVisit(catchClauseAst);

        public virtual object VisitTryStatement(TryStatementAst tryStatementAst) => DefaultVisit(tryStatementAst);

        public virtual object VisitBreakStatement(BreakStatementAst breakStatementAst) => DefaultVisit(breakStatementAst);

        public virtual object VisitContinueStatement(ContinueStatementAst continueStatementAst) => DefaultVisit(continueStatementAst);

        public virtual object VisitReturnStatement(ReturnStatementAst returnStatementAst) => DefaultVisit(returnStatementAst);

        public virtual object VisitExitStatement(ExitStatementAst exitStatementAst) => DefaultVisit(exitStatementAst);

        public virtual object VisitThrowStatement(ThrowStatementAst throwStatementAst) => DefaultVisit(throwStatementAst);

        public virtual object VisitDoUntilStatement(DoUntilStatementAst doUntilStatementAst) => DefaultVisit(doUntilStatementAst);

        public virtual object VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst) => DefaultVisit(assignmentStatementAst);

        public virtual object VisitPipeline(PipelineAst pipelineAst) => DefaultVisit(pipelineAst);

        public virtual object VisitCommand(CommandAst commandAst) => DefaultVisit(commandAst);

        public virtual object VisitCommandExpression(CommandExpressionAst commandExpressionAst) => DefaultVisit(commandExpressionAst);

        public virtual object VisitCommandParameter(CommandParameterAst commandParameterAst) => DefaultVisit(commandParameterAst);

        public virtual object VisitFileRedirection(FileRedirectionAst fileRedirectionAst) => DefaultVisit(fileRedirectionAst);

        public virtual object VisitMergingRedirection(MergingRedirectionAst mergingRedirectionAst) => DefaultVisit(mergingRedirectionAst);

        public virtual object VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst) => DefaultVisit(binaryExpressionAst);

        public virtual object VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst) => DefaultVisit(unaryExpressionAst);

        public virtual object VisitConvertExpression(ConvertExpressionAst convertExpressionAst) => DefaultVisit(convertExpressionAst);

        public virtual object VisitConstantExpression(ConstantExpressionAst constantExpressionAst) => DefaultVisit(constantExpressionAst);

        public virtual object VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst) => DefaultVisit(stringConstantExpressionAst);

        public virtual object VisitSubExpression(SubExpressionAst subExpressionAst) => DefaultVisit(subExpressionAst);

        public virtual object VisitUsingExpression(UsingExpressionAst usingExpressionAst) => DefaultVisit(usingExpressionAst);

        public virtual object VisitVariableExpression(VariableExpressionAst variableExpressionAst) => DefaultVisit(variableExpressionAst);

        public virtual object VisitTypeExpression(TypeExpressionAst typeExpressionAst) => DefaultVisit(typeExpressionAst);

        public virtual object VisitMemberExpression(MemberExpressionAst memberExpressionAst) => DefaultVisit(memberExpressionAst);

        public virtual object VisitInvokeMemberExpression(InvokeMemberExpressionAst invokeMemberExpressionAst) => DefaultVisit(invokeMemberExpressionAst);

        public virtual object VisitArrayExpression(ArrayExpressionAst arrayExpressionAst) => DefaultVisit(arrayExpressionAst);

        public virtual object VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst) => DefaultVisit(arrayLiteralAst);

        public virtual object VisitHashtable(HashtableAst hashtableAst) => DefaultVisit(hashtableAst);

        public virtual object VisitParenExpression(ParenExpressionAst parenExpressionAst) => DefaultVisit(parenExpressionAst);

        public virtual object VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst) => DefaultVisit(expandableStringExpressionAst);

        public virtual object VisitIndexExpression(IndexExpressionAst indexExpressionAst) => DefaultVisit(indexExpressionAst);

        public virtual object VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst) => DefaultVisit(attributedExpressionAst);

        public virtual object VisitBlockStatement(BlockStatementAst blockStatementAst) => DefaultVisit(blockStatementAst);

        public virtual object VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst) => DefaultVisit(functionDefinitionAst);

        public virtual object VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst) => DefaultVisit(scriptBlockExpressionAst);
    }

    
    public abstract class DefaultCustomAstVisitor2 : DefaultCustomAstVisitor, ICustomAstVisitor2
    {
        public virtual object VisitPropertyMember(PropertyMemberAst propertyMemberAst) => DefaultVisit(propertyMemberAst);

        public virtual object VisitBaseCtorInvokeMemberExpression(BaseCtorInvokeMemberExpressionAst baseCtorInvokeMemberExpressionAst) => DefaultVisit(baseCtorInvokeMemberExpressionAst);

        public virtual object VisitUsingStatement(UsingStatementAst usingStatement) => DefaultVisit(usingStatement);

        public virtual object VisitConfigurationDefinition(ConfigurationDefinitionAst configurationAst) => DefaultVisit(configurationAst);

        public virtual object VisitDynamicKeywordStatement(DynamicKeywordStatementAst dynamicKeywordAst) => DefaultVisit(dynamicKeywordAst);

        public virtual object VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst) => DefaultVisit(typeDefinitionAst);

        public virtual object VisitFunctionMember(FunctionMemberAst functionMemberAst) => DefaultVisit(functionMemberAst);

        public virtual object VisitTernaryExpression(TernaryExpressionAst ternaryExpressionAst) => DefaultVisit(ternaryExpressionAst);

        public virtual object VisitPipelineChain(PipelineChainAst statementChainAst) => DefaultVisit(statementChainAst);
    }
}
