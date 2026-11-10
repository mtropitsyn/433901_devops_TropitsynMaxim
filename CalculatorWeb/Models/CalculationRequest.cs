namespace CalculatorWeb.Models;

public sealed record CalculationRequest(Guid RequestId, decimal FirstOperand, decimal SecondOperand, string Operation);
