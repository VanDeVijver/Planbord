# Planbord – Notes

Last updated: 2026-10-10

## Employee dashboard

A dashboard for a single employee that shows how many projects they are assigned to, grouped by project length, plus a small display of their clocked hours.

### What it shows

- The selected employee and their number of assigned projects (active and recently finished).
- The projects grouped into five length categories, with a count per category.
- Each project shown as a card, colour coded by its category.
- A legend that explains which colour belongs to which category.
- A small pie chart of clocked hours, with dropdowns to change the view and the time period.

### Which projects are shown

- Only projects that are **active** or **recently finished**.
- Recently finished means the project ended within the last 3 months.
- Projects that finished longer than 3 months ago are not shown.
- Projects that have not started yet are not shown.

### Length categories (legend)

| Category | Project length | Suggested colour | Hex |
| --- | --- | --- | --- |
| 1 | <= 1 month | Light mint | #CDEBDD |
| 2 | <= 3 months | Light sky blue | #CFE4F7 |
| 3 | <= 6 months | Light lavender | #DDD6F3 |
| 4 | <= 12 months | Light peach | #FBE0CC |
| 5 | 1 year+ | Light rose | #F7D3DA |

- Project length is the planned duration from start date to end date, not the time remaining from today.
- Each project falls into exactly one category: the first one whose limit it does not exceed (a 2-month project is "<= 3 months", not "<= 6 months").
- A project with an unknown length (no end date) goes into "1 year+".

### Layout

- One row per legend category, in legend order (<= 1 month at the top, 1 year+ at the bottom).
- Each row starts with the category label, its colour and its project count.
- Within a row, the project cards are laid out in columns next to each other; they wrap to a new line when the row is full.
- All five rows are always shown, also those with zero projects for this employee.
- The counts of all rows add up to the employee's number of shown projects.

### Project card

- Project name.
- Start date and end date (or "no end date").
- Status, always visible on the card: "Active" or "Recently finished", for example as a small badge. A recently finished card also shows when it ended (for example "Finished 3 weeks ago").
- Background in the pastel colour of its category.

### Clocked hours (pie chart)

- A small display on the dashboard showing the hours this employee has clocked.
- Time period: shows the current calendar year by default (not the last 12 months). A period dropdown offers other options (for example this month, this quarter, last year, all time).
- All hours clocked in the chosen period count, including hours on projects that are no longer shown as cards (finished more than 3 months ago).
- A second dropdown select switches the view:
  - **Per project**: a pie chart with one slice per project.
  - **Per category**: a pie chart with one slice per length category.
  - **Total**: no pie chart, just the total number of clocked hours as a single figure.
- Slices use the same pastel category colours as the legend. In the per-project view, a project's slice takes the colour of its category (projects in the same category are told apart by a label or a slightly different shade).
- Each slice shows its hours and percentage, on the slice or on hover.
- The total hours for the chosen period stay visible in every view (for example in the centre of the chart or under it).

### Colour rules

- Always smooth, light (pastel) versions of colours, so they are easy on the eyes.
- No saturated or neon colours, also not for hover or selected states; use a slightly darker shade of the same pastel instead.
- Text on the colours stays dark so it remains readable.
- The colour is never the only signal: the category label is always shown next to it.
- The hex values above are a starting suggestion and can be changed.

## Availability forecast (after the dashboard)

Goal: predict per month how much room an employee has, and find the best person for a new project of a given size.

Chosen approach: **planned allocation**. Every project takes a fixed, standardized share of an employee's week based on its length category. Planbord does not store planned hours yet; this structure introduces them.

### Capacity

- Every employee works **38 hours a week** (7.6 hours per working day, 5 days a week). This is the same for everyone and does not change.
- Per year: 52 weeks x 38 h = 1,976 h.
- Holidays: **2 weeks in summer** and **1 week in the Christmas period**, so 3 weeks x 38 h = 114 h off.
- Yearly capacity: 49 working weeks x 38 h = **1,862 h**.
- The holiday weeks are not fixed: each employee's summer and Christmas weeks are stored per employee, and the forecast uses those dates.
- During an employee's holiday weeks their capacity is 0 and projects take no hours.
- Public holidays do lower real capacity, but they are out of scope for this app and are not part of the calculation.
- Overtime is possible, but it is not planned capacity: the forecast always plans against 38 h a week.

### Planned load per category

Each project takes a standard percentage of the 38-hour week, set by its length category.

| Category | Project length | Share of the week | Hours per week |
| --- | --- | --- | --- |
| 1 | <= 1 month | 35% | 13.3 h |
| 2 | <= 3 months | 25% | 9.5 h |
| 3 | <= 6 months | 20% | 7.6 h |
| 4 | <= 12 months | 10% | 3.8 h |
| 5 | 1 year+ | 10% | 3.8 h |

- A project's load starts on its start date and stops on its end date.
- Short projects are heavy, long projects are light. An employee without any 1-month or 3-month projects has a lot of free time.
- Example: one 6-month project (20%) and one 1 year+ project (10%) use 30% of the week, 11.4 h. That leaves 70%, 26.6 h a week, free.
- The percentages are stored as one setting per category, so they can be changed later without touching the projects.

### Monthly forecast

- The forecast is calculated **per month**, for the next 12 months.
- Per month: capacity = working days in that month x 7.6 h (holiday days count as 0).
- Planned load = for each working day, the sum of the shares of all projects running that day x 7.6 h, added up over the month. This handles projects that start or end mid-month.
- Free hours = capacity minus planned load. Free share = free hours divided by capacity.
- If the planned load is above 100%, the month is shown as overbooked: the work only fits with overtime. This is a warning, not a block, since overtime is allowed.
- Display: a free-capacity bar per month in the same pastel style, with the free share and free hours.

### Is there room for a project of size X?

- A new project is described by its duration, which picks its category and so its share of the week.
- It fits for an employee from the first month in which the free share is at least the project's share, and stays at least that for every month of its duration.
- Display: per length category, an "available from" month for this employee.

### Finding the best person

- For a new project, the forecast checks every employee and lists them ordered by availability.
- Best person = the one who can start earliest.
- On a tie: the one with the most free hours over the project's duration.
- For now the choice is based on availability only.
- Later: employees get expertise fields, and projects require expertise. The forecast then first filters on matching expertise and ranks the remaining employees by availability as above.

### Rules for uncertain cases

- A project with no end date counts as 1 year+ (10%) and keeps its load for the whole forecast horizon.
- A project past its end date but still active keeps its load and is flagged so the end date gets corrected.
- Future projects that are already assigned count in the forecast, even though the dashboard does not show them as cards.

### Suggested build order

1. Store the share per category as a setting, and capacity (38 h) and the holiday weeks per employee.
2. Monthly capacity and planned-load calculation per employee, using their own holiday weeks.
3. Monthly free-capacity bar on the dashboard.
4. "Available from" per category and the fit check for a new project.
5. Ranking of all employees for a new project.
6. Later: expertise fields on employees and projects, as a filter before the ranking.