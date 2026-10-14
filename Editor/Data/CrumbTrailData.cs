using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CookieJarTools.CrumbTrail.Editor
{
	/// <summary>
	/// Every list in the project, kept in ProjectSettings so it travels with the project and does
	/// not leave a loose asset in the root of anything that installs the package.
	/// <para>
	/// Everything that changes a list or a task calls <see cref="Touch"/>. The old window edited
	/// the data and then mostly forgot to say so, which meant Unity had nothing to save and threw
	/// the work away on quit without asking. One method, called from every edit, is the whole fix.
	/// </para>
	/// </summary>
	[FilePath("ProjectSettings/CrumbTrail.asset", FilePathAttribute.Location.ProjectFolder)]
	public class CrumbTrailData : ScriptableSingleton<CrumbTrailData>
	{
		[SerializeField] private List<ToDoList> lists = new List<ToDoList>();
		[SerializeField] private bool compactMode;
		[SerializeField] private bool showCompleted = true;

		private bool waitingToSave;

		public List<ToDoList> Lists => lists;

		public bool CompactMode
		{
			get => compactMode;
			set
			{
				compactMode = value;
				Touch();
			}
		}

		public bool ShowCompleted
		{
			get => showCompleted;
			set
			{
				showCompleted = value;
				Touch();
			}
		}

		/// <summary>
		/// Something changed, so the file needs writing.
		/// <para>
		/// Typing a task name raises this on every keystroke, and writing the file each time would
		/// be wasteful, so the write is folded into one on the next editor tick.
		/// </para>
		/// </summary>
		public void Touch()
		{
			if (waitingToSave)
			{
				return;
			}

			waitingToSave = true;
			EditorApplication.delayCall += Flush;
		}

		/// <summary>Writes it out now, for the moments where waiting a tick is not safe.</summary>
		public void Flush()
		{
			waitingToSave = false;
			Save(true);
		}

		private void OnDisable()
		{
			// Quitting and domain reloads both come through here, so anything typed in the last
			// tick still reaches the file.
			if (waitingToSave)
			{
				Flush();
			}
		}
	}
}
