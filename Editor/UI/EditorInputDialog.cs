using UnityEditor;
using UnityEngine;

namespace CookieJarTools.CrumbTrail.Editor
{
	/// <summary>
	/// Asks for one line of text and waits for the answer.
	/// <para>
	/// Unity has a dialog for yes and no but none for a word, and a whole window for renaming a
	/// list is more than the job needs.
	/// </para>
	/// </summary>
	internal class EditorInputDialog : EditorWindow
	{
		private const float Width = 300f;
		private const float Height = 96f;

		private static string answer;
		private static bool answered;

		private string value = string.Empty;
		private string heading = string.Empty;
		private bool focused;

		/// <summary>Shows the dialog and returns what was typed, or null if it was cancelled.</summary>
		public static string Ask(string heading, string value)
		{
			answer = null;
			answered = false;

			var window = CreateInstance<EditorInputDialog>();
			window.heading = heading;
			window.value = value ?? string.Empty;
			window.position = new Rect(
				Screen.currentResolution.width / 2f - Width / 2f,
				Screen.currentResolution.height / 2f - Height / 2f,
				Width,
				Height);
			window.ShowModalUtility();
			return answered ? answer : null;
		}

		private void OnGUI()
		{
			GUILayout.Space(8f);
			GUILayout.Label(heading, EditorStyles.boldLabel);

			GUI.SetNextControlName("CrumbTrailInput");
			value = EditorGUILayout.TextField(value);
			if (!focused)
			{
				// Focusing before the field has been drawn once does nothing.
				GUI.FocusControl("CrumbTrailInput");
				focused = true;
			}

			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
			{
				Accept();
			}

			GUILayout.FlexibleSpace();
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Cancel", GUILayout.Width(70f)))
				{
					Close();
				}

				if (GUILayout.Button("Rename", GUILayout.Width(70f)))
				{
					Accept();
				}
			}

			GUILayout.Space(6f);
		}

		private void Accept()
		{
			answer = value;
			answered = true;
			Close();
		}
	}
}
