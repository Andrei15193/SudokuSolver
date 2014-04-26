using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Andrei15193.ConstraintSatisfaction.Tuples;
namespace Andrei15193.ConstraintSatisfaction
{
	public class ArcConsistencySearch<TName, TValue>
		: IConstraintSatisfactionSearch<TName, TValue>
	{
		public ArcConsistencySearch()
		{
			if (typeof(IComparable<TName>).IsAssignableFrom(typeof(TName))
				|| typeof(IComparable).IsAssignableFrom(typeof(TName)))
			{
				_CreateEmptyIndexMap = ((capacity) => new SortedList<TName, IEnumerable<TValue>>(capacity));
				_CloneSolution = ((solution) => new SortedList<TName, TValue>(solution));
			}
			else
			{
				_CreateEmptyIndexMap = ((capacity) => new Dictionary<TName, IEnumerable<TValue>>(capacity));
				_CloneSolution = ((solution) => new Dictionary<TName, TValue>(solution));
			}
		}
		public ArcConsistencySearch(IComparer<TName> variableNameComparer)
		{
			if (variableNameComparer == null)
				throw new ArgumentNullException("variableNameComparer");

			_CreateEmptyIndexMap = ((capacity) => new SortedList<TName, IEnumerable<TValue>>(capacity, variableNameComparer));
			_CloneSolution = ((solution) => new SortedList<TName, TValue>(solution, variableNameComparer));
		}
		public ArcConsistencySearch(IEqualityComparer<TName> variableNameComparer)
		{
			if (variableNameComparer == null)
				throw new ArgumentNullException("variableNameComparer");

			_CreateEmptyIndexMap = ((capacity) => new Dictionary<TName, IEnumerable<TValue>>(capacity, variableNameComparer));
			_CloneSolution = ((solution) => new Dictionary<TName, TValue>(solution, variableNameComparer));
		}

		#region IConstraintSatisfactionSearch<TName,TValue> Members
		public IEnumerable<IReadOnlyDictionary<TName, TValue>> Find(IEnumerable<KeyValuePair<TName, IEnumerable<TValue>>> domains, IBinaryConstraints<TName, TValue> binaryConstraints, Constraint<IReadOnlyDictionary<TName, TValue>> solutionConstraint, IHeuristic<TName, TValue> heuristic = null)
		{
			if (domains == null)
				throw new ArgumentNullException("domains");
			if (binaryConstraints == null && solutionConstraint == null)
				throw new ArgumentException("binaryConstraints or solutionConstraint must be provided!");

			IDictionary<TName, IEnumerable<TValue>> indexedDomains = _CreateIndex(domains);
			IReadOnlyDictionary<TName, ISet<TName>> _inconsistentArcs = domains.ToDictionary(domain => domain.Key, domain => (ISet<TName>)new HashSet<TName>(indexedDomains.Keys));

			while (_inconsistentArcs.Values.Any(adjacentVertices => adjacentVertices.Any()) && indexedDomains.Values.All(domain => domain.Count() > 0))
			{
				TName sourceVertex;
				if (heuristic != null)
					sourceVertex = heuristic.Apply(indexedDomains.Where(indexedDomain => _inconsistentArcs[indexedDomain.Key].Any())).First().Key;
				else
					sourceVertex = indexedDomains.Where(indexedDomain => _inconsistentArcs[indexedDomain.Key].Any()).First().Key;

				IEnumerable<TValue> sourceVertexValues = indexedDomains[sourceVertex];
				IReadOnlyCollection<TValue> invalidSourceVertexValues = _inconsistentArcs[sourceVertex].SelectMany(destinationVertex => sourceVertexValues.Where(sourceVertexValue => indexedDomains[destinationVertex].All(destinationVertexValue => binaryConstraints != null && !binaryConstraints.AreSatisfiedBy(Variables.Create(Variable.Create(sourceVertex, sourceVertexValue), Variable.Create(destinationVertex, destinationVertexValue))))))
																									   .Distinct()
																									   .ToList();

				if (invalidSourceVertexValues.Count > 0)
				{
					indexedDomains[sourceVertex] = indexedDomains[sourceVertex].Except(invalidSourceVertexValues).ToList();
					foreach (TName sourceAdjacentVertex in binaryConstraints.AdjacentVertices(sourceVertex))
						_inconsistentArcs[sourceAdjacentVertex].Add(sourceVertex);
				}

				_inconsistentArcs[sourceVertex].Clear();
			}

			return _ExpandToSolutions(new ReadOnlyDictionary<TName, IEnumerable<TValue>>(indexedDomains), binaryConstraints, solutionConstraint);
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

		private IEnumerable<IReadOnlyDictionary<TName, TValue>> _ExpandToSolutions(IReadOnlyDictionary<TName, IEnumerable<TValue>> arcConsistentDomains, IBinaryConstraints<TName, TValue> binaryConstraints, Constraint<IReadOnlyDictionary<TName, TValue>> solutionConstraint)
		{
			Queue<IDictionary<TName, TValue>> solutions = new Queue<IDictionary<TName, TValue>>();

			if (arcConsistentDomains.Values.All(domain => domain.Count() > 0))
			{
				solutions.Enqueue(new Dictionary<TName, TValue>());

				foreach (KeyValuePair<TName, IEnumerable<TValue>> domain in arcConsistentDomains)
					for (int stateCount = solutions.Count; stateCount > 0; stateCount--)
					{
						IDictionary<TName, TValue> partialSolution = solutions.Dequeue();

						foreach (TValue domainValue in domain.Value.Where(domainValue => partialSolution.All(variable => binaryConstraints == null || binaryConstraints.AreSatisfiedBy(Variables.Create(Variable.Create(variable.Key, variable.Value), Variable.Create(domain.Key, domainValue))))))
						{
							IDictionary<TName, TValue> newPartialSolution = _CloneSolution(partialSolution);
							newPartialSolution.Add(domain.Key, domainValue);
							if (solutionConstraint == null || solutionConstraint.IsSatisfiedBy(new ReadOnlyDictionary<TName, TValue>(partialSolution)))
								solutions.Enqueue(newPartialSolution);
						}
					}
			}
			else
				if (arcConsistentDomains.Count == 0)
				{
					Dictionary<TName, TValue> solution = new Dictionary<TName, TValue>();
					if (solutionConstraint == null || solutionConstraint.IsSatisfiedBy(solution))
						solutions.Enqueue(solution);
				}

			return solutions.Select(solution => new ReadOnlyDictionary<TName, TValue>(solution)).ToList();
		}
		private IDictionary<TName, IEnumerable<TValue>> _CreateIndex(IEnumerable<KeyValuePair<TName, IEnumerable<TValue>>> domains)
		{
			if (domains == null)
				throw new ArgumentNullException("domains");

			IDictionary<TName, IEnumerable<TValue>> indexedDomains = _CreateEmptyIndexMap(domains.Count());

			foreach (KeyValuePair<TName, IEnumerable<TValue>> domain in domains)
				indexedDomains.Add(domain);

			return indexedDomains;
		}

		private readonly SolutionMapCloner _CloneSolution;
		private readonly DomainIndexMapCreator _CreateEmptyIndexMap;

		private delegate IDictionary<TName, TValue> SolutionMapCloner(IDictionary<TName, TValue> solution);
		private delegate IDictionary<TName, IEnumerable<TValue>> DomainIndexMapCreator(int capacity);
	}
}