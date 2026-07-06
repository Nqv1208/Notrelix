using Notrelix.Domain.WorkManagement.Formulas;

namespace Notrelix.Infrastructure.Data.Configurations.WorkManagement;

public class FormulaDependencyConfiguration : IEntityTypeConfiguration<FormulaDependency>
{
    public void Configure(EntityTypeBuilder<FormulaDependency> builder)
    {
        builder.ToTable("formula_dependencies", DbSchemas.Work);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.FormulaFieldId).HasColumnName("formula_field_id").IsRequired();
        builder.Property(x => x.DependsOnFieldId).HasColumnName("depends_on_field_id").IsRequired();

        builder.HasIndex(x => new { x.FormulaFieldId, x.DependsOnFieldId }).IsUnique().HasDatabaseName("idx_formula_dependencies_pair");
        builder.HasIndex(x => x.DependsOnFieldId).HasDatabaseName("idx_formula_dependencies_depends_on");
    }
}
