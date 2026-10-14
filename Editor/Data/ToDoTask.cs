using System;
using UnityEngine;

namespace CookieJarTools.CrumbTrail.Editor
{
	/// <summary>One thing to do.</summary>
	[Serializable]
	public class ToDoTask
	{
		[SerializeField] private string text = string.Empty;
		[SerializeField] private string description = string.Empty;
		[SerializeField] private bool done;
		[SerializeField] private TaskPriority priority = TaskPriority.Medium;
		[SerializeField] private UnityEngine.Object link;

		public string Text
		{
			get => text;
			set => text = value;
		}

		public string Description
		{
			get => description;
			set => description = value;
		}

		public bool Done
		{
			get => done;
			set => done = value;
		}

		public TaskPriority Priority
		{
			get => priority;
			set => priority = value;
		}

		/// <summary>
		/// The thing the task is about: a scene object, a prefab, a script, a material.
		/// <para>
		/// This is the one thing a list living in the editor can do that a note elsewhere cannot.
		/// A comment in the code goes stale when the code moves, and a ticket in a tracker cannot
		/// point at a GameObject at all, but this can take you straight to it.
		/// </para>
		/// </summary>
		public UnityEngine.Object Link
		{
			get => link;
			set => link = value;
		}
	}
}
