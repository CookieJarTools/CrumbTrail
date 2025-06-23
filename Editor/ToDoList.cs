using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookieJarTools.CrumbTrail.Editor
{
	[Serializable]
	public class ToDoList
	{
		[SerializeField] 
		private string name = "New List";
		[SerializeField] 
		private string description = "";
		[SerializeField] 
		private List<ToDoTask> tasks = new();

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
	}
}