using DOL.Geometry;
using System;

namespace DOL.GS.Geometry;

public static class CoordinateTransitionExtensions
{
    [Obsolete("This extension is transitional and going to be removed.")]
    public static Point3D ToPoint3D(this Coordinate coordinate)
        => new Point3D(coordinate.X, coordinate.Y, coordinate.Z);
    
    [Obsolete("This extension is transitional and going to be removed.")]
    public static Point2D ToPoint2D(this Coordinate coordinate)
        => new Point2D(coordinate.X, coordinate.Y);

    [Obsolete("This extension is transitional and going to be removed.")]
    public static Coordinate ToCoordinate(this IPoint3D point)
            => Coordinate.Create((int)point.Position.X, (int)point.Position.Y, (int)point.Position.Z);
}