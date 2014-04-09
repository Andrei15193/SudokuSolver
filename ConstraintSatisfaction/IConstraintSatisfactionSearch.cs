using System.Collections.Generic;
namespace Andrei15193.ConstraintSatisfaction
{
	public interface IConstraintSatisfactionSearch<TName, TValue>
	{
		bool IsApproximative
		{
			get;
		}
		IEnumerable<IReadOnlyDictionary<TName, TValue>> Find(IEnumerable<KeyValuePair<TName, IEnumerable<TValue>>> domains, Constraint<IVariables<IVariable<TName, TValue>, IVariable<TName, TValue>>> constraint);
	}
}