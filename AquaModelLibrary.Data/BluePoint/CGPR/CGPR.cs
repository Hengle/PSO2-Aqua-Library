﻿using AquaModelLibrary.Helpers.Readers;

namespace AquaModelLibrary.Data.BluePoint.CGPR
{
    public class CGPR
    {
        public List<CGPRObject> objects = new List<CGPRObject>();
        public CFooter cfooter;
        public int cgprObjCount;
        public int int_04; //For 0 count cgpr

        public CGPR()
        {

        }
        public CGPR(byte[] file)
        {
            file = CompressionHandler.CheckCompression(file);
            using (MemoryStream ms = new MemoryStream(file))
            using (BufferedStreamReaderBE<MemoryStream> sr = new BufferedStreamReaderBE<MemoryStream>(ms))
            {
                Read(sr);
            }
        }

        private void Read(BufferedStreamReaderBE<MemoryStream> sr)
        {
            var cgprObjCount = sr.Read<int>(); //Doesn't always line up right. Correlates, but needs more research

            //Should just be an empty cgpr
            if (cgprObjCount == 0)
            {
                int_04 = sr.Read<int>();
                cfooter = sr.Read<CFooter>();
            }

            uint type0 = sr.Peek<uint>();
            while (!(type0 == 0 || type0 == 0x47505250))
            {
                switch (type0)
                {
                    case (uint)CGPRMagic.xC1A69458:
                        objects.Add(new _C1A69458_Object(sr));
                        break;
                    case (uint)CGPRMagic.xFAE88582:
                        objects.Add(new _FAE88582_Object(sr));
                        break;
                    case (uint)CGPRMagic.x427AC0E6:
                        objects.Add(new _427AC0E6_Object(sr));
                        break;
                    case (uint)CGPRMagic.x2C146841:
                        objects.Add(new _2C146841_Object(sr));
                        break;
                    case (uint)CGPRMagic.x7FB9F5F0:
                        objects.Add(new _7FB9F5F0_Object(sr));
                        break;
                    default:
                        throw new Exception($"Unexpected object {type0.ToString("X")} discovered");
                }

                type0 = sr.Peek<uint>();

                //Try to account for weird scenarios where sizes don't align? Idk wtf the game is doing
                if (sr.Peek<byte>() == 0)
                {
                    sr.Seek(1, System.IO.SeekOrigin.Current);
                    type0 = sr.Peek<uint>();
                }

            }
        }
    }
}
