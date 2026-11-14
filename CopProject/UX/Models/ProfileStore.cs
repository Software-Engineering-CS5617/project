using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace UX.Models;

// The User ID and picture a person chose. Saved on this computer so they are remembered next time.
public class SavedProfile
{
    public string UserId { get; set; } = "";
    public string PicturePath { get; set; } = "";
}
public static class ProfileStore
{
    private static readonly string s_folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SecureMeet");
    private static readonly string s_file = Path.Combine(s_folder, "profiles.json");

    static Dictionary<string, SavedProfile> ReadAll()
    {
        try
        {
            if (File.Exists(s_file))
            {
                return JsonSerializer.Deserialize<Dictionary<string, SavedProfile>>(File.ReadAllText(s_file)) ?? new();
            }
        }
        catch { }
        return new();
    }

    public static SavedProfile? Load(string email)
    {
        ReadAll().TryGetValue(email.ToLowerInvariant(), out SavedProfile? profile);
        return profile;
    }

    public static void Save(string email, string userId, string picturePath)
    {
        Directory.CreateDirectory(s_folder);
        Dictionary<string, SavedProfile> all = ReadAll();
        all[email.ToLowerInvariant()] = new SavedProfile { UserId = userId, PicturePath = picturePath };
        File.WriteAllText(s_file, JsonSerializer.Serialize(all));
    }
    public static string CopyPicture(string sourcePath)
    {
        string pics = Path.Combine(s_folder, "pics");
        Directory.CreateDirectory(pics);
        string dest = Path.Combine(pics, Guid.NewGuid() + Path.GetExtension(sourcePath));
        File.Copy(sourcePath, dest, true);
        return dest;
    }
}
