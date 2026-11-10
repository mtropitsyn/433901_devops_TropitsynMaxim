using CalculatorWeb.Controllers;
using CalculatorWeb.Data;
using CalculatorWeb.Models;
using CalculatorWeb.Services;
using Confluent.Kafka;
using Microsoft.AspNetCore.Http;
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
var published = new RecordingCalculationPublisher();
CalculatorController CreateController(ICalculationPublisher publisher) =>
    new(calculator, saved, publisher, NullLogger<CalculatorController>.Instance)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
    };
var controller = CreateController(published);
var success = new CalculatorViewModel { FirstNumber = "12,5", SecondNumber = "4", Operation = "multiply" };
var redirect = await controller.Index(success) as RedirectToActionResult;
var request = published.Requests.Single();
Check(redirect?.ActionName == "Result" && Equals(redirect.RouteValues?["id"], request.RequestId),
    "controller redirects to the result for the published request");
Check(request.RequestId != Guid.Empty && request.FirstOperand == 12.5m && request.SecondOperand == 4 &&
    request.Operation == "multiply" && saved.Records.Count == 0, "form publishes operands without saving a result");
Check(await controller.Result(request.RequestId, default) is ViewResult { ViewName: "Pending" } &&
    controller.Response.StatusCode == StatusCodes.Status202Accepted, "result waits for the consumer");
await saved.SaveAsync(new CalculationRecord(12.5m, 4, "multiply", 50, request.RequestId));
controller = CreateController(published);
Check(await controller.Result(request.RequestId, default) is ViewResult
    { Model: CalculatorViewModel { Result: "50", Expression: "12,5 × 4" } },
    "result page reads and formats the persisted result");
controller = CreateController(published);
var zero = new CalculatorViewModel { FirstNumber = "10", SecondNumber = "0", Operation = "divide" };
await controller.Index(zero);
Check(!controller.ModelState.IsValid && zero.Result is null, "controller handles division by zero");
Check(published.Requests.Count == 1, "division by zero is not published");
controller = CreateController(published);
var malformed = new CalculatorViewModel { FirstNumber = "abc", SecondNumber = "4", Operation = "add" };
await controller.Index(malformed);
Check(!controller.ModelState.IsValid && malformed.Result is null, "controller handles malformed input");
Check(published.Requests.Count == 1, "malformed input is not published");
controller = CreateController(published);
var overflow = new CalculatorViewModel { FirstNumber = decimal.MaxValue.ToString(), SecondNumber = "2", Operation = "multiply" };
await controller.Index(overflow);
Check(!controller.ModelState.IsValid && overflow.Result is null && published.Requests.Count == 1,
    "overflow is not published");
controller = CreateController(published);
await controller.Index(new CalculatorViewModel { FirstNumber = "1", SecondNumber = "2", Operation = "invalid" });
Check(!controller.ModelState.IsValid && published.Requests.Count == 1, "invalid operation is not published");
var failing = new RecordingCalculationPublisher { Failure = new KafkaException(new Error(ErrorCode.Local_TimedOut)) };
controller = CreateController(failing);
var failed = new CalculatorViewModel { FirstNumber = "1", SecondNumber = "2", Operation = "add" };
Check(await controller.Index(failed) is ViewResult && !controller.ModelState.IsValid &&
    controller.Response.StatusCode == StatusCodes.Status503ServiceUnavailable, "Kafka failure is shown as a form error");
Check(failed.Result is null && failed.Expression is null && failing.Requests.Count == 0,
    "unsaved result is not presented as successful");
controller = CreateController(published);
Check(controller.Index() is ViewResult && published.Requests.Count == 1, "GET does not publish calculations");
using var cancelled = new CancellationTokenSource();
cancelled.Cancel();
try
{
    await controller.Index(new CalculatorViewModel { FirstNumber = "1", SecondNumber = "2", Operation = "add" }, cancelled.Token);
    throw new Exception("FAIL: cancellation was ignored");
}
catch (OperationCanceledException)
{
    Check(published.Requests.Count == 1, "request cancellation reaches the publisher");
}
Console.WriteLine($"Checks passed: {passed}");

sealed class RecordingCalculationRepository : ICalculationRepository
{
    public List<CalculationRecord> Records { get; } = [];
    public Task SaveAsync(CalculationRecord calculation, CancellationToken cancellationToken = default)
    {
        Records.Add(calculation);
        return Task.CompletedTask;
    }

    public Task<CalculationRecord?> FindAsync(Guid requestId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Records.SingleOrDefault(record => record.RequestId == requestId));
}

sealed class RecordingCalculationPublisher : ICalculationPublisher
{
    public List<CalculationRequest> Requests { get; } = [];
    public Exception? Failure { get; init; }

    public Task PublishAsync(CalculationRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Failure is not null) return Task.FromException(Failure);
        Requests.Add(request);
        return Task.CompletedTask;
    }
}
