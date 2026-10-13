using CalculatorWeb.Models;
using MySqlConnector;

namespace CalculatorWeb.Data;

public sealed class MariaDbCalculationRepository(MySqlDataSource dataSource) : ICalculationRepository
{
    public async Task SaveAsync(CalculationRecord calculation, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO calculations (first_operand, second_operand, operation, result)
                VALUES (@first, @second, @operation, @result);
                """;
            command.Parameters.AddWithValue("@first", calculation.FirstOperand);
            command.Parameters.AddWithValue("@second", calculation.SecondOperand);
            command.Parameters.AddWithValue("@operation", calculation.Operation);
            command.Parameters.AddWithValue("@result", calculation.Result);
            if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
                throw new CalculationStorageException("The calculation was not inserted.");
        }
        catch (Exception exception) when (exception is MySqlException or TimeoutException)
        {
            throw new CalculationStorageException("Could not save the calculation to MariaDB.", exception);
        }
    }
}
