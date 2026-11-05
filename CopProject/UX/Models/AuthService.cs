using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;
using SecureMeet.Shared.Models;

namespace UX.Models;

public sealed class AuthService
{
    public const string AllowedDomain = "@smail.iitpkd.ac.in";

    readonly string file;
    readonly List<UserProfile> users = new();

    public AuthService(string folder)
    {
        file = Path.Combine(folder, "users.json");
        try
        {
            if (File.Exists(file))
                users = JsonSerializer.Deserialize<List<UserProfile>>(File.ReadAllText(file)) ?? new();
        }
        catch (Exception) { }
    }

    public IReadOnlyList<UserProfile> Users => users.ToList();

    public UserProfile? LastUser => users.LastOrDefault();

    public UserProfile? Find(string email) => users.FirstOrDefault(u => u.Email == email);

    public UserProfile? SignIn(string email, string userId, out string error)
    {
        error = "";
        bool taken = users.Any(u => u.Email != email && u.UserId.ToLower() == userId.ToLower());
        if (taken)
        {
            error = "This User ID is already used by someone else.";
            return null;
        }

        UserProfile? old = Find(email);
        var user = new UserProfile { UserId = userId, Email = email, HasProfilePicture = old?.HasProfilePicture ?? false };
        if (old != null) users.Remove(old);
        users.Add(user);
        Save();
        return user;
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLower();

    public static bool IsValidEmail(string email)
    {
        if (!email.EndsWith(AllowedDomain)) return false;
        try { return new MailAddress(email).Address == email; }
        catch (FormatException) { return false; }
    }

    public static string UserIdProblem(string userId)
    {
        if (userId.Length < 2) return "User ID must be at least 2 characters.";
        if (userId.Length > 30) return "User ID can be at most 30 characters.";
        if (!Regex.IsMatch(userId, "^[A-Za-z0-9._-]+$")) return "User ID can only have letters, numbers, dot, dash and underscore.";
        return "";
    }

    void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, JsonSerializer.Serialize(users));
        }
        catch (Exception) { }
    }
}
