using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SimpleFileBrowser;

public class FileManager : MonoBehaviour
{
    public string _cpbrtFilePath;
    public Parser _parser;

    // Start is called before the first frame update
    void Start()
    {
        FileBrowser.SetFilters(false, new FileBrowser.Filter("SceneObject", ".cpbrt"));

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void OpenFileBrowser()
    {
        FileBrowser.AddQuickLink("Users", "C:\\Users", null);
        StartCoroutine(ShowLoadDialogCorotine());
    }

    IEnumerator ShowLoadDialogCorotine()
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Load Files and Folders", "Load");
        Debug.Log(FileBrowser.Success);
        if (FileBrowser.Success)
        {
            // Print paths of the selected files (FileBrowser.Result) (null, if FileBrowser.Success is false)
            for (int i = 0; i < FileBrowser.Result.Length; i++)
                Debug.Log(FileBrowser.Result[i]);

            _cpbrtFilePath = FileBrowser.Result[0];
            string rootFolder = Directory.GetParent(_cpbrtFilePath).FullName;
            _parser.Parse(_cpbrtFilePath);
        }
    }
}
