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
		
		private MovementSpacePlane calcPlane(MovementSpacePoint3D p)
		{
			int min = origin.Z - 2*d;
			int max = origin.Z + 2*d;
			
			
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
			
			int Ymin = origin.Y - d/4;
			int Ymax = origin.Y + d/4;
			
			int Xmin = origin.X + d/2;
			int Xmax = origin.X + d + d/2;
			
			
			
			if (p.X < Xmin)
			{
				if (p.Y < Ymin) 
				{
					return MovementSpaceQuadrant.DOWN;
				}
				else if (p.Y > Ymin && p.Y < Ymax)
				{
					return MovementSpaceQuadrant.LEFT;
				}
				else if (p.Y > Ymax)
				{
					return MovementSpaceQuadrant.UP;
				}
			}
			else if (p.X > Xmin && p.X < Xmax)
			{
				if (p.Y < Ymin) 
				{
					return MovementSpaceQuadrant.DOWN;
				}
				else if (p.Y > Ymin && p.Y < Ymax)
				{
					return MovementSpaceQuadrant.CENTER;
				}
				else if (p.Y > Ymax)
				{
					return MovementSpaceQuadrant.UP;
				}
			}
			else if (p.X > Xmax)
			{
				if (p.Y < Ymin) 
				{
					return MovementSpaceQuadrant.DOWN;
				}
				else if (p.Y > Ymin && p.Y < Ymax)
				{
					return MovementSpaceQuadrant.RIGHT;
				}
				else if (p.Y > Ymax)
				{
					return MovementSpaceQuadrant.UP;
				}
			}
			
			return MovementSpaceQuadrant.CENTER;
		}
		
		public MovementSpaceCoordinate calcCoordinate(Point3D LHand, Point3D RHand)
		{
			MovementSpacePoint3D l = pointConv(LHand);
			MovementSpacePoint3D r = pointConv(RHand);
			
			MovementSpaceQuadrant originQuadrant = this.calcQuadrant(l);
			if (originQuadrant != MovementSpaceQuadrant.LEFT)
			{
				Logger.Log.getInstance().DEBUG("NAV", "Navigation ended");
				return null;
			}
			
			MovementSpacePlane retPlane;
			MovementSpaceQuadrant retQuadrant;
			
			retPlane = this.calcPlane(r);
			
			if (retPlane != MovementSpacePlane.POV)
			{
				retQuadrant = MovementSpaceQuadrant.CENTER;
			}
			else
			{
				retQuadrant = this.calcQuadrant(r);
			}
				
			
			return new MovementSpaceCoordinate(retPlane, retQuadrant);
		}
	}
}

