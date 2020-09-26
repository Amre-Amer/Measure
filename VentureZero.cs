using FantomLib;
using Mapbox.Directions;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VentureZero : MonoBehaviour
{
    public DirectionsFactory directionsFactory;
    public StepsFactory stepsFactory;
    public ProgressFactory progressFactory;
    public VibratorController vibratorController;
    public FeedbackFactory feedbackFactory;
    public StreamManager streamManager;
    public DirectionsResponse response;
    public Vector2d latLonDestination;
    public string tripName;
    public Vector2d latLonStart;
    //
    public GameObject goARRoot;
    public Camera camera2D;
    public Camera cameraAR;
    public GameObject goMapHolder;
    public AbstractMap map;
    public GameObject goUser;
    public GameObject goStart;
    public GameObject goDestination;
    public TextMeshProUGUI textManeuver;
    public Image imageManeuver;
    public Text textInfo;
    //
    public Text textFPS;
    public Text textNorth;
    public Text textGPS;
    public Text textAPI;
    public Text textWWW;
    public Text textLoc;
    public Image imageGPS;
    public Image imageNorth;
    public Image imageAPI;
    public Image imageOffCourse;
    public Button buttonUseGPS;
    public Button buttonDebug;
    public Button buttonManual;
    public Button buttonRobot;
    public Button buttonReRoute;
    public GameObject goDebugUI;
    public GameObject goStreamManagerUI;
    public GameObject goDirectionButtonsUI;
    public GameObject goNoWWWUI;
    public GameObject goNoGPSUI;
    //
    public Vector2d gps;
    public Vector2d gpsLast;
    public string gpsString;
    public string gpsLastString;
    public bool ynGPS;
    //
    bool ynAR;
    bool ynARLast;
    bool ynARFirst;
    public bool ynUseGPS = true;
    public bool ynUseGPSLast = false;
    bool ynGPSLast;
    bool ynCompass = false;
    bool ynCompassLast = true;
    public bool ynDebug;
    public bool ynDebugLast;
    bool ynMapFirst;
    int cntFPS;
    bool ynReRoute;
    bool ynReRouteLast;
    bool ynGPSFirst;
    Vector2d latLonFirst;
    public float north;
    public float northLast;
    float northFirst;
    const float smoothNorth = .05f;
    const int targetFrameRate2D = 90;
    const int targetFrameRateAR = 30;
    public float deltaGPS = .0001f;
    const float deltaNorth = 3f;
    const float zoomMin = 16;
    const float zoomMax = 18;
    const float deltaZoom = 1;

    private void Awake()
    {
        Input.location.Start();  // .5,1
        Input.compass.enabled = true;
        Application.targetFrameRate = targetFrameRate2D;
        goDebugUI.SetActive(false);
        goDirectionButtonsUI.SetActive(false);
        goStreamManagerUI.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        InvokeRepeating("ShowFPS", 1, 1);
    }

    // Update is called once per frame
    private void Update()
    {
        streamManager.UpdateStreamManager();
        stepsFactory.UpdateStepsFactory();
        CheckYnDebug();
        CheckButtonAR();
        CheckYnUseGPS();
        CheckYnRobot();
        CheckYnManual();
        CheckYnReRoute();
        UpdateCompass();
        UpdateGPS();
    }

    void LateUpdate()
    {
        gpsLast = gps;
        gpsLastString = gpsString;
        northLast = north;
        ynARLast = ynAR;
        ynUseGPSLast = ynUseGPS;
        ynGPSLast = ynGPS;
        ynCompassLast = ynCompass;
        ynDebugLast = ynDebug;
        streamManager.LateUpdateStreamManager();
        stepsFactory.LateUpdateStepsFactory();
        ynReRouteLast = ynReRoute;
        cntFPS++;
    }

    void CheckYnReRoute()
    {
        if (ynReRouteLast != ynReRoute)
        {
            Color color = Color.white;
            if (ynReRoute == true)
            {
                ReRoute();
                color = Color.green;
            }
            buttonReRoute.GetComponent<Image>().color = color;
        }
    }

    void ColorImageGPS()
    {
        if (ynGPS == true)
        {
            imageGPS.color = Color.green;
            textGPS.color = Color.grey;
        }
        else
        {
            imageGPS.color = Color.red;
            textGPS.color = Color.red;
        }
    }

    void HighlightImageGPS()
    {
        imageGPS.color = Color.magenta;
        Invoke("ColorImageGPS", .25f);
    }

    public void SetGPS(Vector2d latLon)
    {
        gps = latLon;
        gpsString = LatLonToString(gps);
        textGPS.text = gpsString;
    }

    void CheckYnGPS()
    {
        if (Input.location.status == LocationServiceStatus.Running)
        {
            ynGPS = true;
        } else
        {
            ynGPS = false;
        }
        if (Application.isEditor == false)
        {
            if (ynGPSLast != ynGPS)
            {
                goNoGPSUI.SetActive(!ynGPS);
            }
        }
        if (ynGPSLast != ynGPS)
        {
            ColorImageGPS();
        }
    }

    void UpdateGPS()
    {
        CheckYnGPS();
        if (ynUseGPS == true)
        {
            if (ynGPS == true)
            {
                Vector2d latLon = new Vector2d(Input.location.lastData.latitude, Input.location.lastData.longitude);
                SetGPS(latLon);
            } else
            {
                if (ynGPSFirst == false)
                {
                    latLonFirst = map.CenterLatitudeLongitude;
                    ynGPSFirst = true;
                }
                SetGPS(latLonFirst);
            }
        }
        OnKeyboardGPS();
        if (ynGPSLast != ynGPS || gpsLastString != gpsString || ynUseGPSLast != ynUseGPS || ynDebugLast != ynDebug)
        {
            if (ynMapFirst == true)
            {
                CenteMap();
            }
        }
    }

    public void CenteMap()
    {
        ColorImageGPS();
//        map.UpdateMap(gps);
        OnCenterMap();
        HighlightImageGPS();
    }

    void OnKeyboardGPS()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            OnPanLeft();
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            OnPanRight();
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            OnPanUp();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            OnPanDown();
        }
    }

    void CheckYnUseGPS()
    {
        if (ynUseGPSLast != ynUseGPS || ynGPSLast != ynGPS)
        {
            Color color;
            if (ynGPS == true)
            {
                if (ynUseGPS)
                {
                    color = Color.green;
                } else
                {
                    color = Color.yellow;
                }
            }
            else
            {
                if (ynGPS == true)
                {
                    color = Color.red;
                }
                else
                {
                    color = Color.magenta;
                }
            }
            buttonUseGPS.GetComponent<Image>().color = color;
        }
    }

    void CheckButtonAR()
    {
        if (ynARLast != ynAR)
        {
            goARRoot.SetActive(ynAR);
            camera2D.enabled = !ynAR;
            cameraAR.enabled = ynAR;
            if (ynAR == true)
            {
                Application.targetFrameRate = targetFrameRateAR;
                if (ynARFirst == false && Input.compass.enabled == true)
                {
                    northFirst = north;
                    ynARFirst = true;
                }
                else
                {
                    goMapHolder.transform.rotation = Quaternion.Euler(0, -northFirst, 0);
                }
            }
            else
            {
                goMapHolder.transform.rotation = Quaternion.Euler(0, 0, 0);
                Application.targetFrameRate = targetFrameRate2D;
            }
        }
    }

    void CheckYnDebug()
    {
        if (ynDebugLast != ynDebug)
        {
            Debug.Log("CheckYnDebug");
            goDebugUI.SetActive(ynDebug);
            goStreamManagerUI.SetActive(ynDebug);
        }
    }
    void CheckCompass()
    {
        ynCompass = Input.compass.enabled;
        OnKeyboardCompass();
        if (ynCompassLast != ynCompass)
        {
            Color color = Color.red;
            if (ynCompass == true)
            {
                color = Color.white;
            }
            textNorth.color = color;
            imageNorth.color = color;
        }
    }

    void UpdateCompass()
    {
        CheckCompass();
        float northNew = north;
        if (ynCompass == true)
        {
            northNew = Input.compass.trueHeading;
        }
        if (ynAR == false)
        {
            SmoothNorth(northNew);
            FaceMapNorth();
        }
        textNorth.text = north.ToString("F0");
    }

    void SmoothNorth(float northNew)
    {
        if (northNew - north > 180) north = 360 + north;
        if (northNew - north < -180) north = 360 - north;
        north = northNew * smoothNorth + north * (1 - smoothNorth);
    }

    void FaceMapNorth()
    {
//        goMapHolder.transform.rotation = Quaternion.Euler(0, -north, 0);
        goUser.transform.rotation = Quaternion.Euler(0, north, 0);
        imageNorth.transform.rotation = Quaternion.Euler(0, 0, north);
        goDirectionButtonsUI.transform.rotation = Quaternion.Euler(0, 0, north);
    }

    void OnKeyboardCompass()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            north -= deltaNorth;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            north += deltaNorth;
        }
    }

    public string GetTripInfo()
    {
        string txt = tripName + " start " + LatLonToString(latLonStart) + " destination " + LatLonToString(latLonDestination);
        return txt;        
    }

    void ShowFPS()
    {
        textFPS.text = "fps\n" + cntFPS;
        cntFPS = 0;
    }

    public string LatLonToString(Vector2d latLon)
    {
        return latLon.x.ToString("F6") + ", " + latLon.y.ToString("F6");
    }

    public string LatLonsToString(Vector2d latLonFrom, Vector2d latLonTo)
    {
        string txt = LatLonToString(latLonFrom) + ", ";
        txt += LatLonToString(latLonTo);
        return txt;
    }

    public void OnUseGPS()
    {
        ynUseGPS = !ynUseGPS;
        stepsFactory.ynRobot = !ynUseGPS;
        stepsFactory.ynManual = !ynUseGPS;
    }

    public void OnClickAR()
    {
        ynAR = cameraAR.enabled;
    }

    public void OnClickDebug()
    {
        ynDebug = !ynDebug;
    }

    public void OnRequestAPI(string txt)
    {
        streamManager.RecordApiRequest(txt);
        imageAPI.color = Color.red;
    }

    public void OnResponseAPI(string txt)
    {
        streamManager.RecordApiResponse(txt);
        imageAPI.color = Color.white;
    }

    public void OnCenterMap()
    {
        Vector2d latLonStart = map.WorldToGeoPosition(goStart.transform.position);
        Vector2d latLonFinish = map.WorldToGeoPosition(goDestination.transform.position);
        Vector2d latLonUser = map.WorldToGeoPosition(goUser.transform.position);
        //
//        map.UpdateMap(latLonUser);
        map.UpdateMap(gps);
        //
        goUser.transform.position = Vector3.zero;
        goStart.transform.position = map.GeoToWorldPosition(latLonStart);
        goDestination.transform.position = map.GeoToWorldPosition(latLonFinish);
        //
        directionsFactory.HandleDirectionsResponse(directionsFactory.responseLast);
        stepsFactory.HandleDirectionsResponse(stepsFactory.responseLast);
        progressFactory.HandleDirectionsResponse(progressFactory.responseLast);
    }

    public void OnPanUp()
    {
        Pan(deltaGPS, 0);
    }

    public void OnPanDown()
    {
        Pan(-deltaGPS, 0);
    }

    public void OnPanLeft()
    {
        Pan(0, -deltaGPS);
    }

    public void OnPanRight()
    {
        Pan(0, +deltaGPS);
    }

    void Pan(float dx, float dy)
    {
        gps += new Vector2d(dx, dy);
        SetGPS(gps);
    }

    public void OnZoomIn()
    {
        float zoom = map.Zoom + deltaZoom;
        ZoomMap(zoom);
    }

    public void OnZoomOut()
    {
        float zoom = map.Zoom - deltaZoom;
        ZoomMap(zoom);
    }

    void ZoomMap(float zoom)
    {
        if (zoom < zoomMin)
        {
            zoom = zoomMin;
        }
        if (zoom > zoomMax)
        {
            zoom = zoomMax;
        }
        map.UpdateMap(zoom);
    }

    public void OnReRoute()
    {
        ReRoute();
//        ynReRoute = true;
    }

    void ReRoute()
    {
        stepsFactory.steps = null;
        goUser.transform.position = map.GeoToWorldPosition(gps);
        directionsFactory.Query();
        //stepsFactory.Query();
        //routeProgressBarFactory.Query();
        //
        //Invoke("PresentFirstMessageDelayed", 1);
        ynMapFirst = true;
    }

    void PresentFirstMessageDelayed()
    {
        //routeProgressBarController.PresentFirstMessage();
    }

    public void OnTurnRight()
    {
        north += deltaNorth;
    }

    public void OnTurnLeft()
    {
        north -= deltaNorth;
    }

    public void SetScaleWithGeoDist(GameObject go, float geoDist)
    {
        Vector2d latLonA = gps;
        Vector2d latLonB = gps + new Vector2d(0, geoDist);
        Vector3 posA = map.GeoToWorldPosition(latLonA);
        Vector3 posB = map.GeoToWorldPosition(latLonB);
        float dist = Vector3.Distance(posA, posB);
        go.transform.localScale = Vector3.one * dist * 2;
    }

    public void OnYnManual()
    {
        stepsFactory.ynManual = !stepsFactory.ynManual;
        ynUseGPS = !stepsFactory.ynManual;
        stepsFactory.ynRobot = !stepsFactory.ynManual;
    }

    void CheckYnManual()
    {
        if (stepsFactory.ynManualLast != stepsFactory.ynManual)
        {
            Color color = Color.white;
            if (stepsFactory.ynManual == true)
            {
                color = Color.green;
            }
            buttonManual.GetComponent<Image>().color = color;
            goDirectionButtonsUI.SetActive(stepsFactory.ynManual);
        }
    }

    public void OnYnRobot()
    {
        stepsFactory.ynRobot = !stepsFactory.ynRobot;
        ynUseGPS = !stepsFactory.ynRobot;
        stepsFactory.ynManual = !stepsFactory.ynRobot;
    }

    void CheckYnRobot()
    {
        if (stepsFactory.ynRobotLast != stepsFactory.ynRobot)
        {
            Color color = Color.white;
            if (stepsFactory.ynRobot == true)
            {
                color = Color.green;
            }
            buttonRobot.GetComponent<Image>().color = color;
        }
    }
}
