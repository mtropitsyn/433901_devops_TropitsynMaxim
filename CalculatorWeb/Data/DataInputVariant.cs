namespace CalculatorWeb.Data;

public sealed class DataInputVariant
{
    public int ID_DataInputVariant { get; set; }
    public decimal Operand_1 { get; set; }
    public decimal Operand_2 { get; set; }
    public string Type_operation { get; set; } = "";
    public decimal Result { get; set; }
}
