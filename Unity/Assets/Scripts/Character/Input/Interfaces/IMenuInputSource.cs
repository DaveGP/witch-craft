using Data.Enums;


namespace Character.Input.Interfaces
{
	public interface IMenuInputSource
	{
		/// <summary>
		/// Gets the menu actions pressed since the last call.
		/// </summary>
		MenuAction ConsumeMenuInput();
	}
}
