using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace Andrei15193.ConstraintSatisfaction
{
	public class ForwardCheckingConstraintSatisfactionSearch<TName, TValue>
		: IConstraintSatisfactionSearch<TName, TValue>
	{
		#region IConstraintSatisfactionSearch<TName,TValue> Members
		public IEnumerable<IReadOnlyDictionary<TName, TValue>> Find(IEnumerable<KeyValuePair<TName, DomainSpecification<TValue>>> domains, Constraint<IVariables<IVariable<TName, TValue>, IVariable<TName, TValue>>> constraint)
		{
			if (domains == null)
				throw new ArgumentNullException("domains");
			if (constraint == null)
				throw new ArgumentNullException("constraint");

			int toCheckIndex = 1;
			var variables = (from domain in domains
							 orderby domain.Value.Values.Count()
							 select new Variable(domain)
							).ToList();
			bool hasValues = variables.Aggregate((variables.Count > 0), (hasValue, variable) => (hasValue && variable.MoveNext()));
			Predicate<int> moveNext = (moveNextIndex) =>
				{
					while (moveNextIndex > -1 && !variables[moveNextIndex].MoveNext())
					{
						variables[moveNextIndex].Reset();
						variables[moveNextIndex].MoveNext();
						moveNextIndex--;
					}

					toCheckIndex = (moveNextIndex == 0 ? 1 : moveNextIndex);
					return (moveNextIndex != -1);
				};

			while (hasValues)
				if (variables.Take(toCheckIndex).All(variable => constraint.IsSatisfiedBy(Variables.Create(variable.Current, variables[toCheckIndex].Current))))
					if (toCheckIndex == variables.Count - 1)
					{
						yield return variables.ToDictionary(variable => variable.Current.Name, variable => variable.Current.Value);
						hasValues = moveNext(variables.Count - 1);
					}
					else
						toCheckIndex++;
				else
					hasValues = moveNext(toCheckIndex);
		}
		#endregion

		private sealed class Variable
			: IEnumerator<IVariable<TName, TValue>>
		{
			public Variable(TName name, DomainSpecification<TValue> domainSpecification)
			{
				_instance = new Variable<TName, TValue>(name);
				_domainSpecification = domainSpecification;
				Reset();
			}
			public Variable(KeyValuePair<TName, DomainSpecification<TValue>> keyValuePair)
				: this(keyValuePair.Key, keyValuePair.Value)
			{
			}

			#region IEnumerator<IVariable<TName,TValue>> Members
			public IVariable<TName, TValue> Current
			{
				get
				{
					return _instance;
				}
			}
			#endregion
			#region IDisposable Members
			void IDisposable.Dispose()
			{
			}
			#endregion
			#region IEnumerator Members
			object IEnumerator.Current
			{
				get
				{
					return Current;
				}
			}
			public bool MoveNext()
			{
				if (_value.MoveNext())
				{
					_instance.Value = _value.Current;
					return true;
				}
				else
					return false;
			}
			public void Reset()
			{
				_value = _domainSpecification.Values.GetEnumerator();
			}
			#endregion

			private Variable<TName, TValue> _instance;
			private IEnumerator<TValue> _value;
			private readonly DomainSpecification<TValue> _domainSpecification;
		}
	}
}