using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARRaycastManager))]
[RequireComponent(typeof(AudioSource))]

public class Measure : MonoBehaviour
{
    public Text textInfo;
    public Text textInfoLarge;
    public GameObject goFirstPrefab;
    public GameObject goSecondPrefab;
    ARRaycastManager aRRaycastManager;
    GameObject goFirst;
    GameObject goSecond;
    List<ARRaycastHit> hits = new List<ARRaycastHit>();
    Vector2 scrCenter;
    bool ynTap;
    int touchCount;
    int touchCountLast;
    SequenceType sequence;
    const float feetPerMeter = 3.28f;
    public GameObject goTapeMeasurePrefab;
    GameObject goTapeMeasure;
    public GameObject goTape;
    int cntFrames;
    float distMeters;
    int distInches;
    int distInchesLast;
    AudioSource audioSource;
    public AudioClip clipTape;
    public AudioClip clipOk;
    SequenceType sequenceLast;
    public GameObject goPoi;
    string distFormatted;
    public Camera cam;
    public Image imageProgress;
    bool ynFirstPlaneFound;
    public Image imageThumb;
    public Image imageCenterH;
    public Image imageCenterV;
    bool ynPlaneFound;
    bool ynPlaneFoundLast;
    bool ynFirstTap;

    private void Awake()
    {
        aRRaycastManager = GetComponent<ARRaycastManager>();
        audioSource = GetComponent<AudioSource>();
        sequence = SequenceType.searching;
    }

    private void Start()
    {
        goFirst = Instantiate(goFirstPrefab);
        goSecond = Instantiate(goSecondPrefab);
        ShowHideHelpers(false);
        ShowHideFirstTap(false);
        imageThumb.gameObject.SetActive(false);
        ShowInfo();
    }

    private void Update()
    {
        UpdateMeasure();
        if (!ynFirstPlaneFound)
        {
            UpdateProgress();
            return;
        }
        CheckTap();
        UpdateDistance();
        if (sequenceLast != sequence || DidChangeDist())
        {
            FormatDist();
            PlaySound(clipTape);
            ShowInfo();
        }
        AdjustTapeMeasure();
        PlaceMeasurement();
        AdjustPoi();
        //
        sequenceLast = sequence;
        touchCountLast = touchCount;
        distInchesLast = distInches;
        ynPlaneFoundLast = ynPlaneFound;
        cntFrames++;
    }

    void UpdateDistance()
    {
        distMeters = GetDist();
        distInches = MetersToInches(distMeters);
    }

    void ShowHideHelpers(bool yn)
    {
        goTape.SetActive(yn);
        imageCenterH.gameObject.SetActive(yn);
        imageCenterV.gameObject.SetActive(yn);
    }

    void UpdateProgress()
    {
        imageProgress.transform.Rotate(0, 0, -3);
    }

    void CheckTap()
    {
        ynTap = false;
        touchCount = Input.touchCount;
        if (touchCountLast != touchCount)
        {
            if (touchCount == 0)
            {
                imageThumb.gameObject.SetActive(false);
            } else
            {
                ynTap = true;
                CheckFirstTap();
                imageThumb.gameObject.SetActive(true);
            }
        }
        if (touchCount > 0)
        {
            imageThumb.GetComponent<RectTransform>().position = Input.touches[0].position;
        }
    }

    void CheckFirstTap()
    {
        if (!ynFirstTap)
        {
            ShowHideFirstTap(true);
            ynFirstTap = true;
        }
    }

    void ShowHideFirstTap(bool yn)
    {
        goPoi.SetActive(yn);
        goFirst.SetActive(yn);
        goSecond.SetActive(yn);
    }

    void UpdateMeasure()
    {
        RayCastPlane();
        if (ynPlaneFoundLast != ynPlaneFound)
        {
            if (ynPlaneFound)
            {
                imageCenterH.color = Color.white;
                imageCenterV.color = Color.white;
            }
            else
            {
                imageCenterH.color = Color.red;
                imageCenterV.color = Color.red;
            }
        }
        if (ynPlaneFound)
        {
            FirstPlaneFound();
            Pose hitPose = hits[0].pose;
            if (ynTap)
            {
                UpdateMeasureTap(hitPose);
            }
            else
            {
                UpdateMeasureNoTap(hitPose);
            }
        }
    }

    void RayCastPlane()
    {
        scrCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        if (aRRaycastManager.Raycast(scrCenter, hits, TrackableType.Planes))
        {
            ynPlaneFound = true;
        }
        else
        {
            ynPlaneFound = false;
        }
    }

        void UpdateMeasureNoTap(Pose hitPose)
    {
        switch (sequence)
        {
            case SequenceType.first:
                AddEditTapMeasure(hitPose);
                break;
            case SequenceType.second:
                AddEditTapMeasure(hitPose);
                goSecond.transform.position = hitPose.position;
                break;
        }
    }

    void UpdateMeasureTap(Pose hitPose)
    {
        switch (sequence)
        {
            case SequenceType.first:
                goFirst.transform.position = hitPose.position;
                sequence = SequenceType.second;
                break;
            case SequenceType.second:
                sequence = SequenceType.first;
                break;
        }
    }

    void FirstPlaneFound()
    {
        if (!ynFirstPlaneFound)
        {
            imageProgress.gameObject.SetActive(false);
            ynFirstPlaneFound = true;
            sequence = SequenceType.first;
            ShowHideHelpers(true);
        }
    }

    void AdjustPoi()
    {
        goPoi.transform.position = GetPosMid() + new Vector3(0, .15f, 0);
        goPoi.transform.LookAt(cam.transform.position);
        goPoi.transform.Rotate(0, 180, 0);
        SetPoiText();
    }

    void SetPoiText()
    {
        TextMesh[] textMeshes = FindObjectsOfType<TextMesh>();
        foreach(TextMesh textMesh in textMeshes)
        {
            textMesh.text = distFormatted;
        }
    }

    void PlaceMeasurement()
    {
        PlaceTape();
        AdjustMaterialTile();
    }

    void PlaceTape()
    {
        goTape.transform.position = GetPosMid();
        goTape.transform.LookAt(goFirst.transform.position);
        Vector3 sca = goTape.transform.localScale;
        sca.z = distMeters;
        goTape.transform.localScale = sca;
        goTape.transform.Rotate(0, 180, 90);
    }

    void AdjustMaterialTile()
    {
        float tiling = goTape.transform.localScale.z * feetPerMeter;
        Renderer renderer = goTape.GetComponent<Renderer>();
        renderer.material.mainTextureScale = new Vector2(tiling, 1);
    }

    int MetersToInches(float meters)
    {
        return (int)(meters * feetPerMeter * 12);
    }

    bool DidChangeDist()
    {
        if (distInchesLast != distInches)
        {
            return true;
        }
        return false;
    }

    void PlaySound(AudioClip clip)
    {
        audioSource.clip = clip;
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        audioSource.Play();
    }

    Vector3 GetPosMid()
    {
        return (goFirst.transform.position + goSecond.transform.position) / 2;
    }

    float GetDist()
    {
        return Vector3.Distance(goFirst.transform.position, goSecond.transform.position);
    }

    void AdjustTapeMeasure()
    {
        switch (sequence)
        {
            case SequenceType.first:
                RotateTapeMeasure();
                break;
            case SequenceType.second:
                TapeMeasureLookAtFirst();
                break;
        }
    }

    void TapeMeasureLookAtFirst()
    {
        Vector3 posLook = goFirst.transform.position;
        goTapeMeasure.transform.LookAt(posLook);
    }

    void RotateTapeMeasure()
    {
        float yaw = cntFrames;
        Vector3 eul = goTapeMeasure.transform.eulerAngles;
        eul.y = yaw;
        goTapeMeasure.transform.eulerAngles = eul;
    }

    void AddEditTapMeasure(Pose hitPose)
    {
        if (!goTapeMeasure)
        {
            goTapeMeasure = Instantiate(goTapeMeasurePrefab, hitPose.position, hitPose.rotation);
        }
        else
        {
            MatchPose(goTapeMeasure, hitPose);
        }
    }

    void MatchPose(GameObject go, Pose hitPose)
    {
        go.transform.position = hitPose.position;
        go.transform.rotation = hitPose.rotation;
    }

    void ShowInfo()
    {
        textInfo.text = GetMsgForSequence();
        textInfoLarge.text = distFormatted;
    }

    string GetMsgForSequence()
    {
        string txt = "?";
        switch(sequence)
        {
            case SequenceType.searching:
                txt = "Searching...";
                break;
            case SequenceType.first:
                txt = "Tap first point:";
                break;
            case SequenceType.second:
                txt = "Tap second point:";
                break;
        }
        return txt;
    }

    void FormatDist()
    {
        string txt = "";
        int inches = distInches;
        if (distInches >= 12)
        {
            int feet = inches / 12;
            inches -= feet * 12;
            txt +=  feet + "'-" + inches + "\"";
        }
        else
        {
            txt += inches + "\"";
        }
        distFormatted = txt;
    }
}

public enum SequenceType
{
    searching,
    first,
    second
}

