using LeoHR.Server.Models.Entities.Base;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Extensions.Authentication;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LeoHR.Server.Shared.Data;

public class LeoHrDbContext(DbContextOptions<LeoHrDbContext> options, IHttpContextAccessor httpContextAccessor) : DbContext(options)
{
    public const string OpenPromotionIndex = "ix_promotions_employee_id_open";

    public DbSet<PromotionEntity> Promotions => Set<PromotionEntity>();
    public DbSet<EmailTemplateEntity> EmailTemplates => Set<EmailTemplateEntity>();
    public DbSet<EmailWhitelistEntity> EmailWhitelist => Set<EmailWhitelistEntity>();
    public DbSet<AppSettingsEntity> AppSettings => Set<AppSettingsEntity>();
    public DbSet<ActivityEntity> Activities => Set<ActivityEntity>();
    public DbSet<PromotionCriteriaEntity> PromotionCriteria => Set<PromotionCriteriaEntity>();
    public DbSet<EmployeeActivityEntity> EmployeeActivities => Set<EmployeeActivityEntity>();
    public DbSet<EmployeeExtensionEntity> EmployeeExtensions => Set<EmployeeExtensionEntity>();
    public DbSet<ResponsibleRoleMappingEntity> ResponsibleRoleMappings => Set<ResponsibleRoleMappingEntity>();
    public DbSet<ResponsiblePositionEntity> ResponsiblePositions => Set<ResponsiblePositionEntity>();
    public DbSet<OnboardingSettingsEntity> OnboardingSettings => Set<OnboardingSettingsEntity>();
    public DbSet<OnboardingTaskTemplateEntity> OnboardingTaskTemplates => Set<OnboardingTaskTemplateEntity>();
    public DbSet<OnboardingTaskTemplateFieldEntity> OnboardingTaskTemplateFields => Set<OnboardingTaskTemplateFieldEntity>();
    public DbSet<OnboardingTaskTemplateChecklistItemEntity> OnboardingTaskTemplateChecklistItems => Set<OnboardingTaskTemplateChecklistItemEntity>();
    public DbSet<OnboardingMilestoneEntity> OnboardingMilestones => Set<OnboardingMilestoneEntity>();
    public DbSet<OnboardingTypeEntity> OnboardingTypes => Set<OnboardingTypeEntity>();
    public DbSet<OnboardingFlowEntity> OnboardingFlows => Set<OnboardingFlowEntity>();
    public DbSet<OnboardingFlowTaskEntity> OnboardingFlowTasks => Set<OnboardingFlowTaskEntity>();
    public DbSet<OnboardingFlowTaskFieldEntity> OnboardingFlowTaskFields => Set<OnboardingFlowTaskFieldEntity>();
    public DbSet<OnboardingFlowTaskCheckedItemEntity> OnboardingFlowTaskCheckedItems => Set<OnboardingFlowTaskCheckedItemEntity>();
    public DbSet<OnboardingFlowTaskActivityEntity> OnboardingFlowTaskActivities => Set<OnboardingFlowTaskActivityEntity>();
    public DbSet<KnowledgeEvaluationEntity> KnowledgeEvaluations => Set<KnowledgeEvaluationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PromotionEntity>(e =>
        {
            e.ToTable("promotions");
            e.HasKey(x => x.Id);
            e.Property(x => x.EmployeeId).IsRequired().HasMaxLength(100);
            e.Property(x => x.PromotionHolderId).IsRequired().HasMaxLength(100);
            e.Property(x => x.FromRole).IsRequired().HasMaxLength(200);
            e.Property(x => x.ToRole).IsRequired().HasMaxLength(200);
            e.Property(x => x.HrManager).HasMaxLength(200);
            e.Property(x => x.ExceptionNote).HasMaxLength(1000);
            e.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Technology).IsRequired().HasConversion<string>().HasMaxLength(50).HasDefaultValue(Technology.Dotnet);
            e.Property(x => x.ReferenceDate).HasColumnType("timestamp with time zone");
            e.Property(x => x.EmailSentAt).HasColumnType("timestamp with time zone");
            e.Property(x => x.ReadyEmailSentAt).HasColumnType("timestamp with time zone");
            e.Property(x => x.LevelStartDate).HasColumnType("timestamp with time zone");
            e.Property(x => x.CriteriaVerifiedAt).HasColumnType("timestamp with time zone");
            e.Property(x => x.CreatedById).HasMaxLength(100);
            e.HasQueryFilter(x => !x.IsDeleted);
            // Must match PromotionQueryExtensions.Open().
            e.HasIndex(x => x.EmployeeId)
                .IsUnique()
                .HasFilter("\"status\" IN ('InProgress', 'Ready') AND \"is_deleted\" = false")
                .HasDatabaseName(OpenPromotionIndex);
            e.HasOne<PromotionCriteriaEntity>()
                .WithMany()
                .HasForeignKey(x => x.PromotionCriteriaId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ApprovedCriteriaLabels)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => string.IsNullOrWhiteSpace(v) ? new List<string>() : System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>(),
                    ElementwiseListComparer<string>())
                .HasColumnType("text")
                .HasDefaultValue(new List<string>());
            e.Property(x => x.CriteriaOverrides)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => string.IsNullOrWhiteSpace(v) ? new List<CriterionOverride>() : System.Text.Json.JsonSerializer.Deserialize<List<CriterionOverride>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<CriterionOverride>(),
                    ElementwiseListComparer<CriterionOverride>())
                .HasColumnType("text")
                .HasDefaultValue(new List<CriterionOverride>());
        });

        modelBuilder.Entity<KnowledgeEvaluationEntity>(e =>
        {
            e.ToTable("knowledge_evaluations");
            e.HasKey(x => x.Id);
            e.Property(x => x.RequestedOption).IsRequired().HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Comment).HasMaxLength(KnowledgeEvaluationEntity.CommentMaxLength);
            e.Property(x => x.ConductedById).HasMaxLength(100);
            e.Property(x => x.Link).HasMaxLength(KnowledgeEvaluationEntity.LinkMaxLength);
            e.HasOne(x => x.Promotion).WithMany().HasForeignKey(x => x.PromotionId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.PromotionId);
            e.HasQueryFilter(x => !x.Promotion.IsDeleted);
        });

        modelBuilder.Entity<EmailTemplateEntity>(e =>
        {
            e.ToTable("email_templates");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(50);
            e.Property(x => x.Label).IsRequired().HasMaxLength(100);
            e.Property(x => x.Subject).IsRequired().HasMaxLength(300);
            e.Property(x => x.Body).IsRequired();
            e.Property(x => x.Checklist).HasColumnType("jsonb").IsRequired();
            e.PrimitiveCollection(x => x.ToRecipients).ElementType().HasConversion<string>();
            e.PrimitiveCollection(x => x.CcRecipients).ElementType().HasConversion<string>();
        });

        modelBuilder.Entity<EmailWhitelistEntity>(e =>
        {
            e.ToTable("email_whitelist");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).IsRequired().HasMaxLength(256);
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<AppSettingsEntity>(e =>
        {
            e.ToTable("app_settings");
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).IsRequired().HasMaxLength(200);
            e.Property(x => x.PromotionDefaultHolder).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<ActivityEntity>(e =>
        {
            e.ToTable("activities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Category).IsRequired().HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.WithinPeriod).IsRequired().HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Technology).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.Points).IsRequired();
            e.HasIndex(x => x.Category);
            e.HasIndex(x => x.Technology);
        });

        modelBuilder.Entity<PromotionCriteriaEntity>(e =>
        {
            e.ToTable("promotion_criteria");
            e.HasKey(x => x.Id);
            e.Property(x => x.Technology).IsRequired().HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.Seniority).IsRequired().HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.SubLevel).IsRequired().HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.RequiredExperience).HasMaxLength(300);
            e.Property(x => x.EnglishLevel).IsRequired().HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.KeRequired).IsRequired().HasConversion<string>().HasMaxLength(30);
            e.HasOne(x => x.RequiredActivity)
             .WithMany()
             .HasForeignKey(x => x.RequiredActivityId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.RequiredActivityId);
            e.HasIndex(x => new { x.Technology, x.Seniority, x.SubLevel })
                .IsUnique()
                .HasFilter("\"is_active\" = true");
        });

        modelBuilder.Entity<EmployeeActivityEntity>(e =>
        {
            e.ToTable("employee_activities");
            e.HasKey(x => x.Id);
            e.Property(x => x.EmployeeId).IsRequired().HasMaxLength(100);
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.CreatedById).HasMaxLength(100);
            e.Property(x => x.ApprovedById).HasMaxLength(100);
            e.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(20);

            e.HasOne(x => x.Activity)
             .WithMany()
             .HasForeignKey(x => x.ActivityId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Promotion)
             .WithMany()
             .HasForeignKey(x => x.PromotionId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne<PromotionEntity>()
             .WithMany()
             .HasForeignKey(x => x.CarriedFromPromotionId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.EmployeeId);
            e.HasIndex(x => x.PromotionId);
            e.HasIndex(x => x.ActivityId);
        });

        modelBuilder.Entity<EmployeeExtensionEntity>(e =>
        {
            e.ToTable("employee_extensions");
            e.HasKey(x => x.EmployeeId);
            e.Property(x => x.EmployeeId).IsRequired().HasMaxLength(100);
            e.Property(x => x.LevelStartDate).HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<ResponsibleRoleMappingEntity>(e =>
        {
            e.ToTable("responsible_role_mappings");
            e.HasKey(x => x.RoleKey);
            e.Property(x => x.RoleKey).IsRequired().HasMaxLength(100);
            e.Property(x => x.DefaultEmployeeId).HasMaxLength(100);
        });

        modelBuilder.Entity<ResponsiblePositionEntity>(e =>
        {
            e.ToTable("responsible_positions");
            e.HasKey(x => x.Position);
            e.Property(x => x.Position).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<OnboardingSettingsEntity>(e =>
        {
            e.ToTable("onboarding_settings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<OnboardingTaskTemplateEntity>(e =>
        {
            e.ToTable("onboarding_task_templates");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.Phase).IsRequired().HasConversion<string>().HasMaxLength(30);
            e.HasOne(x => x.Milestone).WithMany().HasForeignKey(x => x.MilestoneId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.DueDirection).IsRequired().HasConversion<string>().HasMaxLength(10);
            e.Property(x => x.DueAnchor).IsRequired().HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.AppliesTo).IsRequired().HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ResponsibleRoleKey).HasMaxLength(100);
            e.HasMany(x => x.SuggestedLevels)
                .WithOne(l => l.Template)
                .HasForeignKey(l => l.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Fields)
                .WithOne(f => f.Template)
                .HasForeignKey(f => f.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
            // Title is unique per phase among active templates only — a deactivated template keeps its name.
            e.HasIndex(x => new { x.Phase, x.Title })
                .IsUnique()
                .HasFilter("\"is_active\" = true");
        });

        modelBuilder.Entity<OnboardingMilestoneEntity>(e =>
        {
            e.ToTable("onboarding_milestones");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(OnboardingMilestoneEntity.NameMaxLength);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<OnboardingTaskTemplateSuggestedLevelEntity>(e =>
        {
            e.ToTable("onboarding_task_template_suggested_levels");
            e.HasKey(x => x.Id);
            e.Property(x => x.Level).IsRequired().HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.TemplateId, x.Level }).IsUnique();
        });

        modelBuilder.Entity<OnboardingTaskTemplateFieldEntity>(e =>
        {
            e.ToTable("onboarding_task_template_fields");
            e.HasKey(x => x.Id);
            e.Property(x => x.FieldType).IsRequired().HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Label).IsRequired().HasMaxLength(200);
            e.Property(x => x.Hint).HasMaxLength(300);
            e.Property(x => x.PersonRestrictionKey).HasMaxLength(200);
            e.HasMany(x => x.ChecklistItems)
                .WithOne(i => i.Field)
                .HasForeignKey(i => i.FieldId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.TemplateId);
        });

        modelBuilder.Entity<OnboardingTaskTemplateChecklistItemEntity>(e =>
        {
            e.ToTable("onboarding_task_template_checklist_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Text).IsRequired().HasMaxLength(300);
            e.HasIndex(x => x.FieldId);
        });

        modelBuilder.Entity<OnboardingTypeEntity>(e =>
        {
            e.ToTable("onboarding_types");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(OnboardingTypeEntity.NameMaxLength);
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasMany(x => x.Templates)
                .WithOne()
                .HasForeignKey(t => t.TypeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<OnboardingTypeTemplateEntity>(e =>
        {
            e.ToTable("onboarding_type_templates");
            e.HasKey(x => new { x.TypeId, x.TemplateId });
            e.HasOne(x => x.Template)
                .WithMany()
                .HasForeignKey(x => x.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OnboardingFlowEntity>(e =>
        {
            e.ToTable("onboarding_flows");
            e.HasKey(x => x.Id);
            e.HasOne<OnboardingTypeEntity>()
                .WithMany()
                .HasForeignKey(x => x.OnboardingTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Position).HasMaxLength(200);
            e.Property(x => x.EmploymentType).IsRequired().HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Level).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ResponsiblePersonId).IsRequired().HasMaxLength(100);
            e.Property(x => x.ResponsiblePosition).HasMaxLength(200);
            e.HasMany(x => x.Tasks)
                .WithOne(t => t.Flow)
                .HasForeignKey(t => t.FlowId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OnboardingFlowTaskEntity>(e =>
        {
            e.ToTable("onboarding_flow_tasks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Phase).IsRequired().HasConversion<string>().HasMaxLength(30);
            e.HasOne(x => x.Milestone).WithMany().HasForeignKey(x => x.MilestoneId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.ResponsibleRoleKey).HasMaxLength(100);
            e.Property(x => x.DueDirection).IsRequired().HasConversion<string>().HasMaxLength(10);
            e.Property(x => x.DueAnchor).IsRequired().HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.AssigneeId).HasMaxLength(100);
            // Traceability only — a later template edit or delete must not affect tasks already generated from it.
            e.HasOne<OnboardingTaskTemplateEntity>()
                .WithMany()
                .HasForeignKey(x => x.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.Fields)
                .WithOne(f => f.Task)
                .HasForeignKey(f => f.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Activities)
                .WithOne(a => a.Task)
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.FlowId);
        });

        modelBuilder.Entity<OnboardingFlowTaskActivityEntity>(e =>
        {
            e.ToTable("onboarding_flow_task_activities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Kind).IsRequired().HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.FromAssigneeId).HasMaxLength(100);
            e.Property(x => x.ToAssigneeId).HasMaxLength(100);
            e.Property(x => x.FieldLabel).HasMaxLength(200);
            e.Property(x => x.ChecklistItemText).HasMaxLength(300);
            e.HasIndex(x => x.TaskId);
        });

        modelBuilder.Entity<OnboardingFlowTaskFieldEntity>(e =>
        {
            e.ToTable("onboarding_flow_task_fields");
            e.HasKey(x => x.Id);
            e.Property(x => x.FieldType).IsRequired().HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Label).IsRequired().HasMaxLength(200);
            e.Property(x => x.PersonId).HasMaxLength(100);
            e.HasOne<OnboardingTaskTemplateFieldEntity>()
                .WithMany()
                .HasForeignKey(x => x.TemplateFieldId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.CheckedItems)
                .WithOne(c => c.TaskField)
                .HasForeignKey(c => c.TaskFieldId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.TaskId);
            e.HasIndex(x => x.TemplateFieldId);
        });

        modelBuilder.Entity<OnboardingFlowTaskCheckedItemEntity>(e =>
        {
            e.ToTable("onboarding_flow_task_checked_items");
            e.HasKey(x => new { x.TaskFieldId, x.ChecklistItemId });
            e.HasOne(x => x.ChecklistItem)
                .WithMany()
                .HasForeignKey(x => x.ChecklistItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // Without it EF compares the list reference, so a change made in place to the list is never saved.
    private static ValueComparer<List<T>> ElementwiseListComparer<T>()
    {
        return new ValueComparer<List<T>>(
            (a, b) => a!.SequenceEqual(b!),
            v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
            v => v.ToList());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        IEnumerable<EntityEntry<BaseAuditableEntity>> entries = ChangeTracker.Entries<BaseAuditableEntity>();
        string? userId = httpContextAccessor.HttpContext?.User.GetUserId();
        DateTime now = DateTime.UtcNow;
        foreach (EntityEntry<BaseAuditableEntity> entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(e => e.CreatedAt).CurrentValue = now;
                entry.Property(e => e.UpdatedAt).CurrentValue = now;
                entry.Property(e => e.UpdatedBy).CurrentValue = userId;
                entry.Property(e => e.CreatedById).CurrentValue = userId;
            }
            if (entry.State == EntityState.Modified)
            {
                entry.Property(e => e.UpdatedAt).CurrentValue = now;
                entry.Property(e => e.UpdatedBy).CurrentValue = userId;
            }
        }
        return await base.SaveChangesAsync(ct);
    }
}
