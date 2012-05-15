using System;

namespace libopenhgi
{
	
	public enum MovementSpaceCoordinate {BACKWARD_UP_LEFT, BACKWARD_UP, BACKWARD_UP_RIGHT,
										 BACKWARD_LEFT, BACKWARD_CENTER, BACKWARD_RIGHT,
										 BACKWARD_DOWN_LEFT, BACKWARD_DOWN, BACKWARD_DOWN_RIGHT,
		
										 POV_UP_LEFT, POV_UP, POV_UP_RIGHT,
										 POV_LEFT, POV_CENTER, POV_RIGHT,
										 POV_DOWN_LEFT, POV_DOWN, POV_DOWN_RIGHT,
		
										 FORWARD_UP_LEFT, FORWARD_UP, FORWARD_UP_RIGHT,
										 FORWARD_LEFT, FORWARD_CENTER, FORWARD_RIGHT,
										 FORWARD_DOWN_LEFT, FORWARD_DOWN, FORWARD_DOWN_RIGHT
										};
	
	public class MovementSpace
	{
		public MovementSpaceCoordinate calcCoordinate(int LHand, int RHand)
		{
			
			
			
			
			
			
			
			
			return MovementSpaceCoordinate.POV_CENTER;
		}
	}
}

