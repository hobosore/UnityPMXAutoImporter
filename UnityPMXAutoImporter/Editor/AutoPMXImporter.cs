using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using LibMMD.Unity3D;
using System.Text.RegularExpressions;

public class AutoPMXImporter : AssetPostprocessor
{
    const string AssetFolderName = "Assets";
    const string outputFolderName = "/Prefab/";
    const string MeshFolderName = "Mesh/";
    const string AvatarFolderName = "Avatar/";
    const string TexturesFolderName = "Textures/";
    const string MaterialsFolderName = "Materials/";
    const string PMXExtension = ".pmx";
    const string PNGExtension = ".png";
    const string MeshExtension = ".mesh";
    const string AvatarExtension = ".avatar";
    const string MaterialExtension = ".mat";
    const string PrefabExtension = ".prefab";

    async static void OnPostprocessAllAssets(string[] relativePaths, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string relativePath in relativePaths)
        {
            if (relativePath.EndsWith(PMXExtension))
            {
                string inputFileAbsolutePath = Application.dataPath + relativePath.TrimStart(AssetFolderName.ToCharArray());
                string inputFileName = Path.GetFileName(inputFileAbsolutePath);
                string inputFileNameWithoutExtension = inputFileName.TrimEnd(PMXExtension.ToCharArray());
                string inputFolderAbsolutePath = Path.GetDirectoryName(inputFileAbsolutePath);
                string inputFolderRelativePath = Path.GetDirectoryName(relativePath);
                string outputFolderAbsolutePath = inputFolderAbsolutePath + outputFolderName + SanitizeFolderName(inputFileNameWithoutExtension) + "/";
                string outputFolderRelativePath = inputFolderRelativePath + outputFolderName + SanitizeFolderName(inputFileNameWithoutExtension) + "/";
                string outputFileAbsolutePath = outputFolderAbsolutePath + inputFileNameWithoutExtension + PrefabExtension;

                Transform model = await PMXModelLoader.LoadPMXModel(inputFileAbsolutePath, false);
                if(model == null)
                {
                    UnityEngine.Debug.Log("読み込みに問題がありました");
                    UnityEngine.Debug.Log(inputFileAbsolutePath);
                    continue;
                }

                if (!Directory.Exists(outputFolderAbsolutePath))
                {
                    Directory.CreateDirectory(outputFolderAbsolutePath);
                }

                string meshFolderAbsolutePath = outputFolderAbsolutePath + MeshFolderName;
                if (!Directory.Exists(meshFolderAbsolutePath))
                {
                    Directory.CreateDirectory(meshFolderAbsolutePath);
                }

                string avatarFolderAbsolutePath = outputFolderAbsolutePath + AvatarFolderName;
                if (!Directory.Exists(avatarFolderAbsolutePath))
                {
                    Directory.CreateDirectory(avatarFolderAbsolutePath);
                }

                string texturesFolderPath = outputFolderAbsolutePath + TexturesFolderName;
                if (!Directory.Exists(texturesFolderPath))
                {
                    Directory.CreateDirectory(texturesFolderPath);
                }

                string materialsFolderPath = outputFolderAbsolutePath + MaterialsFolderName;
                if (!Directory.Exists(materialsFolderPath))
                {
                    Directory.CreateDirectory(materialsFolderPath);
                }

                Mesh mesh = model.GetComponent<MMDModel>().Mesh;
                if (mesh != null)
                {
                    AssetDatabase.CreateAsset(mesh, outputFolderRelativePath + MeshFolderName + mesh.name + MeshExtension);
                }

                Animator animator = model.GetComponent<Animator>();
                if (animator != null)
                {
                    Avatar avatar = animator.avatar;
                    if (avatar != null)
                    {
                        AssetDatabase.CreateAsset(avatar, outputFolderRelativePath + AvatarFolderName + avatar.name + AvatarExtension);
                    }
                }

                foreach (Material material in model.GetComponent<MMDModel>().SkinnedMeshRenderer.sharedMaterials)
                {
                    if (material != null && material.mainTexture != null)
                    {
                        string textureAbsolutePath = texturesFolderPath + material.mainTexture.name + PNGExtension;
                        string textureRelativePath = outputFolderRelativePath + TexturesFolderName + material.mainTexture.name + PNGExtension;
                        using (FileStream fileStream = new FileStream(textureAbsolutePath, FileMode.Create))
                        using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
                        {
                            binaryWriter.Write((material.mainTexture as Texture2D).EncodeToPNG());
                        }
                        AssetDatabase.Refresh();
                        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture>(textureRelativePath);
                    }

                    if (material != null)
                    {
                        AssetDatabase.CreateAsset(material, outputFolderRelativePath + MaterialsFolderName + material.name + MaterialExtension);
                    }
                }

                MMDModel mmdModel = model.GetComponent<MMDModel>();
                mmdModel.ShowModel();

#if UNITY_EDITOR
                GameObject.DestroyImmediate(mmdModel);
#else
        GameObject.Destroy(mmdModel);
#endif

                PrefabUtility.SaveAsPrefabAsset(model.gameObject, outputFolderRelativePath + model.name + PrefabExtension);

                GameObject.DestroyImmediate(model.gameObject);
            }
        }
    }

    public static string SanitizeFolderName(string name)
    {
        string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
        return r.Replace(name, "");
    }
}
