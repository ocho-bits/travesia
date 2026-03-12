using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MusicTimeline", menuName = "Travesia/Audio/Music Timeline")]
public sealed class MusicTimelineAsset : ScriptableObject
{
    [SerializeField] private AudioClip clip;
    [SerializeField] private float bpm = 120f;
    [SerializeField] private float startOffsetSeconds = 0f;
    [SerializeField] private List<MusicCue> cues = new List<MusicCue>();

    public AudioClip Clip => clip;
    public float Bpm => bpm;
    public float StartOffsetSeconds => Mathf.Max(0f, startOffsetSeconds);
    public IReadOnlyList<MusicCue> Cues => cues;
    public float SecondsPerBeat => bpm > 0.0001f ? 60f / bpm : 0f;

    public double SongTimeToMusicalTime(double playbackTimeSeconds)
    {
        return playbackTimeSeconds - StartOffsetSeconds;
    }

    public double SongTimeToBeats(double playbackTimeSeconds)
    {
        if (SecondsPerBeat <= 0f)
        {
            return 0d;
        }

        return SongTimeToMusicalTime(playbackTimeSeconds) / SecondsPerBeat;
    }

    private void OnValidate()
    {
        startOffsetSeconds = Mathf.Max(0f, startOffsetSeconds);
        bpm = Mathf.Max(1f, bpm);
        cues.Sort(CompareCues);
    }

    private static int CompareCues(MusicCue a, MusicCue b)
    {
        if (ReferenceEquals(a, b))
        {
            return 0;
        }

        if (a == null)
        {
            return 1;
        }

        if (b == null)
        {
            return -1;
        }

        return a.timeSeconds.CompareTo(b.timeSeconds);
    }
}

[Serializable]
public sealed class MusicCue
{
    [Min(0f)]
    public float timeSeconds;

    public MusicCueActionType actionType;
    public string eventId;

    [Header("Camera Zoom")]
    public OrthoZoomDirector2D zoomDirector;
    public float zoomTarget = 6f;

    [Min(0f)]
    public float zoomDuration = 0.35f;

    public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public bool zoomSnap;

    [Header("Player Overlay")]
    public SimplePlayer2D player;
    public int overlayId;

    [Min(0f)]
    public float overlayDuration = 0.5f;

    public void Invoke(MusicSyncDirector director)
    {
        switch (actionType)
        {
            case MusicCueActionType.CameraZoom:
                zoomDirector?.SetZoom(zoomTarget, zoomDuration, zoomCurve, zoomSnap);
                break;
            case MusicCueActionType.PlayerOverlay:
                player?.PlayOverlayForSeconds(overlayId, overlayDuration);
                break;
            case MusicCueActionType.CustomEvent:
                director.RaiseCustomCue(this);
                break;
        }
    }
}

public enum MusicCueActionType
{
    CameraZoom,
    PlayerOverlay,
    CustomEvent
}
