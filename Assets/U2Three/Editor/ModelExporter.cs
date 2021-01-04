using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace U2Three.Editor
{
    public class ModelExporter : EditorWindow
    {
        private StreamWriter m_JSONWriter;
        private StreamWriter m_XMLWriter;
        private Transform[] m_selections;
        private string m_rootFolder;
        private List<Material> m_materials;
        private List<Transform> m_subMeshes;
        private List<string> m_filesToCollect = new List<string>();

        private float m_progress;
        private int m_processesCompleted;
        private int m_totalProcesses;
        private bool m_isGenerating = false;

        [MenuItem("Tools/Export Models for ThreeJS")]
        static void Init()
        {
            ModelExporter window = (ModelExporter) EditorWindow.GetWindow(typeof(ModelExporter));
        }

        void OnGUI()
        {
            m_selections = Selection.GetTransforms(SelectionMode.Editable);

            // Only allow button if we have selected something
            GUI.enabled = (m_selections != null && m_selections.Length > 0);

            if (GUILayout.Button("Export Selection"))
            {
                m_rootFolder = EditorUtility.SaveFolderPanel("Export Models for ThreeJS", "",
                    m_selections[0].name);
                
                string path = Path.Combine(m_rootFolder, m_selections[0].name + ".xml");

                if (path.Length > 0)
                    BeginExportModel(path);
            }

            if (m_isGenerating)
            {
                if (m_totalProcesses == 0)
                {
                    Debug.LogError("No processes to run; canceling export.");
                    m_isGenerating = false;
                    return;
                }

                EditorUtility.DisplayProgressBar("Generating Model Data",
                    "Current Mesh: " + m_subMeshes[m_processesCompleted].name, m_progress);
                GenerateSubMeshXML(m_processesCompleted);

                if (m_processesCompleted == m_totalProcesses)
                {
                    m_XMLWriter.WriteLine("</Mesh>\n");

                    m_XMLWriter.WriteLine("<Materials>");
                    WriteMaterials();
                    m_XMLWriter.WriteLine("</Materials>");
                    m_XMLWriter.Write("</Model>");

                    m_XMLWriter.Close();

                    foreach (var file in m_filesToCollect)
                    {
                        if (File.Exists(file))
                        {
                            File.Copy(file, Path.Combine(m_rootFolder, Path.GetFileName(file)), true);
                            Debug.Log("Copy from " + file + " to " + Path.Combine(m_rootFolder, Path.GetFileName(file)) + ".");
                        }
                    }
                    
                    m_isGenerating = false;
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        private void BeginExportModel(string outputUrl)
        {
            m_progress = 0;
            m_processesCompleted = 0;
            m_totalProcesses = 0;

            m_subMeshes = new List<Transform>();
            m_XMLWriter = new StreamWriter(outputUrl);
            m_materials = new List<Material>();

            // Recursively generate list of processes so that we can track progress
            RecurseChild(m_selections[0]);

            m_XMLWriter.WriteLine("<Model>");
            m_XMLWriter.WriteLine("<Mesh>");

            m_isGenerating = true;
        }

        private void RecurseChild(Transform transform)
        {
            if (transform.GetComponent<MeshFilter>())
            {
                //Debug.Log(_transform.name);

                m_totalProcesses++;
                m_subMeshes.Add(transform);
            }

            if (transform.GetComponent<MeshRenderer>())
            {
                foreach (var mat in transform.GetComponent<MeshRenderer>().sharedMaterials)
                {
                    if (!m_materials.Contains(mat))
                    {
                        m_materials.Add(mat);
                    }
                }
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                RecurseChild(transform.GetChild(i));
            }
        }

        private void GenerateSubMeshXML(int index)
        {
            Transform currentSubMesh = m_subMeshes[index];
            
            string mats = String.Empty;
            foreach (var material in currentSubMesh.GetComponent<MeshRenderer>().sharedMaterials)
            {
                if (!mats.Equals(String.Empty)) mats += ",";
                mats += material.name;
            }

            m_XMLWriter.WriteLine("\t<Submesh mat=\"" + mats + "\">");
            GenerateSubMeshJSON(currentSubMesh);
            m_XMLWriter.WriteLine("\t</Submesh>");

            m_processesCompleted++;
            m_progress = (float) m_processesCompleted / (float) m_totalProcesses;
        }

        private void GenerateSubMeshJSON(Transform trans)
        {
            Mesh _mesh = trans.GetComponent<MeshFilter>().sharedMesh;
            string jsonOut;

            // Open submesh
            m_XMLWriter.Write("\t{\n");

            // Vertices
            jsonOut = "\t\t\"vertices\":[";
            foreach (Vector3 vertex in _mesh.vertices)
            {
                // Write each chunk as we confirm subsequent data to keep string size small
                //Debug.Log("Writing chunk to json: " + jsonOut);
                m_XMLWriter.Write(jsonOut);
                jsonOut = "";

                Vector3 currentVertex = vertex;

                // Apply transformation to individual vertex
                currentVertex.Scale(trans.lossyScale);
                currentVertex = Quaternion.Euler(trans.eulerAngles) * currentVertex;
                currentVertex += trans.position;

                jsonOut += currentVertex.x.ToString() + "," + currentVertex.y.ToString() + "," +
                           currentVertex.z.ToString() + ",";
            }

            jsonOut = jsonOut.TrimEnd(jsonOut[jsonOut.Length - 1]); // Trim end to remove the last comma
            jsonOut += "],\n";
            m_XMLWriter.Write(jsonOut);

            // Faces
            jsonOut = "\t\t\"faces\":[";
            for (int i = 0; i < _mesh.triangles.Length; i += 3)
            {
                // Write each chunk as we confirm subsequent data to keep string size small
                //Debug.Log("Writing chunk to json: " + jsonOut);
                m_XMLWriter.Write(jsonOut);
                jsonOut = "";

                jsonOut += "40," + _mesh.triangles[i].ToString() + "," + _mesh.triangles[i + 1].ToString() + "," +
                           _mesh.triangles[i + 2].ToString() + ","
                           + _mesh.triangles[i].ToString() + "," + _mesh.triangles[i + 1].ToString() + "," +
                           _mesh.triangles[i + 2].ToString() + ","
                           + _mesh.triangles[i].ToString() + "," + _mesh.triangles[i + 1].ToString() + "," +
                           _mesh.triangles[i + 2].ToString() + ",";
            }

            jsonOut = jsonOut.TrimEnd(jsonOut[jsonOut.Length - 1]); // Trim end to remove the last comma
            jsonOut += "],\n";
            m_XMLWriter.Write(jsonOut);

            // Metadata
            m_XMLWriter.Write("\t\t\"metadata\":{\n");
            m_XMLWriter.Write("\t\t\t\"vertices\":" + _mesh.vertexCount + ",\n");
            m_XMLWriter.Write("\t\t\t\"faces\":" + _mesh.triangles.Length / 3 + ",\n");
            m_XMLWriter.Write("\t\t\t\"generator\":\"io_three\",\n");
            m_XMLWriter.Write("\t\t\t\"type\":\"Geometry\",\n");
            m_XMLWriter.Write("\t\t\t\"normals\":" + _mesh.normals.Length + ",\n");
            m_XMLWriter.Write("\t\t\t\"version\":3,\n");
            m_XMLWriter.Write("\t\t\t\"uvs\":1\n");
            m_XMLWriter.Write("\t\t},\n");

            // UVs
            jsonOut = "\t\t\"uvs\":[[";
            foreach (Vector2 uv in _mesh.uv)
            {
                // Write each chunk as we confirm subsequent data to keep string size small
                //Debug.Log("Writing chunk to json: " + jsonOut);
                m_XMLWriter.Write(jsonOut);
                jsonOut = "";

                jsonOut += uv.x.ToString() + "," + uv.y.ToString() + ",";
            }

            jsonOut = jsonOut.TrimEnd(jsonOut[jsonOut.Length - 1]); // Trim end to remove the last comma

            jsonOut += "]],\n";
            m_XMLWriter.Write(jsonOut);

            // Normals
            jsonOut = "\t\t\"normals\":[";
            foreach (Vector3 normal in _mesh.normals)
            {
                // Write each chunk as we confirm subsequent data to keep string size small
                //Debug.Log("Writing chunk to json: " + jsonOut);
                m_XMLWriter.Write(jsonOut);
                jsonOut = "";

                jsonOut += normal.x.ToString() + "," + normal.y.ToString() + "," + normal.z.ToString() + ",";
            }

            jsonOut = jsonOut.TrimEnd(jsonOut[jsonOut.Length - 1]); // Trim end to remove the last comma
            jsonOut += "]\n";
            m_XMLWriter.Write(jsonOut);

            // Close submesh
            m_XMLWriter.Write("\t}\n");
        }

        //Convert unity material to raw material information for ThreeJS
        //TODO: should consider different shaders used by mat to process uniforms
        private void WriteMaterials()
        {
            foreach (Material mat in m_materials)
            {
                //here we take Unity standard shader for sample
                m_XMLWriter.Write("\t<Material id=\"" + mat.name + "\" ");
                m_XMLWriter.Write("color=\"0x" + ColorUtility.ToHtmlStringRGB(mat.color).ToLower() + "\" ");

                // Diffuse texture URL
                Texture diffuse = mat.GetTexture("_MainTex");
                if (diffuse)
                {
                    m_XMLWriter.Write("diffuse=\"" + diffuse.name + ".png\" ");
                    m_filesToCollect.Add(Path.Combine(Directory.GetCurrentDirectory(), AssetDatabase.GetAssetPath(diffuse)));
                }

                // Normal map URL
                Texture normal = mat.GetTexture("_BumpMap");
                if (normal)
                {
                    m_XMLWriter.Write("normal=\"" + normal.name + ".png\" ");
                    m_filesToCollect.Add(Path.Combine(Directory.GetCurrentDirectory(), AssetDatabase.GetAssetPath(normal)));
                }

                // Metalness map
                Texture metalness = mat.GetTexture("_MetallicGlossMap");
                if (metalness)
                {
                    m_XMLWriter.Write("metalnessMap=\"" + metalness.name + ".png\" ");
                    m_filesToCollect.Add(Path.Combine(Directory.GetCurrentDirectory(), AssetDatabase.GetAssetPath(metalness)));
                }

                Texture roughness = mat.GetTexture("_SpecGlossMap");
                if (roughness)
                {
                    m_XMLWriter.Write("roughnessMap=\"" + roughness.name + ".png\" ");
                    m_filesToCollect.Add(Path.Combine(Directory.GetCurrentDirectory(), AssetDatabase.GetAssetPath(roughness)));
                }

                Texture alpha = mat.GetTexture("_DetailMask");
                if (alpha)
                {
                    m_XMLWriter.Write("alphaMap=\"" + alpha.name + ".png\" ");
                    m_filesToCollect.Add(Path.Combine(Directory.GetCurrentDirectory(), AssetDatabase.GetAssetPath(alpha)));
                }

                // Occlusion map
                // Texture occlusion = mat.GetTexture("_OcclusionMap");
                // if (occlusion)
                // {
                //     m_XMLWriter.Write("occlusion=\"" + occlusion.name + ".png\" ");
                // }

                //m_XMLWriter.Write("metalness=\"" + mat.GetFloat("_Metallic") + "\" ");
                //m_XMLWriter.Write("smoothness=\"" + mat.GetFloat("_GlossMapScale") + "\"");

                m_XMLWriter.WriteLine("/>");
            }
        }

        //Consider combine a single map to represent ambient occlusion, roughness, and metalness,
        //reading from the red, green, and blue channels respectively.
        private Texture2D CombineDetailMaps(Texture2D ao = null, Texture2D metalness = null,
            Texture2D smoothness = null)
        {
            Vector2 dimensions = new Vector2();
            if (ao)
                dimensions = new Vector2(ao.width, ao.height);
            else if (metalness)
                dimensions = new Vector2(metalness.width, metalness.height);
            else if (smoothness)
                dimensions = new Vector2(smoothness.width, smoothness.height);
            else
                return null;

            Texture2D result = new Texture2D((int) dimensions.x, (int) dimensions.y);

            Color[] aoPixels = new Color[0];
            Color[] metalnessPixels = new Color[0];
            Color[] smoothnessPixels = new Color[0];
            int numPixels = 0;

            if (ao)
            {
                aoPixels = ao.GetPixels();
                numPixels = aoPixels.Length;
            }

            if (metalness)
            {
                metalnessPixels = metalness.GetPixels();
                numPixels = metalnessPixels.Length;
            }

            if (smoothness)
            {
                smoothnessPixels = smoothness.GetPixels();
                numPixels = smoothnessPixels.Length;
            }

            for (int i = 0; i < numPixels; i++)
            {
                if (aoPixels.Length > i)
                    aoPixels[i] *= new Vector4(1f, 0f, 0f, 0f);

                if (metalnessPixels.Length > i)
                    metalnessPixels[i] *= new Vector4(0f, 1f, 0f, 0f);

                if (smoothnessPixels.Length > i)
                    smoothnessPixels[i] *= new Vector4(0f, 0f, 1f, 0f);
            }


            return result;
        }
    }
}