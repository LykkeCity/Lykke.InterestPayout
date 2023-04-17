using InterestPayout.Common.Persistence.ReadModels.PayoutSchedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Swisschain.Extensions.Idempotency.EfCore;

namespace InterestPayout.Common.Persistence
{
    public class DatabaseContext : DbContext, IDbContextWithOutbox, IDbContextWithIdGenerator
    {
        public static string SchemaName { get; } = "interest_payout";
        public static string MigrationHistoryTable { get; } = HistoryRepository.DefaultTableName;

        public DbSet<OutboxEntity> Outbox { get; set; }
        public DbSet<IdGeneratorEntity> IdGenerator { get; set; }
        
        public DbSet<PayoutScheduleEntity> PayoutSchedules { get; set; }

        public DatabaseContext(DbContextOptions<DatabaseContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(SchemaName);
            
            modelBuilder.BuildIdempotency(x =>
            {
                x.AddIdGenerator(IdGenerators.PayoutSchedules, 100000000);
            });

            BuildPayoutSchedules(modelBuilder);
            
            base.OnModelCreating(modelBuilder);
        }
        
        private static void BuildPayoutSchedules(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PayoutScheduleEntity>()
                .ToTable(Tables.PayoutSchedules)
                .HasKey(x => x.Id);
            
            modelBuilder.Entity<PayoutScheduleEntity>()
                .Property(x => x.Id)
                .HasDefaultValueSql($"nextval('{SchemaName}.{IdGenerators.PayoutSchedules}')");
            
            modelBuilder.Entity<PayoutScheduleEntity>(e =>
            {
                e.Property(p => p.Version)
                    .HasColumnName("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();
            });
            
            modelBuilder.Entity<PayoutScheduleEntity>()
                .HasIndex(x => x.AssetId)
                .IsUnique()
                .HasDatabaseName("ix_payout_schedule_asset_id_uq");
        }
    }
}
