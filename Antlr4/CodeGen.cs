using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Antlr4
{
    public enum Ty { Int, Double, Bool } // i32, double, i1

    public class CodeGen
    {
        private readonly StringBuilder sb = new StringBuilder();
        private int tmp = 0;
        private int lbl = 0;

        private string Fresh() => "%t" + (tmp++);
        private string FreshLbl(string baseName) => $"{baseName}{lbl++}";

        // Símbolos: nombre -> (alloca, tipo declarativo)
        private readonly Dictionary<string, (string alloca, Ty ty)> sym = new();

        // Constantes de formato para printf
        private bool prologueEmitted = false;

        public void EmitPrologue()
        {
            if (prologueEmitted) return;
            prologueEmitted = true;

            // 🔹 Formatos globales para impresión y lectura
            sb.AppendLine("@.fmt_i = private unnamed_addr constant [4 x i8] c\"%d\\0A\\00\"");
            sb.AppendLine("@.fmt_f = private unnamed_addr constant [4 x i8] c\"%f\\0A\\00\"");
            sb.AppendLine("@.fmt_i_in = private unnamed_addr constant [3 x i8] c\"%d\\00\"");
            sb.AppendLine("@.fmt_f_in = private unnamed_addr constant [3 x i8] c\"%f\\00\"");
            sb.AppendLine();

            // 🔹 Declaraciones de funciones externas
            sb.AppendLine("declare i32 @printf(i8*, ...)");
            sb.AppendLine("declare i32 @scanf(i8*, ...)");
            sb.AppendLine("declare i32 @fflush(i8*)"); // opcional, útil si luego agregas flush automático
            sb.AppendLine();

            // 🔹 Función principal
            sb.AppendLine("define i32 @main() {");
            sb.AppendLine("entry:");
        }

        public void EmitEpilogue()
        {
            sb.AppendLine("  ret i32 0");
            sb.AppendLine("}");
        }

        private static Ty ParseTypeKeyword(string kw)
        {
            // defaults: i=int32, f=double
            return kw switch
            {
                "i" => Ty.Int,
                "f" => Ty.Double,
                _ => throw new Exception($"Tipo no soportado: {kw}")
            };
        }

        private (string val, Ty ty) ZeroOf(Ty ty)
        {
            return ty switch
            {
                Ty.Int => ("0", Ty.Int),
                Ty.Double => ("0.000000e+00", Ty.Double),
                Ty.Bool => ("0", Ty.Bool),
                _ => throw new Exception("Tipo desconocido")
            };
        }

        // === Carga/almacenamiento de variables ===
        private (string alloca, Ty ty) EnsureLocal(string name, Ty declType)
        {
            if (!sym.ContainsKey(name))
            {
                var slot = Fresh();
                sym[name] = (slot, declType);
                var llty = LlvmTy(declType);
                sb.AppendLine($"  {slot} = alloca {llty}");
            }
            return sym[name];
        }

        private (string alloca, Ty ty) Lookup(string name)
        {
            if (!sym.TryGetValue(name, out var info))
                throw new Exception($"Variable no declarada: {name}");
            return info;
        }

        // === Tipos LLVM ===
        private static string LlvmTy(Ty ty) => ty switch
        {
            Ty.Int => "i32",
            Ty.Double => "double",
            Ty.Bool => "i1",
            _ => throw new Exception("Tipo desconocido")
        };

        // === Promociones / coerciones ===
        // Promueve a double si alguno es double; bool en aritmética no se permite.
        private (string v, Ty ty) CastIfNeeded((string v, Ty ty) x, Ty target)
        {
            if (x.ty == target) return x;
            if (x.ty == Ty.Int && target == Ty.Double)
            {
                var t = Fresh();
                sb.AppendLine($"  {t} = sitofp i32 {x.v} to double");
                return (t, Ty.Double);
            }
            if (x.ty == Ty.Bool && target == Ty.Int)
            {
                var t = Fresh();
                sb.AppendLine($"  {t} = zext i1 {x.v} to i32");
                return (t, Ty.Int);
            }
            if (x.ty == Ty.Bool && target == Ty.Double)
            {
                var t = Fresh();
                sb.AppendLine($"  {t} = uitofp i1 {x.v} to double");
                return (t, Ty.Double);
            }
            throw new Exception($"Coerción no soportada de {x.ty} a {target}");
        }

        private Ty ResultNumericTy(Ty a, Ty b)
        {
            if (a == Ty.Double || b == Ty.Double) return Ty.Double;
            if (a == Ty.Int && b == Ty.Int) return Ty.Int;
            throw new Exception($"Aritmética no válida entre {a} y {b}");
        }

        // Convierte cualquier numérico/booleano a i1 "verdad" (x != 0)
        private (string v, Ty ty) AsBool((string v, Ty ty) x)
        {
            if (x.ty == Ty.Bool) return x;
            if (x.ty == Ty.Int)
            {
                var t = Fresh();
                sb.AppendLine($"  {t} = icmp ne i32 {x.v}, 0");
                return (t, Ty.Bool);
            }
            if (x.ty == Ty.Double)
            {
                var t = Fresh();
                sb.AppendLine($"  {t} = fcmp one double {x.v}, 0.000000e+00");
                return (t, Ty.Bool);
            }
            throw new Exception("Tipo no convertible a bool");
        }

        // === EXPRESIONES ===
        // Devuelve (registro/valor, tipo)
        public (string v, Ty ty) GenExpr(ExpressionNode node)
        {
            switch (node)
            {
                case LiteralNode lit:
                    return ParseLiteral(lit.Value);

                case VariableNode var:
                    {
                        var (alloca, ty) = Lookup(var.Name);
                        var t = Fresh();
                        sb.AppendLine($"  {t} = load {LlvmTy(ty)}, {LlvmTy(ty)}* {alloca}");
                        return (t, ty);
                    }

                case BinaryOpNode bin:
                    {
                        // L y R
                        var L = GenExpr(bin.Left);
                        var R = GenExpr(bin.Right);

                        // Lógicos cortocircuito con i1
                        if (bin.Op == "&&" || bin.Op == "||")
                        {
                            var Lb = AsBool(L);
                            var Rb = AsBool(R);
                            var t = Fresh();
                            if (bin.Op == "&&")
                            {
                                sb.AppendLine($"  {t} = and i1 {Lb.v}, {Rb.v}");
                            }
                            else
                            {
                                sb.AppendLine($"  {t} = or i1 {Lb.v}, {Rb.v}");
                            }
                            return (t, Ty.Bool);
                        }

                        // Comparaciones -> i1
                        if (bin.Op is ">" or "<" or ">=" or "<=" or "==" or "!=")
                        {
                            // promover si hace falta
                            if (L.ty == Ty.Double || R.ty == Ty.Double)
                            {
                                var Lt = CastIfNeeded(L, Ty.Double);
                                var Rt = CastIfNeeded(R, Ty.Double);
                                var t = Fresh();
                                var op = bin.Op switch
                                {
                                    ">" => "fcmp ogt",
                                    "<" => "fcmp olt",
                                    ">=" => "fcmp oge",
                                    "<=" => "fcmp ole",
                                    "==" => "fcmp oeq",
                                    "!=" => "fcmp one",
                                    _ => throw new Exception()
                                };
                                sb.AppendLine($"  {t} = {op} double {Lt.v}, {Rt.v}");
                                return (t, Ty.Bool);
                            }
                            else
                            {
                                var t = Fresh();
                                var op = bin.Op switch
                                {
                                    ">" => "icmp sgt",
                                    "<" => "icmp slt",
                                    ">=" => "icmp sge",
                                    "<=" => "icmp sle",
                                    "==" => "icmp eq",
                                    "!=" => "icmp ne",
                                    _ => throw new Exception()
                                };
                                sb.AppendLine($"  {t} = {op} i32 {L.v}, {R.v}");
                                return (t, Ty.Bool);
                            }
                        }

                        // Aritmética
                        var resTy = ResultNumericTy(L.ty, R.ty);
                        var L2 = CastIfNeeded(L, resTy);
                        var R2 = CastIfNeeded(R, resTy);
                        var tmp = Fresh();
                        if (resTy == Ty.Int)
                        {
                            var op = bin.Op switch
                            {
                                "+" => "add",
                                "-" => "sub",
                                "*" => "mul",
                                "/" => "sdiv",
                                _ => throw new Exception($"Operador aritmético no soportado: {bin.Op}")
                            };
                            sb.AppendLine($"  {tmp} = {op} i32 {L2.v}, {R2.v}");
                            return (tmp, Ty.Int);
                        }
                        else // double
                        {
                            var op = bin.Op switch
                            {
                                "+" => "fadd",
                                "-" => "fsub",
                                "*" => "fmul",
                                "/" => "fdiv",
                                _ => throw new Exception($"Operador aritmético no soportado: {bin.Op}")
                            };
                            sb.AppendLine($"  {tmp} = {op} double {L2.v}, {R2.v}");
                            return (tmp, Ty.Double);
                        }
                    }

                default:
                    throw new Exception($"Expresión no soportada: {node.GetType().Name}");
            }
        }

        private (string v, Ty ty) ParseLiteral(string raw)
        {
            // Detecta int vs double por presencia de '.' o 'e/E'
            if (raw.Contains(".") || raw.Contains("e") || raw.Contains("E"))
            {
                // LLVM double literal en notación científica es seguro; si llega "5.0" también funciona
                // Asegura formato válido:
                string lit = raw.Contains("e", StringComparison.OrdinalIgnoreCase) ? raw : raw + "";
                return (lit, Ty.Double);
            }
            else
            {
                return (raw, Ty.Int);
            }
        }

        // === STATEMENTS ===
        public void GenStmt(AstNode node)
        {
            switch (node)
            {
                case DeclarationNode d:
                    {
                        var declTy = ParseTypeKeyword(d.Type);
                        var (slot, _) = EnsureLocal(d.Name, declTy);

                        (string v, Ty ty) init;
                        if (d.Value is null)
                        {
                            init = ZeroOf(declTy);
                        }
                        else
                        {
                            init = GenExpr(d.Value);
                            init = CastIfNeeded(init, declTy); // forzar al tipo declarado
                        }

                        sb.AppendLine($"  store {LlvmTy(declTy)} {init.v}, {LlvmTy(declTy)}* {slot}");
                        break;
                    }

                case AssignmentNode a:
                    {
                        var (slot, ty) = Lookup(a.Name);
                        var rhs = GenExpr(a.Value);
                        rhs = CastIfNeeded(rhs, ty);
                        sb.AppendLine($"  store {LlvmTy(ty)} {rhs.v}, {LlvmTy(ty)}* {slot}");
                        break;
                    }

                case PrintNode p:
                    {
                        var x = GenExpr(p.Value);
                        if (x.ty == Ty.Int)
                        {
                            var fmt = Fresh();
                            sb.AppendLine($"  {fmt} = getelementptr [4 x i8], [4 x i8]* @.fmt_i, i32 0, i32 0");
                            sb.AppendLine($"  call i32 (i8*, ...) @printf(i8* {fmt}, i32 {x.v})");
                        }
                        else if (x.ty == Ty.Double)
                        {
                            var fmt = Fresh();
                            sb.AppendLine($"  {fmt} = getelementptr [4 x i8], [4 x i8]* @.fmt_f, i32 0, i32 0");
                            sb.AppendLine($"  call i32 (i8*, ...) @printf(i8* {fmt}, double {x.v})");
                        }
                        else if (x.ty == Ty.Bool)
                        {
                            // imprime 0/1 como int
                            var asI32 = CastIfNeeded(x, Ty.Int);
                            var fmt = Fresh();
                            sb.AppendLine($"  {fmt} = getelementptr [4 x i8], [4 x i8]* @.fmt_i, i32 0, i32 0");
                            sb.AppendLine($"  call i32 (i8*, ...) @printf(i8* {fmt}, i32 {asI32.v})");
                        }
                        else throw new Exception("Tipo no imprimible");
                        break;
                    }

                case IfNode iff:
                    {
                        var cond = AsBool(GenExpr(iff.Condition));

                        var thenLbl = FreshLbl("then");
                        var endLbl = FreshLbl("endif");
                        var elseLbl = iff.ElseBlock.Any() ? FreshLbl("else") : endLbl;

                        sb.AppendLine($"  br i1 {cond.v}, label %{thenLbl}, label %{elseLbl}");

                        // then:
                        sb.AppendLine($"{thenLbl}:");
                        foreach (var s in iff.ThenBlock) GenStmt(s);
                        sb.AppendLine($"  br label %{endLbl}");

                        if (iff.ElseBlock.Any())
                        {
                            sb.AppendLine($"{elseLbl}:");
                            foreach (var s in iff.ElseBlock) GenStmt(s);
                            sb.AppendLine($"  br label %{endLbl}");
                        }

                        // end:
                        sb.AppendLine($"{endLbl}:");
                        break;
                    }

                case WhileNode w:
                    {
                        var condLbl = FreshLbl("while.cond");
                        var bodyLbl = FreshLbl("while.body");
                        var endLbl = FreshLbl("while.end");

                        sb.AppendLine($"  br label %{condLbl}");
                        sb.AppendLine($"{condLbl}:");

                        var cond = AsBool(GenExpr(w.Condition));
                        sb.AppendLine($"  br i1 {cond.v}, label %{bodyLbl}, label %{endLbl}");

                        sb.AppendLine($"{bodyLbl}:");
                        foreach (var s in w.Body)
                            GenStmt(s);
                        sb.AppendLine($"  br label %{condLbl}");

                        sb.AppendLine($"{endLbl}:");
                        break;
                    }

                case ForNode f:
                    {
                        var condLbl = FreshLbl("for.cond");
                        var bodyLbl = FreshLbl("for.body");
                        var endLbl = FreshLbl("for.end");

                        if (f.Init != null)
                            GenStmt(f.Init);

                        sb.AppendLine($"  br label %{condLbl}");
                        sb.AppendLine($"{condLbl}:");

                        string condValue;
                        if (f.Condition != null)
                        {
                            var cond = AsBool(GenExpr(f.Condition));
                            condValue = cond.v;
                        }
                        else condValue = "true";

                        sb.AppendLine($"  br i1 {condValue}, label %{bodyLbl}, label %{endLbl}");

                        sb.AppendLine($"{bodyLbl}:");
                        foreach (var s in f.Body)
                            GenStmt(s);

                        if (f.Increment != null)
                            GenStmt(f.Increment);

                        sb.AppendLine($"  br label %{condLbl}");

                        sb.AppendLine($"{endLbl}:");
                        break;
                    }
                case ReadNode r:
                    {
                        var (slot, ty) = Lookup(r.Name);
                        var fmt = Fresh();

                        if (ty == Ty.Int)
                        {
                            sb.AppendLine($"  {fmt} = getelementptr [3 x i8], [3 x i8]* @.fmt_i_in, i32 0, i32 0");
                            sb.AppendLine($"  call i32 (i8*, ...) @scanf(i8* {fmt}, {LlvmTy(ty)}* {slot})");
                        }
                        else if (ty == Ty.Double)
                        {
                            sb.AppendLine($"  {fmt} = getelementptr [3 x i8], [3 x i8]* @.fmt_f_in, i32 0, i32 0");
                            sb.AppendLine($"  call i32 (i8*, ...) @scanf(i8* {fmt}, {LlvmTy(ty)}* {slot})");
                        }
                        break;
                    }
                default:
                    throw new Exception($"Statement no soportado: {node.GetType().Name}");
            }
        }

        public void GenProgram(ProgramNode prog)
        {
            EmitPrologue();
            foreach (var s in prog.Statements) GenStmt(s);
            EmitEpilogue();
        }

        public void Save(string path) => File.WriteAllText(path, sb.ToString());
        public override string ToString() => sb.ToString();
    }
}
