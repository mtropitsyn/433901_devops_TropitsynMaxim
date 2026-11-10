using CalculatorWeb.Data;
using CalculatorWeb.Models;
using CalculatorWeb.Services;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;

namespace CalculatorWeb.Controllers;

public sealed class CalculatorController(
    CalculatorService calculator,
    ICalculationRepository calculations,
    ICalculationPublisher publisher,
    ILogger<CalculatorController> logger) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(new CalculatorViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CalculatorViewModel model, CancellationToken cancellationToken = default)
    {
        var firstValid = CalculatorService.TryParseNumber(model.FirstNumber, out var first);
        var secondValid = CalculatorService.TryParseNumber(model.SecondNumber, out var second);

        if (!string.IsNullOrWhiteSpace(model.FirstNumber) && !firstValid)
            ModelState.AddModelError(nameof(model.FirstNumber), "Введите корректное число, например 12,5 или -3.");
        if (!string.IsNullOrWhiteSpace(model.SecondNumber) && !secondValid)
            ModelState.AddModelError(nameof(model.SecondNumber), "Введите корректное число, например 12,5 или -3.");
        if (!CalculatorService.IsOperationSupported(model.Operation))
            ModelState.AddModelError(nameof(model.Operation), "Выберите одну из четырёх операций.");

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            // Validate the range before enqueueing. The consumer calculates the value it stores.
            _ = calculator.Calculate(first, second, model.Operation);
            var request = new CalculationRequest(Guid.NewGuid(), first, second, model.Operation);
            await publisher.PublishAsync(request, cancellationToken);
            return RedirectToAction(nameof(Result), new { id = request.RequestId });
        }
        catch (DivideByZeroException)
        {
            ModelState.AddModelError(nameof(model.SecondNumber), "На ноль делить нельзя. Введите другое число.");
        }
        catch (OverflowException)
        {
            ModelState.AddModelError("", "Результат выходит за допустимый диапазон decimal. Используйте меньшие числа.");
        }
        catch (KafkaException exception)
        {
            logger.LogError(exception, "Could not publish a calculation to Kafka.");
            ModelState.AddModelError("", "Не удалось подтвердить отправку расчёта в Kafka. Проверьте подключение к брокеру.");
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        }

        return View(model);
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Result(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
            return NotFound();

        var calculation = await calculations.FindAsync(id, cancellationToken);
        if (calculation is null)
        {
            Response.StatusCode = StatusCodes.Status202Accepted;
            return View("Pending", id);
        }

        var first = CalculatorService.Format(calculation.FirstOperand);
        var second = CalculatorService.Format(calculation.SecondOperand);
        return View(nameof(Index), new CalculatorViewModel
        {
            FirstNumber = first,
            SecondNumber = second,
            Operation = calculation.Operation,
            Result = CalculatorService.Format(calculation.Result),
            Expression = $"{first} {CalculatorService.Symbol(calculation.Operation)} {second}",
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
