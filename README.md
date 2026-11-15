# FitSphere – All-in-One Fitness Management App

Android app built with **.NET MAUI (C#)**, **SQLite** (on-device) and local authentication.
All UI is written in C# using a single design-token class, so colours, radii and spacing stay consistent.

## Run it
1. Install Visual Studio 2022 (17.8+) or VS Code with the **.NET MAUI** workload (Android), plus an Android emulator or a USB-debugging phone.
2. Open `FitSphere.csproj`. If your SDK is not .NET 8, change `TargetFrameworks` to `net9.0-android` / `net10.0-android`.
3. Restore NuGet packages (`sqlite-net-pcl`, `SQLitePCLRaw.bundle_green`) and press **Run**.

CLI: `dotnet build -t:Run -f net8.0-android`

## Architecture
| Layer | Folder | Responsibility |
|---|---|---|
| UI | `Pages/`, `UI/` | Screens, design system (`Ds`), components (`Ui`, `FormInput`, `ChipGroup`, `Counter`), charts |
| Business logic | `Services/` | Auth, workouts, nutrition, habits, progress, rule-based `InsightEngine`, reminders |
| Data | `Data/`, `Models/` | SQLite connection, entities, seed exercise library |

`Svc` (Services/Core.cs) is a small composition root that holds one instance of each service.

## Features by proposal module
- **Auth & onboarding** – register/login (PBKDF2-hashed passwords), profile setup (sex, age, height, weight, experience, goal, equipment), sex-aware auto-estimated calorie/macro/water targets.
- **Home** – greeting, date, daily goals, today's workout hero card, nutrition ring + macros, water tracker, weekly dots, consistency score, quick actions, top insight.
- **Workouts** – create/edit/delete plans, training days, sets/reps/rest, weekly schedule, start/resume session, progression suggestions.
- **Exercise library** – 59 exercises, 7 muscle groups, equipment filter, search, instructions, built-in line illustrations, replacement suggestions.
- **Workout tracking** – live timer, per-set weight/reps, set completion, automatic rest countdown, history, personal records (PR detection on finish).
- **Nutrition** – day navigation, calorie ring, protein/carbs/fat bars, 4 meals, add/edit/delete foods, recent + common foods.
- **Progress** – body weight line chart, measurements, weekly/monthly workout bar charts, goal indicators, overview, edit/delete entries, insights.
- **Habits** – water logging (+ custom/undo), daily & weekly goals, workout reminders (Android AlarmManager notifications), rest-day management.
- **Profile/Settings** – personal info, goals & targets, notification toggle, change password, log out, delete account.

## Intelligent features (rule-based, explained)
- **Progression suggestion** (`InsightEngine.SuggestAsync`): last 2 sessions per exercise → increase / hold / reduce, always showing the real numbers behind it. Pre-fills the next session.
- **Exercise replacement** (`ExerciseService.ReplacementsAsync`): same muscle group, equipment you own, ranked by movement type, equipment match and past use; each result states why.
- **Consistency score** (`HabitService.ConsistencyAsync`): completed ÷ scheduled workouts already due, last 28 days; rest days and today's pending workout are excluded.
- **Progress insights** (`InsightEngine.BuildAsync`): weight trend vs goal, protein/calorie adherence, volume change, hydration, recent PRs. Insights only appear when there is enough data, and each shows its basis.

## Notes
- Data is stored locally; reminders use inexact weekly alarms and are re-registered each time the app opens (they are not restored after a phone reboot until FitSphere is opened).
- Calorie targets use Mifflin-St Jeor with the sex the user selects (Male +5, Female -161). Sex is required in profile setup and editable later; if it changes, the app offers to recalculate targets. Users can still edit every target manually (Profile > Goals & targets).
- If you enable assembly trimming for Release builds, keep `FitSphere.Models` (sqlite-net uses reflection).
