namespace Character.Animation
{
	// The gestures the casting layer plays. The values are the CastStyle parameter the Animator reads.
	public enum CastStyle
	{
		// One hand pointed: building, unmaking, levitating, the wisp
		Point = 0,

		// Both hands pushed forward: gust, blink
		Push = 1,

		// A hand raised to the sky: bubble, rain call
		Call = 2,

		// Arms swept round: bloom
		Sweep = 3,
	}
}
