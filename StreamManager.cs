using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using Mapbox.Utils;
using TMPro;
using Mapbox.Unity.Map;
using UnityEngine.Networking;
using System.Collections;

public class StreamManager : MonoBehaviour
{
    public DeCouple mgr;
    public GameObject goMap;
    public Button buttonRecord;
    public Button buttonStop;
    public Button buttonPlay;
    public Button buttonPaste;
    public Text textStream;
    public Button buttonMCI;
    public Button buttonVisual;
    public Button buttonNewTrip;
    public TMP_InputField inputFieldSearch;
    public Button buttonLoadTrip;
    public Button buttonSearchResult;
    public Button buttonLoadedTrip;
    public Button buttonWalking;
    public Button buttonBus;
    public GameObject goContentLoadedTrips;
    public GameObject goContentSearchResults;
    public TMP_InputField inputFieldSaveTrip;
    public Button buttonConfirm;
    public Image imgAPI1;
    public Image imgAPI2;
    public Image imgAPI3;
    public Slider sliderStream;
    public Camera cameraAR;
    public Text textGPS;
    public Text textFPS;
    public GameObject goUserLocation;
    public Text textNorth;
    public GameObject goScrollViewTags;
    public GameObject goScrollViewDateTimeStarts;
    public Button buttonTag;
    public Button buttonDateTimeStart;
    public Text textInfo;
    //
    AbstractMap abstractMap;
    string key = "Decouple";
    List<StreamUnit> stream = new List<StreamUnit>();
    StreamState sessionState = StreamState.stop;
    StreamState sessionStateLast = StreamState.stop;
    //    DateTime dateTimeStart;
    string strDateTimeStart;
    float north;
    float northLast;
    //
    float smoothNorth = .05f;
    float northDelta = 1;
    float timeDeltaCurrent;
    float timeDeltaTarget;
    float timeDeltaStart;
    int streamUnitCurrentIndex;
    float timeDelay = .25f;
    Button buttonHighlight;
    bool ynJsonError;
    //
    Color colorHighlighted = (Color.red + Color.clear) / 2;
    Color colorUnHighlighted = Color.white;
    Color colorDisabled = (Color.grey + Color.clear) / 2;
    Color colorEnabled = Color.white;
    Color colorGreen = Color.green;
    Color colorYellow = Color.yellow;
    Color colorRed = Color.red;
    int cntAPI;
    bool ynAR;
    Vector2d gps;
    Vector2d gpsLast;
    int cntFramesFPS;
    bool ynCloud = true;
    const string urlServer = "https://0726482bbe2430902.temporary.link/StreamManager/StreamManager.php";
    float timeDelta;

    private void Awake()
    {
        abstractMap = goMap.GetComponent<AbstractMap>();
        Input.compass.enabled = true;
        Input.location.Start(0.5f, 1.0f);
        RestartSession();
    }

    private void Start()
    {
        //OnClickRecord();
        //        Invoke("Test", 0);    
        InvokeRepeating("ShowFPS", 1, 1);
        Invoke("StartLate", 1);
    }

    private void OnApplicationQuit()
    {
        RecordAction(StreamActionType.sessionEnd, DateTime.Now.ToString());
    }

    void StartLate()
    {
        //InitGPS();
        strDateTimeStart = DateToString(DateTime.UtcNow) + " (" + DateTime.Now.ToString() + " local)";
        timeDeltaStart = Time.realtimeSinceStartup;
        OnClickRecord();
        Debug.Log("strDateStartTime " + strDateTimeStart);
        string txt = DateTime.Now.ToString() + ", model: " + SystemInfo.deviceModel + ", name: " + SystemInfo.deviceName + ", type: " + SystemInfo.deviceType;
        txt += ", app: " + Application.productName + " version: " + Application.version;
        RecordAction(StreamActionType.sessionStart, txt);
        InvokeRepeating("CheckGPS", 1, 1);
        InvokeRepeating("CheckCompass", 1, 1);
    }

    private void Update()
    {
        gps = mgr.gps;
        north = mgr.north;
//        UpdateUserLocation();
//        UpdateGPS();
//        UpdateCompass();
        CheckSessionStateChange();
//        UpdatePasteButton();
//        UpdatePlayButton();
        CheckIfStreamUnitCurrentReady();
        cntFramesFPS++;
    }

    void UpdateUserLocation()
    {
        goUserLocation.transform.localPosition = abstractMap.GeoToWorldPosition(gps);

    }

    void ShowFPS()
    {
        textFPS.text = "fps\n" + cntFramesFPS;
        Color color = colorGreen;
        if (cntFramesFPS < 30) color = colorYellow;
        if (cntFramesFPS < 20) color = colorRed;
        textFPS.color = color;
        cntFramesFPS = 0;
    }

    void InitGPS()
    {
        //        abstractMap.SetCenterLatitudeLongitude(new Vector2d(28.702264, -81.259021));
        if (Application.isEditor == true)
        {
            gps = abstractMap.CenterLatitudeLongitude;
            RecordAction(StreamActionType.gps, LatLonToString(gps));
        }
    }

    void Test()
    {
        strDateTimeStart = DateToString(DateTime.Now);
        StreamUnit su = CreateStreamUnit(StreamActionType.tripType, StreamTripType.Load.ToString());
        su.actionType = StreamActionType.compass.ToString();
        string json = JsonUtility.ToJson(su);
        Debug.Log("|" + json + "|");

        StreamUnit su2 = JsonUtility.FromJson<StreamUnit>(json);
        Debug.Log("su2.actionType " + su2.actionType);
    }

    void UpdateCompass()
    {
        if (sessionState != StreamState.play)
        {
            if (Input.compass.enabled)
            {
                SetCompassNorthFromSensor();
            }
            else
            {
                UpdateKeysCompass();
            }
        }
        if (CompassToString(northLast) != CompassToString(north))
        {
            RecordAction(StreamActionType.compass, CompassToString(north));
            SetMapRotation();
            textNorth.text = "north\n" + CompassToString(north);
        }
        northLast = north;
    }

    void CheckCompass()
    {
        if (CompassToString(northLast) != CompassToString(north))
        {
            RecordAction(StreamActionType.compass, CompassToString(north));
            SetMapRotation();
            textNorth.text = "north\n" + CompassToString(north);
        }
        northLast = north;
    }

    string CompassToString(float f)
    {
        return f.ToString("F0");
    }

    void UpdateGPS()
    {
        if (sessionState != StreamState.play)
        {
            if (Input.location.status == LocationServiceStatus.Running)
            {
                SetGPSFromSensor();
            }
            else
            {
                UpdateKeysGPS();
            }
        }
        if (LatLonToString(gpsLast) != LatLonToString(gps))
        {
            textInfo.text = "gps " + LatLonToString(gpsLast) + " --> " + LatLonToString(gps);
            RecordAction(StreamActionType.gps, GpsToString());
            SetMapGPS();
        }
        gpsLast = gps;
    }

    void CheckGPS()
    {
        if (LatLonToString(gpsLast) != LatLonToString(gps))
        {
            textInfo.text = "gps " + LatLonToString(gpsLast) + " --> " + LatLonToString(gps);
            RecordAction(StreamActionType.gps, GpsToString());
            SetMapGPS();
        }
        gpsLast = gps;
    }

    string GpsToString()
    {
        return latLonToString(gps);
    }

    string latLonToString(Vector2d latLon)
    {
        return latLon.x.ToString("F6") + ", " + latLon.y.ToString("F6");
    }

    void RestartSession()
    {
        return;
    }

    void PlayButton(Button button)
    {
        button.onClick.Invoke();
        HighlightButton(button);
    }

    public void RecordAction(StreamActionType actionType, string actionInfo)
    {
        if (sessionState != StreamState.record && actionType != StreamActionType.apiRequest && actionType != StreamActionType.apiResponse && actionType != StreamActionType.gps) return;
        //
        timeDelta = Time.realtimeSinceStartup - timeDeltaStart;
        StreamUnit su = CreateStreamUnit(actionType, actionInfo);
        stream.Add(su);
        //
        string txt = JsonUtility.ToJson(su);
        SetTextStream(txt);
        if (ynCloud)
        {
            StringToCloud(txt);
        }
    }

    public void RecordUserTypeMCI()
    {
        RecordAction(StreamActionType.userType, StreamUserType.MCI.ToString());
    }

    public void RecordUserTypeVisual()
    {
        RecordAction(StreamActionType.userType, StreamUserType.Visual.ToString());
    }

    public void RecordNewTrip()
    {
        RecordAction(StreamActionType.tripType, StreamTripType.New.ToString());
    }

    public void RecordLoadTrip()
    {
        RecordAction(StreamActionType.tripType, StreamTripType.Load.ToString());
    }

    public void RecordSearch()
    {
        RecordAction(StreamActionType.search, inputFieldSearch.text);
    }

    public void RecordWalking()
    {
        RecordAction(StreamActionType.transportationType, StreamTransportationType.walking.ToString());
    }

    public void RecordBus()
    {
        RecordAction(StreamActionType.transportationType, StreamTransportationType.bus.ToString());
    }

    public void RecordConfirm()
    {
        RecordAction(StreamActionType.confirm, "");
    }

    public void RecordApiRequest(string txt)
    {
        cntAPI++;
        UpdateImgAPIs();
        RecordAction(StreamActionType.apiRequest, txt);
    }

    public void RecordApiResponse(string txt)
    {
        cntAPI--;
        UpdateImgAPIs();
        RecordAction(StreamActionType.apiResponse, txt);
    }

    void UpdateImgAPIs()
    {
        Color colorAPI1 = Color.green;
        Color colorAPI2 = Color.yellow;
        Color colorAPI3 = Color.red;
        Color colorClear = Color.clear;
        switch (cntAPI)
        {
            case 0:
                imgAPI1.color = colorClear;
                imgAPI2.color = colorClear;
                imgAPI3.color = colorClear;
                break;
            case 1:
                imgAPI1.color = colorAPI1;
                imgAPI2.color = colorClear;
                imgAPI3.color = colorClear;
                break;
            case 2:
                imgAPI1.color = colorAPI1;
                imgAPI2.color = colorAPI2;
                imgAPI3.color = colorClear;
                break;
            case 3:
                imgAPI1.color = colorAPI1;
                imgAPI2.color = colorAPI2;
                imgAPI3.color = colorAPI3;
                break;
        }
    }

    public void RecordSaveTrip()
    {
        RecordAction(StreamActionType.saveTrip, inputFieldSaveTrip.text.ToString());
    }

    void SetMapRotation()
    {
        goMap.transform.rotation = Quaternion.Euler(0, north, 0);
    }

    void UpdateKeysCompass()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            north += northDelta;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            north -= northDelta;
        }
        CleanupNorth();
    }

    void CleanupNorth()
    {
        if (north < 0)
        {
            north += 360;
        }
        if (north > 360)
        {
            north -= 360;
        }
    }

    void UpdateKeysGPS()
    {
        float delta = .0001f;
        int dirLat = 0;
        int dirLon = 0;

        if (Input.GetKey(KeyCode.A))
        {
            dirLon = -1;
            gps += new Vector2d(dirLat * delta, dirLon * delta);
            textGPS.text = GpsToString();
        }
        if (Input.GetKey(KeyCode.D))
        {
            dirLon = 1;
            gps += new Vector2d(dirLat * delta, dirLon * delta);
            textGPS.text = GpsToString();
        }
        if (Input.GetKey(KeyCode.W))
        {
            dirLat = 1;
            gps += new Vector2d(dirLat * delta, dirLon * delta);
            textGPS.text = GpsToString();
        }
        if (Input.GetKey(KeyCode.S))
        {
            dirLat = -1;
            gps += new Vector2d(dirLat * delta, dirLon * delta);
            textGPS.text = GpsToString();
        }
    }

    void UpdatePasteButton()
    {
        int clipboardLength = GUIUtility.systemCopyBuffer.Length;
        Color color = colorDisabled;
        if (clipboardLength > 0 && !ynJsonError)
        {
            color = colorEnabled;
        }
        SetButtonColor(buttonPaste, color);
    }

    void UpdatePlayButton()
    {
        int streamLength = stream.Count;
        Color color = colorDisabled;
        if (streamLength > 0)
        {
            color = colorEnabled;
            if (sessionState == StreamState.play)
            {
                color = colorGreen;
            }
        }
        SetButtonColor(buttonPlay, color);
    }

    void CheckSessionStateChange()
    {
        if (sessionStateLast != sessionState)
        {
            switch (sessionState)
            {
                case StreamState.record:
                    stream = new List<StreamUnit>();
                    //strDateTimeStart = DateToString(DateTime.UtcNow);
                    timeDeltaStart = Time.realtimeSinceStartup;
                    break;
                case StreamState.stop:
                    CopyToClipboard();
                    ReportStream();
                    break;
                case StreamState.play:
                    RestartSession();
                    streamUnitCurrentIndex = 0;
                    timeDeltaStart = Time.realtimeSinceStartup;
                    timeDeltaTarget = stream[streamUnitCurrentIndex].timeDelta;
                    break;
            }
        }
        sessionStateLast = sessionState;
    }

    void StringToStream(string txt)
    {
        Debug.Log("StringToStream " + txt);
        ynJsonError = false;
        stream = new List<StreamUnit>();
        string[] lines = txt.Split('\n');
        foreach (string line in lines)
        {
            if (line.Length > 0)
            {
                try
                {
                    StreamUnit su = JsonUtility.FromJson<StreamUnit>(line);
                    stream.Add(su);
                }
                catch (Exception e)
                {
                    ynJsonError = true;
                    Debug.Log("LoadFromString " + e.Message);
                    //                    throw;
                }

            }
        }
        SetTextStream(txt);
        UpdateSliderStream();
        Debug.Log("LoadFromString " + stream.Count + "\n");
    }

    void ReportStream()
    {
        string txt = StreamToString();
        SetTextStream(txt);
    }

    string DateToString(DateTime dt)
    {
        string txt = dt.Year + "-" + dt.Month.ToString("D2") + "-" + dt.Day.ToString("D2") + " ";
        txt += dt.ToString("HH") + ":" + dt.Minute.ToString("D2") + ":" + dt.Second.ToString("D2");
        txt += "." + dt.Millisecond + " UTC";
        return txt;
    }

    StreamUnit CreateStreamUnit(StreamActionType actionType, string actionInfo)
    {
        StreamUnit su = new StreamUnit
        {
            key = key,
            dateTimeStart = strDateTimeStart,
            timeDelta = timeDelta,
            actionType = actionType.ToString(),
            actionInfo = actionInfo
        };
        return su;
    }

    void CheckIfStreamUnitCurrentReady()
    {
        if (sessionState == StreamState.play)
        {
            if (stream.Count > 0)
            {
                timeDeltaCurrent = Time.realtimeSinceStartup - timeDeltaStart;
                if (timeDeltaCurrent > stream[streamUnitCurrentIndex].timeDelta)
                {
                    PlayStreamUnitCurrent();
                    string txt = JsonUtility.ToJson(stream[streamUnitCurrentIndex]);
                    SetTextStream(txt);
                    UpdateSliderStream();
                    //
                    streamUnitCurrentIndex++;
                    if (streamUnitCurrentIndex == stream.Count)
                    {
                        OnClickStop();
                        //                        streamUnitCurrentIndex = 0;
                        //                        timeDeltaStart = Time.realtimeSinceStartup;
                        //                        RestartSession();
                    }
                    else
                    {
                        timeDeltaTarget = stream[streamUnitCurrentIndex].timeDelta;
                    }
                }
            }
        }
    }

    void UpdateSliderStream()
    {
        sliderStream.minValue = 0;
        sliderStream.maxValue = stream.Count;
        sliderStream.value = streamUnitCurrentIndex;
    }

    void SetCompassNorthFromSensor()
    {
        if (cameraAR.enabled) return;
        float value = -1 * Input.compass.trueHeading;
        north = value * smoothNorth + north * (1 - smoothNorth);
    }

    void SetGPSFromSensor()
    {
//        textInfo.text = cntFramesFPS + " " + Input.location.lastData.latitude + ", " + Input.location.lastData.longitude;
        gps = new Vector2d(Input.location.lastData.latitude, Input.location.lastData.longitude);
    }

    void SetMapGPS()
    {
        //        abstractMap.SetCenterLatitudeLongitude(gps);
        abstractMap.UpdateMap(gps);
    }

    StreamTripType StringToTripType(string txt)
    {
        StreamTripType tripType;
        Enum.TryParse(txt, out tripType);
        return tripType;
    }

    StreamTransportationType StringToTransportationType(string txt)
    {
        StreamTransportationType transportationType;
        Enum.TryParse(txt, out transportationType);
        return transportationType;
    }

    StreamActionType StringToActionType(string txt)
    {
        StreamActionType actionType;
        Enum.TryParse(txt, out actionType);
        return actionType;
    }

    StreamUserType StringToUserType(string txt)
    {
        StreamUserType userType;
        Enum.TryParse(txt, out userType);
        return userType;
    }

    Button FindLoadedTripButton(string txtSearch)
    {
        Button button = null;
        foreach (Transform t in goContentLoadedTrips.transform)
        {
            string txt = GetLatLonsFromLoadedTrip(t.gameObject);
            if (txt == txtSearch)
            {
                button = t.transform.Find("Add").GetComponent<Button>();
                break;
            }
        }
        Debug.Log("FindLoadedTripButton " + txtSearch + " " + button);
        return button;
    }

    Button FindSearchResultButton(string txtSearch)
    {
        Button button = null;
        foreach (Transform t in goContentSearchResults.transform)
        {
            string txt = GetLatLonFromSearchResult(t.gameObject);
            if (txt == txtSearch)
            {
                button = t.transform.Find("Add").GetComponent<Button>();
                break;
            }
        }
        Debug.Log("FindSearchResultButton " + txtSearch + " " + button);
        return button;
    }

    public string LatLonsToString(Vector2d latLonFrom, Vector2d latLonTo)
    {
        string txt = LatLonToString(latLonFrom) + ", ";
        txt += LatLonToString(latLonTo);
        return txt;
    }

    public string LatLonToString(Vector2d latLon)
    {
        string txt = latLon.x.ToString("F6") + ", " + latLon.y.ToString("F6");
        return txt;
    }

    public string GetLatLonsFromLoadedTrip(GameObject go)
    {
        LoadTripButtonController ltbc = go.GetComponent<LoadTripButtonController>();
        Vector2d latLonFrom = ltbc.Coordinates[0];
        Vector2d latLonTo = ltbc.Coordinates[1];
        string txt = LatLonsToString(latLonFrom, latLonTo);
        return txt;
    }

    public string GetLatLonFromSearchResult(GameObject go)
    {
        GeocodeResponseData grd = go.GetComponent<GeocodeResponseData>();
        Vector2d latLon = grd.Coordinates;
        string txt = LatLonToString(latLon);
        return txt;
    }

    Vector2d GetLatLonCurrent()
    {
        return new Vector2d(Input.location.lastData.latitude, Input.location.lastData.longitude);
    }

    void PlayStreamUnitCurrent()
    {
        StreamUnit su = stream[streamUnitCurrentIndex];
        switch (StringToActionType(su.actionType))
        {
            case StreamActionType.userType:
                PlayUserType();
                break;
            case StreamActionType.tripType:
                PlayTripType();
                break;
            case StreamActionType.compass:
                PlayCompass();
                break;
            case StreamActionType.loadedTrip:
                PlayLoadedTrip();
                break;
            case StreamActionType.searchResult:
                PlaySearchResult();
                break;
            case StreamActionType.search:
                PlaySearch();
                break;
            case StreamActionType.transportationType:
                PlayTransportationType();
                break;
            case StreamActionType.saveTrip:
                PlaySaveTrip();
                break;
            case StreamActionType.confirm:
                PlayConfirm();
                break;
        }
    }

    void PlayUserType()
    {
        StreamUnit su = stream[streamUnitCurrentIndex];
        StreamUserType userType = StringToUserType(su.actionInfo);
        switch (userType)
        {
            case StreamUserType.MCI:
                PlayButton(buttonMCI);
                break;
            case StreamUserType.Visual:
                PlayButton(buttonVisual);
                break;
        }
    }

    void PlayTripType()
    {
        StreamUnit su = stream[streamUnitCurrentIndex];
        StreamTripType tripType = StringToTripType(su.actionInfo);
        switch (tripType)
        {
            case StreamTripType.New:
                PlayButton(buttonNewTrip);
                break;
            case StreamTripType.Load:
                PlayButton(buttonLoadTrip);
                break;
        }
    }

    void PlayTransportationType()
    {
        StreamUnit su = stream[streamUnitCurrentIndex];
        StreamTransportationType transportationType = StringToTransportationType(su.actionInfo);
        switch (transportationType)
        {
            case StreamTransportationType.walking:
                PlayButton(buttonWalking);
                break;
            case StreamTransportationType.bus:
                PlayButton(buttonBus);
                break;
        }
    }

    void PlaySearch()
    {
        StreamUnit su = stream[streamUnitCurrentIndex];
        //
        inputFieldSearch.text = su.actionInfo;
        inputFieldSearch.onEndEdit.Invoke(su.actionInfo);
    }

    void PlaySaveTrip()
    {
        StreamUnit su = stream[streamUnitCurrentIndex];
        //
        inputFieldSaveTrip.text = su.actionInfo;
        inputFieldSaveTrip.onEndEdit.Invoke(su.actionInfo);
    }

    void PlayConfirm()
    {
        PlayButton(buttonConfirm);
    }

    void PlayLoadedTrip()
    {
        StreamUnit su = stream[streamUnitCurrentIndex];
        string txtLatLons = su.actionInfo;
        Debug.Log("PlayTripLoaded |" + txtLatLons + "|");
        buttonLoadedTrip = FindLoadedTripButton(txtLatLons);
        PlayButton(buttonLoadedTrip);
    }

    void PlaySearchResult()
    {
        StreamUnit su = stream[streamUnitCurrentIndex];
        string txtLatLon = su.actionInfo;
        Debug.Log("PlaySearchResult |" + txtLatLon + "|");
        buttonSearchResult = FindSearchResultButton(txtLatLon);
        if (buttonSearchResult == null) Debug.Log("null buttonSearchResult");
        PlayButton(buttonSearchResult);
    }

    public void PlayCompass()
    {
        StreamUnit su = stream[streamUnitCurrentIndex];
        north = float.Parse(su.actionInfo);
    }

    void SetTextStream(string txt)
    {
        textStream.text = txt;
    }

    string StreamToString()
    {
        string txt = "";
        foreach (StreamUnit su in stream)
        {
            txt += JsonUtility.ToJson(su) + "\n";
        }
        return txt;
    }

    void CopyToClipboard()
    {
        string txt = StreamToString();
        GUIUtility.systemCopyBuffer = txt;
        Debug.Log(stream.Count + " CopyToClipboard " + txt);
    }

    void StringToCloud(string txt)
    {
        string args = StringToArguments(txt);
        StartCoroutine(SendStringWWW("insert", args));
    }

    string StringToArguments(string txt)
    {
        string args = "&tag=" + key + "&dateTimeStart=" + strDateTimeStart + "&timeDelta=" + timeDelta + "&streamUnit=" + txt;
        return args;
    }

    IEnumerator SendStringWWW(string action, string args)
    {
        string url = urlServer + "?action=" + action + args;
        UnityWebRequest www = UnityWebRequest.Get(url);
        SetupWWW(www);
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
            Debug.Log(url);
        }
        else
        {
            Debug.Log("SendStringWWW " + urlServer + "?" + args);
        }
    }

    public void SetupWWW(UnityWebRequest www)
    {
        www.SetRequestHeader("Accept", "*/*");
        www.SetRequestHeader("Accept-Encoding", "gzip, deflate");
        www.SetRequestHeader("User-Agent", "runscope/0.1");
    }

    void CloudToStream()
    {
        string args = "&tag=" + key + "&dateTimeStart=" + strDateTimeStart;
        StartCoroutine(GetStringWWW("select", args));
    }

    void GetTags()
    {
        string args = "";
        StartCoroutine(GetStringWWW("tags", args));
    }

    void GetDateTimeStartsForTag()
    {
        string args = "&tag=" + key;
        StartCoroutine(GetStringWWW("dateTimeStarts", args));
    }

    IEnumerator GetStringWWW(string action, string args)
    {
        UnityWebRequest www = UnityWebRequest.Get(urlServer + "?action=" + action + args);
        SetupWWW(www);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            string txt = www.downloadHandler.text;
            switch (action)
            {
                case "select":
                    StringToStream(txt);
                    CopyToClipboard();
                    OnClickPlay();
                    break;
                case "tags":
                    LoadButtonsUI(txt, buttonTag, goScrollViewTags);
                    break;
                case "dateTimeStarts":
                    LoadButtonsUI(txt, buttonDateTimeStart, goScrollViewDateTimeStarts);
                    break;
            }
        }
    }

    void ClearScrollViewContent(Button button)
    {
        Transform tParent = button.transform.parent;
        for (int n = 0; n < tParent.childCount; n++)
        {
            if (n > 0)
            {
                Destroy(tParent.GetChild(n).gameObject);
            }
        }
    }

    void SetActiveScrollView(GameObject goScrollView)
    {
        if (goScrollViewTags == goScrollView)
        {
            goScrollViewTags.SetActive(true);
        }
        else
        {
            goScrollViewTags.SetActive(false);
        }
        if (goScrollViewDateTimeStarts == goScrollView)
        {
            goScrollViewDateTimeStarts.SetActive(true);
        }
        else
        {
            goScrollViewDateTimeStarts.SetActive(false);
        }
    }

    void LoadButtonsUI(string txt, Button button, GameObject goScrollView)
    {
        ClearScrollViewContent(button);
        SetActiveScrollView(goScrollView);
        Vector2 pos = button.GetComponent<RectTransform>().anchoredPosition;
        int cnt = 0;
        ynJsonError = false;
        string[] lines = txt.Split('\n');
        foreach (string line in lines)
        {
            if (line.Length > 0)
            {
                try
                {
                    // check
                    //StreamUnit su = JsonUtility.FromJson<StreamUnit>(line);
                    //
                    Button b = Instantiate(button, button.transform.parent);
                    b.name = line;
                    b.GetComponentInChildren<Text>().text = line;
                    b.GetComponent<RectTransform>().anchoredPosition = pos - new Vector2(0, button.GetComponent<RectTransform>().sizeDelta.y * cnt);
                    //
                    cnt++;
                }
                catch (Exception e)
                {
                    ynJsonError = true;
                    Debug.Log("LoadTagsUI " + e.Message);
                }

            }
        }
        if (cnt > 0)
        {
            button.gameObject.SetActive(false);
        }
        Debug.Log("LoadTagsUI " + cnt);
    }

    public void OnClickTags()
    {
        GetTags();
    }

    public void OnClickTag(Button button)
    {
        SetKeyFromButton(button);
        GetDateTimeStartsForTag();
    }

    public void OnClickDateTimeStart(Button button)
    {
        SetStrDateTimeStartFromButton(button);
        CloudToStream();
    }

    void SetKeyFromButton(Button button)
    {
        Debug.Log("SetKeyFromButton " + button);
        key = button.name;
    }

    void SetStrDateTimeStartFromButton(Button button)
    {
        strDateTimeStart = button.name;
    }

    public void OnClickRecord()
    {
        sessionState = StreamState.record;
        UpdateButtonsUI();
    }

    public void OnClickStop()
    {
        sessionState = StreamState.stop;
        UpdateButtonsUI();
    }
    public void OnClickPlay()
    {
        sessionState = StreamState.play;
        UpdateButtonsUI();
    }

    void UpdateButtonsUI()
    {
        return;
        Color colorRecord = Color.white;
        Color colorStop = Color.white;
        Color colorPlay = Color.white;
        switch (sessionState)
        {
            case StreamState.record:
                colorRecord = Color.red;
                break;
            case StreamState.stop:
                colorStop = Color.white;
                break;
            case StreamState.play:
                colorPlay = Color.green;
                break;
        }
        SetButtonColor(buttonRecord, colorRecord);
        SetButtonColor(buttonStop, colorStop);
        SetButtonColor(buttonPlay, colorPlay);
    }

    void SetButtonColor(Button button, Color color)
    {
        button.GetComponent<Image>().color = color;
    }

    public void OnClickPaste()
    {
        string txt = "?";
        if (ynCloud)
        {
            CloudToStream();
        }
        else
        {
            txt = GUIUtility.systemCopyBuffer;
            StringToStream(txt);
        }
    }

    void HighlightButton(Button button)
    {
        return;
        buttonHighlight = button;
        SetButtonColor(buttonHighlight, colorHighlighted);
        Invoke("UnHighlightButton", timeDelay);
    }

    void UnHighlightButton()
    {
        buttonHighlight.GetComponent<Image>().color = colorUnHighlighted;
    }
}

public class StreamUnit
{
    public string key;
    public string dateTimeStart;
    public float timeDelta;
    //
    public string actionType;
    public string actionInfo;
}

public enum StreamUserType
{
    none,
    MCI,
    Visual
}

public enum StreamTripType
{
    none,
    New,
    Load
}

public enum StreamTransportationType
{
    none,
    walking,
    bus
}

public enum StreamActionType
{
    none,
    gps,
    compass,
    userType,
    tripType,
    search,
    searchResult,
    loadedTrip,
    transportationType,
    saveTrip,
    confirm,
    apiRequest,
    apiResponse,
    sessionStart,
    sessionEnd
}
public enum StreamState
{
    record,
    stop,
    play
}