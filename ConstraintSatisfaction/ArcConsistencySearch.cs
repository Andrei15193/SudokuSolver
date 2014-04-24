using System;
using System.Collections.Generic;
using System.Linq;
using Andrei15193.ConstraintSatisfaction.Tuples;
namespace Andrei15193.ConstraintSatisfaction
{
	public class ArcConsistencySearch<TName, TValue>
		: IConstraintSatisfactionSearch<TName, TValue>
	{
		#region IConstraintSatisfactionSearch<TName,TValue> Members
		public bool IsApproximative
		{
			get
			{
				return false;
			}
		}
		public IEnumerable<IReadOnlyDictionary<TName, TValue>> Find(IEnumerable<KeyValuePair<TName, IEnumerable<TValue>>> domains, IBinaryConstraints<TName, TValue> binaryConstraints, IHeuristic<TName, TValue> heuristic = null)
		{
			if (domains == null)
				throw new ArgumentNullException("domains");
			if (binaryConstraints == null)
				throw new ArgumentNullException("binaryConstraints");

			IDictionary<TName, IEnumerable<TValue>> indexedDomains = domains.ToDictionary(domain => domain.Key, domain => domain.Value);
			IReadOnlyDictionary<TName, ISet<TName>> _inconsistentArcs = domains.ToDictionary(domain => domain.Key, domain => (ISet<TName>)new HashSet<TName>(indexedDomains.Keys));

			while (_inconsistentArcs.Values.Any(adjacentVertices => adjacentVertices.Any()) && indexedDomains.Values.All(domain => domain.Count() > 0))
			{
				TName sourceVertex;
				if (heuristic != null)
					sourceVertex = heuristic.Apply(indexedDomains.Where(indexedDomain => _inconsistentArcs[indexedDomain.Key].Any())).First().Key;
				else
					sourceVertex = indexedDomains.Where(indexedDomain => _inconsistentArcs[indexedDomain.Key].Any()).First().Key;

				IEnumerable<TValue> sourceVertexValues = indexedDomains[sourceVertex];
				IReadOnlyCollection<TValue> invalidSourceVertexValues = _inconsistentArcs[sourceVertex].SelectMany(destinationVertex => sourceVertexValues.Where(sourceVertexValue => indexedDomains[destinationVertex].All(destinationVertexValue => !binaryConstraints.AreSatisfiedBy(Variables.Create(Variable.Create(sourceVertex, sourceVertexValue), Variable.Create(destinationVertex, destinationVertexValue))))))
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

			if (indexedDomains.Values.All(domain => domain.Count() > 0))
			{
				Queue<Dictionary<TName, TValue>> states = new Queue<Dictionary<TName, TValue>>();
				states.Enqueue(new Dictionary<TName, TValue>());

				foreach (KeyValuePair<TName, IEnumerable<TValue>> domain in indexedDomains)
					for (int stateCount = states.Count; stateCount > 0; stateCount--)
					{
						Dictionary<TName, TValue> state = states.Dequeue();
						foreach (TValue domainValue in domain.Value.Where(domainValue => state.All(variable => binaryConstraints.AreSatisfiedBy(Variables.Create(Variable.Create(variable.Key, variable.Value), Variable.Create(domain.Key, domainValue))))))
						{
							Dictionary<TName, TValue> newState = new Dictionary<TName, TValue>(state);
							newState.Add(domain.Key, domainValue);

							states.Enqueue(newState);
						}
					}

				return states;
			}

			return new IReadOnlyDictionary<TName, TValue>[0];
		}
		#endregion
	}
}