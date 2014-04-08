namespace Andrei15193.ConstraintSatisfaction
{
	public interface IVariable
	{
		object Name
		{
			get;
		}
		object Value
		{
			get;
		}
	}
	public interface IVariable<out TName, out TValue>
		: IVariable
	{
		new TName Name
		{
			get;
		}
		new TValue Value
		{
			get;
		}
	}
}