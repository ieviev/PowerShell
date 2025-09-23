// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace System.Management.Automation.Language
{
    
    public enum AstVisitAction
    {
        
        Continue,

        
        SkipChildren,

        
        StopVisit,
    }

    
    public abstract class AstVisitor
    {
        internal AstVisitAction CheckForPostAction(Ast ast, AstVisitAction action)
        {
            var postActionHandler = this as IAstPostVisitHandler;
            postActionHandler?.PostVisit(ast);

            return action;
        }

        public virtual AstVisitAction DefaultVisit(Ast ast) => AstVisitAction.Continue;

        public virtual AstVisitAction VisitErrorStatement(ErrorStatementAst errorStatementAst) => DefaultVisit(errorStatementAst);

        public virtual AstVisitAction VisitErrorExpression(ErrorExpressionAst errorExpressionAst) => DefaultVisit(errorExpressionAst);

        public virtual AstVisitAction VisitScriptBlock(ScriptBlockAst scriptBlockAst) => DefaultVisit(scriptBlockAst);

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Param")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "param")]
        public virtual AstVisitAction VisitParamBlock(ParamBlockAst paramBlockAst) => DefaultVisit(paramBlockAst);

        public virtual AstVisitAction VisitNamedBlock(NamedBlockAst namedBlockAst) => DefaultVisit(namedBlockAst);

        public virtual AstVisitAction VisitTypeConstraint(TypeConstraintAst typeConstraintAst) => DefaultVisit(typeConstraintAst);

        public virtual AstVisitAction VisitAttribute(AttributeAst attributeAst) => DefaultVisit(attributeAst);

        public virtual AstVisitAction VisitParameter(ParameterAst parameterAst) => DefaultVisit(parameterAst);

        public virtual AstVisitAction VisitTypeExpression(TypeExpressionAst typeExpressionAst) => DefaultVisit(typeExpressionAst);

        public virtual AstVisitAction VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst) => DefaultVisit(functionDefinitionAst);

        public virtual AstVisitAction VisitStatementBlock(StatementBlockAst statementBlockAst) => DefaultVisit(statementBlockAst);

        public virtual AstVisitAction VisitIfStatement(IfStatementAst ifStmtAst) => DefaultVisit(ifStmtAst);

        public virtual AstVisitAction VisitTrap(TrapStatementAst trapStatementAst) => DefaultVisit(trapStatementAst);

        public virtual AstVisitAction VisitSwitchStatement(SwitchStatementAst switchStatementAst) => DefaultVisit(switchStatementAst);

        public virtual AstVisitAction VisitDataStatement(DataStatementAst dataStatementAst) => DefaultVisit(dataStatementAst);

        public virtual AstVisitAction VisitForEachStatement(ForEachStatementAst forEachStatementAst) => DefaultVisit(forEachStatementAst);

        public virtual AstVisitAction VisitDoWhileStatement(DoWhileStatementAst doWhileStatementAst) => DefaultVisit(doWhileStatementAst);

        public virtual AstVisitAction VisitForStatement(ForStatementAst forStatementAst) => DefaultVisit(forStatementAst);

        public virtual AstVisitAction VisitWhileStatement(WhileStatementAst whileStatementAst) => DefaultVisit(whileStatementAst);

        public virtual AstVisitAction VisitCatchClause(CatchClauseAst catchClauseAst) => DefaultVisit(catchClauseAst);

        public virtual AstVisitAction VisitTryStatement(TryStatementAst tryStatementAst) => DefaultVisit(tryStatementAst);

        public virtual AstVisitAction VisitBreakStatement(BreakStatementAst breakStatementAst) => DefaultVisit(breakStatementAst);

        public virtual AstVisitAction VisitContinueStatement(ContinueStatementAst continueStatementAst) => DefaultVisit(continueStatementAst);

        public virtual AstVisitAction VisitReturnStatement(ReturnStatementAst returnStatementAst) => DefaultVisit(returnStatementAst);

        public virtual AstVisitAction VisitExitStatement(ExitStatementAst exitStatementAst) => DefaultVisit(exitStatementAst);

        public virtual AstVisitAction VisitThrowStatement(ThrowStatementAst throwStatementAst) => DefaultVisit(throwStatementAst);

        public virtual AstVisitAction VisitDoUntilStatement(DoUntilStatementAst doUntilStatementAst) => DefaultVisit(doUntilStatementAst);

        public virtual AstVisitAction VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst) => DefaultVisit(assignmentStatementAst);

        public virtual AstVisitAction VisitPipeline(PipelineAst pipelineAst) => DefaultVisit(pipelineAst);

        public virtual AstVisitAction VisitCommand(CommandAst commandAst) => DefaultVisit(commandAst);

        public virtual AstVisitAction VisitCommandExpression(CommandExpressionAst commandExpressionAst) => DefaultVisit(commandExpressionAst);

        public virtual AstVisitAction VisitCommandParameter(CommandParameterAst commandParameterAst) => DefaultVisit(commandParameterAst);

        public virtual AstVisitAction VisitMergingRedirection(MergingRedirectionAst redirectionAst) => DefaultVisit(redirectionAst);

        public virtual AstVisitAction VisitFileRedirection(FileRedirectionAst redirectionAst) => DefaultVisit(redirectionAst);

        public virtual AstVisitAction VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst) => DefaultVisit(binaryExpressionAst);

        public virtual AstVisitAction VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst) => DefaultVisit(unaryExpressionAst);

        public virtual AstVisitAction VisitConvertExpression(ConvertExpressionAst convertExpressionAst) => DefaultVisit(convertExpressionAst);

        public virtual AstVisitAction VisitConstantExpression(ConstantExpressionAst constantExpressionAst) => DefaultVisit(constantExpressionAst);

        public virtual AstVisitAction VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst) => DefaultVisit(stringConstantExpressionAst);

        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "SubExpression")]
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "subExpression")]
        public virtual AstVisitAction VisitSubExpression(SubExpressionAst subExpressionAst) => DefaultVisit(subExpressionAst);

        public virtual AstVisitAction VisitUsingExpression(UsingExpressionAst usingExpressionAst) => DefaultVisit(usingExpressionAst);

        public virtual AstVisitAction VisitVariableExpression(VariableExpressionAst variableExpressionAst) => DefaultVisit(variableExpressionAst);

        public virtual AstVisitAction VisitMemberExpression(MemberExpressionAst memberExpressionAst) => DefaultVisit(memberExpressionAst);

        public virtual AstVisitAction VisitInvokeMemberExpression(InvokeMemberExpressionAst methodCallAst) => DefaultVisit(methodCallAst);

        public virtual AstVisitAction VisitArrayExpression(ArrayExpressionAst arrayExpressionAst) => DefaultVisit(arrayExpressionAst);

        public virtual AstVisitAction VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst) => DefaultVisit(arrayLiteralAst);

        public virtual AstVisitAction VisitHashtable(HashtableAst hashtableAst) => DefaultVisit(hashtableAst);

        public virtual AstVisitAction VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst) => DefaultVisit(scriptBlockExpressionAst);

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Paren")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "paren")]
        public virtual AstVisitAction VisitParenExpression(ParenExpressionAst parenExpressionAst) => DefaultVisit(parenExpressionAst);

        public virtual AstVisitAction VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst) => DefaultVisit(expandableStringExpressionAst);

        public virtual AstVisitAction VisitIndexExpression(IndexExpressionAst indexExpressionAst) => DefaultVisit(indexExpressionAst);

        public virtual AstVisitAction VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst) => DefaultVisit(attributedExpressionAst);

        public virtual AstVisitAction VisitBlockStatement(BlockStatementAst blockStatementAst) => DefaultVisit(blockStatementAst);

        public virtual AstVisitAction VisitNamedAttributeArgument(NamedAttributeArgumentAst namedAttributeArgumentAst) => DefaultVisit(namedAttributeArgumentAst);
    }

    
    public abstract class AstVisitor2 : AstVisitor
    {
        public virtual AstVisitAction VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst) => DefaultVisit(typeDefinitionAst);

        public virtual AstVisitAction VisitPropertyMember(PropertyMemberAst propertyMemberAst) => DefaultVisit(propertyMemberAst);

        public virtual AstVisitAction VisitFunctionMember(FunctionMemberAst functionMemberAst) => DefaultVisit(functionMemberAst);

        public virtual AstVisitAction VisitBaseCtorInvokeMemberExpression(BaseCtorInvokeMemberExpressionAst baseCtorInvokeMemberExpressionAst) => DefaultVisit(baseCtorInvokeMemberExpressionAst);

        public virtual AstVisitAction VisitUsingStatement(UsingStatementAst usingStatementAst) => DefaultVisit(usingStatementAst);

        public virtual AstVisitAction VisitConfigurationDefinition(ConfigurationDefinitionAst configurationDefinitionAst) => DefaultVisit(configurationDefinitionAst);

        public virtual AstVisitAction VisitDynamicKeywordStatement(DynamicKeywordStatementAst dynamicKeywordStatementAst) => DefaultVisit(dynamicKeywordStatementAst);

        public virtual AstVisitAction VisitTernaryExpression(TernaryExpressionAst ternaryExpressionAst) => DefaultVisit(ternaryExpressionAst);

        public virtual AstVisitAction VisitPipelineChain(PipelineChainAst statementChain) => DefaultVisit(statementChain);
    }

    
#nullable enable
    public interface IAstPostVisitHandler
    {
        
        void PostVisit(Ast ast);
    }
}
