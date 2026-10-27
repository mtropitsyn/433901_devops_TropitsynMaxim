namespace CalculatorWeb.Data;

public sealed class CalculationStorageException(string message, Exception? innerException = null)
    : Exception(message, innerException);
