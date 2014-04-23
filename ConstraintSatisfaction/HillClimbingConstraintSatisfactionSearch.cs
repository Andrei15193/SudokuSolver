using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Andrei15193.ConstraintSatisfaction
{
	public class HillClimbingConstraintSatisfactionSearch<TName, TValue>
		: IConstraintSatisfactionSearch<TName, TValue>
	{
		public HillClimbingConstraintSatisfactionSearch(int stepCount = 1000)
		{
			if (stepCount <= 0)
				throw new ArgumentException("Must be strictly positive!", "stepCount");

			_stepCount = stepCount;
		}

		#region IConstraintSatisfactionSearch<TName,TValue> Members
		public bool IsApproximative
		{
			get
			{
				return true;
			}
		}
		public IEnumerable<IReadOnlyDictionary<TName, TValue>> Find(IEnumerable<KeyValuePair<TName, IEnumerable<TValue>>> domains, Constraint<IVariables<IVariable<TName, TValue>, IVariable<TName, TValue>>> constraint)
		{
			IReadOnlyList<Variable> variables = domains.Select(domain => new Variable(domain)).ToList();

			if (!variables.All(variable => variable.HasValue))
				return new IReadOnlyDictionary<TName, TValue>[0];

			using (IEnumerator<int> variableIndex = variables.Select((variable, index) => new
																						  {
																							  Variable = variable,
																							  Index = index
																						  })
															 .Where(indexedVariable => indexedVariable.Variable.HasNextValue)
															 .Select(variable => variable.Index)
															 .ToList()
															 .GetEnumerator())
			{
				if (!variableIndex.MoveNext())
					return new IReadOnlyDictionary<TName, TValue>[0];

				int stepNumber = 0;
				while (stepNumber < _stepCount)
				{
					IEnumerable<Variable> otherVariabes = variables.Take(variableIndex.Current)
																   .Skip(1)
																   .Take(variables.Count);

					if (otherVariabes.Count(variable => constraint.IsSatisfiedBy(Variables.Create(variable, variables[variableIndex.Current])))
						< otherVariabes.Count(variable => constraint.IsSatisfiedBy(Variables.Create(variable, new Variable<TName, TValue>(variables[variableIndex.Current].Name, variables[variableIndex.Current].NextValue)))))
						variables[variableIndex.Current].MoveNext();

					if (!variableIndex.MoveNext())
					{
						variableIndex.Reset();
						variableIndex.MoveNext();
						stepNumber++;
					}
				}
			}

			return new[] { variables.ToDictionary(variable => variable.Name, variable => variable.Value) };
		}
		#endregion

		private readonly int _stepCount;

		private class Variable
			: IVariable<TName, TValue>
		{
			public Variable(TName name, IEnumerable<TValue> values)
			{
				if (name == null)
					throw new ArgumentNullException("name");
				if (values == null)
					throw new ArgumentNullException("values");

				_name = name;
				_values = values;
				_hasValue = _TrySetValue();
				_hasNextValue = _TrySetNextValue();
			}
			public Variable(KeyValuePair<TName, IEnumerable<TValue>> domain)
				: this(domain.Key, domain.Value)
			{
			}

			#region IVariable Members
			object IVariable.Name
			{
				get
				{
					return Name;
				}
			}
			object IVariable.Value
			{
				get
				{
					return Value;
				}
			}
			#endregion
			public TName Name
			{
				get
				{
					return _name;
				}
			}
			public TValue Value
			{
				get
				{
					return _value;
				}
			}
			public TValue NextValue
			{
				get
				{
					return _nextValue.Current;
				}
			}
			public bool HasValue
			{
				get
				{
					return _hasValue;
				}
			}
			public bool HasNextValue
			{
				get
				{
					return _hasNextValue;
				}
			}
			public void MoveNext()
			{
				if (_hasNextValue)
				{
					_value = _nextValue.Current;

					if (!_nextValue.MoveNext())
						_nextValue = _values.GetEnumerator();
				}
			}

			private bool _TrySetValue()
			{
				using (IEnumerator<TValue> value = _values.GetEnumerator())
					if (value.MoveNext())
					{
						_value = value.Current;
						return true;
					}

				return false;
			}
			private bool _TrySetNextValue()
			{
				IEnumerable<TValue> valuesExceptFirst = _values.Skip(1);

				if (valuesExceptFirst.Any())
				{
					_nextValue = valuesExceptFirst.GetEnumerator();
					return true;
				}

				return false;
			}

			private TValue _value;
			private IEnumerator<TValue> _nextValue;
			private readonly bool _hasValue;
			private readonly bool _hasNextValue;
			private readonly TName _name;
			private readonly IEnumerable<TValue> _values;
		}
	}
}