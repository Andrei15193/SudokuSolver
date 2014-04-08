using System;
using System.Collections.Generic;
namespace Andrei15193.ConstraintSatisfaction.Extensions
{
	public static class Object
	{
		public static Queue<T> CreateQueue<T>(this T instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			Queue<T> queue = new Queue<T>();
			queue.Enqueue(instance);
			return queue;
		}
		public static Stack<T> CreateStack<T>(this T instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			Stack<T> stack = new Stack<T>();
			stack.Push(instance);
			return stack;
		}
	}
}