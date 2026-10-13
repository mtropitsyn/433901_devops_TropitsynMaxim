using CalculatorWeb.Data;
using CalculatorWeb.Models;
using CalculatorWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace CalculatorWeb.Controllers;

public sealed class CalculatorController(
    CalculatorService calculator,
    ICalculationRepository calculations,
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
            var result = calculator.Calculate(first, second, model.Operation);
            await calculations.SaveAsync(new CalculationRecord(first, second, model.Operation, result), cancellationToken);
            model.Result = CalculatorService.Format(result);
            model.Expression = $"{CalculatorService.Format(first)} {CalculatorService.Symbol(model.Operation)} {CalculatorService.Format(second)}";
        }
        catch (DivideByZeroException)
        {
            ModelState.AddModelError(nameof(model.SecondNumber), "На ноль делить нельзя. Введите другое число.");
        }
        catch (OverflowException)
        {
            ModelState.AddModelError("", "Результат выходит за допустимый диапазон decimal. Используйте меньшие числа.");
        }
        catch (CalculationStorageException exception)
        {
            logger.LogError(exception, "Could not save a calculation to MariaDB.");
            ModelState.AddModelError("", "Не удалось сохранить расчёт в базе данных. Попробуйте ещё раз позже.");
        }

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
