using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace WinForms_TestApp
{
    public static class ExtensionMethods
    {
        public static Matrix4 ToMatrix4(this Matrix4x4 matrix)
        {
            Matrix4 result = new()
            {
                M11 = matrix.M11,
                M12 = matrix.M12,
                M13 = matrix.M13,
                M14 = matrix.M14,
                M21 = matrix.M21,
                M22 = matrix.M22,
                M23 = matrix.M23,
                M24 = matrix.M24,
                M31 = matrix.M31,
                M32 = matrix.M32,
                M33 = matrix.M33,
                M34 = matrix.M34,
                M41 = matrix.M41,
                M42 = matrix.M42,
                M43 = matrix.M43,
                M44 = matrix.M44
            };
            return result;
        }
        public static Matrix4x4 Rotate(this Matrix4x4 matrix, double angle, double x, double y, double z)
        {
            double mag = Math.Sqrt(x * x + y * y + z * z);
            double sinAngle = Math.Sin(angle * Math.PI / 180.0);
            double cosAngle = Math.Cos(angle * Math.PI / 180.0);

            if (mag > 0)
            {
                double xx, yy, zz, xy, yz, zx, xs, ys, zs;
                double oneMinusCos;
                Matrix4x4 rotMat;

                x /= mag;
                y /= mag;
                z /= mag;

                xx = x * x;
                yy = y * y;
                zz = z * z;
                xy = x * y;
                yz = y * z;
                zx = z * x;
                xs = x * sinAngle;
                ys = y * sinAngle;
                zs = z * sinAngle;
                oneMinusCos = 1.0 - cosAngle;

                rotMat.M11 = (float)((oneMinusCos * xx) + cosAngle);
                rotMat.M12 = (float)((oneMinusCos * xy) - zs);
                rotMat.M13 = (float)((oneMinusCos * zx) + ys);
                rotMat.M14 = 0.0f;

                rotMat.M21 = (float)((oneMinusCos * xy) + zs);
                rotMat.M22 = (float)((oneMinusCos * yy) + cosAngle);
                rotMat.M23 = (float)((oneMinusCos * yz) - xs);
                rotMat.M24 = 0.0f;

                rotMat.M31 = (float)((oneMinusCos * zx) - ys);
                rotMat.M32 = (float)((oneMinusCos * yz) + xs);
                rotMat.M33 = (float)((oneMinusCos * zz) + cosAngle);
                rotMat.M34 = 0.0f;

                rotMat.M41 = 0.0f;
                rotMat.M42 = 0.0f;
                rotMat.M43 = 0.0f;
                rotMat.M44 = 1.0f;

                return rotMat * matrix;
            }
            return matrix;
        }
        public static Matrix4x4 Perspective(this Matrix4x4 matrix, double fieldOfView, double aspectRatio, double nearPlaneDistance, double farPlaneDistance)
        {
            double frustumH = Math.Tan(fieldOfView / 360.0 * Math.PI) * nearPlaneDistance;
            double frustumW = frustumH * aspectRatio;

            return matrix.Frustum(-frustumW, frustumW, -frustumH, frustumH, farPlaneDistance, nearPlaneDistance);
        }

        public static Matrix4x4 Frustum(this Matrix4x4 matrix, double left, double right, double bottom, double top, double farZ, double nearZ)
        {
            double deltaX = right - left;
            double deltaY = top - bottom;
            double deltaZ = farZ - nearZ;
            Matrix4x4 frust;

            if ((nearZ <= 0.0) || (farZ <= 0.0) || (deltaX <= 0.0) || (deltaY <= 0.0) || (deltaZ <= 0.0))
            {
                return matrix;
            }

            frust.M11 = (float)(2.0 * nearZ / deltaX);
            frust.M12 = frust.M13 = frust.M14 = 0.0f;

            frust.M22 = (float)(2.0 * nearZ / deltaY);
            frust.M21 = frust.M23 = frust.M24 = 0.0f;

            frust.M31 = (float)((right + left) / deltaX);
            frust.M32 = (float)((top + bottom) / deltaY);
            frust.M33 = (float)(-(nearZ + farZ) / deltaZ);
            frust.M34 = -1.0f;

            frust.M43 = (float)(-2.0 * nearZ * farZ / deltaZ);
            frust.M41 = frust.M42 = frust.M44 = 0.0f;

            return frust * matrix;
        }
        public static Matrix4x4 Translate(this Matrix4x4 matrix, float x, float y, float z)
        {
            matrix.M41 += matrix.M11 * x + matrix.M21 * y + matrix.M31 * z;
            matrix.M42 += matrix.M12 * x + matrix.M22 * y + matrix.M32 * z;
            matrix.M43 += matrix.M13 * x + matrix.M23 * y + matrix.M33 * z;
            matrix.M44 += matrix.M14 * x + matrix.M24 * y + matrix.M34 * z;

            return matrix;
        }
    }
}
