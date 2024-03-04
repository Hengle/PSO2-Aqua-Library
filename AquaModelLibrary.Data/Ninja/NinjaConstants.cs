﻿namespace AquaModelLibrary.Data.Ninja
{
    public class NinjaConstants
    {
        public const double FromBAMSvalueToRadians = ((2 * Math.PI) / 65536.0);
        public const double ToBAMSValueFromRadians = (65536.0 / (2 * Math.PI));
    }

    public enum AlphaInstruction : int
    {
        Zero = 0,
        One = 1,
        OtherColor = 2,
        InverseOtherColor = 3,
        SourceAlpha = 4,
        InverseSourceAlpha = 5,
        DestinationAlpha = 6,
        InverseDestinationAlpha = 7,
    }

    [Flags]
    public enum AnimFlags : ushort
    {
        Position = 0x1,
        Rotation = 0x2,
        Scale = 0x4,
        Vector = 0x8,
        Vertex = 0x10,
        Normal = 0x20,
        Target = 0x40,
        Roll = 0x80,
        Angle = 0x100,
        Color = 0x200,
        Intensity = 0x400,
        Spot = 0x800,
        Point = 0x1000,
        Quaternion = 0x2000
    }

    public enum InterpolationMode
    {
        Linear,
        Spline,
        User
    }

    public enum NJD_MTYPE_FN
    {
        NJD_MTYPE_LINER = 0x0000, /* use liner                */
        NJD_MTYPE_SPLINE = 0x0040, /* use spline               */
        NJD_MTYPE_USER = 0x0080, /* use user function        */
        NJD_MTYPE_MASK = 0x00c0  /* Sampling mask*/
    }
}
