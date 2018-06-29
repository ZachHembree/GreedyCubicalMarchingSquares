using System;
using System.Collections.Generic;
using UnityEngine;

namespace GreedyCms
{
    /// <summary>
    /// Defines the intersection between a surface and a volume on a grid with a given scale.
    /// </summary>
    public class Octant
    {
        public static readonly Octant zero = new Octant(0, 0, 0, int.MinValue);
        //public readonly bool isInterior;
        public readonly int range;
        public float x, y, z;

        public Octant(float _x, float _y, float _z, int _range)
        {
            x = _x;
            y = _y;
            z = _z;
            range = _range;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            Octant b = (Octant)obj;

            return (b.range == range && x == b.x && y == b.y && z == b.z);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + range.GetHashCode();
                hash = hash * 23 + x.GetHashCode();
                hash = hash * 23 + y.GetHashCode();
                hash = hash * 23 + z.GetHashCode();

                return hash;
            }
        }

        public static Octant operator +(Octant a, Octant b)
        {
            if (a.range == b.range)
                return new Octant(a.x + b.x, a.y + b.y, a.z + b.z, a.range);
            else
                throw new Exception("Cannot perform addition on Octants with different ranges.");
        }

        public static Octant operator -(Octant a, Octant b)
        {
            if (a.range == b.range)
                return new Octant(a.x - b.x, a.y - b.y, a.z - b.z, a.range);
            else
                throw new Exception("Cannot perform subtraction on Octants with different ranges.");
        }

        public static Octant operator *(Octant a, float b) =>
            new Octant(a.x * b, a.y * b, a.z * b, a.range);

        public static Octant operator *(Octant a, int b) =>
            new Octant(a.x * b, a.y * b, a.z * b, a.range);

        public static Octant operator /(Octant a, float b) =>
            new Octant(a.x / b, a.y / b, a.z / b, a.range);

        public static Octant operator /(Octant a, int b) =>
            new Octant(a.x / b, a.y / b, a.z / b, a.range);
    }

    /// <summary>
    /// Abstract class for volume to be voxelized.
    /// </summary>
    public abstract class Volume
    {
        public float LastImportTime { get; protected set; }
        public Vector3 Scale { get; protected set; }
        public Vector3 Step { get; protected set; }
        public Vector3 Delta { get; protected set; }
        public Vector3Int Dimensions { get; protected set; }
        public IList<Octant>[][] Octants { get; protected set; }

        /// <summary>
        /// Reformats octants into something compatible with the algorithm.
        /// Starting octants in each column must be sorted by range in ascending order.
        /// </summary>
        protected void GetFinalOctants(IList<Octant>[][] startingOctants)
        {
            Octants = new List<Octant>[Dimensions.x + 2][];

            for (int x = 0; x < Octants.Length; x++)
                Octants[x] = new List<Octant>[Dimensions.y + 2];

            for (int x = 0; x < Octants.Length; x++)
            {
                Octants[x][0] = new List<Octant>(0);
                Octants[x][Dimensions.y + 1] = new List<Octant>(0);
            }

            for (int y = 1; y < Octants[0].Length - 1; y++)
            {
                Octants[0][y] = new List<Octant>(0);
                Octants[Dimensions.x + 1][y] = new List<Octant>(0);
            }

            int last, avgCount;
            Octant current = null;

            for (int x = 0; x < Dimensions.x; x++)
                for (int y = 0; y < Dimensions.y; y++)
                {
                    if (startingOctants[x][y] != null)
                    {
                        last = int.MinValue;
                        avgCount = 1;
                        Octants[x + 1][y + 1] = new List<Octant>((startingOctants[x][y].Count / 3) + 2);

                        foreach (Octant o in startingOctants[x][y])
                        {
                            if (last < o.range)
                            {
                                if (avgCount > 1)
                                    current /= avgCount;

                                current = o;
                                last = current.range;
                                Octants[x + 1][y + 1].Add(current);
                            }
                            else
                            {
                                current += o;
                                avgCount++;
                            }
                        }
                    }
                    else
                        Octants[x + 1][y + 1] = new List<Octant>(0);
                }
        }
    }   
}