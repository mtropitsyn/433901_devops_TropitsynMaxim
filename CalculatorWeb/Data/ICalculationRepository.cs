using CalculatorWeb.Models;

namespace CalculatorWeb.Data;

public interface ICalculationRepository
{
    Task SaveAsync(CalculationRecord calculation, CancellationToken cancellationToken = default);
}
