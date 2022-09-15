using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Siccity.GLTFUtility;
using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;

public class SkinSwitch : MonoBehaviour
{
    # region variables
    [SerializeField]
    private List<Material> _materials;

    [SerializeField]
    private List<Transform> _parents;

    [SerializeField]
    private Animator _animator;

    private List<KeyValuePair<int, object>> _skins = new List<KeyValuePair<int, object>>();

    [DllImport("__Internal")]
    private static extern void Call_Back(string message);

    [DllImport("__Internal")]
    private static extern void Start_Event(string message);

    [DllImport("__Internal")]
    private static extern void Error_Message(string error);

    private string _filePath;

    private bool _isAnimated;

    private string _uid = "null";

    private bool _inProgress = false;

    #endregion

    #region Messages to front
    public void SendStartEvent()
    {
        Start_Event(JsonUtility.ToJson(new OutputFormat("UID", "scene started", Status.SCENE_STARTED)));
    }

    public void SendCallback(string message)
    {
        Call_Back(message);
    }
    #endregion

    #region Start/Update/Awake
    private void Start()
    {

#if !UNITY_EDITOR && UNITY_WEBGL
        UnityEngine.WebGLInput.captureAllKeyboardInput = false; 
        // disable WebGLInput.captureAllKeyboardInput so elements in web page can handle keyboard inputs
#endif
        _filePath = $"{Application.persistentDataPath}/Files/";
        SendStartEvent();
    }

    public void SetContainer(string json)
    {
        SkinContainer container;
        if (!CheckContainer(json, out container))
        {
            SendCallback(JsonUtility.ToJson(new OutputFormat((container.UID == "" ? "null" : container.UID), "incorrect format", Status.INCORRECT_FORMAT)));
            return;
        }
        else
        {
            if (!_inProgress)
                StartCoroutine(DownloadObjects(container));
            else
                SendCallback(JsonUtility.ToJson(new OutputFormat((container == null ? "null" : container.UID), "already loading container", Status.ALREADY_IN_PROGRESS)));
        }
    }

    private void Update()
    {
        float rotation = 0;
        if (Input.GetMouseButton(0))
            rotation = Input.GetAxis("Mouse X") * 3;
        transform.Rotate(Vector3.down, rotation);
    }
    #endregion

    IEnumerator DownloadObjects(SkinContainer container)
    {
        _skins.Clear();
        _inProgress = true;
        _isAnimated = container.IsAnimated;
        _uid = container.UID;
        if (_isAnimated)
        {
            _animator.SetBool("Loading", true);
            SendCallback(JsonUtility.ToJson(new OutputFormat(_uid, "animation started", Status.ANIMATION_START)));
            yield return new WaitForSeconds(1f);
        }

        foreach (var input in container.Skins)
        {
            _canContinue = false;
            Position position;
            if (Enum.TryParse<Position>(input.Position, out position))
            {
                int pos = (int)position;
                if (input.IsModel)
                {

                    string path = GetFilePath(input.Link);//check, if file already exists
                    // if (File.Exists(path))
                    // {
                    //     Debug.Log("Found file locally, loading...");
                    //     _skins.Add(new KeyValuePair<int, object>(pos, path));
                    //     continue;
                    // }

                    yield return StartCoroutine(GetModel(input.Link,
                    (string error) =>
                    {
                        StopError(error, Status.LOADING_FAILURE, _isAnimated);
                    },
                    () =>
                    {

                        _skins.Add(new KeyValuePair<int, object>(pos, path));
                        _canContinue = true;
                    }
                    ));
                }
                else
                {
                    yield return StartCoroutine(GetTexture(input.Link,
                    (string error) =>
                    {
                        StopError(error, Status.LOADING_FAILURE, _isAnimated);
                    },
                    (Texture2D texture) =>
                    {
                        _skins.Add(new KeyValuePair<int, object>(pos, texture));
                        _canContinue = true;
                    }
                    ));
                }
            }
            else
            {
                StopError("incorrect format", Status.INCORRECT_FORMAT, _isAnimated);
                break;
            }
        }
        SendCallback(JsonUtility.ToJson(new OutputFormat(_uid, "models and textures loaded succesfully", Status.LOADING_SUCCESS)));
        SetObjects();
    }

    private void SetObjects()
    {
        foreach (var skin in _skins)
        {
            int position = skin.Key;
            if (skin.Value is Texture2D)
            {
                try
                {
                    _materials[position - 1].SetTexture("_MainTex", (Texture2D)skin.Value);
                }
                catch
                {
                    StopError("setting texture error", Status.SETTING_FAILURE, _isAnimated);
                }
            }
            else
            {
                try
                {
                    string path = (string)skin.Value;
                    ResetModel(position);
                    GameObject model = Importer.LoadFromFile(path);
                    model.transform.SetParent(_parents[position]);
                    model.transform.localPosition = new Vector3(0, 0, 0);
                    model.transform.localRotation = Quaternion.Euler(0, 0, 0);
                }
                catch
                {
                    StopError("setting model error", Status.SETTING_FAILURE, _isAnimated);
                }
            }
        }
        if (_isAnimated)
        {
            _animator.SetBool("Loading", false);
            SendCallback(JsonUtility.ToJson(new OutputFormat(_uid, "animation ended", Status.ANIMATION_END)));
        }
        SendCallback(JsonUtility.ToJson(new OutputFormat(_uid, "models and textures loaded and settded succesfully", Status.SETTING_SUCCESS)));
        _isAnimated = false;
        _uid = "null";
        _inProgress = false;
    }

    #region Support
    string GetFilePath(string url)
    {
        string[] pieces = url.Split('/');
        string filename = pieces[pieces.Length - 1];
        return $"{_filePath}{filename}";
    }

    public void StopError(string error, Status status, bool IsAnimated)
    {
        StopAllCoroutines();
        if (IsAnimated)
        {
            _animator.SetBool("Loading", false);
            SendCallback(JsonUtility.ToJson(new OutputFormat(_uid, "animation ended", Status.ANIMATION_END)));
        }
        SendCallback(JsonUtility.ToJson(new OutputFormat(_uid, error, status)));
        _inProgress = false;
    }

    public bool CheckContainer(string json, out SkinContainer container)
    {
        container = new SkinContainer(new List<Skin>(), false, "");
        try
        {
            container = JsonUtility.FromJson<SkinContainer>(json);
        }
        catch
        {
            return false;
        }
        if (container == null || container.UID == "" || container.UID == null || container.Skins.Count == 0)
        {
            return false;
        }
        else return true;
    }

    IEnumerator GetModel(string link, Action<string> onError, Action onSucess)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(link))
        {
            request.downloadHandler = new DownloadHandlerFile(GetFilePath(link));
            //save file into folder files
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                onError((request.error == null || request.error == "") ? request.result.ToString() : request.error);
            }
            else
            {

                onSucess();
            }
        }
    }

    private IEnumerator GetTexture(string url, Action<string> onError, Action<Texture2D> onSuccess)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError((request.error == null || request.error == "") ? request.result.ToString() : request.error);
            }
            else
            {
                DownloadHandlerTexture downloadHandlerTexture = request.downloadHandler as DownloadHandlerTexture;
                onSuccess(downloadHandlerTexture.texture);
            }
        }
    }
    void ResetModel(int position)
    {
        if (_parents[position].childCount > 0)
            Destroy(_parents[position].GetChild(0).gameObject);
    }
    #endregion

}

[Serializable]
public class OutputFormat
{
    public string UID;
    public string Message;

    public string Status;

    public OutputFormat(string UID, string Message, Status Status)
    {
        this.UID = UID;
        this.Message = Message;
        this.Status = Status.ToString();
    }
}


public enum Position
{
    hair = 0,
    head = 1,
    body = 2,
    pants = 3
}

/*


{"Skins":[
    {"Position":"hair","Link":"https://s3.mark.online/files/ARy5sCnCebd8xUko3USwNn9oljxm0l2qSp6YOmEC.glb","IsModel":true}
    ],
    "IsAnimated":true,
    "UID":"erg"
}

*/