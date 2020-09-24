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
    public GameObject goTapeMeasure;
    public GameObject goTape;
    int cntFrames;
    float dist;
    AudioSource audioSource;
    float distLast;
    public AudioClip clipTape;
    public AudioClip clipOk;
    SequenceType sequenceLast;
    public GameObject goPoi;
    string distFormatted;
    public Camera cam;

    private void Awake()
    {
        aRRaycastManager = GetComponent<ARRaycastManager>();
        audioSource = GetComponent<AudioSource>();
        sequence = SequenceType.first;
    }

    private void Start()
    {
        goFirst = Instantiate(goFirstPrefab);
        goSecond = Instantiate(goSecondPrefab);
    }

    private void Update()
    {
        UpdateYnTap();
        UpdateMeasure();
        dist = GetDist();
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
        distLast = dist;
        cntFrames++;
    }

    void UpdateYnTap()
    {
        ynTap = false;
        touchCount = Input.touchCount;
        if (touchCountLast != touchCount)
        {
            if (touchCount > 0)
            {
                ynTap = true;
            }
        }
    }

    void UpdateMeasure()
    {
        scrCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        if (aRRaycastManager.Raycast(scrCenter, hits, TrackableType.Planes))
        {
            Pose hitPose = hits[0].pose;
            switch (sequence)
            {
                case SequenceType.first:
                    AddEditTapMeasure(hitPose);
                    if (ynTap)
                    {
                        goFirst.transform.position = hitPose.position;
                        sequence = SequenceType.second;
                    }
                    break;
                case SequenceType.second:
                    AddEditTapMeasure(hitPose);
                    goSecond.transform.position = hitPose.position;
                    if (ynTap)
                    {
                        sequence = SequenceType.first;
                    }
                    break;
            }
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
        sca.z = GetDist();
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
        if (MetersToInches(distLast) != MetersToInches(dist))
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
            goTapeMeasure = Instantiate(goSecondPrefab, hitPose.position, hitPose.rotation);
        }
        else
        {
            goTapeMeasure.transform.position = hitPose.position;
            goTapeMeasure.transform.rotation = hitPose.rotation;
        }
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
            case SequenceType.first:
                txt = "Pick the first point:";
                break;
            case SequenceType.second:
                txt = "Pick the second point:";
                break;
        }
        return txt;
    }

    void FormatDist()
    {
        string txt = "";
        float inches = dist * feetPerMeter * 12;
        if (inches > 12)
        {
            int feet = (int)(inches / 12);
            inches -= feet * 12;
            txt +=  feet.ToString("F0") + "'-" + inches.ToString("F0") + "\"";
        }
        else
        {
            txt += inches.ToString("F0") + "\"";
        }
        distFormatted = txt;
    }
}

public enum SequenceType
{
    first,
    second
}

