using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookieJarTools.CrumbTrail.Editor
{
	/// <summary>A named group of tasks, shown as one tab.</summary>
	[Serializable]
	public class ToDoList
	{
		[SerializeField] private string name = "New list";
		[SerializeField] private string description = string.Empty;
		[SerializeField] private List<ToDoTask> tasks = new List<ToDoTask>();

		public string Name
		{
			get => name;
			set => name = value;
		}

		public string Description
		{
			get => description;
			set => description = value;
		}

		public List<ToDoTask> Tasks => tasks;

		/// <summary>How many are still to do, which is the number worth showing on the tab.</summary>
		public int Remaining
		{
			get
			{
				var count = 0;
				foreach (var task in tasks)
				{
					if (!task.Done)
					{
						count++;
					}
				}

				return count;
			}
		}
	}
}
