# Planbord: 3-minute demo

Planbord replaces Vermeulen BT's weekly Google Sheet with a single database for planning, daily work reports and an availability forecast. The UI is in Dutch.

**Before you start:** run `docker compose up -d` and start the app. Log in as the owner, open **Gebruikers** and use **Voorbeeldgegevens herstellen** (type `RESET`). This loads:

- 6 employees and about 25 projects.
- A planned current week.
- Closed reports for every workday since January 2025, at 38 h a week.
- Each person's summer and Christmas holidays.

Keep a second browser window (private mode) signed in as an employee account if you want to show the employee's view.

---

## 1. Aanmelden (0:00–0:05)
There are three roles: **Eigenaar** (owner), **Planner** and **Medewerker** (employee). Planners land on the planning board; employees land on *Mijn dag*.

## 2. Planning (0:05–0:35)
The weekly board.
- **Per medewerker / Per werf**: the same week shown by employee or by site.
- **Drag and drop** a chip to move an assignment.
- An employee's first site of the day is normal. A second site is an *intervention*, shown in **red** until someone approves it. Point at Jan Peeters' "Werkplaats" chip on Wednesday.
- Click a chip or **+** to add, move, delete or approve. Day tabs give a phone view, and **CSV** exports the week.

## 3. Plannen and Afwezigheden (0:35–0:50)
- **Plannen:** bulk planning for several employees over a date range. Absent employees are skipped, and the page reports who was skipped.
- **Afwezigheden:** recording leave clears any planning in that period and reports what was removed.

## 4. Projecten (0:50–1:00)
Each site has a start and end date (this sets its length category) and a **task checklist** shared by every planned day. Deleting a project that has history archives it instead.

## 5. Berichten and Mijn dag (1:00–1:40)
- **Berichten:** a message per employee per day. The summary of sites and open tasks is **generated live**; the planner only adds extra text. The card shows when the employee has **read** it.
- **Mijn dag** (the employee's station): the message, site tasks to tick off, and the **daily report**.
  - Per site, a status: **Gewerkt** (description required), or **Niet geweest** / **Geannuleerd** (reason required, 0 hours).
  - With two sites, the hours must add up **exactly** to the net working time. Submit a wrong split to show the error, then fix it.
  - **Indienen** locks the report.

## 6. Dagrapporten and Overzichten (1:40–2:00)
- **Dagrapporten:** who has submitted and who is still missing. The table updates **live** when a report comes in. **Sluiten** marks a report as reviewed; **Vrijgeven** unlocks it for correction.
- **Overzichten:** the hours report by day, project or employee, with **Kopiëren**, **CSV** and **Afdrukken / PDF**, plus occupancy per site and next week's gaps and conflicts.

## 7. Dashboard (2:00–2:30)
One employee at a time; use the dropdown at the top right.
- **Project cards** in five rows by planned length (≤ 1 month up to 1 year+), each row with its own pastel colour and count. Only **active** and **recently finished** projects (ended in the last 3 months) appear. Each card shows its dates and status, for example "Beëindigd 3 weken geleden".
- **Clocked hours:** a donut chart with the total in the middle.
  - **Periode:** this year, month, quarter, last year or all time.
  - **Weergave:** per project, per category or just the total. Hover a slice for its hours and percentage.
- **Beschikbaarheid komende 12 maanden:** one bar per month showing the free share of a 38-hour week. Each project takes a fixed share by length (35 / 25 / 20 / 10 / 10%), and holidays count as zero. Show:
  - **Lotte Claes:** overbooked this month (rose bar).
  - **Ines Goossens:** the most free time.
  - **Kevin Wouters:** a warning for a project past its end date that is still planned. It keeps its load until the end date is corrected.
- **Ruimte voor een nieuw project:** per length category, the month a typical project of that size would fit.

## 8. Prognose (2:30–2:50)
"Who can take a new 3-month project?" Enter the duration. The page picks the category and its share of the week, then ranks everyone:
- First by **who can start earliest**.
- On a tie, by **most free hours over the project's duration**.

Try 1 month and then 12 months to show the ranking change. The **Deel van de week** card on the right lets planners adjust the share per category.

## 9. Gebruikers (2:50–3:00), owner only
Invite users with a role and link them to an employee. The owner gets an **activation link** to send on, because email isn't configured. An employee's identity always comes from that link on the server, never from the browser. This is also where the sample-data reset lives.
