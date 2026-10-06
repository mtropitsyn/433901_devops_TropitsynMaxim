using CalculatorWeb.Controllers;
using CalculatorWeb.Models;
using CalculatorWeb.Services;
using Microsoft.AspNetCore.Mvc;

var calculator = new CalculatorService();
var passed = 0;
void Check(bool condition, string title)
{
    if (!condition) throw new Exception($"FAIL: {title}");
    Console.WriteLine($"PASS: {title}");
    passed++;
}
void Throws<T>(Action action, string title) where T : Exception
{
    try { action(); } catch (T) { Check(true, title); return; }
    throw new Exception($"FAIL: {title}");
}

Check(calculator.Calculate(12, 4, "add") == 16, "addition");
Check(calculator.Calculate(12, 4, "subtract") == 8, "subtraction");
Check(calculator.Calculate(-3, 4, "multiply") == -12, "negative multiplication");
Check(calculator.Calculate(7, 2, "divide") == 3.5m, "fractional division");
Check(calculator.Calculate(0.1m, 0.2m, "add") == 0.3m, "decimal precision");
Check(CalculatorService.TryParseNumber(" -12,5 ", out var comma) && comma == -12.5m, "comma input");
Check(CalculatorService.TryParseNumber("12.5", out var dot) && dot == 12.5m, "dot input");
foreach (var invalid in new[] { "", "abc", "1,2.3", "1 000", "NaN", "1e3", "79228162514264337593543950336" })
    Check(!CalculatorService.TryParseNumber(invalid, out _), $"reject invalid number [{invalid}]");
Throws<DivideByZeroException>(() => calculator.Calculate(1, 0, "divide"), "division by zero");
Throws<OverflowException>(() => calculator.Calculate(decimal.MaxValue, 1, "add"), "addition overflow");
Throws<OverflowException>(() => calculator.Calculate(decimal.MinValue, 1, "subtract"), "subtraction overflow");
Throws<OverflowException>(() => calculator.Calculate(decimal.MaxValue, 2, "multiply"), "multiplication overflow");
Throws<ArgumentException>(() => calculator.Calculate(2, 3, "invalid"), "unknown operation");
Check(CalculatorService.Format(3.5m) == "3,5", "Russian result format");

var controller = new CalculatorController(calculator);
var success = new CalculatorViewModel { FirstNumber = "12,5", SecondNumber = "4", Operation = "multiply" };
Check(controller.Index(success) is ViewResult && success.Result == "50", "controller result");
controller = new CalculatorController(calculator);
var zero = new CalculatorViewModel { FirstNumber = "10", SecondNumber = "0", Operation = "divide" };
controller.Index(zero);
Check(!controller.ModelState.IsValid && zero.Result is null, "controller handles division by zero");
controller = new CalculatorController(calculator);
var malformed = new CalculatorViewModel { FirstNumber = "abc", SecondNumber = "4", Operation = "add" };
controller.Index(malformed);
Check(!controller.ModelState.IsValid && malformed.Result is null, "controller handles malformed input");
Console.WriteLine($"Checks passed: {passed}");
