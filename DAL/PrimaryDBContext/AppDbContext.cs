using System;
using System.Collections.Generic;
using IMS.Enums;
using Microsoft.EntityFrameworkCore;

namespace IMS.DAL.PrimaryDBContext;

public partial class AppDbContext : DbContext
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;
    public AppDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }
  

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    public virtual DbSet<AdminCategory> AdminCategories { get; set; }

    public virtual DbSet<AdminExpenseType> AdminExpenseTypes { get; set; }

    public virtual DbSet<AdminFeature> AdminFeatures { get; set; }

    public virtual DbSet<AdminLabel> AdminLabels { get; set; }

    public virtual DbSet<AdminMeasuringUnit> AdminMeasuringUnits { get; set; }

    public virtual DbSet<AdminMeasuringUnitType> AdminMeasuringUnitTypes { get; set; }

    public virtual DbSet<AdminRole> AdminRoles { get; set; }

    public virtual DbSet<AdminSize> AdminSizes { get; set; }

    public virtual DbSet<AdminSupplier> AdminSuppliers { get; set; }

    public virtual DbSet<AdminSystemKey> AdminSystemKeys { get; set; }

    public virtual DbSet<BillPayment> BillPayments { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Expense> Expenses { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ProductRange> ProductRanges { get; set; }

    public virtual DbSet<PurchaseOrder> PurchaseOrders { get; set; }

    public virtual DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }

    public virtual DbSet<RoleFeatureAccessRight> RoleFeatureAccessRights { get; set; }

    public virtual DbSet<Sale> Sales { get; set; }

    public virtual DbSet<SaleDetail> SaleDetails { get; set; }

    public virtual DbSet<StockMaster> StockMasters { get; set; }

    public virtual DbSet<StockTransaction> StockTransactions { get; set; }

    public virtual DbSet<TransactionStatus> TransactionStatuses { get; set; }

    public virtual DbSet<User> Users { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(_connectionString);    
    }

        

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Latin1_General_CI_AS");

        modelBuilder.Entity<AdminCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId);

            entity.ToTable("AdminCategory");

            entity.HasIndex(e => e.CategoryName, "IX_AdminCategory").IsUnique();

            entity.Property(e => e.CategoryDescription).HasMaxLength(100);
            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<AdminExpenseType>(entity =>
        {
            entity.HasKey(e => e.ExpenseTypeId);

            entity.HasIndex(e => e.ExpenseTypeName, "IX_AdminExpenseTypes").IsUnique();

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.ExpenseTypeDescription).HasMaxLength(200);
            entity.Property(e => e.ExpenseTypeName).HasMaxLength(100);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<AdminFeature>(entity =>
        {
            entity.HasKey(e => e.FeatureId);

            entity.HasIndex(e => e.FeatureName, "IX_AdminFeatures").IsUnique();

            entity.Property(e => e.FeatureId).ValueGeneratedNever();
            entity.Property(e => e.FeatureDescription).HasMaxLength(250);
            entity.Property(e => e.FeatureLabel).HasMaxLength(250);
            entity.Property(e => e.FeatureName).HasMaxLength(200);
            entity.Property(e => e.FeatureUielementId)
                .HasMaxLength(250)
                .HasColumnName("FeatureUIElementId");
            entity.Property(e => e.ParentFeatureIdFk).HasColumnName("ParentFeatureId_FK");
        });

        modelBuilder.Entity<AdminLabel>(entity =>
        {
            entity.HasKey(e => e.LabelId);

            entity.HasIndex(e => e.LabelName, "IX_AdminLabels").IsUnique();

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.IsEnabled).HasDefaultValue(false);
            entity.Property(e => e.LabelDescription).HasMaxLength(500);
            entity.Property(e => e.LabelName).HasMaxLength(200);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<AdminMeasuringUnit>(entity =>
        {
            entity.HasKey(e => e.MeasuringUnitId).HasName("PK_AdminMeasuringUnit");

            entity.HasIndex(e => e.MeasuringUnitName, "IX_AdminMeasuringUnits").IsUnique();

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.MeasuringUnitAbbreviation).HasMaxLength(10);
            entity.Property(e => e.MeasuringUnitDescription).HasMaxLength(200);
            entity.Property(e => e.MeasuringUnitName).HasMaxLength(100);
            entity.Property(e => e.MeasuringUnitTypeIdFk).HasColumnName("MeasuringUnitTypeId_FK");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.AdminMeasuringUnits)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AdminMeasuringUnit_Users");

            entity.HasOne(d => d.MeasuringUnitTypeIdFkNavigation).WithMany(p => p.AdminMeasuringUnits)
                .HasForeignKey(d => d.MeasuringUnitTypeIdFk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AdminMeasuringUnit_AdminMeasuringUnitTypes");
        });

        modelBuilder.Entity<AdminMeasuringUnitType>(entity =>
        {
            entity.HasKey(e => e.MeasuringUnitTypeId);

            entity.HasIndex(e => e.MeasuringUnitTypeName, "IX_AdminMeasuringUnitTypes").IsUnique();

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.MeasuringUnitTypeDescription).HasMaxLength(200);
            entity.Property(e => e.MeasuringUnitTypeName).HasMaxLength(100);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<AdminRole>(entity =>
        {
            entity.HasKey(e => e.RoleId);

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.RoleDescription).HasMaxLength(200);
            entity.Property(e => e.RoleName).HasMaxLength(100);
        });

        modelBuilder.Entity<AdminSize>(entity =>
        {
            entity.HasKey(e => e.SizeId);

            entity.HasIndex(e => e.SizeName, "IX_AdminSizes").IsUnique();

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.SizeDescription).HasMaxLength(500);
            entity.Property(e => e.SizeName).HasMaxLength(200);
        });

        modelBuilder.Entity<AdminSupplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId);

            entity.HasIndex(e => e.SupplierName, "IX_AdminSuppliers").IsUnique();

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.SupplierAddress).HasMaxLength(500);
            entity.Property(e => e.SupplierDescription).HasMaxLength(200);
            entity.Property(e => e.SupplierEmail).HasMaxLength(50);
            entity.Property(e => e.SupplierName).HasMaxLength(100);
            entity.Property(e => e.SupplierNtn)
                .HasMaxLength(50)
                .HasColumnName("SupplierNTN");
            entity.Property(e => e.SupplierPhoneNumber).HasMaxLength(50);
        });

        modelBuilder.Entity<AdminSystemKey>(entity =>
        {
            entity.HasKey(e => e.Key);

            entity.Property(e => e.Key)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Value).HasMaxLength(100);
        });

        modelBuilder.Entity<BillPayment>(entity =>
        {
            entity.HasKey(e => e.PaymentId);

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.PaymentAmount).HasColumnType("numeric(18, 3)");
            entity.Property(e => e.PaymentDate).HasColumnType("datetime");
            entity.Property(e => e.SupplierIdFk).HasColumnName("SupplierId_FK");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.CustomerAddress).HasMaxLength(1024);
            entity.Property(e => e.CustomerContactNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CustomerEmail).HasMaxLength(250);
            entity.Property(e => e.CustomerEmailCc)
                .HasMaxLength(250)
                .HasColumnName("CustomerEmailCC");
            entity.Property(e => e.CustomerName).HasMaxLength(200);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.Cnic)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("CNIC");
            entity.Property(e => e.CreatedByUserIdFk).HasColumnName("CreatedByUserId_FK");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.EmailAddress)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.Gender)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.HusbandFatherName).HasMaxLength(100);
            entity.Property(e => e.JoiningDate).HasColumnType("datetime");
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.MaritalStatus)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ModifiedByUserIdFk).HasColumnName("ModifiedByUserId_FK");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.Property(e => e.Amount).HasColumnType("numeric(18, 3)");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.ExpenseDate).HasColumnType("datetime");
            entity.Property(e => e.ExpenseDetail).HasMaxLength(200);
            entity.Property(e => e.ExpenseTypeIdFk).HasColumnName("ExpenseTypeId_FK");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.PaymentAmount).HasColumnType("numeric(18, 3)");
            entity.Property(e => e.PaymentDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => new { e.ProductName, e.ProductCode }, "IX_Products").IsUnique();

            entity.Property(e => e.CategoryIdFk).HasColumnName("CategoryId_FK");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.LabelIdFk).HasColumnName("LabelId_FK");
            entity.Property(e => e.MeasuringUnitTypeIdFk).HasColumnName("MeasuringUnitTypeId_FK");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.ProductCode).HasMaxLength(50);
            entity.Property(e => e.ProductDescription).HasMaxLength(500);
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.SizeIdFk).HasColumnName("SizeId_FK");
            entity.Property(e => e.SupplierIdFk).HasColumnName("SupplierId_FK");
            entity.Property(e => e.UnitPrice).HasColumnType("numeric(18, 3)");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.ImageContent).HasColumnType("image");
            entity.Property(e => e.ImageId)
                .ValueGeneratedOnAdd()
                .HasColumnName("ImageID");
            entity.Property(e => e.ImageName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ProductIdFk).HasColumnName("ProductId_FK");
        });

        modelBuilder.Entity<ProductRange>(entity =>
        {
            entity.ToTable("ProductRange");

            entity.Property(e => e.MeasuringUnitIdFk).HasColumnName("MeasuringUnitId_FK");
            entity.Property(e => e.ProductIdFk).HasColumnName("ProductId_FK");
            entity.Property(e => e.RangeFrom).HasColumnType("numeric(18, 3)");
            entity.Property(e => e.RangeTo).HasColumnType("numeric(18, 3)");
            entity.Property(e => e.UnitPrice).HasColumnType("numeric(18, 3)");
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.PurchaseOrderId).HasName("PK_PurchaseOrder");

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.DiscountAmount).HasColumnType("numeric(18, 3)");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.PurchaseOrderDate).HasColumnType("datetime");
            entity.Property(e => e.SupplierIdFk).HasColumnName("SupplierId_FK");
            entity.Property(e => e.TotalAmount).HasColumnType("numeric(18, 3)");
            entity.Property(e => e.TotalDueAmount).HasColumnType("numeric(18, 3)");
            entity.Property(e => e.TotalReceivedAmount).HasColumnType("numeric(18, 3)");
        });

        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.Property(e => e.LineDiscountAmount).HasColumnType("numeric(18, 3)");
            entity.Property(e => e.PayableAmount).HasColumnType("numeric(18, 3)");
            entity.Property(e => e.PrductIdFk).HasColumnName("PrductId_FK");
            entity.Property(e => e.ProductRangeIdFk).HasColumnName("ProductRangeId_FK");
            entity.Property(e => e.PurchaseOrderIdFk).HasColumnName("PurchaseOrderId_FK");
            entity.Property(e => e.PurchasePrice).HasColumnType("numeric(18, 3)");
            entity.Property(e => e.UnitPrice).HasColumnType("numeric(18, 3)");
        });

        modelBuilder.Entity<RoleFeatureAccessRight>(entity =>
        {
            entity.Property(e => e.FeatureIdFk).HasColumnName("FeatureId_FK");
            entity.Property(e => e.UserIdFk).HasColumnName("UserId_FK");
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.SaleId).HasName("PK_Sale");

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.CustomerIdFk).HasColumnName("CustomerId_FK");
            entity.Property(e => e.DiscountAmount).HasColumnType("numeric(18, 3)");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.SaleDate).HasColumnType("datetime");
            entity.Property(e => e.TotalAmount).HasColumnType("numeric(18, 3)");
            entity.Property(e => e.TotalDueAmount).HasColumnType("numeric(18, 3)");
            entity.Property(e => e.TotalReceivedAmount).HasColumnType("numeric(18, 3)");
        });

        modelBuilder.Entity<SaleDetail>(entity =>
        {
            entity.Property(e => e.LineDiscountAmount).HasColumnType("numeric(18, 3)");
            entity.Property(e => e.PayableAmount).HasColumnType("numeric(18, 3)");
            entity.Property(e => e.PrductIdFk).HasColumnName("PrductId_FK");
            entity.Property(e => e.ProductRangeIdFk).HasColumnName("ProductRangeId_FK");
            entity.Property(e => e.SaleIdFk).HasColumnName("SaleId_FK");
            entity.Property(e => e.SalePrice).HasColumnType("numeric(18, 3)");
            entity.Property(e => e.UnitPrice).HasColumnType("numeric(18, 3)");
        });

        modelBuilder.Entity<StockMaster>(entity =>
        {
            entity.ToTable("StockMaster");

            entity.Property(e => e.AvailableQuantity).HasColumnType("numeric(18, 3)");
            entity.Property(e => e.Comment)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");
            entity.Property(e => e.ProductIdFk).HasColumnName("ProductId_FK");
            entity.Property(e => e.TotalQuantity).HasColumnType("numeric(18, 3)");
            entity.Property(e => e.UploadedDate).HasColumnType("datetime");
            entity.Property(e => e.UsedQuantity).HasColumnType("numeric(18, 3)");
        });

        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.StockMasterIdFk).HasColumnName("StockMasterId_FK");
            entity.Property(e => e.StockQuantity).HasColumnType("numeric(18, 4)");
            entity.Property(e => e.StockTransactionId).ValueGeneratedOnAdd();
            entity.Property(e => e.TransactionDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<TransactionStatus>(entity =>
        {
            entity.HasKey(e => e.StockTransactionStatusId);

            entity.Property(e => e.StockTransactionStatusId).ValueGeneratedNever();
            entity.Property(e => e.TransactionStatusName).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.UserName, "IX_Users").IsUnique();

            entity.Property(e => e.UserName).HasMaxLength(100);
            entity.Property(e => e.UserPassword).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
