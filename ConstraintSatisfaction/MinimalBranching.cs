using System;
using System.Collections.Generic;
using System.Linq;
namespace Andrei15193.ConstraintSatisfaction
{
	public class MinimalBranching<TName, TValue>
		: IConstraintSatisfactionSearch<TName, TValue>
	{
		public MinimalBranching(IConstraintSatisfactionSearch<TName, TValue> constraintSatisfactionSearch)
		{
			if (constraintSatisfactionSearch == null)
				throw new ArgumentNullException("constraintSatisfactionSearch");

			_constraintSatisfactionSearch = constraintSatisfactionSearch;
		}

		#region IConstraintSatisfactionSearch<TName,TValue> Members
		public bool IsApproximative
		{
			get
			{
				return _constraintSatisfactionSearch.IsApproximative;
			}
		}
		public IEnumerable<IReadOnlyDictionary<TName, TValue>> Find(IEnumerable<KeyValuePair<TName, IEnumerable<TValue>>> domains, Constraint<IVariables<IVariable<TName, TValue>, IVariable<TName, TValue>>> constraint)
		{
			return _constraintSatisfactionSearch.Find((from domain in domains
													   let domainValues = domain.Value.ToList()
													   orderby domainValues.Count
													   select domain).ToList(), constraint);
		}
		#endregion

		private readonly IConstraintSatisfactionSearch<TName, TValue> _constraintSatisfactionSearch;
	}
}