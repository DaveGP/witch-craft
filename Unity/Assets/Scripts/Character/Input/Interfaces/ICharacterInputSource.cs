using Data.Input;


namespace Character.Input.Interfaces
{
	public interface ICharacterInputSource
	{
		/// <summary>
		/// Gets the input buffered since the last call.
		/// </summary>
		CharacterInput ConsumeCharacterInput();
	}
}
