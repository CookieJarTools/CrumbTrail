using System;

namespace CookieJarTools.CrumbTrail.Editor
{
	/// <summary>
	/// Where the CookBook plugs in, when it is in the project.
	/// <para>
	/// CrumbTrail must work on its own, so it cannot reference the CookBook. Instead the
	/// integration assembly, which is only built when the CookBook is installed, fills this in.
	/// Null means the CookBook is not there, and a page link says so rather than failing quietly.
	/// </para>
	/// </summary>
	public static class CookbookBridge
	{
		/// <summary>Opens a page by name. Null when the CookBook is not in the project.</summary>
		public static Action<string> OpenPage { get; set; }

		public static bool Available => OpenPage != null;
	}
}
