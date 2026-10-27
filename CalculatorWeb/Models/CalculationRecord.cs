namespace CalculatorWeb.Models;

public sealed record CalculationRecord(
    decimal FirstOperand,
    decimal SecondOperand,
    string Operation,
    decimal Result);
