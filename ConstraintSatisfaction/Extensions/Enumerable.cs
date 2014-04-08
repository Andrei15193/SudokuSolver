using System.Collections.Generic;
namespace Andrei15193.ConstraintSatisfaction.Extensions
{
	public static class Enumerable
	{
		public static Queue<T> ToQueue<T>(this IEnumerable<T> enumerable)
		{
			return new Queue<T>(enumerable);
		}
		public static Stack<T> ToStack<T>(this IEnumerable<T> enumerable)
		{
			return new Stack<T>(enumerable);
		}
	}
}