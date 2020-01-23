using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.Edo.Data.CurrencyExchange;
using HappyTravel.Edo.Data.Customers;
using HappyTravel.Edo.Data.Infrastructure;
using HappyTravel.Edo.Data.Locations;
using HappyTravel.Edo.Data.Management;
using HappyTravel.Edo.Data.Markup;
using HappyTravel.Edo.Data.Numeration;
using HappyTravel.Edo.Data.PaymentLinks;
using HappyTravel.Edo.Data.Payments;
using HappyTravel.Edo.Data.Suppliers;
using HappyTravel.EdoContracts.GeoData.Enums;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Data
{
    public class EdoContext : DbContext
    {
        public EdoContext(DbContextOptions<EdoContext> options) : base(options)
        { }


        private DbSet<ItnNumerator> ItnNumerators { get; set; }

        public DbSet<Country> Countries { get; set; }
        public DbSet<Company> Companies { get; set; }
        public virtual DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerCompanyRelation> CustomerCompanyRelations { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<Booking.Booking> Bookings { get; set; }

        public DbSet<UserInvitation> UserInvitations { get; set; }

        public virtual DbSet<PaymentAccount> PaymentAccounts { get; set; }

        public DbSet<Administrator> Administrators { get; set; }

        public DbSet<ManagementAuditLogEntry> ManagementAuditLog { get; set; }
        public DbSet<CreditCard> CreditCards { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<AccountBalanceAuditLogEntry> AccountBalanceAuditLogs { get; set; }
        public DbSet<CreditCardAuditLogEntry> CreditCardAuditLogs { get; set; }

        public virtual DbSet<MarkupPolicy> MarkupPolicies { get; set; }

        public DbSet<Branch> Branches { get; set; }

        public DbSet<CurrencyRate> CurrencyRates { get; set; }

        public DbSet<AppliedMarkup> MarkupLog { get; set; }

        public DbSet<SupplierOrder> SupplierOrders { get; set; }

        public DbSet<ServiceAccount> ServiceAccounts { get; set; }

        public virtual DbSet<PaymentLink> PaymentLinks { get; set; }

        public DbSet<BookingAuditLogEntry> BookingAuditLog { get; set; }

        [DbFunction("jsonb_to_string")]
        public static string JsonbToString(string target) => throw new Exception();


        public Task<long> GetNextItineraryNumber() => ExecuteScalarCommand<long>($"SELECT nextval('{ItnSequence}')");


        public Task<int> GenerateNextItnMember(string itn)
        {
            var entityInfo = this.GetEntityInfo<ItnNumerator>();
            var currentNumberColumn = entityInfo.PropertyMapping[nameof(ItnNumerator.CurrentNumber)];
            var itnNumberColumn = entityInfo.PropertyMapping[nameof(ItnNumerator.ItineraryNumber)];

            return ItnNumerators
                .FromSql(
                    $"UPDATE {entityInfo.Schema}.\"{entityInfo.Table}\" SET \"{currentNumberColumn}\" = \"{currentNumberColumn}\" + 1 WHERE \"{itnNumberColumn}\" = '{itn}' RETURNING *;",
                    itn)
                .Select(c => c.CurrentNumber)
                .SingleAsync();
        }


        public Task RegisterItn(string itn)
        {
            ItnNumerators.Add(new ItnNumerator
            {
                ItineraryNumber = itn,
                CurrentNumber = 0
            });
            return SaveChangesAsync();
        }


        public async Task<bool> TryAddEntityLock(string lockId, string lockerInfo, string token)
        {
            var entityInfo = this.GetEntityInfo<EntityLock>();
            var lockIdColumn = entityInfo.PropertyMapping[nameof(EntityLock.EntityDescriptor)];
            var lockerInfoColumn = entityInfo.PropertyMapping[nameof(EntityLock.LockerInfo)];
            var tokenColumn = entityInfo.PropertyMapping[nameof(EntityLock.Token)];

            var sql = "WITH inserted AS " +
                $"(INSERT INTO {entityInfo.Schema}.\"{entityInfo.Table}\" (\"{lockIdColumn}\", \"{lockerInfoColumn}\", \"{tokenColumn}\") " +
                $"VALUES ('{lockId}', '{lockerInfo}', '{token}') ON CONFLICT (\"{lockIdColumn}\") DO NOTHING  RETURNING \"{tokenColumn}\") " +
                $"SELECT \"{tokenColumn}\" FROM inserted " +
                $"UNION SELECT \"{tokenColumn}\" FROM public.\"{entityInfo.Table}\" " +
                $"WHERE \"{lockIdColumn}\" = '{lockId}';";

            var currentLockToken = await ExecuteScalarCommand<string>(sql);
            return currentLockToken == token;
        }


        public Task RemoveEntityLock(string lockId)
        {
            var entityMapping = this.GetEntityInfo<EntityLock>();
            return ExecuteNonQueryCommand(
                $"DELETE FROM {entityMapping.Schema}.\"{entityMapping.Table}\" where \"{entityMapping.PropertyMapping[nameof(EntityLock.EntityDescriptor)]}\" = '{lockId}';");
        }


        private async Task<T> ExecuteScalarCommand<T>(string commandText)
        {
            using (var command = CreateCommand(commandText))
            {
                return (T) await command.ExecuteScalarAsync();
            }
        }


        private async Task ExecuteNonQueryCommand(string commandText)
        {
            using (var command = CreateCommand(commandText))
            {
                await command.ExecuteNonQueryAsync();
            }
        }


        private DbCommand CreateCommand(string commandText)
        {
            var command = Database.GetDbConnection().CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = commandText;

            if (command.Connection.State == ConnectionState.Closed)
                command.Connection.Open();

            return command;
        }


        public IQueryable<Location> SearchLocations(string query, int take)
        {
            var sb = new StringBuilder();
            foreach (int locationType in Enum.GetValues(typeof(LocationTypes)))
            {
                sb.Append(sb.Length == 0 ? "SELECT * FROM search_locations({0}," : "UNION SELECT * FROM search_locations({0},");

                sb.Append(locationType);
                sb.Append(", {1}) ");
            }

            return Locations.FromSql(sb.ToString(), query, take);
        }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.HasPostgresExtension("postgis")
                .HasPostgresExtension("uuid-ossp");

            builder.HasSequence<long>(ItnSequence)
                .StartsAt(1)
                .IncrementsBy(1);

            BuildLocation(builder);
            BuildCountry(builder);
            BuildRegion(builder);
            BuildCustomer(builder);
            BuildCompany(builder);
            BuildCustomerCompanyRelation(builder);
            BuildBooking(builder);
            BuildCard(builder);
            BuildPayment(builder);

            BuildItnNumerator(builder);
            BuildInvitations(builder);
            BuildAdministrators(builder);
            BuildPaymentAccounts(builder);
            BuildAuditEventLog(builder);
            BuildAccountAuditEventLog(builder);
            BuildCreditCardAuditEventLog(builder);
            BuildEntityLocks(builder);
            BuildMarkupPolicies(builder);
            BuildCompanyBranches(builder);
            BuildCurrencyRates(builder);
            BuildSupplierOrders(builder);
            BuildMarkupLogs(builder);
            BuildPaymentLinks(builder);
            BuildServiceAccounts(builder);
            BuildBookingAuditLog(builder);
            
            DataSeeder.AddData(builder);
        }


        private void BuildPaymentLinks(ModelBuilder builder)
        {
            builder.Entity<PaymentLink>(link =>
            {
                link.HasKey(l => l.Code);
                link.Property(l => l.Currency).IsRequired();
                link.Property(l => l.ServiceType).IsRequired();
                link.Property(l => l.Amount).IsRequired();
                link.Property(l => l.Created).IsRequired();
                link.Property(l => l.LastPaymentResponse).HasColumnType("jsonb");
                link.Property(l => l.ReferenceCode).IsRequired();
                link.HasIndex(l => l.ReferenceCode);
            });
        }


        private void BuildMarkupLogs(ModelBuilder builder)
        {
            builder.Entity<AppliedMarkup>(appliedMarkup =>
            {
                appliedMarkup.HasKey(m => m.Id);
                appliedMarkup.HasIndex(m => m.ReferenceCode);
                appliedMarkup.HasIndex(m => m.ServiceType);
                appliedMarkup.Property(m => m.Created).IsRequired();
                appliedMarkup.Property(m => m.ServiceType).IsRequired();
                appliedMarkup.Property(m => m.ReferenceCode).IsRequired();
                appliedMarkup.Property(m => m.Policies)
                    .IsRequired()
                    .HasColumnType("jsonb")
                    .HasConversion(list => JsonConvert.SerializeObject(list),
                        list => JsonConvert.DeserializeObject<List<MarkupPolicy>>(list));
            });
        }


        private void BuildSupplierOrders(ModelBuilder builder)
        {
            builder.Entity<SupplierOrder>(order =>
            {
                order.HasKey(o => o.Id);
                order.HasIndex(o => o.ReferenceCode);
                order.HasIndex(o => o.DataProvider);
                order.HasIndex(o => o.Type);
                order.Property(o => o.Price).IsRequired();
                order.Property(o => o.State).IsRequired();
                order.Property(o => o.ReferenceCode).IsRequired();
                order.Property(o => o.Modified).IsRequired();
                order.Property(o => o.Created).IsRequired();
            });
        }


        private void BuildCurrencyRates(ModelBuilder builder)
        {
            builder.Entity<CurrencyRate>(rate =>
            {
                rate.HasKey(r => new {r.SourceCurrency, r.TargetCurrency, r.ValidTo});
                rate.Property(r => r.Rate).IsRequired();
                rate.Property(r => r.SourceCurrency).IsRequired();
                rate.Property(r => r.TargetCurrency).IsRequired();
                rate.Property(r => r.ValidFrom).IsRequired();
                rate.Property(r => r.ValidTo).IsRequired();
            });
        }


        private void BuildCompanyBranches(ModelBuilder builder)
        {
            builder.Entity<Branch>(branch =>
            {
                branch.HasKey(b => b.Id);
                branch.Property(b => b.CompanyId).IsRequired();
                branch.Property(b => b.Modified).IsRequired();
                branch.Property(b => b.Created).IsRequired();
                branch.Property(b => b.Title).IsRequired();
                branch.HasIndex(b => b.CompanyId);
            });
        }


        private void BuildMarkupPolicies(ModelBuilder builder)
        {
            builder.Entity<MarkupPolicy>(policy =>
            {
                policy.HasKey(l => l.Id);
                policy.Property(l => l.Order).IsRequired();
                policy.Property(l => l.ScopeType).IsRequired();
                policy.Property(l => l.Target).IsRequired();

                policy.Property(l => l.Created).IsRequired();
                policy.Property(l => l.Modified).IsRequired();
                policy.Property(l => l.TemplateId).IsRequired();

                policy.Property(l => l.TemplateSettings).HasColumnType("jsonb").IsRequired();
                policy.Property(l => l.TemplateSettings).HasConversion(val => JsonConvert.SerializeObject(val),
                    s => JsonConvert.DeserializeObject<Dictionary<string, decimal>>(s));

                policy.HasIndex(b => b.ScopeType);
                policy.HasIndex(b => b.Target);
                policy.HasIndex(b => b.CompanyId);
                policy.HasIndex(b => b.CustomerId);
                policy.HasIndex(b => b.BranchId);
            });
        }


        private void BuildEntityLocks(ModelBuilder builder)
        {
            builder.Entity<EntityLock>(entityLock =>
            {
                entityLock.HasKey(l => l.EntityDescriptor);
                entityLock.Property(l => l.Token).IsRequired();
                entityLock.Property(l => l.LockerInfo).IsRequired();
                entityLock.ToTable(nameof(EntityLock));
            });
        }


        private void BuildLocation(ModelBuilder builder)
        {
            builder.Entity<Location>(loc =>
            {
                loc.HasKey(l => l.Id);
                loc.Property(l => l.Id).HasDefaultValueSql("uuid_generate_v4()").IsRequired();
                loc.Property(l => l.Coordinates).HasColumnType("geography (point)").IsRequired();
                loc.Property(l => l.Name).HasColumnType("jsonb").IsRequired();
                loc.Property(l => l.Locality).HasColumnType("jsonb").IsRequired();
                loc.Property(l => l.Country).HasColumnType("jsonb").IsRequired();
                loc.Property(l => l.DistanceInMeters).IsRequired();
                loc.Property(l => l.Source).IsRequired();
                loc.Property(l => l.Type).IsRequired();
            });
        }


        private void BuildAuditEventLog(ModelBuilder builder)
        {
            builder.Entity<ManagementAuditLogEntry>(log =>
            {
                log.HasKey(l => l.Id);
                log.Property(l => l.Created).IsRequired();
                log.Property(l => l.Type).IsRequired();
                log.Property(l => l.AdministratorId).IsRequired();
                log.Property(l => l.EventData).IsRequired();
            });
        }


        private void BuildInvitations(ModelBuilder builder)
        {
            builder.Entity<UserInvitation>(inv =>
            {
                inv.HasKey(i => i.CodeHash);
                inv.Property(i => i.Created).IsRequired();
                inv.Property(i => i.Data).IsRequired();
                inv.Property(i => i.Email).IsRequired();
                inv.Property(i => i.IsAccepted).HasDefaultValue(false);
                inv.Property(i => i.InvitationType).IsRequired();
            });
        }


        private void BuildAdministrators(ModelBuilder builder)
        {
            builder.Entity<Administrator>(adm =>
            {
                adm.HasKey(a => a.Id);
                adm.Property(a => a.LastName).IsRequired();
                adm.Property(a => a.FirstName).IsRequired();
                adm.Property(a => a.Position).IsRequired();
                adm.Property(a => a.Email).IsRequired();
                adm.HasIndex(a => a.IdentityHash);
            });
        }


        private void BuildPaymentAccounts(ModelBuilder builder)
        {
            builder.Entity<PaymentAccount>(acc =>
            {
                acc.HasKey(a => a.Id);
                acc.Property(a => a.Currency).IsRequired();
                acc.Property(a => a.CompanyId).IsRequired();
            });
        }


        private void BuildItnNumerator(ModelBuilder builder)
        {
            builder.Entity<ItnNumerator>()
                .HasKey(n => n.ItineraryNumber);

            builder.Entity<ItnNumerator>().ToTable(nameof(ItnNumerator));
        }


        private static void BuildCountry(ModelBuilder builder)
        {
            builder.Entity<Country>()
                .HasKey(c => c.Code);
            builder.Entity<Country>()
                .Property(c => c.Code)
                .IsRequired();
            builder.Entity<Country>()
                .Property(c => c.Names)
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Entity<Country>()
                .Property(c => c.RegionId)
                .IsRequired();
        }


        private static void BuildRegion(ModelBuilder builder)
        {
            builder.Entity<Region>()
                .HasKey(c => c.Id);
            builder.Entity<Region>()
                .Property(c => c.Id)
                .IsRequired();
            builder.Entity<Region>()
                .Property(c => c.Names)
                .HasColumnType("jsonb")
                .IsRequired();
        }


        private void BuildCustomer(ModelBuilder builder)
        {
            builder.Entity<Customer>(customer =>
            {
                customer.HasKey(c => c.Id);
                customer.Property(c => c.Id).ValueGeneratedOnAdd();
                customer.Property(c => c.Email).IsRequired();
                customer.Property(c => c.Title).IsRequired();
                customer.Property(c => c.FirstName).IsRequired();
                customer.Property(c => c.LastName).IsRequired();
                customer.Property(c => c.FirstName).IsRequired();
                customer.Property(c => c.Position).IsRequired();
                customer.Property(c => c.IdentityHash).IsRequired();
                customer.Property(c => c.AppSettings).HasColumnType("jsonb");
                customer.Property(c => c.UserSettings).HasColumnType("jsonb");
            });
        }


        private void BuildCompany(ModelBuilder builder)
        {
            builder.Entity<Company>(company =>
            {
                company.HasKey(c => c.Id);
                company.Property(c => c.Id).ValueGeneratedOnAdd();
                company.Property(c => c.Address).IsRequired();
                company.Property(c => c.City).IsRequired();
                company.Property(c => c.CountryCode).IsRequired();
                company.Property(c => c.Name).IsRequired();
                company.Property(c => c.Phone).IsRequired();
                company.Property(c => c.PreferredCurrency).IsRequired();
                company.Property(c => c.PreferredPaymentMethod).IsRequired();
                company.Property(c => c.State).IsRequired();
            });
        }


        private void BuildCustomerCompanyRelation(ModelBuilder builder)
        {
            builder.Entity<CustomerCompanyRelation>(relation =>
            {
                relation.ToTable("CustomerCompanyRelations");

                relation.HasKey(r => new {r.CustomerId, r.CompanyId, r.Type});
                relation.Property(r => r.CompanyId).IsRequired();
                relation.Property(r => r.CustomerId).IsRequired();
                relation.Property(r => r.Type).IsRequired();
            });
        }


        private void BuildBooking(ModelBuilder builder)
        {
            builder.Entity<Booking.Booking>(booking =>
            {
                booking.HasKey(b => b.Id);

                booking.Property(b => b.CustomerId).IsRequired();
                booking.HasIndex(b => b.CustomerId);

                booking.Property(b => b.CompanyId).IsRequired();
                booking.HasIndex(b => b.CompanyId);

                booking.Property(b => b.ReferenceCode).IsRequired();
                booking.HasIndex(b => b.ReferenceCode);

                booking.Property(b => b.BookingDetails)
                    .HasColumnType("jsonb");

                booking.Property(b => b.ServiceDetails)
                    .HasColumnType("jsonb");

                booking.Property(b => b.Status).IsRequired();
                booking.Property(b => b.ItineraryNumber).IsRequired();
                booking.HasIndex(b => b.ItineraryNumber);

                booking.Property(b => b.MainPassengerName).IsRequired();
                booking.HasIndex(b => b.MainPassengerName);

                booking.Property(b => b.ServiceType).IsRequired();
                booking.HasIndex(b => b.ServiceType);
            });
        }


        private void BuildCard(ModelBuilder builder)
        {
            builder
                .Entity<CreditCard>(card =>
                {
                    card.HasKey(c => c.Id);
                    card.Property(c => c.HolderName).IsRequired();
                    card.Property(c => c.MaskedNumber).IsRequired();
                    card.Property(c => c.ExpirationDate).IsRequired();
                    card.Property(c => c.Token).IsRequired();
                    card.Property(c => c.OwnerId).IsRequired();
                    card.Property(c => c.OwnerType).IsRequired();
                    card.Property(c => c.ReferenceCode).IsRequired();
                });
        }


        private void BuildPayment(ModelBuilder builder)
        {
            builder
                .Entity<Payment>(payment =>
                {
                    payment.HasKey(p => p.Id);
                    payment.Property(p => p.BookingId).IsRequired();
                    payment.HasIndex(p => p.BookingId);
                    payment.Property(p => p.Data).HasColumnType("jsonb").IsRequired();
                    payment.Property(p => p.AccountNumber).IsRequired();
                    payment.Property(p => p.Amount).IsRequired();
                    payment.Property(p => p.Currency).IsRequired();
                    payment.Property(p => p.Created).IsRequired();
                    payment.Property(p => p.Status).IsRequired();
                });
        }


        private void BuildAccountAuditEventLog(ModelBuilder builder)
        {
            builder.Entity<AccountBalanceAuditLogEntry>(log =>
            {
                log.HasKey(l => l.Id);
                log.Property(l => l.Created).IsRequired();
                log.Property(l => l.Type).IsRequired();
                log.Property(l => l.AccountId).IsRequired();
                log.Property(l => l.UserType).IsRequired();
                log.Property(l => l.UserId).IsRequired();
                log.Property(l => l.Amount).IsRequired();
                log.Property(l => l.EventData).IsRequired();
            });
        }


        private void BuildCreditCardAuditEventLog(ModelBuilder builder)
        {
            builder.Entity<CreditCardAuditLogEntry>(log =>
            {
                log.HasKey(l => l.Id);
                log.Property(l => l.Created).IsRequired();
                log.Property(l => l.Type).IsRequired();
                log.Property(l => l.MaskedNumber).IsRequired();
                log.Property(l => l.UserType).IsRequired();
                log.Property(l => l.UserId).IsRequired();
                log.Property(l => l.CustomerId).IsRequired();
                log.Property(l => l.Amount).IsRequired();
                log.Property(l => l.Currency).IsRequired();
                log.Property(l => l.EventData).IsRequired();
            });
        }


        private void BuildServiceAccounts(ModelBuilder builder)
        {
            builder.Entity<ServiceAccount>(account =>
            {
                account.HasKey(a => a.Id);
                account.Property(a => a.ClientId).IsRequired();
            });
        }
        
        
        private void BuildBookingAuditLog(ModelBuilder builder)
        {
            builder.Entity<BookingAuditLogEntry>(br =>
            {
                builder.Entity<BookingAuditLogEntry>().ToTable("BookingAuditLog");
                br.HasKey(b => b.Id);
                br.Property(b => b.Id).ValueGeneratedOnAdd();
                br.HasOne<Booking.Booking>().WithMany().HasForeignKey(b => b.BookingId)
                    .IsRequired();
                br.HasOne<Customer>().WithMany().HasForeignKey(c => c.CustomerId).IsRequired();
                br.Property(b => b.CreatedAt)
                    .HasDefaultValueSql("NOW()")
                    .ValueGeneratedOnAdd();
                br.Property(b => b.PreviousBookingDetails)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        value => JsonConvert.SerializeObject(value),
                        value => JsonConvert.DeserializeObject<BookingDetails>(value))
                    .IsRequired();
                br.Property(b => b.BookingDetails)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        value => JsonConvert.SerializeObject(value),
                        value => JsonConvert.DeserializeObject<BookingDetails>(value))
                    .IsRequired();
            });
        }
        
        private const string ItnSequence = "itn_seq";
    }
}