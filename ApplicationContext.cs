using Microsoft.EntityFrameworkCore;
using CompositionMaster.Models;

namespace CompositionMaster
{
    public class ApplicationContext : DbContext
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
        }

        public DbSet<Role> Roles { get; set; }
        public DbSet<UnitOfMeasurement> UnitOfMeasurements { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<NomenclatureType> NomenclatureTypes { get; set; }
        public DbSet<Nomenclature> Nomenclatures { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Specification> Specifications { get; set; }
        public DbSet<SpecificationComponent> SpecificationComponents { get; set; }
        public DbSet<OperationCard> OperationCards { get; set; }
        public DbSet<NomenclatureChange> NomenclatureChanges { get; set; }
        public DbSet<SpecificationChange> SpecificationChanges { get; set; }
        public DbSet<SpecificationComponentChange> SpecificationComponentChanges { get; set; }
        public DbSet<OperationCardChange> OperationCardChanges { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===================== КЛЮЧИ ========================

            modelBuilder.Entity<Role>().HasKey(r => r.Identifier);
            modelBuilder.Entity<Position>().HasKey(p => p.Identifier);
            modelBuilder.Entity<User>().HasKey(u => u.Identifier);
            modelBuilder.Entity<NomenclatureType>().HasKey(nt => nt.Identifier);
            modelBuilder.Entity<UnitOfMeasurement>().HasKey(u => u.Identifier);
            modelBuilder.Entity<Nomenclature>().HasKey(n => n.Identifier);
            modelBuilder.Entity<NomenclatureChange>().HasKey(nc => nc.Identifier);
            modelBuilder.Entity<Specification>().HasKey(s => s.Identifier);
            modelBuilder.Entity<SpecificationChange>().HasKey(sc => sc.Identifier);

            modelBuilder.Entity<SpecificationComponent>()
                .HasKey(sc => new { sc.Identifier, sc.LineNumber });
            modelBuilder.Entity<OperationCard>()
                .HasKey(oc => new { oc.Identifier, oc.LineNumber });
            modelBuilder.Entity<SpecificationComponentChange>()
                .HasKey(scc => new { scc.Identifier, scc.LineNumber });
            modelBuilder.Entity<OperationCardChange>()
                .HasKey(occ => new { occ.Identifier, occ.LineNumber });

            // ===================== СВЯЗИ ========================

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany()
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===================== ТАБЛИЦЫ ========================

            modelBuilder.Entity<Role>().ToTable("role");
            modelBuilder.Entity<Position>().ToTable("position");
            
            // ВАЖНО: правильное экранирование для таблицы user
            modelBuilder.Entity<User>().ToTable("\"user\"");
            
            modelBuilder.Entity<NomenclatureType>().ToTable("nomenclature_type");
            modelBuilder.Entity<UnitOfMeasurement>().ToTable("unit_of_measurement");
            modelBuilder.Entity<Nomenclature>().ToTable("nomenclature");
            modelBuilder.Entity<NomenclatureChange>().ToTable("nomenclature_change");
            modelBuilder.Entity<Specification>().ToTable("specification");
            modelBuilder.Entity<SpecificationChange>().ToTable("specification_change");
            modelBuilder.Entity<SpecificationComponent>().ToTable("specification_component");
            modelBuilder.Entity<OperationCard>().ToTable("operation_card");
            modelBuilder.Entity<SpecificationComponentChange>().ToTable("specification_component_change");
            modelBuilder.Entity<OperationCardChange>().ToTable("operation_card_change");

            // ===================== НАСТРОЙКА АВТОИНКРЕМЕНТА ========================

            modelBuilder.Entity<User>()
                .Property(u => u.Identifier)
                .UseIdentityAlwaysColumn();

            modelBuilder.Entity<Role>()
                .Property(r => r.Identifier)
                .UseIdentityAlwaysColumn();
            
            modelBuilder.Entity<Position>()
                .Property(p => p.Identifier)
                .UseIdentityAlwaysColumn();
            
            modelBuilder.Entity<NomenclatureType>()
                .Property(nt => nt.Identifier)
                .UseIdentityAlwaysColumn();
            
            modelBuilder.Entity<UnitOfMeasurement>()
                .Property(u => u.Identifier)
                .UseIdentityAlwaysColumn();
            
            modelBuilder.Entity<Specification>()
                .Property(s => s.Identifier)
                .UseIdentityAlwaysColumn();

            // ===================== КОЛОНКИ ========================

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.Identifier).HasColumnName("identifier");
                entity.Property(u => u.FullName).HasColumnName("fullname");
                entity.Property(u => u.Login).HasColumnName("login");
                entity.Property(u => u.Password).HasColumnName("password");
                entity.Property(u => u.RoleId).HasColumnName("roleid");
            });

            // Автоматический lowercase для всех остальных сущностей
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                if (entity.ClrType == typeof(User))
                    continue;

                foreach (var property in entity.GetProperties())
                {
                    property.SetColumnName(property.Name.ToLower());
                }
            }
        }
    }
}