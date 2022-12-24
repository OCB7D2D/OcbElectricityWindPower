using UnityEngine;
using UnityEditor;
using System.IO;

public class FBXMeshExtractor
{
    private static string _progressTitle = "Extracting Meshes";
    private static string _targetExtension = ".asset";


    [MenuItem("Assets/Extract Meshes", validate = true)]
    private static bool ExtractMeshesMenuItemValidate()
    {
        for (int i = 0; i < Selection.objects.Length; i++)
        {
            var path = Path.GetExtension(AssetDatabase.GetAssetPath(Selection.objects[i]));
            if (!path.ToLower().Equals(".dae") && !path.ToLower().Equals(".obj") &&
                !path.ToLower().Equals(".3ds") && !path.ToLower().Equals(".fbx"))
                    return false;
        }
        return true;
    }

    [MenuItem("Assets/Extract Meshes")]
    private static void ExtractMeshesMenuItem()
    {
        EditorUtility.DisplayProgressBar(_progressTitle, "", 0);
        for (int i = 0; i < Selection.objects.Length; i++)
        {
            EditorUtility.DisplayProgressBar(_progressTitle, Selection.objects[i].name, (float)i / (Selection.objects.Length - 1));
            ExtractMeshes(Selection.objects[i]);
        }
        EditorUtility.ClearProgressBar();
    }

    private static void ExtractMeshes(Object selectedObject)
    {
        //Create Folder Hierarchy
        string selectedObjectPath = AssetDatabase.GetAssetPath(selectedObject);
        string parentfolderPath = selectedObjectPath.Substring(0, selectedObjectPath.Length - (selectedObject.name.Length + 5));
        string objectFolderName = selectedObject.name;
        string objectFolderPath = parentfolderPath + "/" + objectFolderName;
        string meshFolderName = "Meshes";
        string meshFolderPath = objectFolderPath + "/" + meshFolderName;

        if (!AssetDatabase.IsValidFolder(objectFolderPath))
        {
            AssetDatabase.CreateFolder(parentfolderPath, objectFolderName);

            if (!AssetDatabase.IsValidFolder(meshFolderPath))
            {
                AssetDatabase.CreateFolder(objectFolderPath, meshFolderName);
            }
        }

        //Create Meshes
        Object[] objects = AssetDatabase.LoadAllAssetsAtPath(selectedObjectPath);

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] is Mesh)
            {
                EditorUtility.DisplayProgressBar(_progressTitle, selectedObject.name + " : " + objects[i].name, (float)i / (objects.Length - 1));

                Mesh mesh = Object.Instantiate(objects[i]) as Mesh;

                AssetDatabase.CreateAsset(mesh, meshFolderPath + "/" + objects[i].name + _targetExtension);
            }
        }

        //Cleanup
        var ext = Path.GetExtension(AssetDatabase.GetAssetPath(selectedObject));
        AssetDatabase.MoveAsset(selectedObjectPath, Path.Combine(
            objectFolderPath, selectedObject.name + ext));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}