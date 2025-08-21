using Script.CoreUObject;
using UnityEngine;

/**
 * U3.X---U1.Y---world right direction
 * U3.Y---U1.Z---world up direction
 * U3.Z---U1.X---world forward direction
 */
namespace GUSD.Utils;

public class U3VectorUtil
{
    public static Vector3 GetU3PositionFromU1(FVector location)
    {
        var position = new Vector3();
        position.x = (float)location.Y;
        position.y = (float)location.Z;
        position.z = (float)location.X;
        position /= U3Constants.U3VectorScale;
        return position;
    }
    
    public static Vector3 GetU3PositionFromU1(FVector2D location)
    {
        var position = new Vector3((float)location.X, (float)location.Y);
        return position;
    }

    public static Vector3 GetU3AccelerationFromU1(FVector u1Acceleration)
    {
        return new Vector3(
            (float)u1Acceleration.Y,  // U1.Y → U3.X
            (float)u1Acceleration.Z,  // U1.Z → U3.Y
            (float)u1Acceleration.X   // U1.X → U3.Z
        );
    }
    public static FVector GetU1LocationFromU3(Vector3 position)
    {
        var location = new FVector();
        location.X = position.z;
        location.Y = position.x;
        location.Z = position.y;
        location *= U3Constants.U3VectorScale;
        return location;
    }

    public static FVector2D GetUILocationFromU3(Vector3 position)
    {
        var location = new FVector2D(position.x, position.y);
        return location;
    }
    
    public static Vector3 GetU3DirectionFromU1(FVector u1Direction)
    {
        u1Direction.Normalize();
        var u3Direction = new Vector3();
        u3Direction.x = (float)u1Direction.Y;
        u3Direction.y = (float)u1Direction.Z;
        u3Direction.z = (float)u1Direction.X;
        return u3Direction;
    }

    public static Vector3 GetU3FromU1(FVector3f u1Vector)
    {
        var u3Vector = new Vector3();
        u3Vector.x = u1Vector.Y;
        u3Vector.y = u1Vector.Z;
        u3Vector.z = u1Vector.X;
        return u3Vector;
    }
    
    public static FVector GetU1DirectionFromU3(Vector3 u3Direction)
    {
        u3Direction.Normalize();
        var u1Direction = new FVector();
        u1Direction.X = u3Direction.z;
        u1Direction.Y = u3Direction.x;
        u1Direction.Z = u3Direction.y;
        return u1Direction;
    }

    /**
     * Cursor position transformation from u3 to u1
     */
    public static FVector GetU1ScreenPointFromU3(Vector3 u3ScreenPoint)
    {
        var u1ScreenPoint = new FVector();
        u1ScreenPoint.X = u3ScreenPoint.x;
        u1ScreenPoint.Y = Screen.height - u3ScreenPoint.y;
        return u1ScreenPoint;
    }

    /**
     * Cursor position transformation from u1 to u3
     */
    public static Vector3 GetU3ScreenPointFromU1(FVector u1ScreenPoint)
    {
        var u3ScreenPoint = new Vector3();
        u3ScreenPoint.x = (float)u1ScreenPoint.X;
        u3ScreenPoint.y = (float)(Screen.height - u1ScreenPoint.Y);
        return u3ScreenPoint;
    }

    /**
     * Quaternion.Euler() in unity returns a rotation that rotates z degrees around the z-axis
     * , x degrees around the x-axis, and y degrees around the y-axis;
     * and applied in that order.
     *
     * notice：
     * u1.X = u3.z;
     * u1.Y = u3.x;
     * u1.Z = u3.y;
     */
    public static FVector GetU1EulerFromU3(Vector3 u3Euler)
    {
        var u1Euler = new FVector();
        u1Euler.X = u3Euler.z;
        u1Euler.Y = u3Euler.x;
        u1Euler.Z = u3Euler.y;
        return u1Euler;
    }

    public static Vector3 GetU3EulerFromU1(FVector u1Euler)
    {
        var u3Euler = new Vector3();
        u3Euler.x = (float)u1Euler.Y;
        u3Euler.y = (float)u1Euler.Z;
        u3Euler.z = (float)u1Euler.X;
        return u3Euler;
    }

    public static FVector GetU1ScaleFromU3(Vector3 u3Scale)
    {
        var u1Scale = new FVector();
        u1Scale.X = u3Scale.z;
        u1Scale.Y = u3Scale.x;
        u1Scale.Z = u3Scale.y;
        return u1Scale;
    }

    public static Vector3 GetU3ScaleFromU1(FVector u1Scale)
    {
        var u3Scale = new Vector3();
        u3Scale.x = (float)u1Scale.Y;
        u3Scale.y = (float)u1Scale.Z;
        u3Scale.z = (float)u1Scale.X;
        return u3Scale;
    }

    public static FVector GetU1ForceFromU3(Vector3 u3Force)
    {
        var u1Force = new FVector();
        u1Force.X = u3Force.z;
        u1Force.Y = u3Force.x;
        u1Force.Z = u3Force.y;
        u1Force *= 100;
        return u1Force;
    }

    public static Vector3 GetU3ForceFromU1(FVector u1Force)
    {
        var u3Force = new Vector3();
        u3Force.x = (float)u1Force.Y;
        u3Force.y = (float)u1Force.Z;
        u3Force.z = (float)u1Force.X;
        u3Force /= 100;
        return u3Force;
    }

	public static FVector2D GetFVector2DFromVector2(Vector2 vector2)
    {
        var fVector2D = new FVector2D();
        fVector2D.X = vector2.x;
        fVector2D.Y = vector2.y;
        return fVector2D;
    }

    public static float GetU1DistanceFromU3(float u3Distance)
    {
        return u3Distance * U3Constants.U3VectorScale;
    }

    public static float GetU3DistanceFromU1(float u1Distance)
    {
        return  u1Distance / U3Constants.U3VectorScale;
    }
    
    public static Vector3 GetU3SizeFromU1Extent(FVector u1Extent)
    {
        FVector u1FullSize = new FVector(
            u1Extent.X * 2, 
            u1Extent.Y * 2,
            u1Extent.Z * 2
        );
        return GetU3PositionFromU1(u1FullSize);
    }

    public static FVector GetU1ExtentFromU3Size(Vector3 u3FullSize)
    {
        FVector u1FullSize = GetU1LocationFromU3(u3FullSize);
        return new FVector(
            u1FullSize.X / 2,
            u1FullSize.Y / 2,
            u1FullSize.Z / 2
        );
    }
    
}