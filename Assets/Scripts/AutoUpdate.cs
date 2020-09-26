using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AutoUpdate : MonoBehaviour
{
    const string urlServer = "https://0726482bbe2430902.temporary.link/Measure/AutoUpdate.txt";
    const string urlAutoUpdate = "https://drive.google.com/file/d/17iyiKoizo54Bi1M8a40S-4Te2h_Jyue8/view?usp=sharing";
    public GameObject goAutoUpdate;
    public Text textAutoUpdate;

    private void Awake()
    {
        goAutoUpdate.SetActive(false);    
    }

    void Start()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            Invoke(nameof(CheckAutoUpdate), 1);
        }
    }

    void CheckAutoUpdate()
    {
        StartCoroutine(CheckAutoUpdateWWW());
    }

    void ShowUpdateAvailable(string txt)
    {
        goAutoUpdate.SetActive(true);
        textAutoUpdate.text = txt;
    }

    public void OnClickYes()
    {
        GetUpdate();
        goAutoUpdate.SetActive(false);
    }

    public void OnClickCancel()
    {
        goAutoUpdate.SetActive(false);
    }

    void GetUpdate() {
        Application.OpenURL(urlAutoUpdate);
    }

    IEnumerator CheckAutoUpdateWWW()
    {
        UnityWebRequest www = UnityWebRequest.Get(urlServer);
        SetupWWW(www);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            string txt = www.downloadHandler.text;
            if (txt.ToLower() != "no")
            {
                ShowUpdateAvailable(txt);
            }            
        }
    }

    public void SetupWWW(UnityWebRequest www)
    {
        www.SetRequestHeader("Accept", "*/*");
        www.SetRequestHeader("Accept-Encoding", "gzip, deflate");
        www.SetRequestHeader("User-Agent", "runscope/0.1");
    }
}
