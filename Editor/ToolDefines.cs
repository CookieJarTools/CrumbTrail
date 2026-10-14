using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace CookieJarTools.CrumbTrail.Editor
{
	/// <summary>
	/// Keeps the define that says the CookBook is in the project, so the part of CrumbTrail that
	/// links to it builds itself in when it is there and vanishes when it is not. CrumbTrail
	/// itself needs none of it.
	/// <para>
	/// Nothing to set up, and nothing to undo: the define is added when the assembly is found,
	/// taken away when it is gone, and only ever written when it actually changes.
	/// </para>
	/// </summary>
	[InitializeOnLoad]
	public static class ToolDefines
	{
		private static readonly (string assembly, string define)[] Tools =
		{
			("CookbookEditor", "COOKIEJAR_COOKBOOK")
		};

		static ToolDefines()
		{
			EditorApplication.delayCall += Check;
		}

		private static void Check()
		{
			var target = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
			var defines = PlayerSettings.GetScriptingDefineSymbols(target).Split(';').Where(d => d.Length > 0).ToList();
			var wanted = defines.ToList();
			foreach (var (assembly, define) in Tools)
			{
				Want(wanted, define, Loaded(assembly));
			}

			if (!wanted.SequenceEqual(defines))
			{
				PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", wanted));
			}
		}

		private static void Want(List<string> defines, string define, bool wanted)
		{
			var has = defines.Contains(define);
			if (wanted && !has)
			{
				defines.Add(define);
			}
			else if (!wanted && has)
			{
				defines.Remove(define);
			}
		}

		private static bool Loaded(string assembly)
		{
			return AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == assembly);
		}
	}
}
