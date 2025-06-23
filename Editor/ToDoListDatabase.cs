using System.Collections.Generic;
using UnityEngine;

namespace CookieJarTools.CrumbTrail.Editor
{
	public class ToDoListDatabase : ScriptableObject
	{
		[SerializeField]
		private List<ToDoList> lists = new();
		[SerializeField]
		private bool compactMode;

		public List<ToDoList> Lists => lists;
		
		public bool CompactMode
		{
			get => compactMode;
			set => compactMode = value;
		}
	}
}