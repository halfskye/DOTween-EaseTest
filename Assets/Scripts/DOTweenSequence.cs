using System.Collections;
using DG.Tweening;
using UnityEngine;

public class DOTweenSequence : MonoBehaviour
{
    [Header("Tween")]
    [SerializeField] private float _moveDuration = 1f;

    private enum EaseStrategy
    {
        EaseType = 0,
        AnimationCurve = 1,
        CustomFunction = 2,
    }

    [Header("Ease")]
    [SerializeField] private EaseStrategy _easeStrategy = EaseStrategy.EaseType;

    [SerializeField] private Ease _easeType = Ease.Linear;

    [Tooltip("Only used for Elastic Ease types")]
    [SerializeField] private float _elasticEaseAmplitude = 1.70158f;

    [Tooltip("Only used for Elastic Ease types")]
    [SerializeField] private float _elasticEasePeriod = 0f;

    [SerializeField] private AnimationCurve _easeAnimationCurve = null;

    private IEnumerator _testCoroutine = null; 

    private enum EaseCustomFunction
    {
        OutElastic = 0,
        InOutElastic = 1,
    }

    [SerializeField]
    private EaseCustomFunction _easeCustomFunction = EaseCustomFunction.OutElastic;
    
    [Header("DOTween Setup")]
    [SerializeField] private Transform _start = null;
    [SerializeField] private Transform _end = null;
    [SerializeField] private Transform _actor = null;

    [Header("Test Setup")]
    [SerializeField] private Transform _testStart = null;
    [SerializeField] private Transform _testEnd = null;
    [SerializeField] private Transform _testActor = null;

    [Header("Debug Log")]
    [SerializeField] private bool _enableDOTweenSequenceLog = true;
    [SerializeField] private string _debugLabelDOTween = "DOTween";
    [SerializeField] private bool _enableTestSequenceLog = true;
    [SerializeField] private string _debugLabelTest = "Test";

    private Sequence _sequence = null;

    private void Update()
    {
        UpdateInput();

        DebugLog();
    }

    private void UpdateInput()
    {
        if (Input.anyKeyDown)
        {
            RunSequence();
        }
    }

    private void RunSequence()
    {
        RunDOTweenSequence();
        
        RunTestSequence();
    }

    #region DOTween Sequence
    
    private void SetupSequence()
    {
        _actor.position = _start.position;

        if (_sequence != null)
        {
            StopSequence();
        }

        _sequence = DOTween.Sequence();
        SetSequenceEaseType();

        _sequence.OnComplete(StopSequence);
    }

    private void RunDOTweenSequence()
    {
        SetupSequence();
        
        _sequence.Prepend(_actor.transform
            .DOMove(_end.position, _moveDuration)
            .From(_start.position)
        );

        _sequence.Play();
    }

    private void StopSequence()
    {
        _sequence.Kill();
        _sequence = null;
    }

    private void SetSequenceEaseType()
    {
        switch (_easeStrategy)
        {
            case EaseStrategy.EaseType:
                switch (_easeType)
                {
                    case Ease.InElastic:
                    case Ease.OutElastic:
                    case Ease.InOutElastic:
                        _sequence.SetEase(_easeType, _elasticEaseAmplitude, _elasticEasePeriod);
                        break;
                    default:
                        _sequence.SetEase(_easeType);
                        break;
                }

                break;
            case EaseStrategy.AnimationCurve:
                _sequence.SetEase(_easeAnimationCurve);
                break;
            case EaseStrategy.CustomFunction:
                _sequence.SetEase(GetCustomEaseFunction());
                break;
        }
    }
    
    #endregion DOTween Sequence

    #region Test Sequence
    
    private void RunTestSequence()
    {
        _testActor.position = _testStart.position;
        
        if (_testCoroutine != null)
        {
            StopCoroutine(_testCoroutine);
            _testCoroutine = null;
        }
        _testCoroutine = RunTestEaseStrategy();
        StartCoroutine(_testCoroutine);
    }
    
    private IEnumerator RunTestEaseStrategy()
    {
        var startToEnd = _testEnd.position - _testStart.position;
        
        var time = 0f;
        var timeAdjusted = 0f;
        var sample = 0f;
        while (time < 1f ||
               timeAdjusted < 1f)
        {
            time += Time.deltaTime;
            timeAdjusted += (Time.deltaTime / _moveDuration);

            sample = _easeStrategy switch
            {
                EaseStrategy.AnimationCurve => _easeAnimationCurve.Evaluate(timeAdjusted),
                EaseStrategy.CustomFunction => GetCustomEaseFunction()(time, _moveDuration, 0f, 0f),
                _ => sample
            };

            DebugLogSample(_debugLabelTest, sample);

            _testActor.position = _testStart.position + (startToEnd * sample);

            yield return null;
        }
    }
    
    #endregion Test Sequence

    #region Custom Ease Functions

    private EaseFunction GetCustomEaseFunction()
    {
        switch (_easeCustomFunction)
        {
            case EaseCustomFunction.OutElastic:
                return EaseOutElastic;
            case EaseCustomFunction.InOutElastic:
                return EaseInOutElastic;
        }

        return null;
    }

    // Reference: https://easings.net/#easeOutElastic
    private float EaseOutElastic(float time,
        float duration,
        float overshootOrAmplitude,
        float period)
    {
        time /= duration;
        const float c4 = (2f * Mathf.PI) / 3f;

        return Mathf.Approximately(time, 0f)
            ? 0f
            : Mathf.Approximately(time, 1f)
                ? 1f
                : Mathf.Pow(2f, -10f * time) * Mathf.Sin((time * 10f - 0.75f) * c4) + 1f;
    }

    // Reference: https://easings.net/#easeInOutElastic
    private float EaseInOutElastic(float time,
        float duration,
        float overshootOrAmplitude,
        float period)
    {
        time /= duration;
        const float c5 = (2f * Mathf.PI) / 4.5f;

        return Mathf.Approximately(time, 0f)
            ? 0f
            : Mathf.Approximately(time, 1f)
                ? 1f
                : time < 0.5
                    ? -(Mathf.Pow(2f, 20f * time - 10f) * Mathf.Sin((20f * time - 11.125f) * c5)) / 2f
                    : (Mathf.Pow(2f, -20f * time + 10f) * Mathf.Sin((20f * time - 11.125f) * c5)) / 2f + 1f;
    }

    #endregion Custom Ease Functions

    #region Debug

    private void DebugLog()
    {
        if (_sequence == null || !_sequence.IsPlaying()) return;
        
        var startToEnd = (_end.position - _start.position).magnitude;
        var startToActor = (_actor.position - _start.position).magnitude;
        var sample = startToActor / startToEnd;

        DebugLogSample(_debugLabelDOTween, sample);
    }

    private void DebugLogSample(string label, float sample)
    {
        if(label.Equals(_debugLabelDOTween) && !_enableDOTweenSequenceLog) return;
        if(label.Equals(_debugLabelTest) && !_enableTestSequenceLog) return;
        
        if (sample < 0f ||
            sample > 1f)
        {
            Debug.LogError($"{label} Sample: {sample}");
        }
        else
        {
            Debug.Log($"{label} Sample: {sample}");
        }
    }

    #endregion Debug
}
