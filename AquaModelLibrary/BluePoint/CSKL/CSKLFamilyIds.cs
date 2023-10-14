﻿using Reloaded.Memory.Streams;

namespace AquaModelLibrary.BluePoint.CSKL
{
    public class CSKLFamilyIds
    {
        public float unkFloat; //Seemingly always 0 or -0??
        public int parentId;
        public int firstChildId;
        public int nextSiblingId;

        public CSKLFamilyIds()
        {

        }

        public CSKLFamilyIds(BufferedStreamReader sr)
        {
            unkFloat = sr.Read<float>();
            parentId = sr.Read<int>();
            firstChildId = sr.Read<int>();
            nextSiblingId = sr.Read<int>();
        }
    }
}
