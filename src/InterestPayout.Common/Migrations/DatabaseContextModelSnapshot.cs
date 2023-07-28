﻿// <auto-generated />
using System;
using InterestPayout.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace InterestPayout.Common.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    partial class DatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("interest_payout")
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.13")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.HasSequence("id_generator_asset_interests")
                .StartsAt(110000000L);

            modelBuilder.HasSequence("id_generator_payout_schedules")
                .StartsAt(100000000L);

            modelBuilder.Entity("InterestPayout.Common.Persistence.ReadModels.AssetInterests.AssetInterestEntity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasDefaultValueSql("nextval('interest_payout.id_generator_asset_interests')");

                    b.Property<string>("AssetId")
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("InterestRate")
                        .HasColumnType("numeric");

                    b.Property<DateTimeOffset>("ValidUntil")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("Version")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("asset_interests");
                });

            modelBuilder.Entity("InterestPayout.Common.Persistence.ReadModels.PayoutSchedules.PayoutScheduleEntity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasDefaultValueSql("nextval('interest_payout.id_generator_payout_schedules')");

                    b.Property<string>("AssetId")
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("CronSchedule")
                        .HasColumnType("text");

                    b.Property<string>("PayoutAssetId")
                        .HasColumnType("text");

                    b.Property<int>("Sequence")
                        .HasColumnType("integer");

                    b.Property<bool>("ShouldNotifyUser")
                        .HasColumnType("boolean");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id");

                    b.HasIndex("AssetId")
                        .IsUnique()
                        .HasDatabaseName("ix_payout_schedule_asset_id_uq");

                    b.ToTable("payout_schedules");
                });

            modelBuilder.Entity("Swisschain.Extensions.Idempotency.EfCore.IdGeneratorEntity", b =>
                {
                    b.Property<string>("IdempotencyId")
                        .HasColumnType("text");

                    b.Property<long>("Value")
                        .HasColumnType("bigint");

                    b.HasKey("IdempotencyId");

                    b.ToTable("id_generator");
                });

            modelBuilder.Entity("Swisschain.Extensions.Idempotency.EfCore.OutboxEntity", b =>
                {
                    b.Property<string>("IdempotencyId")
                        .HasColumnType("text");

                    b.Property<string>("Commands")
                        .HasColumnType("text");

                    b.Property<string>("Events")
                        .HasColumnType("text");

                    b.Property<bool>("IsDispatched")
                        .HasColumnType("boolean");

                    b.Property<string>("Response")
                        .HasColumnType("text");

                    b.HasKey("IdempotencyId");

                    b.ToTable("outbox");
                });
#pragma warning restore 612, 618
        }
    }
}
