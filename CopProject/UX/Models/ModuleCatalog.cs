using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SecureMeet.Shared.Modules;

namespace UX.Models;

public sealed class ModuleCatalog
{
    readonly List<IAppModule> modules = new();

    public ModuleCatalog(string folder)
    {
        if (Directory.Exists(folder))
        {
            foreach (string dll in Directory.GetFiles(folder, "*.dll"))
                LoadModules(dll);
        }

        MeetingService = modules.OfType<IMeetingService>().FirstOrDefault() ?? new PlaceholderMeetingService();
    }

    public IMeetingService MeetingService { get; }

    void LoadModules(string dll)
    {
        try
        {
            Assembly assembly = Assembly.LoadFrom(dll);
            if (assembly == typeof(IAppModule).Assembly) return;

            foreach (Type type in assembly.GetTypes())
            {
                bool isModule = typeof(IAppModule).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract;
                if (isModule && type.GetConstructor(Type.EmptyTypes) != null)
                    modules.Add((IAppModule)Activator.CreateInstance(type)!);
            }
        }
        catch (Exception) { }
    }

    public void Initialize(IMeetingSession session)
    {
        foreach (IAppModule module in modules)
        {
            try { module.Initialize(session); }
            catch (Exception) { }
        }
    }

    public object ViewFor(ModuleSlot slot)
    {
        var module = modules.OfType<IMeetingModule>().FirstOrDefault(m => m.Slot == slot);
        return module?.View ?? slot + " module goes here";
    }
}

public sealed class PlaceholderMeetingService : IMeetingService
{
    public void Initialize(IMeetingSession session) { }
    public (string Id, string Port) CreateMeeting() => ("123-456-789", "5000");
}
