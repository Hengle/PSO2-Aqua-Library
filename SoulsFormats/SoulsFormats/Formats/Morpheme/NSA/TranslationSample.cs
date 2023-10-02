﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SoulsFormats.Formats.Morpheme.NSA
{
    /// <summary>
    /// Translation keyframe values
    /// </summary>
    public class TranslationSample
    {
        /// <summary>
        /// X position
        /// </summary>
        public int X;
        /// <summary>
        /// Y position
        /// </summary>
        public int Y;
        /// <summary>
        /// Z position
        /// </summary>
        public int Z;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public TranslationSample() { }

        /// <summary>
        /// Read in translation values. The raw value is an int that must be
        /// </summary>
        /// <param name="br"></param>
        public TranslationSample(BinaryReaderEx br)
        {
            var sample = br.ReadUInt32();
            X = ExtractBits((int)sample, 21, 0xFFFF);
            Y = ExtractBits((int)sample, 10, 0x7FF);
            Z = ExtractBits((int)sample, 0, 0x3FF);
        }

        /// <summary>
        /// Translations are compcated to a single int32 value. 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="shiftValue"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public int ExtractBits(int value, int shiftValue, uint mask)
        {
            uint extractedBits = (uint)(value >> shiftValue) & mask;

            return (int)extractedBits;
        }

        /// <summary>
        /// Produces the final translation value for use given a DequantizationFactor
        /// </summary>
        /// <param name="factor"></param>
        /// <returns></returns>
        public Vector3 DequantizeTranslation(DequantizationFactor factor)
        {
            return new Vector3(X * factor.scaledExtent.X + factor.min.X, Y * factor.scaledExtent.Y + factor.min.Y, Z * factor.scaledExtent.Z + factor.min.Z);
        }
    }
}
