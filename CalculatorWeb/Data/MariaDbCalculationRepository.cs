using CalculatorWeb.Models;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace CalculatorWeb.Data;

public sealed class MariaDbCalculationRepository(CalculatorDbContext database) : ICalculationRepository
{
    public async Task SaveAsync(CalculationRecord calculation, CancellationToken cancellationToken = default)
    {
        try
        {
            database.DataInputVariants.Add(new DataInputVariant
            {
                Operand_1 = calculation.FirstOperand,
                Operand_2 = calculation.SecondOperand,
                Type_operation = calculation.Operation,
                Result = calculation.Result,
            });
            if (await database.SaveChangesAsync(cancellationToken) != 1)
                throw new CalculationStorageException("The calculation was not inserted.");
        }
        catch (Exception exception) when (exception is DbUpdateException or MySqlException or TimeoutException)
        {
            throw new CalculationStorageException("Could not save the calculation to MariaDB.", exception);
        }
    }
}
