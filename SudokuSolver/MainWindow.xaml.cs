using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Andrei15193.ConstraintSatisfaction;
namespace SudokuSolver
{
	using System;
	using System.Xml.Linq;
	using Microsoft.Win32;
	using Variables = Andrei15193.ConstraintSatisfaction.IVariables<Andrei15193.ConstraintSatisfaction.IVariable<SudokuPosition, int>, Andrei15193.ConstraintSatisfaction.IVariable<SudokuPosition, int>>;
	public partial class MainWindow
		: Window, INotifyPropertyChanged
	{
		public MainWindow()
		{
			_sudokuMatrix = new int[9][];
			for (int index = 0; index < 9; index++)
				_sudokuMatrix[index] = new int[9];

			_loadCommand = new DelegateCommand(delegate
				{
					OpenFileDialog openFileDialog = new OpenFileDialog
						{
							AddExtension = true,
							CheckFileExists = true,
							Filter = "XML Document (*.xml) | *.xml",
							InitialDirectory = Environment.GetEnvironmentVariable("CSIDL_COMMON_DOCUMENTS")
						};
					if (openFileDialog.ShowDialog(this).GetValueOrDefault())
						try
						{
							XDocument xDocument = XDocument.Load(openFileDialog.FileName);

							_ClearSudokuMap();
							foreach (XElement positionXElement in xDocument.Root.Elements("Position"))
							{
								XAttribute rowXAttribute = positionXElement.Attribute("Row");
								XAttribute columnXAttribute = positionXElement.Attribute("Column");
								XAttribute valueXAttribute = positionXElement.Attribute("Value");

								if (rowXAttribute != null && columnXAttribute != null && valueXAttribute != null)
								{
									int row, column, value;

									if (int.TryParse(rowXAttribute.Value, out row) && int.TryParse(columnXAttribute.Value, out column) && int.TryParse(valueXAttribute.Value, out value)
										&& 0 <= row && row < 9 && 0 <= column && column < 9 && 1 <= value && value <= 9)
										_sudokuMatrix[row][column] = value;
								}
							}

							if (PropertyChanged != null)
								PropertyChanged(this, new PropertyChangedEventArgs("SudokuMatrix"));
						}
						catch
						{
							MessageBox.Show(this, "Could not load sudoku board!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
						}
				});
			_saveCommand = new DelegateCommand(delegate
				{
					SaveFileDialog saveFileDialog = new SaveFileDialog
						{
							AddExtension = true,
							CheckFileExists = false,
							Filter = "XML Document (*.xml) | *.xml",
							InitialDirectory = Environment.GetEnvironmentVariable("CSIDL_COMMON_DOCUMENTS")
						};
					if (saveFileDialog.ShowDialog(this).GetValueOrDefault())
						try
						{
							XDocument xDocument = new XDocument(new XElement("SudokuMap"));

							for (int row = 0; row < 9; row++)
								for (int column = 0; column < 9; column++)
									if (_sudokuMatrix[row][column] != 0)
										xDocument.Root.Add(new XElement("Position",
														   new XAttribute("Row", row.ToString()),
														   new XAttribute("Column", column.ToString()),
														   new XAttribute("Value", _sudokuMatrix[row][column].ToString())));

							xDocument.Save(saveFileDialog.FileName);
						}
						catch
						{
							MessageBox.Show(this, "Could not save sudoku board!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
						}
				});
			_clearCommand = new DelegateCommand(delegate
				{
					_ClearSudokuMap();

					if (PropertyChanged != null)
						PropertyChanged(this, new PropertyChangedEventArgs("SudokuMatrix"));
				});

			_incrementCommand = new DelegateCommand((parameter) =>
				{
					if (parameter != null)
					{
						int[] indexes = new int[2];

						if (parameter.ToString()
									 .Split(new[] { ',' }, 2)
									 .Select((splitResult, splitIndex) => int.TryParse(splitResult, out indexes[splitIndex]))
									 .Aggregate(true, (intParseResult, tryParseResult) => (intParseResult && tryParseResult))
							&& 0 <= indexes[0] && indexes[0] < 9
							&& 0 <= indexes[1] && indexes[1] <= 9)
						{
							_sudokuMatrix[indexes[0]][indexes[1]] = ((_sudokuMatrix[indexes[0]][indexes[1]] + 1) % 10);

							if (PropertyChanged != null)
								PropertyChanged(this, new PropertyChangedEventArgs("SudokuMatrix"));
						}
					}
				});
			_decrementCommand = new DelegateCommand((parameter) =>
				{
					if (parameter != null)
					{
						int[] indexes = new int[2];

						if (parameter.ToString()
									 .Split(new[] { ',' }, 2)
									 .Select((splitResult, splitIndex) => int.TryParse(splitResult, out indexes[splitIndex]))
									 .Aggregate(true, (intParseResult, tryParseResult) => (intParseResult && tryParseResult))
							&& 0 <= indexes[0] && indexes[0] < 9
							&& 0 <= indexes[1] && indexes[1] <= 9)
						{
							if (_sudokuMatrix[indexes[0]][indexes[1]] == 0)
								_sudokuMatrix[indexes[0]][indexes[1]] = 9;
							else
								_sudokuMatrix[indexes[0]][indexes[1]]--;

							if (PropertyChanged != null)
								PropertyChanged(this, new PropertyChangedEventArgs("SudokuMatrix"));
						}
					}
				});

			_solveCommand = new DelegateCommand(delegate
				{
					_SolveSudoku();
				});
			InitializeComponent();
		}

		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion
		public int[][] SudokuMatrix
		{
			get
			{
				return _sudokuMatrix;
			}
		}
		public ICommand LoadCommand
		{
			get
			{
				return _loadCommand;
			}
		}
		public ICommand SaveCommand
		{
			get
			{
				return _saveCommand;
			}
		}
		public ICommand ClearCommand
		{
			get
			{
				return _clearCommand;
			}
		}
		public ICommand IncrementCommand
		{
			get
			{
				return _incrementCommand;
			}
		}
		public ICommand DecrementCommand
		{
			get
			{
				return _decrementCommand;
			}
		}
		public ICommand SolveCommand
		{
			get
			{
				return _solveCommand;
			}
		}

		private void _ClearSudokuMap()
		{
			for (int row = 0; row < 9; row++)
				for (int column = 0; column < 9; column++)
					_sudokuMatrix[row][column] = 0;
		}
		private void _SolveSudoku()
		{
			IEnumerable<int> sudokuValues = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

			new ResultWindow(new ForwardCheckingConstraintSatisfactionSearch<SudokuPosition, int>().Find(
					_sudokuMatrix.SelectMany(row => row)
								 .Select((cellValue, cellCount) =>
										 {
											 SudokuPosition position = new SudokuPosition(cellCount / 9, cellCount % 9, (cellValue != 0));

											 if (cellValue == 0)
												 return new KeyValuePair<SudokuPosition, DomainSpecification<int>>(position, DomainSpecification.Create(sudokuValues.Except(_sudokuMatrix[position.Row].Union(_sudokuMatrix.SelectMany(row => row.Skip(position.Column)
																																																												 .Take(1)))
																																																	   .Union(_sudokuMatrix.Skip(position.Row / 3 * 3)
																																																						   .Take(3)
																																																						   .SelectMany(row => row.Skip(position.Column / 3 * 3)
																																																												 .Take(3)))
																																																	   .Distinct()
																																																	   .ToList())));
											 else
												 return new KeyValuePair<SudokuPosition, DomainSpecification<int>>(position, DomainSpecification.Create(cellValue));
										 }), _validSudokuConstraint)).Show();
		}
		private static int _GetGroup(SudokuPosition sudokuPosition)
		{
			return ((sudokuPosition.Row / 3 * 3) + sudokuPosition.Column / 3);
		}

		private readonly int[][] _sudokuMatrix;
		private readonly ICommand _loadCommand;
		private readonly ICommand _saveCommand;
		private readonly ICommand _clearCommand;
		private readonly ICommand _incrementCommand;
		private readonly ICommand _decrementCommand;
		private readonly ICommand _solveCommand;
		private static Constraint<Variables> _validSudokuConstraint =
			Constraint.Create<Variables>(variables => variables.First.Name.Row != variables.Second.Name.Row || variables.First.Value != variables.Second.Value)
					  .And(Constraint.Create<Variables>(variables => variables.First.Name.Column != variables.Second.Name.Column || variables.First.Value != variables.Second.Value))
					  .And(Constraint.Create<Variables>(variables => _GetGroup(variables.First.Name) != _GetGroup(variables.Second.Name) || variables.First.Value != variables.Second.Value));
	}
}