using System;
using UnityEngine;

namespace CookieJarTools.CrumbTrail.Editor
{
	[Serializable]
	public class ToDoTask
	{
		[SerializeField] 
		private string text = "New Task";
		[SerializeField] 
		private string description = "";
		[SerializeField]
		private bool done = false;
		[SerializeField] 
		private TaskPriority priority = TaskPriority.Medium;
		
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
	}
}