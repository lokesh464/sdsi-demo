using Xunit;
using SlaBreach.Models;
using SlaBreach.Services;

namespace SlaBreach.Tests;

public class SlaCalculatorTests
{
    private static SlaCalculator BuildCalculator()
    {
        var policies = new List<SlaPolicy>
        {
            new() { Priority = "P1", SlaTarget = "response",
                MaxBusinessHours = 4 },
            new() { Priority = "P2", SlaTarget = "resolution",
                MaxBusinessHours = 8 },
            new() { Priority = "P3", SlaTarget = "response",
                MaxBusinessHours = 24 },
            new() { Priority = "P4", SlaTarget = "none",
                MaxBusinessHours = 0 }
        };

        var holidays = new List<Holiday>
        {
            new() { Date = new DateOnly(2025, 1, 1),
                Name = "New Year's Day" },
            new() { Date = new DateOnly(2025, 7, 4),
                Name = "Independence Day" },
            new() { Date = new DateOnly(2025, 12, 25),
                Name = "Christmas Day" }
        };

        var config = new BusinessHoursConfig
        {
            TimeZone = "Eastern Standard Time",
            BusinessHoursStart = "08:00",
            BusinessHoursEnd = "17:00",
            BusinessDays = new List<string>
                { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" }
        };

        return new SlaCalculator(policies, holidays, config);
    }

    [Fact]
    public void Ticket_Within_SLA_Not_Breached()
    {
        var calc = BuildCalculator();
        var ticket = new Ticket
        {
            Id = "TKT-WITHIN",
            Priority = "P2",
            Status = "resolved",
            CreatedAt = new DateTime(2025, 10, 1, 12, 0, 0,
                DateTimeKind.Utc),
            ResolvedAt = new DateTime(2025, 10, 1, 14, 0, 0,
                DateTimeKind.Utc)
        };

        var result = calc.Compute(ticket, DateTime.UtcNow);

        Assert.NotNull(result);
        Assert.False(result.Breached);
        Assert.True(result.ElapsedBusinessHours
            <= result.SlaThresholdHours);
        Assert.Equal(0, result.BreachAmount);
    }

    [Fact]
    public void Ticket_Breached_Exceeds_SLA()
    {
        var calc = BuildCalculator();
        var ticket = new Ticket
        {
            Id = "TKT-BREACH",
            Priority = "P1",
            Status = "resolved",
            CreatedAt = new DateTime(2025, 10, 1, 12, 0, 0,
                DateTimeKind.Utc),
            ResolvedAt = new DateTime(2025, 10, 1, 22, 0, 0,
                DateTimeKind.Utc)
        };

        var result = calc.Compute(ticket, DateTime.UtcNow);

        Assert.NotNull(result);
        Assert.True(result.Breached);
        Assert.True(result.BreachAmount > 0);
        Assert.True(result.ElapsedBusinessHours
            > result.SlaThresholdHours);
    }

    [Fact]
    public void Weekend_Span_Deducts_Weekend_Hours()
    {
        var calc = BuildCalculator();
        var ticket = new Ticket
        {
            Id = "TKT-WEEKEND",
            Priority = "P3",
            Status = "resolved",
            CreatedAt = new DateTime(2025, 10, 24, 20, 0, 0,
                DateTimeKind.Utc),
            ResolvedAt = new DateTime(2025, 10, 27, 13, 0, 0,
                DateTimeKind.Utc)
        };

        var result = calc.Compute(ticket, DateTime.UtcNow);

        Assert.NotNull(result);
        Assert.False(result.Breached);
        Assert.True(result.ElapsedBusinessHours < 4);
    }

    [Fact]
    public void Holiday_Span_Deducts_Holiday_Hours()
    {
        var calc = BuildCalculator();
        var ticket = new Ticket
        {
            Id = "TKT-HOLIDAY",
            Priority = "P2",
            Status = "resolved",
            CreatedAt = new DateTime(2025, 7, 3, 20, 0, 0,
                DateTimeKind.Utc),
            ResolvedAt = new DateTime(2025, 7, 7, 13, 0, 0,
                DateTimeKind.Utc)
        };

        var result = calc.Compute(ticket, DateTime.UtcNow);

        Assert.NotNull(result);
        Assert.False(result.Breached);
        Assert.True(result.ElapsedBusinessHours < 4);
    }

    [Fact]
    public void Unresolved_Ticket_Uses_Now_As_End()
    {
        var calc = BuildCalculator();
        var fixedNow = new DateTime(2025, 10, 1, 16, 0, 0,
            DateTimeKind.Utc);

        var ticket = new Ticket
        {
            Id = "TKT-OPEN",
            Priority = "P1",
            Status = "open",
            CreatedAt = new DateTime(2025, 10, 1, 12, 0, 0,
                DateTimeKind.Utc),
            ResolvedAt = null
        };

        var result = calc.Compute(ticket, fixedNow);

        Assert.NotNull(result);
        Assert.False(result.Breached);
        Assert.Equal(4, result.ElapsedBusinessHours);
    }

    [Fact]
    public void On_Hold_Periods_Deducted_From_Business_Hours()
    {
        var calc = BuildCalculator();
        var ticket = new Ticket
        {
            Id = "TKT-HOLD",
            Priority = "P2",
            Status = "resolved",
            CreatedAt = new DateTime(2025, 10, 1, 12, 0, 0,
                DateTimeKind.Utc),
            ResolvedAt = new DateTime(2025, 10, 1, 22, 0, 0,
                DateTimeKind.Utc),
            OnHoldPeriods = new List<OnHoldPeriod>
            {
                new()
                {
                    Start = new DateTime(2025, 10, 1, 14, 0, 0,
                        DateTimeKind.Utc),
                    End = new DateTime(2025, 10, 1, 18, 0, 0,
                        DateTimeKind.Utc)
                }
            }
        };

        var result = calc.Compute(ticket, DateTime.UtcNow);

        Assert.NotNull(result);
        Assert.Equal(5, result.ElapsedBusinessHours);
        Assert.False(result.Breached);
    }

    [Fact]
    public void P4_No_Sla_Returns_Null()
    {
        var calc = BuildCalculator();
        var ticket = new Ticket
        {
            Id = "TKT-P4",
            Priority = "P4",
            Status = "resolved",
            CreatedAt = new DateTime(2025, 9, 1, 13, 0, 0,
                DateTimeKind.Utc),
            ResolvedAt = new DateTime(2025, 10, 1, 13, 0, 0,
                DateTimeKind.Utc)
        };

        var result = calc.Compute(ticket, DateTime.UtcNow);
        Assert.Null(result);
    }

    [Fact]
    public void Unknown_Priority_Returns_Null()
    {
        var calc = BuildCalculator();
        var ticket = new Ticket
        {
            Id = "TKT-BAD",
            Priority = "P99",
            Status = "resolved",
            CreatedAt = new DateTime(2025, 10, 1, 13, 0, 0,
                DateTimeKind.Utc),
            ResolvedAt = new DateTime(2025, 10, 1, 15, 0, 0,
                DateTimeKind.Utc)
        };

        var result = calc.Compute(ticket, DateTime.UtcNow);
        Assert.Null(result);
    }

    [Fact]
    public void MergeSpans_Merges_Overlapping()
    {
        var spans = new List<(DateTime, DateTime)>
        {
            (new DateTime(2025, 1, 1, 10, 0, 0),
             new DateTime(2025, 1, 1, 12, 0, 0)),
            (new DateTime(2025, 1, 1, 11, 0, 0),
             new DateTime(2025, 1, 1, 14, 0, 0))
        };

        var merged = SlaCalculator.MergeSpans(spans);

        Assert.Single(merged);
        Assert.Equal(new DateTime(2025, 1, 1, 10, 0, 0),
            merged[0].Start);
        Assert.Equal(new DateTime(2025, 1, 1, 14, 0, 0),
            merged[0].End);
    }

    [Fact]
    public void MapWindowsTz_Keeps_Unknown_Unchanged()
    {
        Assert.Equal("SomeCustomTZ",
            SlaCalculator.MapWindowsTz("SomeCustomTZ"));
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, true)]
    [InlineData(DayOfWeek.Tuesday, true)]
    [InlineData(DayOfWeek.Wednesday, true)]
    [InlineData(DayOfWeek.Thursday, true)]
    [InlineData(DayOfWeek.Friday, true)]
    [InlineData(DayOfWeek.Saturday, false)]
    [InlineData(DayOfWeek.Sunday, false)]
    public void IsBusinessDay_Returns_Correct(DayOfWeek day,
        bool expected)
    {
        var calc = BuildCalculator();
        Assert.Equal(expected, calc.IsBusinessDay(day));
    }

    [Fact]
    public void IsHoliday_Returns_True_For_Holiday_Date()
    {
        var calc = BuildCalculator();
        Assert.True(calc.IsHoliday(new DateOnly(2025, 7, 4)));
        Assert.False(calc.IsHoliday(new DateOnly(2025, 7, 3)));
    }
}
