using System.Globalization;
using System.Text;

namespace Notrelix.Domain.WorkManagement.Formulas;

/// <summary>A node of a parsed Formula expression.</summary>
public abstract record FormulaNode;
public sealed record NumberNode(decimal Value) : FormulaNode;
public sealed record TextNode(string Value) : FormulaNode;
public sealed record BooleanNode(bool Value) : FormulaNode;
/// <summary>A reference to a field of the same Board by its name: <c>[Effort]</c>.</summary>
public sealed record FieldRefNode(string Name) : FormulaNode;
public sealed record UnaryMinusNode(FormulaNode Operand) : FormulaNode;
public sealed record BinaryNode(string Op, FormulaNode Left, FormulaNode Right) : FormulaNode;
public sealed record CallNode(string Function, IReadOnlyList<FormulaNode> Args) : FormulaNode;

/// <summary>
/// Formula language v1: field references <c>[Name]</c>, numbers, "text", TRUE/FALSE,
/// <c>+ - * /</c>, <c>&amp;</c> (text concat), comparisons <c>= &lt;&gt; &lt; &lt;= &gt; &gt;=</c>, parentheses and
/// IF, SUM, MIN, MAX, ROUND, DAYS(end, start). No user code. At most <see cref="MaxNodes"/> nodes.
/// </summary>
public static class FormulaLanguage
{
    public const int MaxNodes = 200;
    public static readonly IReadOnlySet<string> Functions =
        new HashSet<string>(["IF", "SUM", "MIN", "MAX", "ROUND", "DAYS"], StringComparer.Ordinal);

    /// <summary>Parses <paramref name="text"/>; a syntax error or an oversize expression is a business-rule failure.</summary>
    public static FormulaNode Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldSettings_FormulaExpressionRequired, "A Formula field needs an expression.");
        return new Parser(text).ParseAll();
    }

    /// <summary>The distinct field names the expression references.</summary>
    public static IReadOnlyCollection<string> References(FormulaNode node)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Walk(FormulaNode n)
        {
            switch (n)
            {
                case FieldRefNode r: names.Add(r.Name); break;
                case UnaryMinusNode u: Walk(u.Operand); break;
                case BinaryNode b: Walk(b.Left); Walk(b.Right); break;
                case CallNode c: foreach (var a in c.Args) Walk(a); break;
            }
        }
        Walk(node);
        return names;
    }

    /// <summary>
    /// Rewrites every <c>[reference]</c> outside text literals with <paramref name="map"/>; everything
    /// else is kept character for character. Used to store ids and to show names again.
    /// </summary>
    public static string RewriteReferences(string expression, Func<string, string> map)
    {
        var sb = new StringBuilder(expression.Length);
        var inText = false;
        for (var i = 0; i < expression.Length; i++)
        {
            var c = expression[i];
            if (c == '"') { inText = !inText; sb.Append(c); continue; }
            if (!inText && c == '[' && expression.IndexOf(']', i) is var end and > 0)
            {
                sb.Append('[').Append(map(expression[(i + 1)..end].Trim())).Append(']');
                i = end;
                continue;
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    private static BusinessRuleException Syntax(string message) =>
        new(WorkManagementRuleCodes.WorkManagement_FieldSettings_FormulaSyntaxInvalid, message);

    private sealed class Parser(string src)
    {
        private int _pos;
        private int _nodes;
        private int _depth;

        public FormulaNode ParseAll()
        {
            var node = Comparison();
            SkipWs();
            if (_pos < src.Length) throw Syntax($"Unexpected '{src[_pos]}' at position {_pos + 1}.");
            return node;
        }

        private T Count<T>(T node) where T : FormulaNode
        {
            // Fails as soon as the cap is passed: a huge input never builds a huge tree.
            if (++_nodes > MaxNodes)
                throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldSettings_FormulaTooComplex, $"A formula may have at most {MaxNodes} nodes.");
            return node;
        }

        private void SkipWs() { while (_pos < src.Length && char.IsWhiteSpace(src[_pos])) _pos++; }

        private bool Eat(string token)
        {
            SkipWs();
            if (string.CompareOrdinal(src, _pos, token, 0, token.Length) != 0) return false;
            _pos += token.Length;
            return true;
        }

        private FormulaNode Comparison()
        {
            var left = Concat();
            foreach (var op in new[] { "<>", "<=", ">=", "=", "<", ">" })
                if (Eat(op)) return Count(new BinaryNode(op, left, Concat()));
            return left;
        }

        private FormulaNode Concat()
        {
            var left = Additive();
            while (Eat("&")) left = Count(new BinaryNode("&", left, Additive()));
            return left;
        }

        private FormulaNode Additive()
        {
            var left = Multiplicative();
            while (true)
            {
                if (Eat("+")) left = Count(new BinaryNode("+", left, Multiplicative()));
                else if (Eat("-")) left = Count(new BinaryNode("-", left, Multiplicative()));
                else return left;
            }
        }

        private FormulaNode Multiplicative()
        {
            var left = Unary();
            while (true)
            {
                if (Eat("*")) left = Count(new BinaryNode("*", left, Unary()));
                else if (Eat("/")) left = Count(new BinaryNode("/", left, Unary()));
                else return left;
            }
        }

        private FormulaNode Unary() => Eat("-") ? Count(new UnaryMinusNode(Unary())) : Primary();

        private FormulaNode Primary()
        {
            SkipWs();
            if (_pos >= src.Length) throw Syntax("The expression ends unexpectedly.");
            var c = src[_pos];
            if (c == '(')
            {
                _pos++;
                // Parentheses add no node, so they are capped on their own against stack exhaustion.
                if (++_depth > MaxNodes)
                    throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FieldSettings_FormulaTooComplex, $"A formula may have at most {MaxNodes} nodes.");
                var inner = Comparison();
                if (!Eat(")")) throw Syntax("A ')' is missing.");
                _depth--;
                return inner;
            }
            if (c == '[')
            {
                var end = src.IndexOf(']', _pos);
                if (end < 0) throw Syntax("A ']' is missing.");
                var name = src[(_pos + 1)..end].Trim();
                if (name.Length == 0) throw Syntax("A field reference is empty.");
                _pos = end + 1;
                return Count(new FieldRefNode(name));
            }
            if (c == '"')
            {
                var sb = new StringBuilder();
                _pos++;
                while (true)
                {
                    if (_pos >= src.Length) throw Syntax("A closing quote is missing.");
                    if (src[_pos] == '"')
                    {
                        if (_pos + 1 < src.Length && src[_pos + 1] == '"') { sb.Append('"'); _pos += 2; continue; }
                        _pos++;
                        break;
                    }
                    sb.Append(src[_pos++]);
                }
                return Count(new TextNode(sb.ToString()));
            }
            if (char.IsAsciiDigit(c) || c == '.')
            {
                var start = _pos;
                while (_pos < src.Length && (char.IsAsciiDigit(src[_pos]) || src[_pos] == '.')) _pos++;
                if (!decimal.TryParse(src[start.._pos], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number))
                    throw Syntax($"'{src[start.._pos]}' is not a number.");
                return Count(new NumberNode(number));
            }
            if (char.IsAsciiLetter(c))
            {
                var start = _pos;
                while (_pos < src.Length && (char.IsAsciiLetterOrDigit(src[_pos]) || src[_pos] == '_')) _pos++;
                var word = src[start.._pos].ToUpperInvariant();
                if (word == "TRUE") return Count(new BooleanNode(true));
                if (word == "FALSE") return Count(new BooleanNode(false));
                if (!Functions.Contains(word)) throw Syntax($"Unknown function '{src[start.._pos]}'.");
                if (!Eat("(")) throw Syntax($"'(' is expected after {word}.");
                var args = new List<FormulaNode>();
                if (!Eat(")"))
                {
                    do args.Add(Comparison()); while (Eat(","));
                    if (!Eat(")")) throw Syntax("A ')' is missing.");
                }
                var arity = args.Count;
                var valid = word switch
                {
                    "IF" => arity == 3,
                    "ROUND" => arity is 1 or 2,
                    "DAYS" => arity == 2,
                    _ => arity >= 1,
                };
                if (!valid) throw Syntax($"{word} is called with the wrong number of arguments.");
                return Count(new CallNode(word, args));
            }
            throw Syntax($"Unexpected '{c}' at position {_pos + 1}.");
        }
    }
}
