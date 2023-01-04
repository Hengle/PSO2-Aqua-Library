﻿using SoulsFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using static AquaModelLibrary.AquaNode;
using NvTriStripDotNet;
using AquaModelLibrary.Extra.FromSoft;
using AquaModelLibrary.Native.Fbx;
using System.Diagnostics;

namespace AquaModelLibrary.Extra
{
    public static class SoulsConvert
    {
        public enum SoulsGame
        {
            DemonsSouls,
            DarkSouls1,
            DarkSouls2,
            Bloodborne,
            DarkSouls3,
            Sekiro
        }

        public static Matrix4x4 mirrorMatX = new Matrix4x4(-1, 0, 0, 0,
                                                            0, 1, 0, 0,
                                                            0, 0, 1, 0,
                                                            0, 0, 0, 1);

        public static Matrix4x4 mirrorMatY = new Matrix4x4(1, 0, 0, 0,
                                                            0, -1, 0, 0,
                                                            0, 0, 1, 0,
                                                            0, 0, 0, 1);

        public static Matrix4x4 mirrorMatZ = new Matrix4x4(1, 0, 0, 0,
                                                            0, 1, 0, 0,
                                                            0, 0, -1, 0,
                                                            0, 0, 0, 1);

        public static AquaObject ReadFlver(string filePath, out AquaNode aqn, bool useMetaData = false)
        {
            SoulsFormats.IFlver flver = null;
            var raw = File.ReadAllBytes(filePath);

            JsonSerializerSettings jss = new JsonSerializerSettings() { Formatting = Formatting.Indented };
            if (SoulsFormats.SoulsFile<SoulsFormats.FLVER0>.Is(raw))
            {
                flver = SoulsFormats.SoulsFile<SoulsFormats.FLVER0>.Read(raw);
                //Dump metadata
                if (useMetaData)
                {
                    string materialData = JsonConvert.SerializeObject(flver.Materials, jss);
                    string dummyData = JsonConvert.SerializeObject(flver.Dummies, jss);
                    File.WriteAllText(filePath + ".matData.json", materialData);
                    File.WriteAllText(filePath + ".dummyData.json", dummyData);
                }
            }
            else if (SoulsFormats.SoulsFile<SoulsFormats.FLVER2>.Is(raw))
            {
                flver = SoulsFormats.SoulsFile<SoulsFormats.FLVER2>.Read(raw);
            } else if(SoulsFormats.SoulsFile<SoulsFormats.Other.MDL4>.Is(raw))
            {
                var mdl4 = SoulsFormats.SoulsFile<SoulsFormats.Other.MDL4>.Read(raw);

                if(useMetaData)
                {
                    string materialData = JsonConvert.SerializeObject(mdl4.Materials, jss);
                    string dummyData = JsonConvert.SerializeObject(mdl4.Dummies, jss);
                    File.WriteAllText(filePath + ".matData.json", materialData);
                    File.WriteAllText(filePath + ".dummyData.json", dummyData);
                }

                aqn = null;
                return MDL4ToAqua(mdl4, out aqn, useMetaData);
            }
            aqn = null;
            return FlverToAqua(flver, out aqn, useMetaData);
        }

        public static void DebugDumpToFile(FLVER0 flver, int id)
        {
#if DEBUG
            StringBuilder sb = new StringBuilder();
            for (int m = 0; m < flver.Meshes.Count; m++)
            {
                var faces = flver.Meshes[m].Triangulate(flver.Header.Version);
                for (int f = 0; f < faces.Count; f++)
                {
                    sb.AppendLine(flver.Meshes[m].Vertices[faces[f]].Normal.ToString() + " "  + flver.Meshes[m].Vertices[faces[f]].Position.ToString());
                }
            }
            StringBuilder sb2 = new StringBuilder();
            for (int m = 0; m < flver.Meshes.Count; m++)
            {
                var faces = flver.Meshes[m].Triangulate(flver.Header.Version);
                for (int f = 0; f < faces.Count; f++)
                {
                    //sb2.AppendLine(flver.Meshes[m].Vertices[faces[f]].Tangents[0].ToString());
                }
            }
            StringBuilder sb3 = new StringBuilder();
            for (int m = 0; m < flver.Meshes.Count; m++)
            {
                var faces = flver.Meshes[m].Triangulate(flver.Header.Version);
                for (int f = 0; f < faces.Count; f++)
                {
                    var indices = flver.Meshes[m].Vertices[faces[f]].BoneIndices;
                    sb3.AppendLine($"{indices[0]} {indices[1]} {indices[2]} {indices[3]}");
                }
            }

            StringBuilder sb4 = new StringBuilder();
            for (int b = 0; b < flver.Bones.Count; b++)
            {
                var bn = flver.Bones[b];
                var m = bn.ComputeLocalTransform();
                sb4.AppendLine($"{b} {bn.Name}");
                sb4.AppendLine($"{m.M11:F6} {m.M12:F6} {m.M13:F6} {m.M14:F6}");
                sb4.AppendLine($"{m.M21:F6} {m.M22:F6} {m.M23:F6} {m.M24:F6}");
                sb4.AppendLine($"{m.M31:F6} {m.M32:F6} {m.M33:F6} {m.M34:F6}");
                sb4.AppendLine($"{m.M41:F6} {m.M42:F6} {m.M43:F6} {m.M44:F6}");
                Matrix4x4.Decompose(m, out var scale, out var rot, out var pos);
                sb4.AppendLine($"Scale: {scale.X:F6} {scale.Y:F6} {scale.Z:F6}");
                sb4.AppendLine($"Rotation: {rot.X:F6} {rot.Y:F6} {rot.Z:F6} {rot.W:F6}");
                var euler = MathExtras.QuaternionToEuler(rot);
                sb4.AppendLine($"Rotation (Euler): {euler.X:F6} {euler.Y:F6} {euler.Z:F6}");
                sb4.AppendLine($"Rotation (Original Euler): {(bn.Rotation.X * 180 / Math.PI):F6} {(bn.Rotation.Y * 180 / Math.PI):F6} {(bn.Rotation.Z * 180 / Math.PI):F6}");
                sb4.AppendLine($"Position: {pos.X:F6} {pos.Y:F6} {pos.Z:F6}");
            }

            File.WriteAllText($"C:\\Normals_{id}", sb.ToString());
            File.WriteAllText($"C:\\NormalsTan_{id}", sb2.ToString());
            File.WriteAllText($"C:\\NormalsWeight_{id}", sb3.ToString());
            File.WriteAllText($"C:\\NormalsBones_{id}", sb4.ToString());
#endif
        }
        public static void DebugDumpLocalBonesToFile(FLVER0 flver, int id)
        {
#if DEBUG
            StringBuilder sb4 = new StringBuilder();
            for (int b = 0; b < flver.Bones.Count; b++)
            {
                var bn = flver.Bones[b];
                var m = bn.ComputeLocalTransform();
                sb4.AppendLine($"{b} {bn.Name}");
                sb4.AppendLine($"{m.M11:F6} {m.M12:F6} {m.M13:F6} {m.M14:F6}");
                sb4.AppendLine($"{m.M21:F6} {m.M22:F6} {m.M23:F6} {m.M24:F6}");
                sb4.AppendLine($"{m.M31:F6} {m.M32:F6} {m.M33:F6} {m.M34:F6}");
                sb4.AppendLine($"{m.M41:F6} {m.M42:F6} {m.M43:F6} {m.M44:F6}");
                Matrix4x4.Decompose(m, out var scale, out var rot, out var pos);
                sb4.AppendLine($"Scale: {bn.Scale.X:F6} {bn.Scale.Y:F6} {bn.Scale.Z:F6}");
                sb4.AppendLine($"Rotation: {rot.X:F6} {rot.Y:F6} {rot.Z:F6} {rot.W:F6}");
                var euler = MathExtras.QuaternionToEuler(rot);
                var eulRot = bn.Rotation * (float)(180 / Math.PI);
                sb4.AppendLine($"Rotation (Euler): {eulRot.X :F6} {eulRot.Y:F6} {eulRot.Z:F6}");
                sb4.AppendLine($"Position: {bn.Translation.X:F6} {bn.Translation.Y:F6} {bn.Translation.Z:F6}");
            }

            File.WriteAllText($"C:\\NormalsBonesLocal_{id}", sb4.ToString());
#endif
        }
        public static void DebugDumpWorldBonesToFile(AquaNode aqn, int id)
        {
#if DEBUG
            StringBuilder sb4 = new StringBuilder();
            for (int b = 0; b < aqn.nodeList.Count; b++)
            {
                var bn = aqn.nodeList[b];
                Matrix4x4.Invert(bn.GetInverseBindPoseMatrix(), out var m);
                sb4.AppendLine($"{b} {bn.boneName.GetString()}");
                sb4.AppendLine($"{m.M11:F6} {m.M12:F6} {m.M13:F6} {m.M14:F6}");
                sb4.AppendLine($"{m.M21:F6} {m.M22:F6} {m.M23:F6} {m.M24:F6}");
                sb4.AppendLine($"{m.M31:F6} {m.M32:F6} {m.M33:F6} {m.M34:F6}");
                sb4.AppendLine($"{m.M41:F6} {m.M42:F6} {m.M43:F6} {m.M44:F6}");
                Matrix4x4.Decompose(m, out var scale, out var rot, out var pos);
                sb4.AppendLine($"Scale: {scale.X:F6} {scale.Y:F6} {scale.Z:F6}");
                sb4.AppendLine($"Rotation: {rot.X:F6} {rot.Y:F6} {rot.Z:F6} {rot.W:F6}");
                var euler = MathExtras.QuaternionToEuler(rot);
                sb4.AppendLine($"Rotation (Euler): {euler.X:F6} {euler.Y:F6} {euler.Z:F6}");
                sb4.AppendLine($"Position: {pos.X:F6} {pos.Y:F6} {pos.Z:F6}");
            }

            File.WriteAllText($"C:\\NormalsBonesWorld_{id}", sb4.ToString());
#endif
        }
        public static void DebugDumpLocalM4BonesToFile(List<Matrix4x4> matList, int id)
        {
#if DEBUG
            StringBuilder sb4 = new StringBuilder();
            for (int b = 0; b < matList.Count; b++)
            {
                var m = matList[b];
                sb4.AppendLine($"{b}");
                sb4.AppendLine($"{m.M11:F6} {m.M12:F6} {m.M13:F6} {m.M14:F6}");
                sb4.AppendLine($"{m.M21:F6} {m.M22:F6} {m.M23:F6} {m.M24:F6}");
                sb4.AppendLine($"{m.M31:F6} {m.M32:F6} {m.M33:F6} {m.M34:F6}");
                sb4.AppendLine($"{m.M41:F6} {m.M42:F6} {m.M43:F6} {m.M44:F6}");
                Matrix4x4.Decompose(m, out var scale, out var rot, out var pos);
                sb4.AppendLine($"Scale: {scale.X:F6} {scale.Y:F6} {scale.Z:F6}");
                sb4.AppendLine($"Rotation: {rot.X:F6} {rot.Y:F6} {rot.Z:F6} {rot.W:F6}");
                var euler = MathExtras.QuaternionToEuler(rot);
                sb4.AppendLine($"Rotation (Euler): {euler.X:F6} {euler.Y:F6} {euler.Z:F6}");
                sb4.AppendLine($"Position: {pos.X:F6} {pos.Y:F6} {pos.Z:F6}");
            }

            File.WriteAllText($"C:\\NormalsBonesM4Local_{id}", sb4.ToString());
#endif
        }

        public static AquaObject MDL4ToAqua(SoulsFormats.Other.MDL4 mdl4, out AquaNode aqn, bool useMetaData = false)
        {
            AquaObject aqp = new NGSAquaObject();

            aqn = new AquaNode();
            for (int i = 0; i < mdl4.Bones.Count; i++)
            {
                var flverBone = mdl4.Bones[i];
                var parentId = flverBone.ParentIndex;

                FLVER.Bone.RotationOrder order = FLVER.Bone.RotationOrder.XZY;
                var tfmMat = Matrix4x4.Identity;

                Matrix4x4 mat = flverBone.ComputeLocalTransform();
                mat *= tfmMat;

                //If there's a parent, multiply by it
                if (parentId != -1)
                {
                    var pn = aqn.nodeList[parentId];
                    var parentInvTfm = new Matrix4x4(pn.m1.X, pn.m1.Y, pn.m1.Z, pn.m1.W,
                                                  pn.m2.X, pn.m2.Y, pn.m2.Z, pn.m2.W,
                                                  pn.m3.X, pn.m3.Y, pn.m3.Z, pn.m3.W,
                                                  pn.m4.X, pn.m4.Y, pn.m4.Z, pn.m4.W);

                    Matrix4x4.Invert(parentInvTfm, out var invParentInvTfm);
                    mat = mat * invParentInvTfm;
                }
                if (parentId == -1 && i != 0)
                {
                    parentId = 0;
                }

                //Create AQN node
                NODE aqNode = new NODE();
                aqNode.boneShort1 = 0x1C0;
                aqNode.animatedFlag = 1;
                aqNode.parentId = parentId;
                aqNode.unkNode = -1;

                aqNode.scale = new Vector3(1, 1, 1);

                Matrix4x4.Invert(mat, out var invMat);
                aqNode.m1 = new Vector4(invMat.M11, invMat.M12, invMat.M13, invMat.M14);
                aqNode.m2 = new Vector4(invMat.M21, invMat.M22, invMat.M23, invMat.M24);
                aqNode.m3 = new Vector4(invMat.M31, invMat.M32, invMat.M33, invMat.M34);
                aqNode.m4 = new Vector4(invMat.M41, invMat.M42, invMat.M43, invMat.M44);
                aqNode.boneName.SetString(flverBone.Name);
                aqn.nodeList.Add(aqNode);
            }
            //I 100% believe there's a better way to do this when constructing the matrix, but for now we do this.
            for (int i = 0; i < aqn.nodeList.Count; i++)
            {
                var bone = aqn.nodeList[i];
                Matrix4x4.Invert(bone.GetInverseBindPoseMatrix(), out var mat);
                mat *= mirrorMatX;
                Matrix4x4.Decompose(mat, out var scale, out var rot, out var translation);
                bone.pos = translation;
                bone.eulRot = MathExtras.QuaternionToEuler(rot);

                Matrix4x4.Invert(mat, out var invMat);
                bone.SetInverseBindPoseMatrix(invMat);
                aqn.nodeList[i] = bone;
            }

            for (int i = 0; i < mdl4.Meshes.Count; i++)
            {
                var mesh = mdl4.Meshes[i];

                var nodeMatrix = Matrix4x4.Identity;

                //Vert data
                var vertCount = mesh.Vertices.Count;
                AquaObject.VTXL vtxl = new AquaObject.VTXL();
                /*
                if (mesh.Dynamic > 0)
                {
                    for (int b = 0; b < flv.BoneIndices.Length; b++)
                    {
                        if (flv.BoneIndices[b] == -1)
                        {
                            break;
                        }
                        vtxl.bonePalette.Add((ushort)flv.BoneIndices[b]);
                    }
                }*/
                SoulsFormats.Other.MDL4.Mesh mesh0 = mesh;
                vtxl.bonePalette = new List<ushort>();
                for (int b = 0; b < mesh0.BoneIndices.Length; b++)
                {
                    if (mesh0.BoneIndices[b] == -1)
                    {
                        break;
                    }
                    vtxl.bonePalette.Add((ushort)mesh0.BoneIndices[b]);
                }
                var indices = mesh0.ToTriangleList();

                for (int v = 0; v < vertCount; v++)
                {
                    var vert = mesh.Vertices[v];
                    vtxl.vertPositions.Add(Vector3.Transform(vert.Position, mirrorMatX));
                    vtxl.vertNormals.Add(Vector3.Transform(new Vector3(vert.Normal.X, vert.Normal.Y, vert.Normal.Z), mirrorMatX));

                    if (vert.UVs.Count > 0)
                    {
                        var uv1 = vert.UVs[0];
                        vtxl.uv1List.Add(new Vector2(uv1.X, uv1.Y));
                    }
                    if (vert.UVs.Count > 1)
                    {
                        var uv2 = vert.UVs[1];
                        vtxl.uv2List.Add(new Vector2(uv2.X, uv2.Y));
                    }
                    if (vert.UVs.Count > 2)
                    {
                        var uv3 = vert.UVs[2];
                        vtxl.uv3List.Add(new Vector2(uv3.X, uv3.Y));
                    }
                    if (vert.UVs.Count > 3)
                    {
                        var uv4 = vert.UVs[3];
                        vtxl.uv4List.Add(new Vector2(uv4.X, uv4.Y));
                    }
                    var color = vert.Color;
                    vtxl.vertColors.Add(new byte[] { (color[2]), (color[1]), (color[0]), (color[3]) });

                    if (vert.BoneWeights?.Length > 0)
                    {
                        vtxl.vertWeights.Add(new Vector4(vert.BoneWeights[0], vert.BoneWeights[1], vert.BoneWeights[2], vert.BoneWeights[3]));
                        vtxl.vertWeightIndices.Add(new int[] { vert.BoneIndices[0], vert.BoneIndices[1], vert.BoneIndices[2], vert.BoneIndices[3] });
                    }
                    else if (vert.BoneIndices?.Length > 0)
                    {
                        vtxl.vertWeights.Add(new Vector4(1, 0, 0, 0));
                        vtxl.vertWeightIndices.Add(new int[] { vert.BoneIndices[0], 0, 0, 0 });
                    }
                }

                vtxl.convertToLegacyTypes();
                aqp.vtxeList.Add(AquaObjectMethods.ConstructClassicVTXE(vtxl, out int vc));
                aqp.vtxlList.Add(vtxl);

                //Face data
                AquaObject.GenericTriangles genMesh = new AquaObject.GenericTriangles();

                List<Vector3> triList = new List<Vector3>();
                for (int id = 0; id < indices.Length - 2; id += 3)
                {
                    triList.Add(new Vector3(indices[id], indices[id + 1], indices[id + 2]));
                }

                genMesh.triList = triList;

                //Extra
                genMesh.vertCount = vertCount;
                genMesh.matIdList = new List<int>(new int[genMesh.triList.Count]);
                for (int j = 0; j < genMesh.matIdList.Count; j++)
                {
                    genMesh.matIdList[j] = aqp.tempMats.Count;
                }
                aqp.tempTris.Add(genMesh);

                //Material
                var mat = new AquaObject.GenericMaterial();
                var flverMat = mdl4.Materials[mesh.MaterialIndex];
                mat.matName = $"{flverMat.Name}|{mesh.MaterialIndex}";
                mat.texNames = flverMat.GetTexList();
                aqp.tempMats.Add(mat);
            }

            return aqp;
        }

        public static AquaObject FlverToAqua(IFlver flver, out AquaNode aqn, bool useMetaData = false)
        {
            AquaObject aqp = new NGSAquaObject();

            if (flver is FLVER2 flver2)
            {
                if (flver2.Header.Version > 0x20010)
                {
                    for (int i = 0; i < flver2.Bones.Count; i++)
                    {
                        aqp.bonePalette.Add((uint)i);
                    }
                }
            }
            aqn = new AquaNode();
            Vector3 maxTest = new Vector3();
            Vector3 minTest = new Vector3();
            for (int i = 0; i < flver.Bones.Count; i++)
            {
                var flverBone = flver.Bones[i];
                var parentId = flverBone.ParentIndex;

                FLVER.Bone.RotationOrder order = FLVER.Bone.RotationOrder.XZY;
                var tfmMat = Matrix4x4.Identity;

                if(flverBone.Rotation.X > maxTest.X)
                {
                    maxTest.X = flverBone.Rotation.X;
                } else if(flverBone.Rotation.X < minTest.X)
                {
                    minTest.X = flverBone.Rotation.X;
                }
                if (flverBone.Rotation.Y > maxTest.Y)
                {
                    maxTest.Y = flverBone.Rotation.Y;
                }
                else if (flverBone.Rotation.Y < minTest.Y)
                {
                    minTest.Y = flverBone.Rotation.Y;
                }
                if (flverBone.Rotation.Z > maxTest.Z)
                {
                    maxTest.Z = flverBone.Rotation.Z;
                }
                else if (flverBone.Rotation.Z < minTest.Z)
                {
                    minTest.Z = flverBone.Rotation.Z;
                }
                Matrix4x4 mat = flverBone.ComputeLocalTransform();
                mat *= tfmMat;

                //If there's a parent, multiply by it
                if (parentId != -1)
                {
                    var pn = aqn.nodeList[parentId];
                    var parentInvTfm = new Matrix4x4(pn.m1.X, pn.m1.Y, pn.m1.Z, pn.m1.W,
                                                  pn.m2.X, pn.m2.Y, pn.m2.Z, pn.m2.W,
                                                  pn.m3.X, pn.m3.Y, pn.m3.Z, pn.m3.W,
                                                  pn.m4.X, pn.m4.Y, pn.m4.Z, pn.m4.W);

                    Matrix4x4.Invert(parentInvTfm, out var invParentInvTfm);
                    mat = mat * invParentInvTfm;
                }
                if (parentId == -1 && i != 0)
                {
                    parentId = 0;
                }

                //Create AQN node
                NODE aqNode = new NODE();
                aqNode.boneShort1 = 0x1C0;
                aqNode.animatedFlag = 1;
                aqNode.parentId = parentId;
                aqNode.unkNode = -1;

                aqNode.scale = new Vector3(1, 1, 1);

                Matrix4x4.Invert(mat, out var invMat);
                aqNode.m1 = new Vector4(invMat.M11, invMat.M12, invMat.M13, invMat.M14);
                aqNode.m2 = new Vector4(invMat.M21, invMat.M22, invMat.M23, invMat.M24);
                aqNode.m3 = new Vector4(invMat.M31, invMat.M32, invMat.M33, invMat.M34);
                aqNode.m4 = new Vector4(invMat.M41, invMat.M42, invMat.M43, invMat.M44);
                aqNode.boneName.SetString(flverBone.Name);
                //Debug.WriteLine($"{i} " + aqNode.boneName.GetString());
                aqn.nodeList.Add(aqNode);
            }
            Debug.WriteLine(maxTest);
            Debug.WriteLine(minTest);
            DebugDumpToFile((FLVER0)flver, 0);
            DebugDumpWorldBonesToFile(aqn, 0);
            DebugDumpLocalBonesToFile((FLVER0)flver, 0);
            List<Matrix4x4> testRecompile = new List<Matrix4x4>();
            //I 100% believe there's a better way to do this when constructing the matrix, but for now we do this.
            for (int i = 0; i < aqn.nodeList.Count; i++)
            {
                var bone = aqn.nodeList[i];
                Matrix4x4.Invert(bone.GetInverseBindPoseMatrix(), out var mat);
                mat *= mirrorMatX;
                Matrix4x4.Decompose(mat, out var scale, out var rot, out var translation);
                bone.pos = translation;
                bone.eulRot = MathExtras.QuaternionToEuler(rot);

                Matrix4x4.Invert(mat, out var invMat);
                bone.SetInverseBindPoseMatrix(invMat);
                aqn.nodeList[i] = bone;
                testRecompile.Add(MathExtras.SetMatrixScale(mat));
            }
            DebugDumpLocalM4BonesToFile(testRecompile, 0);
            DebugDumpWorldBonesToFile(aqn, -1);
#if DEBUG
            AquaUtil.WriteBones(@"A:\Games\Demon's Souls (USA)\PS3_GAME\USRDIR\chr\c2000\" + "c2000 - Copy.fbx_initialMirror.aqn", aqn);
#endif

            for (int i = 0; i < flver.Meshes.Count; i++)
            {
                var mesh = flver.Meshes[i];

                var nodeMatrix = Matrix4x4.Identity;

                //Vert data
                var vertCount = mesh.Vertices.Count;
                AquaObject.VTXL vtxl = new AquaObject.VTXL();

                if (mesh.Dynamic > 0)
                {
                    if (mesh is FLVER0.Mesh flv)
                    {

                        for (int b = 0; b < flv.BoneIndices.Length; b++)
                        {
                            if (flv.BoneIndices[b] == -1)
                            {
                                break;
                            }
                            vtxl.bonePalette.Add((ushort)flv.BoneIndices[b]);
                        }
                    }
                    else if (mesh is FLVER2.Mesh flv2)
                    {
                        for (int b = 0; b < flv2.BoneIndices.Count; b++)
                        {
                            if (flv2.BoneIndices[b] == -1)
                            {
                                break;
                            }
                            vtxl.bonePalette.Add((ushort)flv2.BoneIndices[b]);
                        }
                    }
                }
                List<int> indices = new List<int>();
                if (flver is FLVER0)
                {
                    FLVER0.Mesh mesh0 = (FLVER0.Mesh)mesh;

                    vtxl.bonePalette = new List<ushort>();
                    for (int b = 0; b < mesh0.BoneIndices.Length; b++)
                    {
                        if (mesh0.BoneIndices[b] == -1)
                        {
                            break;
                        }
                        vtxl.bonePalette.Add((ushort)mesh0.BoneIndices[b]);
                    }
                    indices = mesh0.Triangulate(((FLVER0)flver).Header.Version);
                }
                else if (flver is FLVER2)
                {
                    FLVER2.Mesh mesh2 = (FLVER2.Mesh)mesh;

                    //Dark souls 3+ (Maybe bloodborne too) use direct bone id references instead of a bone palette
                    vtxl.bonePalette = new List<ushort>();
                    for (int b = 0; b < mesh2.BoneIndices.Count; b++)
                    {
                        if (mesh2.BoneIndices[b] == -1)
                        {
                            break;
                        }
                        vtxl.bonePalette.Add((ushort)mesh2.BoneIndices[b]);
                    }

                    FLVER2.FaceSet faceSet = mesh2.FaceSets[0];
                    indices = faceSet.Triangulate(mesh2.Vertices.Count < ushort.MaxValue);
                }
                else
                {
                    throw new Exception("Unexpected flver variant");
                }

                List<Vector3> normals = new List<Vector3>();
                for (int v = 0; v < vertCount; v++)
                {
                    var vert = mesh.Vertices[v];
                    vtxl.vertPositions.Add(Vector3.Transform(vert.Position, mirrorMatX));
                    vtxl.vertNormals.Add(Vector3.Transform(vert.Normal, mirrorMatX));

                    if (vert.UVs.Count > 0)
                    {
                        var uv1 = vert.UVs[0];
                        vtxl.uv1List.Add(new Vector2(uv1.X, uv1.Y));
                    }
                    if (vert.UVs.Count > 1)
                    {
                        var uv2 = vert.UVs[1];
                        vtxl.uv2List.Add(new Vector2(uv2.X, uv2.Y));
                    }
                    if (vert.UVs.Count > 2)
                    {
                        var uv3 = vert.UVs[2];
                        vtxl.uv3List.Add(new Vector2(uv3.X, uv3.Y));
                    }
                    if (vert.UVs.Count > 3)
                    {
                        var uv4 = vert.UVs[3];
                        vtxl.uv4List.Add(new Vector2(uv4.X, uv4.Y));
                    }

                    if (vert.Colors.Count > 0)
                    {
                        var color = vert.Colors[0];
                        vtxl.vertColors.Add(new byte[] { (byte)(color.B * 255), (byte)(color.G * 255), (byte)(color.R * 255), (byte)(color.A * 255) });
                    }
                    if (vert.Colors.Count > 1)
                    {
                        var color2 = vert.Colors[1];
                        vtxl.vertColor2s.Add(new byte[] { (byte)(color2.B * 255), (byte)(color2.G * 255), (byte)(color2.R * 255), (byte)(color2.A * 255) });
                    }

                    if (vert.BoneWeights.Length > 0)
                    {
                        vtxl.vertWeights.Add(new Vector4(vert.BoneWeights[0], vert.BoneWeights[1], vert.BoneWeights[2], vert.BoneWeights[3]));
                        vtxl.vertWeightIndices.Add(new int[] { vert.BoneIndices[0], vert.BoneIndices[1], vert.BoneIndices[2], vert.BoneIndices[3] });
                    }
                    else if (vert.BoneIndices.Length > 0)
                    {
                        vtxl.vertWeights.Add(new Vector4(1, 0, 0, 0));
                        vtxl.vertWeightIndices.Add(new int[] { vert.BoneIndices[0], 0, 0, 0 });
                    }
                    else if (vert.NormalW < 65535)
                    {
                        vtxl.vertWeights.Add(new Vector4(1, 0, 0, 0));
                        vtxl.vertWeightIndices.Add(new int[] { vert.NormalW, 0, 0, 0 });
                    }
                }

                vtxl.convertToLegacyTypes();
                aqp.vtxeList.Add(AquaObjectMethods.ConstructClassicVTXE(vtxl, out int vc));
                aqp.vtxlList.Add(vtxl);

                //Face data
                AquaObject.GenericTriangles genMesh = new AquaObject.GenericTriangles();

                List<Vector3> triList = new List<Vector3>();
                for (int id = 0; id < indices.Count - 2; id += 3)
                {
                    triList.Add(new Vector3(indices[id], indices[id + 1], indices[id + 2]));
                }

                genMesh.triList = triList;

                //Extra
                genMesh.vertCount = vertCount;
                genMesh.matIdList = new List<int>(new int[genMesh.triList.Count]);
                for (int j = 0; j < genMesh.matIdList.Count; j++)
                {
                    genMesh.matIdList[j] = aqp.tempMats.Count;
                }
                aqp.tempTris.Add(genMesh);

                //Material
                var mat = new AquaObject.GenericMaterial();
                var flverMat = flver.Materials[mesh.MaterialIndex];
                if(useMetaData)
                {
                    mat.matName = $"{flverMat.Name}|{Path.GetFileName(flverMat.MTD)}|{mesh.MaterialIndex}";
                } else
                {
                    mat.matName = $"{flverMat.Name}";
                }
                mat.texNames = new List<string>();
                foreach (var tex in flverMat.Textures)
                {
                    mat.texNames.Add(Path.GetFileName(tex.Path));
                }
                aqp.tempMats.Add(mat);
            }

            return aqp;
        }

        public static void ConvertModelToFlverAndWrite(string initialFilePath, string outPath, float scaleFactor, bool preAssignNodeIds, bool isNGS, SoulsGame game)
        {
            ((FLVER0)ConvertModelToFlver(initialFilePath, scaleFactor, preAssignNodeIds, isNGS, game, null)).Write(outPath);
        }

        public static IFlver ConvertModelToFlver(string initialFilePath, float scaleFactor, bool preAssignNodeIds, bool isNGS, SoulsGame game, IFlver referenceFlver = null)
        {
            var aqp = ModelImporter.AssimpAquaConvertFull(initialFilePath, scaleFactor, preAssignNodeIds, isNGS, out AquaNode aqn);

            //Demon's Souls has a limit of 28 bones per mesh.
            AquaObjectMethods.BatchSplitByBoneCount(aqp, 28, true);
            return AquaToFlver(initialFilePath, aqp, aqn, game, referenceFlver);
        }

        public static IFlver AquaToFlver(string initialFilePath, AquaObject aqp, AquaNode aqn, SoulsGame game, IFlver referenceFlver = null)
        {
            switch (game)
            {
                case SoulsGame.DemonsSouls:
                    break;
                default:
                    return null;
            }

            var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var mtdDict = ReadMTDLayoutData(Path.Combine(exePath, "DeSMtdLayoutData.bin"));
            FLVER0 flver = new FLVER0();
            flver.Header = new FLVER0Header();
            flver.Header.BigEndian = true;
            flver.Header.Unicode = true;
            flver.Header.Version = 0x15;
            flver.Header.Unk4A = 0x1;
            flver.Header.Unk4B = 0;
            flver.Header.Unk4C = 0xFFFF;
            flver.Header.Unk5C = 0;
            flver.Header.VertexIndexSize = 16; //Probably needs to be 32 if we go over 65535 faces in a mesh, but DeS PS3 probably never does that.

            List<string> usedMaterials = new List<string>();
            Dictionary<string, FLVER0.Material> matDict = new Dictionary<string, FLVER0.Material>();
            if (referenceFlver != null)
            {
                for (int i = 0; i < referenceFlver.Materials.Count; i++)
                {
                    matDict.Add(referenceFlver.Materials[i].Name, (FLVER0.Material)referenceFlver.Materials[i]);
                }
            }

            var dummyPath = Path.ChangeExtension(initialFilePath, "flver.dummyData.json");
            var matPath = Path.ChangeExtension(initialFilePath, "flver.matData.json");
            var dummyDataText = File.Exists(dummyPath) ? File.ReadAllText(dummyPath) : null;
            var materialDataText = File.Exists(matPath) ? File.ReadAllText(matPath) : null;
            //Dummies - Deserialize from JSON and apply if there's not a reference flver selected. Use reference flver if available
            if (referenceFlver != null)
            {
                flver.Dummies = (List<FLVER.Dummy>)referenceFlver.Dummies;
            } else if(dummyDataText != null)
            {
                List<FLVER.Dummy> metaDummies = null;
                metaDummies = JsonConvert.DeserializeObject<List<FLVER.Dummy>>(dummyDataText);
                flver.Dummies = metaDummies;
            } else
            {
                flver.Dummies = new List<FLVER.Dummy>();
            }

            //Materials - Deserialize tex lists from JSON and apply. Use reference flver for tex names if available
            List<FLVER0.Material> metaMats = null;
            if(materialDataText != null)
            {
                metaMats = JsonConvert.DeserializeObject<List<FLVER0.Material>>(materialDataText);
                for(int i = 0; i < metaMats.Count; i++)
                {
                    if (!matDict.ContainsKey(metaMats[i].Name))
                    {
                        matDict.Add(metaMats[i].Name, metaMats[i]);
                    }
                }

            }

            flver.Materials = new List<FLVER0.Material>();
            for (int i = 0; i < aqp.meshList.Count; i++)
            {
                int pso2MatId = aqp.meshList[i].mateIndex;
                var texList = AquaObjectMethods.GetTexListNames(aqp, aqp.meshList[i].tsetIndex);
                var pso2Mat = aqp.mateList[pso2MatId];
                string rawName;
                if(aqp.matUnicodeNames.Count > i)
                {
                    rawName = aqp.matUnicodeNames[i];
                } else
                {
                    rawName = pso2Mat.matName.GetString();
                }
                var nameSplit = rawName.Split('|');
                string name = nameSplit[0];
                var matIndex = usedMaterials.IndexOf(name);
                string mtd = null;
                int ogMatIndex = -1;
                if(nameSplit.Length > 1)
                {
                    mtd = nameSplit[1];
                    if(nameSplit.Length > 2)
                    {
                        ogMatIndex = Int32.Parse(nameSplit[2]);
                    }
                }
                FLVER0.Material flvMat;
                if (metaMats != null && ogMatIndex < metaMats.Count && ogMatIndex != -1)
                {
                    flvMat = metaMats[ogMatIndex];
                    flver.Materials.Add(flvMat);
                    continue;
                }
                if (matIndex != -1)
                {
                    continue;
                }
                else
                {
                    usedMaterials.Add(name);
                    if (matDict.TryGetValue(name, out flvMat))
                    {
                        flver.Materials.Add(flvMat);
                        continue;
                    }
                    else
                    {
                        flvMat = new FLVER0.Material(true); 
                        flvMat.Name = name;
                        mtd = mtd ?? "n:\\orthern\\limit\\p_metal[dsb]_skin.mtd";
                        flvMat.MTD = mtd;
                        if(texList.Count > 0)
                        {
                            FLVER0.Texture tex = new FLVER0.Texture();
                            flvMat.Textures = new List<FLVER0.Texture>();
                            tex.Path = texList[0];
                            tex.Type = "g_Diffuse";
                            flvMat.Textures.Add(tex);
                        }
                    }
                }
                flvMat.Layouts = new List<FLVER0.BufferLayout>();
                flvMat.Name = name;
                var mtdShortName = Path.GetFileName(mtd).ToLower();
                if (mtdDict.ContainsKey(mtdShortName))
                {
                    flvMat.Layouts.Add(mtdDict[mtdShortName]);
                    flvMat.MTD = mtd;
                } else
                {
                    flvMat.Layouts.Add(mtdDict["p_metal[dsb]_skin.mtd"]);
                    flvMat.MTD = "p_metal[dsb]_skin.mtd";
                }
                flver.Materials.Add(flvMat);

            }

            //Bones store bounding which encompass the extents of all vertices onto which they are weighted.
            //When no vertices are weighted to them, this bounding is -3.402823e+38 for all min bound values and 3.402823e+38 for all max bound values
            Dictionary<int, Vector3> MaxBoundingBoxByBone = new Dictionary<int, Vector3>();
            Dictionary<int, Vector3> MinBoundingBoxByBone = new Dictionary<int, Vector3>();
            var defaultMaxBound = new Vector3(3.402823e+38f, 3.402823e+38f, 3.402823e+38f);
            var defaultMinBound = new Vector3(-3.402823e+38f, -3.402823e+38f, -3.402823e+38f);
            Vector3? maxBounding = null;
            Vector3? minBounding = null;

            flver.Meshes = new List<FLVER0.Mesh>();
            for (int i = 0; i < aqp.meshList.Count; i++)
            {
                var mesh = aqp.meshList[i];
                var vtxl = aqp.vtxlList[mesh.vsetIndex];
                var faces = aqp.strips[mesh.psetIndex];
                var shader = aqp.shadList[mesh.shadIndex];
                var render = aqp.rendList[mesh.rendIndex];
                var flvMat = flver.Materials[mesh.mateIndex];

                FLVER0.Mesh flvMesh = new FLVER0.Mesh();
                flvMesh.MaterialIndex = (byte)mesh.mateIndex;
                flvMesh.BackfaceCulling = render.twosided > 0;
                //flvMesh.Dynamic = vtxl.vertWeights.Count > 0 ? (byte)1 : (byte)0;
                flvMesh.Dynamic = 1;
                flvMesh.Vertices = new List<FLVER.Vertex>();
                flvMesh.VertexIndices = new List<int>();
                flvMesh.DefaultBoneIndex = 0; //Maybe set properly later from the aqp version if important
                flvMesh.BoneIndices = new short[28];
                for(int b = 0; b < 28; b++)
                {
                    if(vtxl.bonePalette.Count > 0)
                    {

                        if (vtxl.bonePalette.Count > b)
                        {
                            flvMesh.BoneIndices[b] = (short)vtxl.bonePalette[b];
                        }
                        else
                        {
                            flvMesh.BoneIndices[b] = -1;
                        }
                    } else
                    {
                        if (aqp.bonePalette.Count > b)
                        {
                            flvMesh.BoneIndices[b] = (short)aqp.bonePalette[b];
                        }
                        else
                        {
                            flvMesh.BoneIndices[b] = -1;
                        }
                    }
                }

                //Handle faces
                //Possibly implement tristripping? 
                flvMesh.UseTristrips = false;
                foreach(var ind in faces.triStrips)
                {
                    flvMesh.VertexIndices.Add(ind);
                }

                //Handle vertices
                for (int v = 0; v < vtxl.vertPositions.Count; v++)
                {
                    var vert = new FLVER.Vertex();

                    for (int l = 0; l < flvMat.Layouts[0].Count; l++)
                    {
                        switch (flvMat.Layouts[0][l].Semantic)
                        {
                            case FLVER.LayoutSemantic.Position:
                                var pos = Vector3.Transform(vtxl.vertPositions[v], mirrorMatX);
                                vert.Position = pos;

                                //Calc model bounding
                                if (maxBounding == null)
                                {
                                    maxBounding = pos;
                                    minBounding = pos;
                                }
                                else
                                {
                                    maxBounding = AquaObjectMethods.GetMaximumBounding(pos, (Vector3)maxBounding);
                                    minBounding = AquaObjectMethods.GetMinimumBounding(pos, (Vector3)minBounding);
                                }
                                break;
                            case FLVER.LayoutSemantic.UV:
                                AddUV(vert, vtxl, v);
                                if (flvMat.Layouts[0][l].Type == FLVER.LayoutType.UVPair)
                                {
                                    AddUV(vert, vtxl, v);
                                }
                                break;
                            case FLVER.LayoutSemantic.Normal:
                                if (vtxl.vertNormals.Count > 0)
                                {
                                    vert.Normal = Vector3.Transform(vtxl.vertNormals[v], mirrorMatX);
                                }
                                else
                                {
                                    vert.Normal = Vector3.One;
                                }
                                vert.NormalW = 127;
                                break;
                            case FLVER.LayoutSemantic.Tangent:
                                if (vtxl.vertTangentList.Count > 0)
                                {
                                    vert.Tangents.Add(new Vector4(Vector3.Transform(vtxl.vertTangentList[v], mirrorMatX), 0));
                                }
                                else
                                {
                                    vert.Tangents.Add(Vector4.One);
                                }
                                break;
                            case FLVER.LayoutSemantic.Bitangent:
                                if (vtxl.vertBinormalList.Count > 0)
                                {
                                    vert.Bitangent = new Vector4(Vector3.Transform(vtxl.vertBinormalList[v], mirrorMatX), 0);
                                }
                                else
                                {
                                    vert.Bitangent = Vector4.One;
                                }
                                break;
                            case FLVER.LayoutSemantic.VertexColor:
                                if (vert.Colors.Count > 0)
                                {
                                    if (vtxl.vertColor2s.Count > 0)
                                    {
                                        vert.Colors.Add(new FLVER.VertexColor(vtxl.vertColor2s[v][3], vtxl.vertColor2s[v][2], vtxl.vertColor2s[v][1], vtxl.vertColor2s[v][0]));
                                    }
                                    else
                                    {
                                        vert.Colors.Add(new FLVER.VertexColor(1.0f, 1.0f, 1.0f, 1.0f));
                                    }
                                }
                                else
                                {
                                    if (vtxl.vertColors.Count > 0)
                                    {
                                        vert.Colors.Add(new FLVER.VertexColor(vtxl.vertColors[v][3], vtxl.vertColors[v][2], vtxl.vertColors[v][1], vtxl.vertColors[v][0]));
                                    }
                                    else
                                    {
                                        vert.Colors.Add(new FLVER.VertexColor(1.0f, 1.0f, 1.0f, 1.0f));
                                    }
                                }
                                break;
                            case FLVER.LayoutSemantic.BoneIndices:
                                int[] indices;
                                if(vtxl.vertWeightIndices.Count == 0)
                                {
                                    indices = new int[4];
                                } else
                                {
                                    indices = vtxl.vertWeightIndices[v];
                                }
                                vert.BoneIndices = new FLVER.VertexBoneIndices() { };
                                vert.BoneIndices[0] = indices[0];
                                vert.BoneIndices[1] = indices[1];
                                vert.BoneIndices[2] = indices[2];
                                vert.BoneIndices[3] = indices[3];

                                int bone0 = indices[0];
                                int bone1 = indices[1];
                                int bone2 = indices[2];
                                int bone3 = indices[3];
                                if (aqp is ClassicAquaObject)
                                {
                                    bone0 = vtxl.bonePalette[bone0];
                                    bone1 = vtxl.bonePalette[bone1];
                                    bone2 = vtxl.bonePalette[bone2];
                                    bone3 = vtxl.bonePalette[bone3];
                                }

                                List<int> boneCheckList = new List<int>();
                                boneCheckList.Add(bone0);
                                if (boneCheckList.Contains(bone1))
                                {
                                    bone1 = -1;
                                }
                                if (boneCheckList.Contains(bone2))
                                {
                                    bone2 = -1;
                                }
                                if (boneCheckList.Contains(bone3))
                                {
                                    bone3 = -1;
                                }

                                //Calc bone bounding. Bone bounding is made up of extents from each vertex with it assigned. 
                                CheckBounds(MaxBoundingBoxByBone, MinBoundingBoxByBone, vert.Position, bone0);
                                CheckBounds(MaxBoundingBoxByBone, MinBoundingBoxByBone, vert.Position, bone1);
                                CheckBounds(MaxBoundingBoxByBone, MinBoundingBoxByBone, vert.Position, bone2);
                                CheckBounds(MaxBoundingBoxByBone, MinBoundingBoxByBone, vert.Position, bone3);
                                break;
                            case FLVER.LayoutSemantic.BoneWeights:
                                Vector4 weights;
                                if (vtxl.vertWeights.Count == 0)
                                {
                                    weights = new Vector4();
                                    weights.X = 1.0f;
                                } else
                                {
                                    weights = vtxl.vertWeights[v];
                                }
                                    vert.BoneWeights = new FLVER.VertexBoneWeights() { };
                                vert.BoneWeights[0] = weights.X;
                                vert.BoneWeights[1] = weights.Y;
                                vert.BoneWeights[2] = weights.Z;
                                vert.BoneWeights[3] = weights.W;
                                break;
                        }
                    }

                    flvMesh.Vertices.Add(vert);
                }

                TangentSolver.SolveTangentsDemonsSouls(flvMesh, flver.Header.Version);
                flver.Meshes.Add(flvMesh);
            }

            List<int> rootSiblings = new List<int>();
            flver.Bones = new List<FLVER.Bone>();

            AquaUtil.WriteBones(initialFilePath + "_pre.aqn", aqn);
            DebugDumpWorldBonesToFile(aqn, 1);
            List<Matrix4x4> matList = new List<Matrix4x4>();
            AquaNode aqn2 = new AquaNode();
            aqn2.nodeList = new List<NODE>();

            for (int i = 0; i < aqn.nodeList.Count; i++)
            {
                var aqBone = aqn.nodeList[i];
                Matrix4x4.Invert(aqBone.GetInverseBindPoseMatrix(), out Matrix4x4 aqBoneWorldTfm);
                aqBoneWorldTfm *= mirrorMatX;
                aqBoneWorldTfm = MathExtras.SetMatrixScale(aqBoneWorldTfm, new Vector3(1, 1, 1));

                //Set the inverted transform so when we read it back we can use it for parent calls
                Matrix4x4.Invert(aqBoneWorldTfm, out Matrix4x4 aqBoneInvWorldTfm);
                aqBone.SetInverseBindPoseMatrix(aqBoneInvWorldTfm);
                aqn.nodeList[i] = aqBone;

                FLVER.Bone bone = new FLVER.Bone();
                var name = bone.Name = aqBone.boneName.GetString();
                bone.BoundingBoxMax = MaxBoundingBoxByBone.ContainsKey(i) ? MaxBoundingBoxByBone[i] : defaultMaxBound;
                bone.BoundingBoxMin = MinBoundingBoxByBone.ContainsKey(i) ? MinBoundingBoxByBone[i] : defaultMinBound;
                bone.Unk3C = bone.Name.ToUpper().EndsWith("NUB") ? 1 : 0;
                bone.ParentIndex = (short)aqBone.parentId;
                bone.ChildIndex = (short)aqBone.firstChild;
                bone.PreviousSiblingIndex = (short)GetPreviousSibling(aqn.nodeList, i, rootSiblings);
                bone.NextSiblingIndex = (short)aqn.nodeList[i].nextSibling;
                bone.ChildIndex = (short)aqn.nodeList[i].firstChild;

                Matrix4x4 localTfm;
                if (aqBone.parentId == -1)
                {
                    rootSiblings.Add(i);
                    localTfm = aqBoneWorldTfm;
                } else
                {
                    //Calc local transforms
                    //Parent is already mirrored from earlier processing
                    var parBoneInvTfm = aqn.nodeList[aqBone.parentId].GetInverseBindPoseMatrix();
                    localTfm = Matrix4x4.Multiply(aqBoneWorldTfm, parBoneInvTfm);

                    Matrix4x4.Invert(aqn.nodeList[aqBone.parentId].GetInverseBindPoseMatrix(), out var parBoneTfm);
                    Matrix4x4.Invert(localTfm * parBoneTfm, out var newInvTfm);
                    AquaNode.NODE node = aqBone;
                    node.SetInverseBindPoseMatrix(newInvTfm);
                    aqn2.nodeList.Add(node);
                    
                    Matrix4x4.Decompose(localTfm, out var tempScale, out var tempRotation, out var tempTranslation);
                    var eulerAngles2 = MathExtras.QuaternionToEulerRadiansNoHandle(tempRotation);

                    var matrix = Matrix4x4.Identity;
                    matrix *= Matrix4x4.CreateScale(tempScale);
                    var tempRotationMtx = Matrix4x4.CreateFromQuaternion(tempRotation);
                    matrix *= tempRotationMtx;
                    matrix *= Matrix4x4.CreateTranslation(tempTranslation);



                    var matrix2 = Matrix4x4.Identity;
                    matrix2 *= Matrix4x4.CreateScale(tempScale);
                    var tempRotationMtx2 = Matrix4x4.CreateRotationX(eulerAngles2.X) *
                        Matrix4x4.CreateRotationY(eulerAngles2.Y) *
                        Matrix4x4.CreateRotationZ(eulerAngles2.Z);

                    matrix2 *= tempRotationMtx2;
                    matrix2 *= Matrix4x4.CreateTranslation(tempTranslation);

                    var matrix3 = Matrix4x4.Identity;
                    matrix3 *= Matrix4x4.CreateScale(tempScale);
                    var tempQuat = MathExtras.EulerToQuaternion(eulerAngles2 * (float)(180 / Math.PI));
                    matrix *= Matrix4x4.CreateFromQuaternion(tempQuat);
                    matrix3 *= tempRotationMtx2;
                    matrix3 *= Matrix4x4.CreateTranslation(tempTranslation);



                    var matrix4 = Matrix4x4.Identity;
                    matrix4 *= Matrix4x4.CreateScale(tempScale);
                    var tempRotationMtx3 = Matrix4x4.CreateRotationX(eulerAngles2.X) *
                        Matrix4x4.CreateRotationZ(eulerAngles2.Z) *
                        Matrix4x4.CreateRotationY(eulerAngles2.Y);

                    matrix4 *= tempRotationMtx3;
                    matrix4 *= Matrix4x4.CreateTranslation(tempTranslation);


                    var eulerAngles3 = MathExtras.QuaternionToEulerRadiansNoHandle(tempRotation);
                    var afafQuat = tempRotation;
                    var tempQuat2 = MathExtras.EulerToQuaternion(eulerAngles3 * (float)(180 / Math.PI));

                    var world = aqBoneWorldTfm;
                    var recalcedWorld = localTfm * parBoneTfm;
                    var recalcedWorld2 = matrix * parBoneTfm;
                    var recalcedWorld3 = matrix2 * parBoneTfm;
                    var recalcedWorld4 = matrix3 * parBoneTfm;
                    var recalcedWorld5 = matrix4 * parBoneTfm;
                }
                Matrix4x4.Decompose(localTfm, out var scale, out var rotation, out var translation);
                matList.Add(localTfm);

                bone.Translation = translation;

                //Rotate order based on scale x y z values as a hack? (ex. if direction for y is -1 instead of x, do different order 
                var eulerAngles = MathExtras.QuaternionToEulerRadiansNoHandle(rotation);
                bone.Rotation = eulerAngles;
                bone.Scale = new Vector3(1, 1, 1);

                var mat = bone.ComputeLocalTransform();
                matList.Add(mat);
                flver.Bones.Add(bone);
            }
            DebugDumpLocalBonesToFile((FLVER0)flver, 1);
            DebugDumpWorldBonesToFile(aqn, 2);
            DebugDumpWorldBonesToFile(aqn2, 3);
            DebugDumpLocalM4BonesToFile(matList, 1);
            AquaUtil.WriteBones(initialFilePath + "_post.aqn", aqn);

            flver.Header.BoundingBoxMax = (Vector3)maxBounding;
            flver.Header.BoundingBoxMin = (Vector3)minBounding;

            DebugDumpToFile((FLVER0)flver, 1);
            return flver;
        }

        private static int GetPreviousSibling(List<NODE> nodeList, int boneIndex, List<int> rootSiblings)
        {
            if(boneIndex == 0)
            {
                return -1;
            }

            var curBone = nodeList[boneIndex];
            if(curBone.parentId == -1)
            {
                int currentPreviousRootSibling = -1;
                for(int i = 0; i < rootSiblings.Count; i++)
                {
                    if (rootSiblings[i] < boneIndex && rootSiblings[i] > currentPreviousRootSibling)
                    {
                        currentPreviousRootSibling = rootSiblings[i];
                    }
                }

                return currentPreviousRootSibling;
            }

            var parBone = nodeList[curBone.parentId];

            if (parBone.firstChild == boneIndex)
            {
                return -1;
            }

            var curSiblingBone = nodeList[parBone.firstChild];

            while (curSiblingBone.nextSibling != -1)
            {
                if (curSiblingBone.nextSibling == boneIndex)
                {
                    return curSiblingBone.nextSibling;
                }

                curSiblingBone = nodeList[curSiblingBone.nextSibling];
            }

            throw new Exception("Misconfigured ");
        }

        private static void AddUV(FLVER.Vertex vert, AquaObject.VTXL vtxl, int vertId)
        {
            switch (vert.UVs.Count)
            {
                case 0:
                    if(vtxl.uv1List.Count > vertId)
                    {
                        vert.UVs.Add(new Vector3(vtxl.uv1List[vertId], 0));
                    } else
                    {
                        vert.UVs.Add(new Vector3(0, 0, 0));
                    }
                    break;
                case 1:
                    if (vtxl.uv2List.Count > vertId)
                    {
                        vert.UVs.Add(new Vector3(vtxl.uv2List[vertId], 0));
                    }
                    else
                    {
                        vert.UVs.Add(new Vector3(0, 0, 0));
                    }
                    break;
                case 2:
                    if (vtxl.uv3List.Count > vertId)
                    {
                        vert.UVs.Add(new Vector3(vtxl.uv3List[vertId], 0));
                    }
                    else
                    {
                        vert.UVs.Add(new Vector3(0, 0, 0));
                    }
                    break;
                case 3:
                    if (vtxl.uv4List.Count > vertId)
                    {
                        vert.UVs.Add(new Vector3(vtxl.uv4List[vertId], 0));
                    }
                    else
                    {
                        vert.UVs.Add(new Vector3(0, 0, 0));
                    }
                    break;
            }

        }

        private static void CheckBounds(Dictionary<int, Vector3> MaxBoundingBoxByBone, Dictionary<int, Vector3> MinBoundingBoxByBone, Vector3 vec3, int boneId)
        {
            if (boneId != -1 && !MaxBoundingBoxByBone.ContainsKey(boneId))
            {
                MaxBoundingBoxByBone[boneId] = vec3;
                MinBoundingBoxByBone[boneId] = vec3;
            }
            else if (boneId != -1)
            {
                MaxBoundingBoxByBone[boneId] = AquaObjectMethods.GetMaximumBounding(MaxBoundingBoxByBone[boneId], vec3);
                MinBoundingBoxByBone[boneId] = AquaObjectMethods.GetMinimumBounding(MinBoundingBoxByBone[boneId], vec3);
            }
        }

        public static void GetDeSLayoutMTDInfo(string desPath)
        {
            Dictionary<string, FLVER0.BufferLayout> mtdLayoutsRawDict = new Dictionary<string, FLVER0.BufferLayout>();
            var files = Directory.EnumerateFiles(desPath, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var loadedFiles = ReadFilesNullable<FLVER0>(file);
                if (loadedFiles != null && loadedFiles.Count > 0)
                {
                    foreach (var fileSet in loadedFiles)
                    {
                        var flver = fileSet.File;

                        foreach (var mat in flver.Materials)
                        {
                            var layout = mat.Layouts[0];
                            if (!mtdLayoutsRawDict.ContainsKey(mat.MTD))
                            {
                                mtdLayoutsRawDict.Add(mat.MTD, layout);
                            }
                        }
                    }
                }

                List<byte> mtdLayoutBytes = new List<byte>();
                mtdLayoutBytes.AddRange(BitConverter.GetBytes(mtdLayoutsRawDict.Count));
                foreach (var entry in mtdLayoutsRawDict)
                {
                    mtdLayoutBytes.AddRange(BitConverter.GetBytes(entry.Key.Length * 2));
                    mtdLayoutBytes.AddRange(UnicodeEncoding.Unicode.GetBytes(entry.Key));
                    mtdLayoutBytes.AddRange(BitConverter.GetBytes(entry.Value.Count));
                    foreach (var layoutEntry in entry.Value)
                    {
                        mtdLayoutBytes.Add((byte)layoutEntry.Type);
                        mtdLayoutBytes.Add((byte)layoutEntry.Semantic);
                    }
                }

                var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                File.WriteAllBytes(Path.Combine(exePath, "DeSMtdLayoutData.bin"), mtdLayoutBytes.ToArray());
            }
        }

        public static Dictionary<string, FLVER0.BufferLayout> ReadMTDLayoutData(string dataPath)
        {
            var layoutsRaw = File.ReadAllBytes(dataPath);
            int offset = 0;
            int entryCount = BitConverter.ToInt32(layoutsRaw, 0);
            offset += 4;
            Dictionary<string, FLVER0.BufferLayout> layouts = new Dictionary<string, FLVER0.BufferLayout>();
            for (int i = 0; i < entryCount; i++)
            {
                int strByteLen = BitConverter.ToInt32(layoutsRaw, offset);
                offset += 4;
                string mtd = Encoding.Unicode.GetString(layoutsRaw, offset, strByteLen).ToLower();
                offset += strByteLen;

                int layoutLen = BitConverter.ToInt32(layoutsRaw, offset);
                offset += 4;
                FLVER0.BufferLayout layout = new FLVER0.BufferLayout();
                for (int j = 0; j < layoutLen; j++)
                {
                    byte type = layoutsRaw[offset];
                    offset += 1;
                    byte semantic = layoutsRaw[offset];
                    offset += 1;

                    layout.Add(new FLVER.LayoutMember((FLVER.LayoutType)type, (FLVER.LayoutSemantic)semantic, j, 0));
                }
                if (!layouts.ContainsKey(Path.GetFileName(mtd)))
                {
                    layouts.Add(Path.GetFileName(mtd), layout);
                }
            }

            return layouts;
        }

        public class URIFlver0Pair
        {
            public string Uri { get; set; }
            public FLVER0 File { get; set; }
        }

        public static List<URIFlver0Pair> ReadFilesNullable<TFormat>(string path)
    where TFormat : SoulsFile<TFormat>, new()
        {
            if (BND3.Is(path))
            {
                var bnd3 = BND3.Read(path);
                var selected = bnd3.Files.Where(f => Path.GetExtension(f.Name) == ".flver");
                List<URIFlver0Pair> Files = new List<URIFlver0Pair>();
                foreach (var file in bnd3.Files)
                {
                    if (Path.GetExtension(file.Name) == ".flver")
                    {
                        Files.Add(new URIFlver0Pair() { Uri = file.Name, File = SoulsFile<FLVER0>.Read(file.Bytes) });
                    }
                }
                return Files;
            }
            else if (BND4.Is(path))
            {
                var bnd4 = BND4.Read(path);
                var selected = bnd4.Files.Where(f => Path.GetExtension(f.Name) == ".flver");
                List<URIFlver0Pair> Files = new List<URIFlver0Pair>();
                foreach (var file in bnd4.Files)
                {
                    if (Path.GetExtension(file.Name) == ".flver")
                    {
                        Files.Add(new URIFlver0Pair() { Uri = file.Name, File = SoulsFile<FLVER0>.Read(file.Bytes) });
                    }
                }
                return Files;
            }
            else
            {
                var file = File.ReadAllBytes(path);
                if (FLVER0.Is(file))
                {
                    return new List<URIFlver0Pair>() { new URIFlver0Pair() { Uri = path, File = SoulsFile<FLVER0>.Read(file) } };
                }
                return null;
            }

        }

        public static bool IsMaterialUsingSkinning(FLVER0.Material mat)
        {
            for (int i = 0; i < mat.Layouts.Count; i++)
            {
                //HOPEFULLY this should always be 0. If not, pain
                if (mat.Layouts[0][i].Semantic == FLVER.LayoutSemantic.BoneWeights)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
