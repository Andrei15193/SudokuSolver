using System;
using System.Collections.Generic;
using System.Linq;
namespace Andrei15193.ConstraintSatisfaction
{
	public class MinimumRemainingValuesHeuristic<TName, TValue>
		: IConstraintSatisfactionSearch<TName, TValue>
	{
		public MinimumRemainingValuesHeuristic(IConstraintSatisfactionSearch<TName, TValue> constraintSatisfactionSearch)
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
			if (domains == null)
				throw new ArgumentNullException("domains");
			if (constraint == null)
				throw new ArgumentNullException("constraint");

			IDictionary<TName, IEnumerable<TValue>> oneValueDomains = new Dictionary<TName, IEnumerable<TValue>>();
			IDictionary<TName, IEnumerable<TValue>> multipleValueDomains = new Dictionary<TName, IEnumerable<TValue>>();

			int valueCount = 0;
			using (IEnumerator<KeyValuePair<TName, IEnumerable<TValue>>> domain = domains.GetEnumerator())
				while (domain.MoveNext() && (valueCount = domain.Current.Value.Count()) > 0)
					if (valueCount == 1)
						oneValueDomains.Add(domain.Current);
					else
						multipleValueDomains.Add(domain.Current);

			if (valueCount == 0)
				return new IReadOnlyDictionary<TName, TValue>[0];

			bool hasChanges;
			do
			{
				hasChanges = false;

				foreach (KeyValuePair<TName, IEnumerable<TValue>> multipleValueDomain in multipleValueDomains.ToList())
				{
					bool hasInvalidValues = false;
					IList<TValue> nonConflictingValues = new List<TValue>();

					foreach (TValue domainValue in multipleValueDomain.Value)
						if (oneValueDomains.All(oneValueDomain => constraint.IsSatisfiedBy(Variables.Create(Variable.Create(multipleValueDomain.Key, domainValue),
																											Variable.Create(oneValueDomain.Key, oneValueDomain.Value.First())))))
							nonConflictingValues.Add(domainValue);
						else
							hasInvalidValues = true;

					if (hasInvalidValues)
					{
						hasChanges = true;
						
						if (nonConflictingValues.Count == 1)
						{
							multipleValueDomains.Remove(multipleValueDomain.Key);
							oneValueDomains.Add(multipleValueDomain.Key, nonConflictingValues);
						}
						else
							multipleValueDomains[multipleValueDomain.Key] = nonConflictingValues;
					}
				}
			}
			while (hasChanges);

			return _constraintSatisfactionSearch.Find(oneValueDomains.Concat(multipleValueDomains), constraint);
		}
		#endregion

		private readonly IConstraintSatisfactionSearch<TName, TValue> _constraintSatisfactionSearch;
	}
}