using System.Globalization;

namespace CalculatorWeb.Services;

public sealed class CalculatorService
{
    private const NumberStyles InputStyles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    public static bool TryParseNumber(string? input, out decimal value)
    {
        var normalized = input?.Trim().Replace(',', '.');
        return decimal.TryParse(normalized, InputStyles, CultureInfo.InvariantCulture, out value);
    }

    public static bool IsOperationSupported(string? operation) =>
        operation is "add" or "subtract" or "multiply" or "divide";

    public decimal Calculate(decimal first, decimal second, string operation) => operation switch
    {
        "add" => checked(first + second),
        "subtract" => checked(first - second),
        "multiply" => checked(first * second),
        "divide" when second == 0 => throw new DivideByZeroException("На ноль делить нельзя."),
        "divide" => first / second,
        _ => throw new ArgumentException("Неизвестная операция.", nameof(operation))
    };

    public static string Format(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture).Replace('.', ',');

    public static string Symbol(string operation) => operation switch
    {
        "add" => "+", "subtract" => "−", "multiply" => "×", "divide" => "÷", _ => "?"
    };
}
