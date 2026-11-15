using SQLite;

namespace FitSphere.Models;

public static class Dates
{
    public static readonly string[] Days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

    public static DayOfWeek ToDow(string s) => s switch
    {
        "Mon" => DayOfWeek.Monday, "Tue" => DayOfWeek.Tuesday, "Wed" => DayOfWeek.Wednesday,
        "Thu" => DayOfWeek.Thursday, "Fri" => DayOfWeek.Friday, "Sat" => DayOfWeek.Saturday,
        _ => DayOfWeek.Sunday
    };

    public static string Short(DayOfWeek d) => d switch
    {
        DayOfWeek.Monday => "Mon", DayOfWeek.Tuesday => "Tue", DayOfWeek.Wednesday => "Wed",
        DayOfWeek.Thursday => "Thu", DayOfWeek.Friday => "Fri", DayOfWeek.Saturday => "Sat",
        _ => "Sun"
    };

    public static DateTime StartOfWeek(DateTime d) => d.Date.AddDays(-(((int)d.DayOfWeek + 6) % 7));

    public static string Friendly(DateTime d)
    {
        if (d.Date == DateTime.Today) return "Today";
        if (d.Date == DateTime.Today.AddDays(-1)) return "Yesterday";
        return d.ToString("ddd, d MMM");
    }
}

public class User
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public string Email { get; set; }
    public string FullName { get; set; }
    public string PasswordHash { get; set; }
    public string Salt { get; set; }
    public DateTime CreatedAt { get; set; }

    public string Sex { get; set; } = "";   // "Male" or "Female"; empty for accounts created before this field existed
    public int Age { get; set; }
    public double HeightCm { get; set; }
    public double WeightKg { get; set; }
    public double StartWeightKg { get; set; }
    public double TargetWeightKg { get; set; }
    public string Experience { get; set; } = "Beginner";
    public string Goal { get; set; } = "General Fitness";
    public string Equipment { get; set; } = "Bodyweight,Dumbbell,Barbell,Machine,Cable";

    public int CalorieTarget { get; set; }
    public int ProteinTarget { get; set; }
    public int CarbTarget { get; set; }
    public int FatTarget { get; set; }
    public int WaterTargetMl { get; set; } = 2500;
    public int WeeklyWorkoutGoal { get; set; } = 3;

    public bool NotificationsEnabled { get; set; } = true;
    public bool ReminderEnabled { get; set; }
    public string ReminderTime { get; set; } = "18:00";
    public bool ProfileComplete { get; set; }

    [Ignore] public double Bmi => HeightCm > 0 ? WeightKg / Math.Pow(HeightCm / 100.0, 2) : 0;
    [Ignore] public List<string> EquipmentList => (Equipment ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
}

public class Exercise
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Name { get; set; }
    public string MuscleGroup { get; set; }
    public string Equipment { get; set; }
    public bool IsCompound { get; set; }
    public string Instructions { get; set; } // steps separated by '|'

    [Ignore] public string[] Steps => (Instructions ?? "").Split('|', StringSplitOptions.RemoveEmptyEntries);
    [Ignore] public string Icon => "mg_" + (MuscleGroup ?? "core").ToLowerInvariant() + ".png";
}

public class WorkoutPlan
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int UserId { get; set; }
    public string Name { get; set; }
    public string Days { get; set; } = "";   // e.g. "Mon,Wed,Fri"
    public DateTime CreatedAt { get; set; }

    [Ignore] public List<string> DayList => (Days ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
    public bool HasDay(DayOfWeek d) => DayList.Contains(Dates.Short(d));
}

public class PlanExercise
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int PlanId { get; set; }
    public int ExerciseId { get; set; }
    public int Sets { get; set; } = 3;
    public int Reps { get; set; } = 10;
    public int RestSeconds { get; set; } = 90;
    public int OrderIndex { get; set; }
}

/// <summary>A plan row joined with its exercise (not a table).</summary>
public class PlanItem
{
    public PlanExercise Pe { get; set; }
    public Exercise Ex { get; set; }
}

public class WorkoutSession
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int UserId { get; set; }
    public int PlanId { get; set; }
    public string PlanName { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int DurationSeconds { get; set; }
    public bool Completed { get; set; }
    public double TotalVolume { get; set; }
}

public class SetLog
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int SessionId { get; set; }
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; }
    public int SetNumber { get; set; }
    public int TargetReps { get; set; }
    public int RestSeconds { get; set; }
    public int Reps { get; set; }
    public double WeightKg { get; set; }
    public bool Completed { get; set; }
}

public class FoodEntry
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int UserId { get; set; }
    public DateTime Date { get; set; }
    public string Meal { get; set; } // Breakfast, Lunch, Dinner, Snacks
    public string Name { get; set; }
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double Carbs { get; set; }
    public double Fat { get; set; }
}

public class WaterLog
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int UserId { get; set; }
    public DateTime Date { get; set; }
    public int Ml { get; set; }
}

/// <summary>Body weight and tape measurements share one table (Type = Weight, Chest, Waist...).</summary>
public class BodyRecord
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int UserId { get; set; }
    public DateTime Date { get; set; }
    public string Type { get; set; }
    public double Value { get; set; }
}

public class RestDay
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int UserId { get; set; }
    public DateTime Date { get; set; }
}
