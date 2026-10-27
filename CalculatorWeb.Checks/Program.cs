using CalculatorWeb.Controllers;
using CalculatorWeb.Data;
using CalculatorWeb.Models;
using CalculatorWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

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

var saved = new RecordingCalculationRepository();
CalculatorController CreateController(ICalculationRepository repository) =>
    new(calculator, repository, NullLogger<CalculatorController>.Instance);
var controller = CreateController(saved);
var success = new CalculatorViewModel { FirstNumber = "12,5", SecondNumber = "4", Operation = "multiply" };
Check(await controller.Index(success) is ViewResult && success.Result == "50", "controller result");
Check(saved.Records.Count == 1 && saved.Records[0] == new CalculationRecord(12.5m, 4, "multiply", 50),
    "successful calculation saves both operands, operation and result exactly once");
controller = CreateController(saved);
var zero = new CalculatorViewModel { FirstNumber = "10", SecondNumber = "0", Operation = "divide" };
await controller.Index(zero);
Check(!controller.ModelState.IsValid && zero.Result is null, "controller handles division by zero");
Check(saved.Records.Count == 1, "division by zero is not saved");
controller = CreateController(saved);
var malformed = new CalculatorViewModel { FirstNumber = "abc", SecondNumber = "4", Operation = "add" };
await controller.Index(malformed);
Check(!controller.ModelState.IsValid && malformed.Result is null, "controller handles malformed input");
Check(saved.Records.Count == 1, "malformed input is not saved");
controller = CreateController(saved);
var overflow = new CalculatorViewModel { FirstNumber = decimal.MaxValue.ToString(), SecondNumber = "2", Operation = "multiply" };
await controller.Index(overflow);
Check(!controller.ModelState.IsValid && overflow.Result is null && saved.Records.Count == 1,
    "overflow does not create a history entry");
controller = CreateController(saved);
await controller.Index(new CalculatorViewModel { FirstNumber = "1", SecondNumber = "2", Operation = "invalid" });
Check(!controller.ModelState.IsValid && saved.Records.Count == 1, "invalid operation is not saved");
var failing = new RecordingCalculationRepository { Failure = new CalculationStorageException("Unavailable") };
controller = CreateController(failing);
var failed = new CalculatorViewModel { FirstNumber = "1", SecondNumber = "2", Operation = "add" };
Check(await controller.Index(failed) is ViewResult && !controller.ModelState.IsValid,
    "database failure is shown as a form error");
Check(failed.Result is null && failed.Expression is null && failing.Records.Count == 0,
    "unsaved result is not presented as successful");
controller = CreateController(saved);
Check(controller.Index() is ViewResult && saved.Records.Count == 1, "GET does not insert calculations");
using var cancelled = new CancellationTokenSource();
cancelled.Cancel();
try
{
    await controller.Index(new CalculatorViewModel { FirstNumber = "1", SecondNumber = "2", Operation = "add" }, cancelled.Token);
    throw new Exception("FAIL: cancellation was ignored");
}
catch (OperationCanceledException)
{
    Check(saved.Records.Count == 1, "request cancellation reaches database persistence");
}
Console.WriteLine($"Checks passed: {passed}");

sealed class RecordingCalculationRepository : ICalculationRepository
{
    public List<CalculationRecord> Records { get; } = [];
    public Exception? Failure { get; init; }

    public Task SaveAsync(CalculationRecord calculation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Failure is not null) return Task.FromException(Failure);
        Records.Add(calculation);
        return Task.CompletedTask;
    }
}
