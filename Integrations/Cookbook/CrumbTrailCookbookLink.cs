using CookieJarTools.Cookbook.Editor.Window;
using CookieJarTools.CrumbTrail.Editor;
using UnityEditor;

namespace CookieJarTools.CrumbTrail.Integrations.Cookbook
{
	/// <summary>
	/// Makes a [[Page]] link in a task note open that page in the CookBook. This assembly is only
	/// built when the CookBook is in the project, so CrumbTrail works just as well without it.
	/// </summary>
	[InitializeOnLoad]
	public static class CrumbTrailCookbookLink
	{
		static CrumbTrailCookbookLink()
		{
			CookbookBridge.OpenPage = page => CookbookWindow.OpenPage(page);
		}
	}
}
