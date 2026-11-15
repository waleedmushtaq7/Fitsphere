using System.Security.Cryptography;
using System.Text;
using FitSphere.Data;
using FitSphere.Models;

namespace FitSphere.Services;

/// <summary>Local (on-device) registration, login and account management.</summary>
public class AuthService
{
    readonly AppDatabase _db;
    public User Current { get; private set; }

    public AuthService(AppDatabase db) => _db = db;

    static string Hash(string password, byte[] salt) =>
        Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32));

    public async Task<bool> RestoreAsync()
    {
        var id = Preferences.Default.Get("uid", 0);
        if (id == 0) return false;
        Current = await _db.Get<User>(id);
        return Current != null;
    }

    public async Task<OpResult> RegisterAsync(string name, string email, string password)
    {
        name = name?.Trim();
        email = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(name)) return OpResult.Fail("Please enter your name.");
        if (!Check.IsEmail(email)) return OpResult.Fail("Please enter a valid email address.");
        if ((password ?? "").Length < 6) return OpResult.Fail("Password must be at least 6 characters.");

        var existing = await _db.Query<User>("SELECT * FROM User WHERE Email = ?", email);
        if (existing.Count > 0) return OpResult.Fail("An account with this email already exists.");

        var salt = RandomNumberGenerator.GetBytes(16);
        var user = new User
        {
            FullName = name,
            Email = email,
            Salt = Convert.ToBase64String(salt),
            PasswordHash = Hash(password, salt),
            CreatedAt = DateTime.Now
        };
        await _db.Save(user);
        SignIn(user);
        return OpResult.Success();
    }

    public async Task<OpResult> LoginAsync(string email, string password)
    {
        email = email?.Trim().ToLowerInvariant();
        var rows = await _db.Query<User>("SELECT * FROM User WHERE Email = ?", email ?? "");
        var user = rows.FirstOrDefault();
        if (user == null || Hash(password ?? "", Convert.FromBase64String(user.Salt)) != user.PasswordHash)
            return OpResult.Fail("Incorrect email or password.");
        SignIn(user);
        return OpResult.Success();
    }

    void SignIn(User user)
    {
        Current = user;
        Preferences.Default.Set("uid", user.Id);
    }

    public async Task LogoutAsync()
    {
        await Svc.Reminders.CancelAllAsync();
        Preferences.Default.Remove("uid");
        Current = null;
    }

    public Task SaveUserAsync() => _db.Save(Current);

    public async Task<OpResult> ChangePasswordAsync(string oldPw, string newPw)
    {
        if (Hash(oldPw ?? "", Convert.FromBase64String(Current.Salt)) != Current.PasswordHash)
            return OpResult.Fail("Your current password is incorrect.");
        if ((newPw ?? "").Length < 6) return OpResult.Fail("New password must be at least 6 characters.");
        var salt = RandomNumberGenerator.GetBytes(16);
        Current.Salt = Convert.ToBase64String(salt);
        Current.PasswordHash = Hash(newPw, salt);
        await _db.Save(Current);
        return OpResult.Success();
    }

    public async Task DeleteAccountAsync()
    {
        var id = Current.Id;
        await _db.Exec("DELETE FROM SetLog WHERE SessionId IN (SELECT Id FROM WorkoutSession WHERE UserId = ?)", id);
        await _db.Exec("DELETE FROM PlanExercise WHERE PlanId IN (SELECT Id FROM WorkoutPlan WHERE UserId = ?)", id);
        foreach (var t in new[] { "WorkoutSession", "WorkoutPlan", "FoodEntry", "WaterLog", "BodyRecord", "RestDay" })
            await _db.Exec($"DELETE FROM {t} WHERE UserId = ?", id);
        await _db.Exec("DELETE FROM User WHERE Id = ?", id);
        await LogoutAsync();
    }
}
