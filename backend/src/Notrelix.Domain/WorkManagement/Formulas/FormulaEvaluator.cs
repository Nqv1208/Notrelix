using System.Globalization;

namespace Notrelix.Domain.WorkManagement.Formulas;

public enum FormulaValueKind { Empty, Number, Text, Boolean, Date, Error }

/// <summary>The result of evaluating a Formula or Rollup for one Item. An error carries a stable code.</summary>
public readonly record struct FormulaValue(FormulaValueKind Kind, decimal Number = 0, string? Text = null, bool Flag = false, DateTimeOffset? Date = null)
{
    public static readonly FormulaValue Empty = new(FormulaValueKind.Empty);
    public static FormulaValue Of(decimal value) => new(FormulaValueKind.Number, Number: value);
    public static FormulaValue Of(string value) => new(FormulaValueKind.Text, Text: value);
    public static FormulaValue Of(bool value) => new(FormulaValueKind.Boolean, Flag: value);
    public static FormulaValue Of(DateTimeOffset value) => new(FormulaValueKind.Date, Date: value);
    public static FormulaValue Error(string code) => new(FormulaValueKind.Error, Text: code);

    public bool IsError => Kind == FormulaValueKind.Error;
}

/// <summary>
/// Evaluates a parsed Formula. Empty counts as 0 in arithmetic and "" in text; a type
/// mismatch, a division by zero or an error operand yields an error value (never a guess).
/// </summary>
public static class FormulaEvaluator
{
    public const string ErrDivideByZero = "divide-by-zero";
    public const string ErrType = "type";

    public static FormulaValue Evaluate(FormulaNode node, Func<string, FormulaValue> resolve) => node switch
    {
        NumberNode n => FormulaValue.Of(n.Value),
        TextNode t => FormulaValue.Of(t.Value),
        BooleanNode b => FormulaValue.Of(b.Value),
        FieldRefNode r => resolve(r.Name),
        UnaryMinusNode u => Num(Evaluate(u.Operand, resolve), x => FormulaValue.Of(-x)),
        BinaryNode b => Binary(b, resolve),
        CallNode c => Call(c, resolve),
        _ => FormulaValue.Error(ErrType),
    };

    private static FormulaValue Num(FormulaValue v, Func<decimal, FormulaValue> next) => v.Kind switch
    {
        FormulaValueKind.Error => v,
        FormulaValueKind.Empty => next(0),
        FormulaValueKind.Number => next(v.Number),
        _ => FormulaValue.Error(ErrType),
    };

    private static FormulaValue Binary(BinaryNode node, Func<string, FormulaValue> resolve)
    {
        var left = Evaluate(node.Left, resolve);
        var right = Evaluate(node.Right, resolve);
        if (left.IsError) return left;
        if (right.IsError) return right;

        switch (node.Op)
        {
            case "&":
                return FormulaValue.Of(Show(left) + Show(right));
            case "=":
            case "<>":
            case "<":
            case "<=":
            case ">":
            case ">=":
                return Compare(node.Op, left, right);
        }

        // A date minus a date is a day count; every other arithmetic is numeric.
        if (node.Op == "-" && left.Kind == FormulaValueKind.Date && right.Kind == FormulaValueKind.Date)
            return FormulaValue.Of(DayDiff(left.Date!.Value, right.Date!.Value));
        return Num(left, a => Num(right, b => node.Op switch
        {
            "+" => FormulaValue.Of(a + b),
            "-" => FormulaValue.Of(a - b),
            "*" => FormulaValue.Of(a * b),
            _ => b == 0 ? FormulaValue.Error(ErrDivideByZero) : FormulaValue.Of(a / b),
        }));
    }

    private static decimal DayDiff(DateTimeOffset end, DateTimeOffset start) =>
        (decimal)(end.UtcDateTime.Date - start.UtcDateTime.Date).TotalDays;

    private static string Show(FormulaValue v) => v.Kind switch
    {
        FormulaValueKind.Number => v.Number.ToString(CultureInfo.InvariantCulture),
        FormulaValueKind.Text => v.Text!,
        FormulaValueKind.Boolean => v.Flag ? "TRUE" : "FALSE",
        FormulaValueKind.Date => v.Date!.Value.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        _ => "",
    };

    private static FormulaValue Compare(string op, FormulaValue a, FormulaValue b)
    {
        // Empty compares as the other side's zero value.
        if (a.Kind == FormulaValueKind.Empty) a = Zero(b);
        if (b.Kind == FormulaValueKind.Empty) b = Zero(a);
        if (a.Kind != b.Kind) return FormulaValue.Error(ErrType);
        var c = a.Kind switch
        {
            FormulaValueKind.Number => a.Number.CompareTo(b.Number),
            FormulaValueKind.Text => string.Compare(a.Text, b.Text, StringComparison.OrdinalIgnoreCase),
            FormulaValueKind.Boolean => a.Flag.CompareTo(b.Flag),
            FormulaValueKind.Date => a.Date!.Value.CompareTo(b.Date!.Value),
            _ => 0,
        };
        return FormulaValue.Of(op switch
        {
            "=" => c == 0,
            "<>" => c != 0,
            "<" => c < 0,
            "<=" => c <= 0,
            ">" => c > 0,
            _ => c >= 0,
        });
    }

    private static FormulaValue Zero(FormulaValue like) => like.Kind switch
    {
        FormulaValueKind.Number => FormulaValue.Of(0m),
        FormulaValueKind.Text => FormulaValue.Of(""),
        FormulaValueKind.Boolean => FormulaValue.Of(false),
        _ => like,
    };

    private static FormulaValue Call(CallNode node, Func<string, FormulaValue> resolve)
    {
        if (node.Function == "IF")
        {
            var cond = Evaluate(node.Args[0], resolve);
            if (cond.IsError) return cond;
            if (cond.Kind != FormulaValueKind.Boolean) return FormulaValue.Error(ErrType);
            return Evaluate(node.Args[cond.Flag ? 1 : 2], resolve);
        }

        var args = node.Args.Select(a => Evaluate(a, resolve)).ToList();
        if (args.FirstOrDefault(a => a.IsError) is { IsError: true } failed) return failed;

        switch (node.Function)
        {
            case "DAYS":
                return args[0].Kind == FormulaValueKind.Date && args[1].Kind == FormulaValueKind.Date
                    ? FormulaValue.Of(DayDiff(args[0].Date!.Value, args[1].Date!.Value))
                    : args.Any(a => a.Kind == FormulaValueKind.Empty) ? FormulaValue.Empty : FormulaValue.Error(ErrType);
            case "ROUND":
                return Num(args[0], x => args.Count == 1
                    ? FormulaValue.Of(Math.Round(x, MidpointRounding.AwayFromZero))
                    : Num(args[1], d => FormulaValue.Of(Math.Round(x, (int)Math.Clamp(d, 0, 10), MidpointRounding.AwayFromZero))));
        }

        // SUM / MIN / MAX: numbers only; empty arguments are skipped.
        var numbers = new List<decimal>();
        foreach (var a in args)
        {
            if (a.Kind == FormulaValueKind.Empty) continue;
            if (a.Kind != FormulaValueKind.Number) return FormulaValue.Error(ErrType);
            numbers.Add(a.Number);
        }
        return node.Function switch
        {
            "SUM" => FormulaValue.Of(numbers.Sum()),
            _ when numbers.Count == 0 => FormulaValue.Empty,
            "MIN" => FormulaValue.Of(numbers.Min()),
            _ => FormulaValue.Of(numbers.Max()),
        };
    }
}
