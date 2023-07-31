using InterestPayout.Common.Persistence.ReadModels.AssetInterests;
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
        
        public DbSet<AssetInterestEntity> AssetInterests { get; set; }

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
                x.AddIdGenerator(IdGenerators.AssetInterests, 110000000);
            });

            BuildPayoutSchedules(modelBuilder);
            BuildAssetInterests(modelBuilder);

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

        private static void BuildAssetInterests(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AssetInterestEntity>()
                .ToTable(Tables.AssetInterests)
                .HasKey(x => x.Id);

            modelBuilder.Entity<AssetInterestEntity>()
                .Property(x => x.Id)
                .HasDefaultValueSql($"nextval('{SchemaName}.{IdGenerators.AssetInterests}')");
            
            modelBuilder.Entity<AssetInterestEntity>()
                .HasIndex(x => x.AssetId)
                .IsUnique()
                .HasDatabaseName("ix_asset_interest_asset_id_uq");
        }
    }
}
