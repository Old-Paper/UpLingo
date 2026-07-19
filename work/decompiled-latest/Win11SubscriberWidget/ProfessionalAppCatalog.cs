using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace Win11SubscriberWidget;

internal sealed class ProfessionalAppDefinition
{
	public string Id;

	public string Label;

	public Color Accent;

	public string[] ProcessNames;
}

internal sealed class ProfessionalActivitySnapshot
{
	public HashSet<string> RunningIds { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	public HashSet<string> ActiveIds { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}

internal static class ProfessionalAppCatalog
{
	private static readonly List<ProfessionalAppDefinition> Apps = new List<ProfessionalAppDefinition>
	{
		Create("obs", "OBS Studio", Theme.BiliAccent, "obs64", "obs32", "obs"),
		Create("premiere", "Premiere Pro", Color.FromArgb(167, 139, 250), "Adobe Premiere Pro", "Premiere Pro"),
		Create("after_effects", "After Effects", Color.FromArgb(129, 140, 248), "AfterFX"),
		Create("photoshop", "Photoshop", Color.FromArgb(56, 189, 248), "Photoshop"),
		Create("davinci", "DaVinci Resolve", Color.FromArgb(251, 146, 60), "Resolve"),
		Create("capcut", "剪映 / CapCut", Color.FromArgb(45, 212, 191), "JianyingPro", "Jianying", "CapCut", "CapCutPro"),
		Create("audition", "Adobe Audition", Color.FromArgb(103, 232, 249), "Adobe Audition", "Audition"),
		Create("fl_studio", "FL Studio", Color.FromArgb(163, 230, 53), "FL64", "FL", "FLStudio"),
		Create("blender", "Blender", Color.FromArgb(249, 115, 22), "blender"),
		Create("cinema4d", "Cinema 4D", Color.FromArgb(96, 165, 250), "Cinema 4D", "c4d"),
		Create("ableton", "Ableton Live", Color.FromArgb(250, 204, 21), "Ableton Live", "Ableton Live 10", "Ableton Live 11", "Ableton Live 12"),
		Create("reaper", "REAPER", Color.FromArgb(74, 222, 128), "reaper"),
		Create("studio_one", "Studio One", Color.FromArgb(244, 114, 182), "Studio One"),
		Create("cubase", "Cubase", Color.FromArgb(192, 132, 252), "Cubase", "Cubase12", "Cubase13"),
		Create("unity", "Unity", Color.FromArgb(203, 213, 225), "Unity"),
		Create("unreal", "Unreal Engine", Color.FromArgb(148, 163, 184), "UnrealEditor", "UE4Editor", "UE5Editor"),
		Create("clip_studio", "Clip Studio Paint", Color.FromArgb(236, 72, 153), "CLIPStudioPaint")
	};

	private static readonly Dictionary<string, ProfessionalAppDefinition> AppsById = BuildIdMap();

	private static readonly Dictionary<string, ProfessionalAppDefinition> AppsByProcessName = BuildProcessMap();

	public static IReadOnlyList<ProfessionalAppDefinition> All => Apps;

	public static ProfessionalAppDefinition Find(string appId)
	{
		if (!string.IsNullOrEmpty(appId) && AppsById.TryGetValue(appId, out var value))
		{
			return value;
		}
		return null;
	}

	public static ProfessionalActivitySnapshot DetectActivity()
	{
		ProfessionalActivitySnapshot professionalActivitySnapshot = new ProfessionalActivitySnapshot();
		int foregroundProcessId = NativeMethods.ForegroundProcessId();
		bool flag = NativeMethods.HasRecentUserInput(300);
		Process[] processes = Process.GetProcesses();
		foreach (Process process in processes)
		{
			try
			{
				if (AppsByProcessName.TryGetValue(process.ProcessName, out var value))
				{
					professionalActivitySnapshot.RunningIds.Add(value.Id);
					if (flag && process.Id == foregroundProcessId)
					{
						professionalActivitySnapshot.ActiveIds.Add(value.Id);
					}
				}
			}
			catch
			{
			}
			finally
			{
				process.Dispose();
			}
		}
		return professionalActivitySnapshot;
	}

	public static HashSet<string> DetectRunningIds()
	{
		return DetectActivity().RunningIds;
	}

	private static ProfessionalAppDefinition Create(string id, string label, Color accent, params string[] processNames)
	{
		return new ProfessionalAppDefinition
		{
			Id = id,
			Label = label,
			Accent = accent,
			ProcessNames = processNames
		};
	}

	private static Dictionary<string, ProfessionalAppDefinition> BuildIdMap()
	{
		Dictionary<string, ProfessionalAppDefinition> dictionary = new Dictionary<string, ProfessionalAppDefinition>(StringComparer.OrdinalIgnoreCase);
		foreach (ProfessionalAppDefinition app in Apps)
		{
			dictionary[app.Id] = app;
		}
		return dictionary;
	}

	private static Dictionary<string, ProfessionalAppDefinition> BuildProcessMap()
	{
		Dictionary<string, ProfessionalAppDefinition> dictionary = new Dictionary<string, ProfessionalAppDefinition>(StringComparer.OrdinalIgnoreCase);
		foreach (ProfessionalAppDefinition app in Apps)
		{
			foreach (string processName in app.ProcessNames)
			{
				dictionary[processName] = app;
			}
		}
		return dictionary;
	}
}
