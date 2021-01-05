using UnityEngine;
using System.Collections.Generic;

namespace U2Three.Editor
{
    public static class MeshExtension
    {
        private class Vertices
        {
            List<Vector3> verts = null;
            List<Vector2> uv1 = null;
            List<Vector2> uv2 = null;
            List<Vector2> uv3 = null;
            List<Vector2> uv4 = null;
            List<Vector3> normals = null;
            List<Vector4> tangents = null;
            List<Color32> colors = null;
            List<BoneWeight> boneWeights = null;

            public Vertices()
            {
                verts = new List<Vector3>();
            }

            public Vertices(Mesh mesh)
            {
                verts = CreateList(mesh.vertices);
                uv1 = CreateList(mesh.uv);
                uv2 = CreateList(mesh.uv2);
                uv3 = CreateList(mesh.uv3);
                uv4 = CreateList(mesh.uv4);
                normals = CreateList(mesh.normals);
                tangents = CreateList(mesh.tangents);
                colors = CreateList(mesh.colors32);
                boneWeights = CreateList(mesh.boneWeights);
            }

            private List<T> CreateList<T>(T[] source)
            {
                if (source == null || source.Length == 0)
                    return null;
                return new List<T>(source);
            }

            private void Copy<T>(ref List<T> dst, List<T> src, int index)
            {
                if (src == null)
                    return;
                if (dst == null)
                    dst = new List<T>();
                dst.Add(src[index]);
            }

            public int Add(Vertices src, int index)
            {
                int i = verts.Count;
                Copy(ref verts, src.verts, index);
                Copy(ref uv1, src.uv1, index);
                Copy(ref uv2, src.uv2, index);
                Copy(ref uv3, src.uv3, index);
                Copy(ref uv4, src.uv4, index);
                Copy(ref normals, src.normals, index);
                Copy(ref tangents, src.tangents, index);
                Copy(ref colors, src.colors, index);
                Copy(ref boneWeights, src.boneWeights, index);
                return i;
            }

            public void AssignTo(Mesh target)
            {
                if (verts.Count > 65535)
                    target.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                target.SetVertices(verts);
                if (uv1 != null) target.SetUVs(0, uv1);
                if (uv2 != null) target.SetUVs(1, uv2);
                if (uv3 != null) target.SetUVs(2, uv3);
                if (uv4 != null) target.SetUVs(3, uv4);
                if (normals != null) target.SetNormals(normals);
                if (tangents != null) target.SetTangents(tangents);
                if (colors != null) target.SetColors(colors);
                if (boneWeights != null) target.boneWeights = boneWeights.ToArray();
            }
        }

        //Create new mesh from a certain mesh and its sub mesh index
        public static Mesh GetSubmesh(this Mesh mesh, int subMeshIndex)
        {
            if (subMeshIndex < 0 || subMeshIndex >= mesh.subMeshCount)
                return null;
            int[] indices = mesh.GetTriangles(subMeshIndex);
            Vertices source = new Vertices(mesh);
            Vertices dest = new Vertices();
            Dictionary<int, int> map = new Dictionary<int, int>();
            int[] newIndices = new int[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                int o = indices[i];
                int n;
                if (!map.TryGetValue(o, out n))
                {
                    n = dest.Add(source, o);
                    map.Add(o, n);
                }

                newIndices[i] = n;
            }

            Mesh m = new Mesh();
            dest.AssignTo(m);
            m.triangles = newIndices;
            return m;
        }
    }
}