﻿using AquaModelLibrary.Extra.AM2;
using SoulsFormats;
using SoulsFormats.Formats.Morpheme.NSA;
using System.Collections.Generic;
using System.Numerics;
using System.Windows.Documents;
using static AquaModelLibrary.PSU.NOM;

namespace AquaModelLibrary.Extra.Morpheme
{
    public class NSAConvert
    {
        public static AquaMotion GetAquaMotionFromNSA(NSA nsa, IFlver flv)
        {
            GetNSAKeyframes(nsa, GetFlverTrueRoot(flv), out var translationKeyFrameListList, out var rotationkeyFrameListList);

            AquaMotion aqm = new AquaMotion();
            aqm.moHeader = new AquaMotion.MOHeader();
            aqm.moHeader.frameSpeed = nsa.header.fps;
            aqm.moHeader.endFrame = (int)(nsa.rootMotionSegment.sampleCount - 1);
            aqm.moHeader.unkInt0 = 2;
            aqm.moHeader.variant = 0x2;
            aqm.moHeader.testString.SetString("test");

            for(int i = 0; i < translationKeyFrameListList.Count; i++)
            {
                var keySet = new AquaMotion.KeyData();
                keySet.mseg.nodeDataCount = 2;
                keySet.mseg.nodeId = i;
                keySet.mseg.nodeType = 2;
                keySet.mseg.nodeName = AquaCommon.PSO2String.GeneratePSO2String(flv.Bones[i].Name);
                
                var pos = new AquaMotion.MKEY();
                var rot = new AquaMotion.MKEY();
                
                //Position
                var frameSet = translationKeyFrameListList[i];
                pos.dataType = 1;
                pos.keyCount = frameSet.Count;
                pos.keyType = 1;
                pos.vector4Keys = frameSet.ConvertAll(frame => new Vector4(frame.X, frame.Y, frame.Z, 0));

                //Position Timings
                for(int f = 0; f < pos.vector4Keys.Count; f++)
                {
                    int flag = 0;
                    if (f == 0)
                    {
                        flag = 1;
                    }
                    else if (f == pos.vector4Keys.Count - 1)
                    {
                        flag = 2;
                    }
                    pos.frameTimings.Add((uint)((f * 0x10) + flag));
                }

                //Rotation
                var rotFrameSet = rotationkeyFrameListList[i];
                rot.dataType = 3;
                rot.keyCount = rotFrameSet.Count;
                rot.keyType = 2;
                rot.vector4Keys = rotFrameSet;

                //Rotation Timings
                for (int f = 0; f < rot.vector4Keys.Count; f++)
                {
                    int flag = 0;
                    if (f == 0)
                    {
                        flag = 1;
                    }
                    else if (f == rot.vector4Keys.Count - 1)
                    {
                        flag = 2;
                    }
                    rot.frameTimings.Add((uint)((f * 0x10) + flag));
                }

                keySet.keyData.Add(pos);
                keySet.keyData.Add(rot);
            }

            aqm.moHeader.nodeCount = aqm.motionKeys.Count;
            return null;
        }

        /// <summary>
        /// There are some dummy bones that get exported related to each mesh in the model. We want to skip these until we get to the first real bone in the hierarchy.
        /// </summary>
        /// <returns></returns>
        public static int GetFlverTrueRoot(IFlver flv)
        {
            int bone = -1;
            for(int i = 0; i < flv.Bones.Count; i++)
            {
                if (flv.Bones[i].ParentIndex != -1)
                {
                    return bone;
                }
                bone++;
            }

            return bone;
        }

        public static void GetNSAKeyframes(NSA nsa, int trueRoot, out List<List<Vector3>> translationKeyframeListList, out List<List<Vector4>> rotationKeyframeListList)
        {
            translationKeyframeListList = new List<List<Vector3>>();
            rotationKeyframeListList = new List<List<Vector4>>();

            translationKeyframeListList.Add(nsa.rootMotionSegment.translationFrames.Count > 0 ? nsa.rootMotionSegment.translationFrames : new List<Vector3>() { new Vector3(0,0,0) });
            rotationKeyframeListList.Add(nsa.rootMotionSegment.rotationFrames.Count > 0 ? nsa.rootMotionSegment.rotationFrames.ConvertAll(frame => new Vector4(frame.X, frame.Y, frame.Z, frame.W)) : new List<Vector4>() { nsa.rootMotionSegment.rotation.ToVec4() });

            //Fill in dummy bones
            for(int i = 1; i < trueRoot; i++)
            {
                translationKeyframeListList.Add(new List<Vector3>() { new Vector3() });
                rotationKeyframeListList.Add(new List<Vector4>() { new Vector4(0,0,0,1) });
            }
            
            for(ushort i = 0; i < nsa.header.boneCount; i++)
            {
                //Translation keys
                var translationDynamicIndex = nsa.dynamicTranslationIndices.IndexOf(i);
                var translationStaticIndex = nsa.staticTranslationIndices.IndexOf(i);
                if (translationDynamicIndex != -1)
                {
                    List<Vector3> translationKeys = new List<Vector3>();
                    foreach(var list in nsa.dynamicSegment.translationFrameLists)
                    {
                        translationKeys.Add(list[translationDynamicIndex]);
                    }
                    translationKeyframeListList.Add(translationKeys);
                } else if (translationStaticIndex != -1)
                {
                    translationKeyframeListList.Add(new List<Vector3>(){ nsa.staticSegment.translationFrames[translationStaticIndex] });
                } else //Default - in theory we never reach here
                {
                    translationKeyframeListList.Add(new List<Vector3>());
                }

                //Rotation keys
                var rotationDynamicIndex = nsa.dynamicTranslationIndices.IndexOf(i);
                var rotationStaticIndex = nsa.staticTranslationIndices.IndexOf(i);
                if (rotationDynamicIndex != -1)
                {
                    List<Vector4> rotationKeys = new List<Vector4>();
                    foreach (var list in nsa.dynamicSegment.rotationFrameLists)
                    {
                        rotationKeys.Add(list[rotationDynamicIndex].ToVec4());
                    }
                    rotationKeyframeListList.Add(rotationKeys);
                }
                else if (rotationStaticIndex != -1)
                {
                    rotationKeyframeListList.Add(new List<Vector4>() { nsa.staticSegment.rotationFrames[translationStaticIndex].ToVec4() });
                }
                else //Default - in theory we never reach here
                {
                    rotationKeyframeListList.Add(new List<Vector4>());
                }
            }
        }
    }
}
