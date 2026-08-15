using System.Linq.Expressions;
using ERP.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ERP.Infrastructure.Persistence;

/// <summary>
/// Conventions applied to the whole model. Anything written here is a rule you
/// never have to remember again in an individual entity configuration.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Applies one filter to every entity implementing <typeparamref name="TInterface"/>.
    /// Named filters (EF Core 10) stack, so soft-delete and tenant filters coexist
    /// instead of overwriting each other.
    /// </summary>
    public static ModelBuilder ApplyGlobalFilters<TInterface>(
        this ModelBuilder builder,
        Expression<Func<TInterface, bool>> filter,
        string? filterKey = null)
    {
        filterKey ??= typeof(TInterface).Name;

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned() || entityType.BaseType is not null)
                continue;

            if (!typeof(TInterface).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var body = new ParameterReplacer(filter.Parameters[0], parameter).Visit(filter.Body);

            builder.Entity(entityType.ClrType)
                .HasQueryFilter(filterKey, Expression.Lambda(body, parameter));
        }

        return builder;
    }

    /// <summary>Money-ish columns must never silently round. 18,4 everywhere.</summary>
    public static ModelBuilder ApplyDecimalPrecision(this ModelBuilder builder, int precision = 18, int scale = 4)
    {
        foreach (var property in builder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(precision);
            property.SetScale(scale);
        }

        return builder;
    }

    /// <summary>
    /// Timestamps go to the database as UTC and come back as UTC, whatever the
    /// server's regional settings happen to be.
    /// </summary>
    public static ModelBuilder ApplyUtcDateTimeConversion(this ModelBuilder builder)
    {
        var converter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v : v.Value.ToUniversalTime()) : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                    property.SetValueConverter(converter);
                else if (property.ClrType == typeof(DateTime?))
                    property.SetValueConverter(nullableConverter);
            }
        }

        return builder;
    }

    /// <summary>Identity's default table names are ugly in an ERP schema. Rename once.</summary>
    public static ModelBuilder ApplyIdentityTableNames(this ModelBuilder builder, string schema = "security")
    {
        var names = new Dictionary<string, string>
        {
            ["AspNetUsers"] = "Users",
            ["AspNetRoles"] = "Roles",
            ["AspNetUserRoles"] = "UserRoles",
            ["AspNetUserClaims"] = "UserClaims",
            ["AspNetUserLogins"] = "UserLogins",
            ["AspNetUserTokens"] = "UserTokens",
            ["AspNetRoleClaims"] = "RoleClaims"
        };

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var table = entityType.GetTableName();
            if (table is not null && names.TryGetValue(table, out var renamed))
                entityType.SetTableName(renamed);

            if (table is not null && names.ContainsKey(table))
                entityType.SetSchema(schema);
        }

        return builder;
    }

    private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == from ? to : base.VisitParameter(node);
    }
}
