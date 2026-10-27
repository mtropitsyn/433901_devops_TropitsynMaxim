using Microsoft.EntityFrameworkCore;

namespace CalculatorWeb.Data;

public sealed class CalculatorDbContext(DbContextOptions<CalculatorDbContext> options) : DbContext(options)
{
    public DbSet<DataInputVariant> DataInputVariants => Set<DataInputVariant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasCharSet("utf8mb4");
        var calculation = modelBuilder.Entity<DataInputVariant>();
        calculation.ToTable("DataInputVariants");
        calculation.HasKey(row => row.ID_DataInputVariant);
        calculation.Property(row => row.ID_DataInputVariant).ValueGeneratedOnAdd();
        // Preserve the entire System.Decimal range, including 28 fractional digits.
        calculation.Property(row => row.Operand_1).HasPrecision(57, 28);
        calculation.Property(row => row.Operand_2).HasPrecision(57, 28);
        calculation.Property(row => row.Result).HasPrecision(57, 28);
        calculation.Property(row => row.Type_operation).HasMaxLength(16).IsRequired();
    }
}
