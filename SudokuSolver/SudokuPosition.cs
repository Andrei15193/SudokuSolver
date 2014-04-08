namespace SudokuSolver
{
	public sealed class SudokuPosition
	{
		public SudokuPosition(int row, int column, bool isSet = false)
		{
			_isSet = isSet;
			_row = row;
			_column = column;
		}

		public int Row
		{
			get
			{
				return _row;
			}
		}
		public int Column
		{
			get
			{
				return _column;
			}
		}
		internal bool IsSet
		{
			get
			{
				return _isSet;
			}
		}

		private readonly bool _isSet;
		private readonly int _row;
		private readonly int _column;
	}
}