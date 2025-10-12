using System;
using Antlr4;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

class Program
{
    static void Main(string[] args)
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "code.txt");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"El archivo '{filePath}' no existe. Crea un archivo llamado code.txt con tu código fuente.");
            return;
        }

        var code = File.ReadAllText(filePath);

        AntlrInputStream inputStream = new AntlrInputStream(code);
        var lexer = new RedLangLexer(inputStream);
        CommonTokenStream tokenStream = new CommonTokenStream(lexer);
        var parser = new RedLangParser(tokenStream);

        var tree = parser.program();

        // AST personalizado
        var builder = new AstBuilder();
        var ast = (ProgramNode)builder.Visit(tree);

        // Parse Tree (debug opcional)
       // Console.WriteLine(tree.ToStringTree(parser));

        Console.WriteLine("\n--- AST ---");
        PrintAst(ast);

        var cg = new CodeGen();
        cg.GenProgram(ast);
        var outPath = Path.Combine(Directory.GetCurrentDirectory(), "out.ll");
        cg.Save(outPath);
        Console.WriteLine("Wrote LLVM IR to: " + outPath);
        Console.WriteLine("Para compilar:");
        Console.WriteLine("  llvm-as out.ll -o out.bc");
        Console.WriteLine("  llc out.bc -filetype=obj -o out.o");
        Console.WriteLine("  clang -no-pie out.o -o out");
        Console.WriteLine("  ./out");
    }

    static void PrintAst(AstNode node, string indent = "")
    {
        switch (node)
        {
            case ProgramNode prog:
                Console.WriteLine(indent + "Program");
                foreach (var stmt in prog.Statements)
                    PrintAst(stmt, indent + "  ");
                break;

            case DeclarationNode decl:
                Console.WriteLine($"{indent}Declare {decl.Name} : {decl.Type}");
                if (decl.Value != null) PrintAst(decl.Value, indent + "  ");
                break;

            case AssignmentNode assign:
                Console.WriteLine($"{indent}Assign {assign.Name} =");
                PrintAst(assign.Value, indent + "  ");
                break;

            case IfNode ifn:
                Console.WriteLine($"{indent}If");
                Console.WriteLine(indent + "  Condition:");
                PrintAst(ifn.Condition, indent + "    ");
                Console.WriteLine(indent + "  Then:");
                foreach (var stmt in ifn.ThenBlock)
                    PrintAst(stmt, indent + "    ");
                if (ifn.ElseBlock.Count > 0)
                {
                    Console.WriteLine(indent + "  Else:");
                    foreach (var stmt in ifn.ElseBlock)
                        PrintAst(stmt, indent + "    ");
                }
                break;

            case PrintNode print:
                Console.WriteLine(indent + "Print:");
                PrintAst(print.Value, indent + "  ");
                break;

            case WhileNode w:
                Console.WriteLine($"{indent}Repeat (while)");
                Console.WriteLine(indent + "  Condition:");
                PrintAst(w.Condition, indent + "    ");
                Console.WriteLine(indent + "  Body:");
                foreach (var stmt in w.Body)
                    PrintAst(stmt, indent + "    ");
                break;

            case ForNode f:
                Console.WriteLine($"{indent}Loop (for)");
                if (f.Init != null)
                {
                    Console.WriteLine(indent + "  Init:");
                    PrintAst(f.Init, indent + "    ");
                }
                if (f.Condition != null)
                {
                    Console.WriteLine(indent + "  Condition:");
                    PrintAst(f.Condition, indent + "    ");
                }
                if (f.Increment != null)
                {
                    Console.WriteLine(indent + "  Increment:");
                    PrintAst(f.Increment, indent + "    ");
                }
                Console.WriteLine(indent + "  Body:");
                foreach (var stmt in f.Body)
                    PrintAst(stmt, indent + "    ");
                break;

            case ReadNode read:
                Console.WriteLine($"{indent}Ask input for variable {read.Name}");
                break;

            case FunctionNode fn:
                Console.WriteLine($"{indent}Function {fn.Name} -> {fn.ReturnType}");
                if (fn.Parameters.Any())
                {
                    Console.WriteLine(indent + "  Params:");
                    foreach (var (n, t) in fn.Parameters)
                        Console.WriteLine($"{indent}    {n}: {t}");
                }
                Console.WriteLine(indent + "  Body:");
                foreach (var stmt in fn.Body)
                    PrintAst(stmt, indent + "    ");
                break;

            case CallNode call:
                Console.WriteLine($"{indent}Call {call.Name}");
                foreach (var arg in call.Arguments)
                    PrintAst(arg, indent + "  ");
                break;

            case ReturnNode ret:
                Console.WriteLine($"{indent}Return:");
                PrintAst(ret.Value, indent + "  ");
                break;

            case BinaryOpNode bin:
                Console.WriteLine($"{indent}BinaryOp {bin.Op}");
                PrintAst(bin.Left, indent + "  ");
                PrintAst(bin.Right, indent + "  ");
                break;

            case LiteralNode lit:
                Console.WriteLine($"{indent}Literal {lit.Value}");
                break;

            case VariableNode var:
                Console.WriteLine($"{indent}Variable {var.Name}");
                break;
        }
    }


}

public class MyListener : RedLangParserBaseListener
{
    public override void ExitAssignment(RedLangParser.AssignmentContext ctx)
    {
        Console.WriteLine("Se encontró una asignación: " + ctx.GetText());
    }
}

public class MyVisitor : RedLangParserBaseVisitor<object>
{
    public override object VisitLiteral(RedLangParser.LiteralContext ctx)
    {
        if (ctx.INT_LIT() != null)
        {
            Console.WriteLine("Número detectado: " + ctx.INT_LIT().GetText());
        }
        return base.VisitLiteral(ctx);
    }
}