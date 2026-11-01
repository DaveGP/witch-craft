using Data.Input;


namespace Character.Input.Interfaces
{
	public interface ICameraInputSource
	{
		/// <summary>
		/// Gets the latest input for this source.
		/// </summary>
		CameraInput GetCameraInput();
	}
}
