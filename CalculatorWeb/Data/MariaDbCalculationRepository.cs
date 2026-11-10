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
            if (calculation.RequestId is Guid requestId &&
                await database.DataInputVariants.AnyAsync(row => row.RequestId == requestId, cancellationToken))
                return;

            database.DataInputVariants.Add(new DataInputVariant
            {
                Operand_1 = calculation.FirstOperand,
                Operand_2 = calculation.SecondOperand,
                Type_operation = calculation.Operation,
                Result = calculation.Result,
                RequestId = calculation.RequestId,
            });
            if (await database.SaveChangesAsync(cancellationToken) != 1)
                throw new CalculationStorageException("The calculation was not inserted.");
        }
        catch (Exception exception) when (exception is DbUpdateException or MySqlException or TimeoutException)
        {
            throw new CalculationStorageException("Could not save the calculation to MariaDB.", exception);
        }
    }

    public Task<CalculationRecord?> FindAsync(Guid requestId, CancellationToken cancellationToken = default) =>
        database.DataInputVariants.AsNoTracking()
            .Where(row => row.RequestId == requestId)
            .Select(row => new CalculationRecord(row.Operand_1, row.Operand_2, row.Type_operation, row.Result, row.RequestId))
            .SingleOrDefaultAsync(cancellationToken);
}
