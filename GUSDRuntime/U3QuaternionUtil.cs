using Script.CoreUObject;
using UnityEngine;

namespace GUSD.Utils;

public class U3QuaternionUtil
{
    public static FQuat ConvertU3QuatToU1(Quaternion quat)
    {
        return new FQuat(quat.z, quat.x, quat.y, quat.w);
    }
    
    public static Quaternion ConvertU1QuatToU3(FQuat quat)
    {
        return new Quaternion((float)quat.Y, (float)quat.Z, (float)quat.X, (float)quat.W);
    }

    public static FRotator GetU1RotatorFromU3(Quaternion quat)
    {
        var u1Quat = ConvertU3QuatToU1(quat);
        return u1Quat.Rotator();
    }
}