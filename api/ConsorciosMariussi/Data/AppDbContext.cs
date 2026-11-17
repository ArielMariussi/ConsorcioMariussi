using ConsorciosMariussi.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ConsorciosMariussi.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Consorcio> Consorcios => Set<Consorcio>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // citext: texto que compara sem diferenciar maiúsculas de minúsculas.
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.Entity<Consorcio>(e =>
        {
            e.ToTable("Consorcios", t =>
            {
                t.HasCheckConstraint("CK_Consorcios_Administradora",
                    "\"Administradora\" <> '' AND \"Administradora\" = btrim(\"Administradora\")");
                t.HasCheckConstraint("CK_Consorcios_Grupo", "\"Grupo\" ~ '^[0-9]+$'");
                t.HasCheckConstraint("CK_Consorcios_Cota", "\"Cota\" ~ '^[0-9]+$'");
                t.HasCheckConstraint("CK_Consorcios_Credito", "\"Credito\" IS NULL OR \"Credito\" >= 0");
            });

            e.Property(c => c.Administradora).IsRequired().HasColumnType("citext");
            e.Property(c => c.Grupo).IsRequired().HasMaxLength(20);
            e.Property(c => c.Cota).IsRequired().HasMaxLength(20);

            e.Property(c => c.Credito).HasPrecision(18, 2);
            e.Property(c => c.ValorPago).HasPrecision(18, 2);
            e.Property(c => c.Comissao).HasPrecision(18, 2);
            e.Property(c => c.PrevisaoLucro).HasPrecision(18, 2);
            e.Property(c => c.FundoReserva).HasPrecision(18, 2);
            e.Property(c => c.TaxaTransferencia).HasPrecision(18, 2);
            e.Property(c => c.Lucro).HasPrecision(18, 2);

            // Administradora + grupo + cota é única (administradora sem diferenciar maiúsculas/minúsculas).
            e.HasIndex(c => new { c.Administradora, c.Grupo, c.Cota }).IsUnique();
        });
    }
}
