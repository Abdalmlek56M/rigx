using System.Data;
using System.Globalization;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HardwareStore.Modules.Identity;

public sealed class PersonCodeGenerator(
    IdentityDbContext dbContext,
    TimeProvider timeProvider,
    TimeZoneInfo businessTimeZone)
{
    public async Task<Person> CreatePersonAsync(Person person, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(person);
        if (string.IsNullOrWhiteSpace(person.FullName))
            throw new ArgumentException("Person full name is required.", nameof(person));

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var created = await CreatePersonInCurrentTransactionAsync(person, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return created;
    }

    public async Task<Person> CreatePersonInCurrentTransactionAsync(
        Person person,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(person);
        if (dbContext.Database.CurrentTransaction is null)
            throw new InvalidOperationException("PersonCode allocation requires an active caller-owned transaction.");
        if (string.IsNullOrWhiteSpace(person.FullName))
            throw new ArgumentException("Person full name is required.", nameof(person));

        var nowUtc = timeProvider.GetUtcNow();
        var yearMonth = TimeZoneInfo.ConvertTime(nowUtc, businessTimeZone)
            .ToString("yyyyMM", CultureInfo.InvariantCulture);
        var connection = dbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = dbContext.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = """
            INSERT INTO person_code_counters (year_month, last_value)
            VALUES (CAST(@year_month AS character(6)), 1)
            ON CONFLICT (year_month) DO UPDATE
                SET last_value = person_code_counters.last_value + 1
                WHERE person_code_counters.last_value < 99999
            RETURNING last_value
            """;
        var monthParameter = command.CreateParameter();
        monthParameter.ParameterName = "year_month";
        monthParameter.DbType = DbType.String;
        monthParameter.Value = yearMonth;
        command.Parameters.Add(monthParameter);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        var nextValue = result is null or DBNull
            ? 0
            : Convert.ToInt32(result, CultureInfo.InvariantCulture);

        if (nextValue == 0)
            throw new InvalidOperationException($"PersonCode capacity for {yearMonth} has reached 99,999.");

        person.AssignPersonCode(PersonCodeFormatter.Format(nowUtc, businessTimeZone, nextValue));
        person.CreatedAtUtc = nowUtc;
        if (person.Id == Guid.Empty)
            person.Id = Guid.CreateVersion7();

        dbContext.Persons.Add(person);
        await dbContext.SaveChangesAsync(cancellationToken);
        return person;
    }
}

public static class PersonCodeFormatter
{
    public static string Format(DateTimeOffset utcNow, TimeZoneInfo businessTimeZone, int sequence)
    {
        ArgumentNullException.ThrowIfNull(businessTimeZone);
        if (sequence is < 1 or > 99999)
            throw new ArgumentOutOfRangeException(nameof(sequence), "PersonCode sequence must be between 1 and 99,999.");

        var localTime = TimeZoneInfo.ConvertTime(utcNow.ToUniversalTime(), businessTimeZone);
        return $"PER-{localTime:yyyyMM}-{sequence:00000}";
    }
}
