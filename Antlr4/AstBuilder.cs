using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Antlr4
{
    public class AstBuilder : RedLangParserBaseVisitor<AstNode>
    {
        public override AstNode VisitProgram(RedLangParser.ProgramContext ctx)
        {
            var program = new ProgramNode();

            foreach (var stmt in ctx.statement())
                program.Statements.Add(Visit(stmt));

            return program;
        }

        public override AstNode VisitStatement(RedLangParser.StatementContext ctx)
        {
            if (ctx.declaration() != null) return Visit(ctx.declaration());
            if (ctx.assignment() != null) return Visit(ctx.assignment());
            if (ctx.ifStmt() != null) return Visit(ctx.ifStmt());
            if (ctx.printStmt() != null) return Visit(ctx.printStmt());
            if (ctx.readStmt() != null) return Visit(ctx.readStmt());
            if (ctx.whileStmt() != null) return Visit(ctx.whileStmt());
            if (ctx.forStmt() != null) return Visit(ctx.forStmt());
            return base.VisitStatement(ctx);
        }

        public override AstNode VisitDeclaration(RedLangParser.DeclarationContext ctx)
        {

            var ident = ctx.IDENT();
            var type = ctx.type();

            if (ident == null)
                throw new Exception("Error: se esperaba un identificador en la declaración.");

            if (type == null)
                throw new Exception($"Error: se esperaba un tipo para '{ident.GetText()}'.");

            return new DeclarationNode
            {
                Name = ctx.IDENT().GetText(),
                Type = ctx.type().GetText(),
                Value = ctx.expression() != null ? (ExpressionNode)Visit(ctx.expression()) : null
            };
        }

        public override AstNode VisitAssignment(RedLangParser.AssignmentContext ctx)
        {
            return new AssignmentNode
            {
                Name = ctx.IDENT().GetText(),
                Value = (ExpressionNode)Visit(ctx.expression())
            };
        }

        public override AstNode VisitIfStmt(RedLangParser.IfStmtContext ctx)
        {
            var node = new IfNode
            {
                Condition = (ExpressionNode)Visit(ctx.expression())
            };

            foreach (var stmt in ctx.block(0).statement())
                node.ThenBlock.Add(Visit(stmt));

            if (ctx.block().Length > 1)
            {
                foreach (var stmt in ctx.block(1).statement())
                    node.ElseBlock.Add(Visit(stmt));
            }

            return node;
        }

        public override AstNode VisitPrintStmt(RedLangParser.PrintStmtContext ctx)
        {
            return new PrintNode
            {
                Value = (ExpressionNode)Visit(ctx.expression())
            };
        }

        public override AstNode VisitLiteral(RedLangParser.LiteralContext ctx)
        {
            return new LiteralNode { Value = ctx.GetText() };
        }

        public override AstNode VisitPrimary(RedLangParser.PrimaryContext ctx)
        {
            if (ctx.IDENT() != null)
                return new VariableNode { Name = ctx.IDENT().GetText() };
            return base.VisitPrimary(ctx);
        }

        public override AstNode VisitComparison(RedLangParser.ComparisonContext ctx)
        {
            if (ctx.op != null)
            {
                return new BinaryOpNode
                {
                    Op = ctx.op.Text,
                    Left = (ExpressionNode)Visit(ctx.left),
                    Right = (ExpressionNode)Visit(ctx.right)
                };
            }
            return base.VisitComparison(ctx);
        }

        public override AstNode VisitTerm(RedLangParser.TermContext ctx)
        {
            if (ctx.op != null)
            {
                return new BinaryOpNode
                {
                    Op = ctx.op.Text,
                    Left = (ExpressionNode)Visit(ctx.left),
                    Right = (ExpressionNode)Visit(ctx.right)
                };
            }
            return base.VisitTerm(ctx);
        }

        public override AstNode VisitWhileStmt(RedLangParser.WhileStmtContext ctx)
        {
            var node = new WhileNode
            {
                Condition = (ExpressionNode)Visit(ctx.expression())
            };

            foreach (var stmt in ctx.block().statement())
                node.Body.Add(Visit(stmt));

            return node;
        }

        public override AstNode VisitForStmt(RedLangParser.ForStmtContext ctx)
        {
            var node = new ForNode();

            // Inicialización (declaración o asignación)
            if (ctx.declaration() != null)
                node.Init = Visit(ctx.declaration());
            else if (ctx.assignment().Length > 0)
                node.Init = Visit(ctx.assignment(0));

            // Condición (opcional)
            if (ctx.expression() != null)
                node.Condition = (ExpressionNode)Visit(ctx.expression());

            // Incremento (segunda asignación opcional)
            if (ctx.assignment().Length >= 1)
                node.Increment = Visit(ctx.assignment(ctx.assignment().Length - 1));

            // Cuerpo
            foreach (var stmt in ctx.block().statement())
                node.Body.Add(Visit(stmt));

            return node;
        }

        public override AstNode VisitReadStmt(RedLangParser.ReadStmtContext ctx)
        {
            return new ReadNode
            {
                Name = ctx.IDENT().GetText()
            };
        }

    }
}
