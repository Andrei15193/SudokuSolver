using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Andrei15193.ConstraintSatisfaction.Tuples;
namespace Andrei15193.ConstraintSatisfaction
{
	public class MinimumConflictsSearch<TName, TValue>
		: IConstraintSatisfactionSearch<TName, TValue>
	{
		public MinimumConflictsSearch(int numberOfSteps = 1000)
		{
			if (numberOfSteps <= 0)
				throw new ArgumentNullException("Must be strictly positive!", "numberOfSteps");

			_numberOfSteps = numberOfSteps;

			if (typeof(IComparable<TName>).IsAssignableFrom(typeof(TName))
				|| typeof(IComparable).IsAssignableFrom(typeof(TName)))
			{
				_CreateEmptyIndexMap = ((capacity) => new SortedList<TName, IEnumerable<TValue>>(capacity));
				_CreateEmptySolutionMap = ((capacity) => new SortedList<TName, TValue>(capacity));
			}
			else
			{
				_CreateEmptyIndexMap = ((capacity) => new Dictionary<TName, IEnumerable<TValue>>(capacity));
				_CreateEmptySolutionMap = ((capacity) => new Dictionary<TName, TValue>(capacity));
			}
		}
		public MinimumConflictsSearch(IComparer<TName> variableNameComparer, int numberOfSteps = 1000)
		{
			if (numberOfSteps <= 0)
				throw new ArgumentNullException("Must be strictly positive!", "numberOfSteps");
			if (variableNameComparer == null)
				throw new ArgumentNullException("variableNameComparer");

			_numberOfSteps = numberOfSteps;
			_CreateEmptyIndexMap = ((capacity) => new SortedList<TName, IEnumerable<TValue>>(capacity, variableNameComparer));
			_CreateEmptySolutionMap = ((capacity) => new SortedList<TName, TValue>(capacity, variableNameComparer));
		}
		public MinimumConflictsSearch(IEqualityComparer<TName> variableNameComparer, int numberOfSteps = 1000)
		{
			if (numberOfSteps <= 0)
				throw new ArgumentNullException("Must be strictly positive!", "numberOfSteps");
			if (variableNameComparer == null)
				throw new ArgumentNullException("variableNameComparer");

			_numberOfSteps = numberOfSteps;
			_CreateEmptyIndexMap = ((capacity) => new Dictionary<TName, IEnumerable<TValue>>(capacity, variableNameComparer));
			_CreateEmptySolutionMap = ((capacity) => new Dictionary<TName, TValue>(capacity, variableNameComparer));
		}

		#region IConstraintSatisfactionSearch<TName,TVale> Members
		public IEnumerable<IReadOnlyDictionary<TName, TValue>> Find(IEnumerable<KeyValuePair<TName, IEnumerable<TValue>>> domains, IBinaryConstraints<TName, TValue> binaryConstraints, Constraint<IReadOnlyDictionary<TName, TValue>> solutionConstraint, IHeuristic<TName, TValue> heuristic = null)
		{
			if (domains == null)
				throw new ArgumentNullException("domains");
			if (binaryConstraints == null && solutionConstraint == null)
				throw new ArgumentException("binaryConstraints or solutionConstraint must be provided!");

			if (domains.Any(domain => !domain.Value.Any()))
				return new IReadOnlyDictionary<TName, TValue>[0];

			int stepCount = 0, totalConflicts;
			IDictionary<TName, TValue> solution = _CreateEmptySolutionMap(domains.Count());
			IReadOnlyDictionary<TName, TValue> solutionReadOnlyView = new ReadOnlyDictionary<TName, TValue>(solution);

			foreach (KeyValuePair<TName, IEnumerable<TValue>> domain in domains)
				solution.Add(domain.Key, domain.Value.First());
			do
			{
				totalConflicts = 0;

				foreach (KeyValuePair<TName, IEnumerable<TValue>> domain in (heuristic == null ? domains : heuristic.Apply(domains)))
				{
					var minimumConflictedValue = (from domainValue in domain.Value
												  let conflictedDomainValue = new
												  {
													  Value = solution[domain.Key] = domainValue,
													  ConflictCount = ((binaryConstraints == null ? 0 : solution.Count(variable => !binaryConstraints.AreSatisfiedBy(Variables.Create(Variable.Create(domain.Key, domainValue), Variable.Create(variable.Key, variable.Value)))))
																	   + (solutionConstraint == null || solutionConstraint.IsSatisfiedBy(solutionReadOnlyView) ? 0 : 1))
												  }
												  orderby conflictedDomainValue.ConflictCount
												  select conflictedDomainValue
												 ).First();

					totalConflicts += minimumConflictedValue.ConflictCount;
					solution[domain.Key] = minimumConflictedValue.Value;
				}

				stepCount++;
			} while (stepCount < _numberOfSteps && totalConflicts > 0);

			if (totalConflicts == 0)
				return new[] { solutionReadOnlyView };
			else
				return new IReadOnlyDictionary<TName, TValue>[0];
		}
		public IEnumerable<IReadOnlyDictionary<TName, TValue>> Find(IEnumerable<KeyValuePair<TName, IEnumerable<TValue>>> domains, IBinaryConstraints<TName, TValue> binaryConstraints, IHeuristic<TName, TValue> heuristic = null)
		{
			if (binaryConstraints == null)
				throw new ArgumentNullException("binaryConstraints");

			return Find(domains, binaryConstraints, null, heuristic);
		}
		public IEnumerable<IReadOnlyDictionary<TName, TValue>> Find(IEnumerable<KeyValuePair<TName, IEnumerable<TValue>>> domains, Constraint<IReadOnlyDictionary<TName, TValue>> solutionConstraint, IHeuristic<TName, TValue> heuristic = null)
		{
			if (solutionConstraint == null)
				throw new ArgumentNullException("solutionConstraint");

			return Find(domains, null, solutionConstraint, heuristic);
		}
		#endregion

		private IDictionary<TName, IEnumerable<TValue>> _CreateIndex(IEnumerable<KeyValuePair<TName, IEnumerable<TValue>>> domains)
		{
			if (domains == null)
				throw new ArgumentNullException("domains");

			IDictionary<TName, IEnumerable<TValue>> indexedDomains = _CreateEmptyIndexMap(domains.Count());

			foreach (KeyValuePair<TName, IEnumerable<TValue>> domain in domains)
				indexedDomains.Add(domain);

			return indexedDomains;
		}

		private readonly int _numberOfSteps;
		private readonly DomainIndexMapCreator _CreateEmptyIndexMap;
		private readonly EmptySolutionMapCreator _CreateEmptySolutionMap;

		private delegate IDictionary<TName, IEnumerable<TValue>> DomainIndexMapCreator(int capacity);
		private delegate IDictionary<TName, TValue> EmptySolutionMapCreator(int capacity);
	}
}