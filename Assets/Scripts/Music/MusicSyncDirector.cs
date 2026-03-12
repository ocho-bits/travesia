using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public sealed class MusicSyncDirector : MonoBehaviour
{
    [Serializable]
    public sealed class MusicCueEvent : UnityEvent<MusicCue>
    {
    }

    [Header("References")]
    [SerializeField] private MusicTimelineAsset timeline;
    [SerializeField] private AudioSource audioSource;

    [Header("Playback")]
    [Tooltip("Lead time before playback starts to ensure deterministic scheduling.")]
    [Min(0.01f)]
    [SerializeField] private float scheduleLeadTime = 0.1f;
    [SerializeField] private bool playOnStart = true;

    [Header("Events")]
    [SerializeField] private MusicCueEvent onCustomCue;

    private double _songStartDspTime;
    private int _nextCueIndex;
    private bool _isPlaying;

    public MusicTimelineAsset Timeline => timeline;
    public bool IsPlaying => _isPlaying;

    public double PlaybackTimeSeconds
    {
        get
        {
            if (!_isPlaying)
            {
                return 0d;
            }

            return Math.Max(0d, AudioSettings.dspTime - _songStartDspTime);
        }
    }

    public double MusicalTimeSeconds => timeline == null ? 0d : PlaybackTimeSeconds - timeline.StartOffsetSeconds;

    public double BeatTime
    {
        get
        {
            if (timeline == null)
            {
                return 0d;
            }

            return timeline.SongTimeToBeats(PlaybackTimeSeconds);
        }
    }

    private void Reset()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
        }
    }

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    private void Update()
    {
        if (!_isPlaying || timeline == null)
        {
            return;
        }

        double playbackTime = PlaybackTimeSeconds;
        DispatchDueCues(playbackTime);

        if (audioSource != null && !audioSource.isPlaying && playbackTime > 0d)
        {
            _isPlaying = false;
        }
    }

    public void Play()
    {
        if (timeline == null || timeline.Clip == null || audioSource == null)
        {
            Debug.LogWarning("MusicSyncDirector cannot play without a timeline, clip, and audio source.", this);
            return;
        }

        audioSource.Stop();
        audioSource.clip = timeline.Clip;

        double dspStartTime = AudioSettings.dspTime + scheduleLeadTime;
        audioSource.PlayScheduled(dspStartTime);

        _songStartDspTime = dspStartTime;
        _nextCueIndex = 0;
        _isPlaying = true;
    }

    public void Stop()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        _isPlaying = false;
        _nextCueIndex = 0;
    }

    public void RaiseCustomCue(MusicCue cue)
    {
        onCustomCue?.Invoke(cue);
    }

    private void DispatchDueCues(double playbackTimeSeconds)
    {
        IReadOnlyList<MusicCue> cues = timeline.Cues;
        while (_nextCueIndex < cues.Count)
        {
            MusicCue cue = cues[_nextCueIndex];
            _nextCueIndex++;

            if (cue == null)
            {
                continue;
            }

            if (cue.timeSeconds > playbackTimeSeconds)
            {
                _nextCueIndex--;
                break;
            }

            cue.Invoke(this);
        }
    }
}
