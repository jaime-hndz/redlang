using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Antlr4
{
    public abstract class AstNode { }

    // Programa completo
    public class ProgramNode : AstNode
    {
        public List<AstNode> Statements { get; } = new List<AstNode>();
    }

    // Declaración de variables
    public class DeclarationNode : AstNode
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public ExpressionNode Value { get; set; }  // puede ser null si no hay inicialización
    }

    // Asignación
    public class AssignmentNode : AstNode
    {
        public string Name { get; set; }
        public ExpressionNode Value { get; set; }
    }

    // If / Otherwise
    public class IfNode : AstNode
    {
        public ExpressionNode Condition { get; set; }
        public List<AstNode> ThenBlock { get; set; } = new List<AstNode>();
        public List<AstNode> ElseBlock { get; set; } = new List<AstNode>();
    }

    // Print / Show
    public class PrintNode : AstNode
    {
        public ExpressionNode Value { get; set; }
    }

    // Expresiones
    public abstract class ExpressionNode : AstNode { }

    public class LiteralNode : ExpressionNode
    {
        public string Value { get; set; }
    }

    public class VariableNode : ExpressionNode
    {
        public string Name { get; set; }
    }

    public class BinaryOpNode : ExpressionNode
    {
        public string Op { get; set; }
        public ExpressionNode Left { get; set; }
        public ExpressionNode Right { get; set; }
    }

    // Bucle tipo while (repeat)
    public class WhileNode : AstNode
    {
        public ExpressionNode Condition { get; set; }
        public List<AstNode> Body { get; set; } = new List<AstNode>();
    }

    // Bucle tipo for (loop)
    public class ForNode : AstNode
    {
        public AstNode Init { get; set; }              // puede ser DeclarationNode o AssignmentNode
        public ExpressionNode Condition { get; set; }  // puede ser null
        public AstNode Increment { get; set; }         // puede ser null
        public List<AstNode> Body { get; set; } = new List<AstNode>();
    }

    public class ReadNode : AstNode
    {
        public string Name { get; set; }   // variable donde se almacena la entrada
    }
}
