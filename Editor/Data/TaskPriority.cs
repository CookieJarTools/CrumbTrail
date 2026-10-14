namespace CookieJarTools.CrumbTrail.Editor
{
	/// <summary>
	/// How much a task matters.
	/// <para>
	/// There is deliberately no All here. It used to carry one so the filter dropdown could reuse
	/// the enum, which meant a task could be saved with a priority that means nothing.
	/// </para>
	/// </summary>
	public enum TaskPriority
	{
		Low,
		Medium,
		High
	}
}
