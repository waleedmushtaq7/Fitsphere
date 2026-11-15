using System.Globalization;
using System.Text.RegularExpressions;
using FitSphere.Data;
using FitSphere.Models;

namespace FitSphere.Services;

/// <summary>Tiny composition root: one instance of each service for the whole app.</summary>
public static class Svc
{
    public static readonly AppDatabase Db = new();
    public static readonly AuthService Auth = new(Db);
    public static readonly ExerciseService Exercises = new(Db);
    public static readonly WorkoutService Workouts = new(Db);
    public static readonly NutritionService Nutrition = new(Db);
    public static readonly HabitService Habits = new(Db);
    public static readonly ProgressService Progress = new(Db);
    public static readonly InsightEngine Insights = new(Db);
    public static readonly ReminderService Reminders = new();

    public static User User => Auth.Current;
    public static int Uid => Auth.Current.Id;
}

public record OpResult(bool Ok, string Error = null)
{
    public static OpResult Success() => new(true);
    public static OpResult Fail(string error) => new(false, error);
}

public static class Num
{
    public static bool TryParse(string s, out double v)
    {
        v = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;
        return double.TryParse(s.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out v);
    }

    public static string F(double v, string fmt = "0.#") => v.ToString(fmt, CultureInfo.InvariantCulture);
}

/// <summary>Input validation helpers. Each returns an error message, or null when the value is valid.</summary>
public static class Check
{
    static readonly Regex EmailRx = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static bool IsEmail(string s) => !string.IsNullOrWhiteSpace(s) && EmailRx.IsMatch(s.Trim());

    public static string Range(string text, double min, double max, string name, out double v, bool required = true)
    {
        v = 0;
        if (string.IsNullOrWhiteSpace(text)) return required ? $"{name} is required" : null;
        if (!Num.TryParse(text, out v)) return $"{name} must be a number";
        if (v < min || v > max) return $"{name} must be between {Num.F(min)} and {Num.F(max)}";
        return null;
    }
}
