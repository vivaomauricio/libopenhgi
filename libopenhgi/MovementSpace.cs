using System;
using OpenNI;

namespace libopenhgi
{
	
	public enum MovementSpacePlane {BACKWARD, POV, FORWARD};
	public enum MovementSpaceQuadrant {UP_LEFT, UP, UP_RIGHT,
									   LEFT, CENTER, RIGHT,
									   DOWN_LEFT, DOWN, DOWN_RIGHT};
	
	public class MovementSpaceCoordinate
	{
		public MovementSpacePlane plane;
		public MovementSpaceQuadrant quadrant;
		
		public MovementSpaceCoordinate(MovementSpacePlane plane, MovementSpaceQuadrant quadrant)
		{
			this.plane = plane;
			this.quadrant = quadrant;
		}
	}
	
	public class MovementSpacePoint3D
	{
		public int X;
		public int Y;
		public int Z;
		
		public MovementSpacePoint3D(int x, int y, int z)
		{
			this.X = x;
			this.Y = y;
			this.Z = z;
		}
	}
	
	public class MovementSpace
	{	
		
		private MovementSpacePoint3D origin;
		private int d;
		
		
		public MovementSpace(Point3D LHand, Point3D RHand)
		{
			//Logger.Log.getInstance().printPoint("left hand", (int) LHand.X, (int) LHand.Y, (int) LHand.Z);
			//Logger.Log.getInstance().printPoint("right hand", (int) RHand.X, (int) RHand.Y, (int) RHand.Z);
		
			this.origin = new MovementSpacePoint3D((int) LHand.X, (int) LHand.Y, (int) LHand.Z);
			
			double Xa = (double) origin.X;
			double Xb = (double) RHand.X;
			
			double Ya = (double) origin.Y;
			double Yb = (double) RHand.Y;
			
			this.d = (int) Math.Sqrt(Math.Pow(Xb - Xa, 2) + Math.Pow(Yb - Ya, 2)); 
			
			Logger.Log.getInstance().DEBUG("MovementSpace", "new frame of reference");
		}
		
		private MovementSpacePoint3D pointConv(Point3D p)
		{
			return new MovementSpacePoint3D((int) p.X, (int) p.Y, (int) p.Z);
		}
		
		private bool didTheOriginMoved(MovementSpacePoint3D o)
		{
			
			return false;
		}
		
		private MovementSpacePlane calcPlane(MovementSpacePoint3D p)
		{
			int min = origin.Z - d;
			int max = origin.Z + d;
			
			
			Logger.Log.getInstance().toFile("calcPLANE", " minZ: " + min + "  maxZ: " + max + "   pointZ: " + p.Z);
			
			
			if (p.Z < min) {
				return MovementSpacePlane.FORWARD;
			}
			else if (p.Z > max)
			{
				return MovementSpacePlane.BACKWARD;
			}
			
			return MovementSpacePlane.POV;
		}
		
		private MovementSpaceQuadrant calcQuadrant(MovementSpacePoint3D p)
		{
			return MovementSpaceQuadrant.CENTER;
		}
		
		public MovementSpaceCoordinate calcCoordinate(Point3D LHand, Point3D RHand)
		{
			MovementSpacePoint3D l = pointConv(LHand);
			MovementSpacePoint3D r = pointConv(RHand);
			
			if (this.didTheOriginMoved(l))
			{
				return new MovementSpaceCoordinate(MovementSpacePlane.POV, MovementSpaceQuadrant.CENTER);	
			}
			
			MovementSpacePlane retPlane;
			MovementSpaceQuadrant retQuadrant;
			
			retPlane = this.calcPlane(r);
			retQuadrant = this.calcQuadrant(r);
			
			return new MovementSpaceCoordinate(retPlane, retQuadrant);
		}
	}
}

