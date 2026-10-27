# SLA Breach Calculator

.NET 8 console app that reads tickets, SLA policies, business hours, and
holidays from JSON, then computes which tickets breached their SLA.

## Quick start

```bash
cd SlaBreach
dotnet run -- --now "2025-10-27T12:00:00"
```

If you omit `--now`, the current UTC time is used.

## Input files

Place these in `Data/` (or alongside the executable):

| File | Purpose |
|---|---|
| `tickets.json` | Array of tickets with id, priority, status, createdAt, resolvedAt, onHoldPeriods |
| `sla_policy.json` | Array of priority → SLA target and max business hours |
| `business_hours.json` | Timezone, business hours window, working days |
| `holiday.json` | Array of dates excluded from business time |

### Example `sla_policy.json`

```json
[
  { "priority": "P1", "slaTarget": "response", "maxBusinessHours": 4 },
  { "priority": "P2", "slaTarget": "resolution", "maxBusinessHours": 8 },
  { "priority": "P3", "slaTarget": "response", "maxBusinessHours": 24 },
  { "priority": "P4", "slaTarget": "none", "maxBusinessHours": 0 }
]
```

- `slaTarget`: `response`, `resolution`, or `none`
- `maxBusinessHours`: threshold in business hours (not wall-clock)
- Set `slaTarget` to `none` to skip SLA for a priority

### Example `tickets.json`

```json
[
  {
    "id": "TKT-001",
    "title": "Production DB down",
    "priority": "P1",
    "status": "resolved",
    "createdAt": "2025-10-01T09:00:00",
    "resolvedAt": "2025-10-01T16:00:00",
    "onHoldPeriods": []
  },
  {
    "id": "TKT-006",
    "title": "Still open",
    "priority": "P1",
    "status": "open",
    "createdAt": "2025-10-26T10:00:00",
    "resolvedAt": null,
    "onHoldPeriods": []
  }
]
```

- For unresolved tickets (`status` ≠ `resolved`), elapsed time is measured
  against the `--now` parameter
- `onHoldPeriods` is an array of `{ "start": "...", "end": "..." }` ranges
  that are subtracted from business time (e.g. waiting on customer)

### Example `business_hours.json`

```json
{
  "timeZone": "Eastern Standard Time",
  "businessHoursStart": "08:00",
  "businessHoursEnd": "17:00",
  "businessDays": [ "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" ]
}
```

### Example `holiday.json`

```json
[
  { "date": "2025-01-01", "name": "New Year's Day" },
  { "date": "2025-07-04", "name": "Independence Day" }
]
```

## Output

### Console table
Shows every evaluated ticket with elapsed business hours, SLA threshold,
breach amount, and whether it breached.

### `output/breaches.json`
A summary JSON file with:

- `Results` — array of each ticket's breach computation
- `BreachCountByPriority` — count of breaches per priority
- `TicketsProcessed` / `TicketsSkipped` — counts
- `SkipReasons` — why each ticket was skipped

## How it works

1. Loads all four JSON files with `System.Text.Json` (no extra NuGet
   packages)
2. Converts every timestamp to UTC via the configured timezone
3. Iterates the ticket span in 15-minute increments, counting only
   minutes that fall within business hours, on a business day, and not on
   a holiday
4. Subtracts any `onHoldPeriods` from the elapsed time
5. Compares elapsed business time against the SLA policy threshold

## Tests

```bash
dotnet test
```

18 xUnit tests cover:

- Within SLA (not breached)
- Breached
- Weekend span deduction
- Holiday span deduction
- Unresolved ticket (uses `--now`)
- On-hold period deduction
- P4 / `none` SLA skipped
- Unknown priority skipped
- Overlapping on-hold span merge
- Timezone mapping
- Business day / holiday helpers

## Constraints

- No NuGet packages beyond the .NET SDK
- `System.Text.Json` only
- Missing/invalid tickets are skipped with a reason logged
- `DateTime.Now` is never called inside the logic — the `--now` parameter
  controls the reference time
