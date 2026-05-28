using AiTalentGenome.VacancyService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiTalentGenome.VacancyService.Infrastructure.Persistence;

public class VacancyDbContext(DbContextOptions<VacancyDbContext> options) : DbContext(options)
{
    public DbSet<Vacancy> Vacancies => Set<Vacancy>();
    public DbSet<Domain.Entities.Application> Applications => Set<Domain.Entities.Application>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Vacancy>(entity =>
        {
            entity.HasKey(v => v.Id);
            
            entity.HasIndex(v => v.HhId).IsUnique().HasFilter("\"HhId\" IS NOT NULL");

            entity.Property(v => v.Title).IsRequired().HasMaxLength(250);
            entity.Property(v => v.Description).IsRequired();

            entity.OwnsOne(v => v.Salary, sa =>
            {
                sa.Property(p => p.From).HasColumnName("SalaryFrom");
                sa.Property(p => p.To).HasColumnName("SalaryTo");
                sa.Property(p => p.Currency).HasColumnName("SalaryCurrency").HasMaxLength(10);
            });

            entity.Property(v => v.KeySkills)
                .HasColumnType("text[]");

            entity.Property(v => v.OwnerId).IsRequired();
            entity.Property(v => v.CompanyId).IsRequired();
        });

        modelBuilder.Entity<Domain.Entities.Application>(entity =>
        {
            entity.HasKey(a => a.Id);
    
            // Уникальный индекс для откликов из HH, чтобы не дублировать их
            entity.HasIndex(a => a.HhNegotiationId).IsUnique().HasFilter("\"HhNegotiationId\" IS NOT NULL");

            entity.Property(a => a.CandidateName).IsRequired().HasMaxLength(200);
            entity.Property(a => a.CandidateEmail).IsRequired().HasMaxLength(150);
    
            // Храним статус как строку
            entity.Property(a => a.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            // Связь: Одна вакансия - много откликов
            entity.HasOne(a => a.Vacancy)
                .WithMany(v => v.Applications)
                .HasForeignKey(a => a.VacancyId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // В блоке entity.HasKey(a => a.Id) для Application:
            entity.Property(a => a.CandidateSkills)
                .HasColumnType("text[]"); // Убрали HasDefaultValue

            entity.Property(a => a.CriticalMismatches)
                .HasColumnType("text[]");
        });
    }
}