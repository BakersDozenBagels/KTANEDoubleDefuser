using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using DoubleDefuser;
using UnityEngine.Networking;
using KModkit;
using System.Collections.Generic;
#if !UNITY_EDITOR
using System.Reflection;
using System.Text.RegularExpressions;
#endif

public class DoubleDefuserScript : MonoBehaviour
{
    [SerializeField]
    private GameObject _buttons;
    [SerializeField]
    private KMAudio _audio;
    [SerializeField]
    private AudioClip[] _clips;
    [SerializeField]
    private KMBombModule _module;
    [SerializeField]
    private KMBombInfo _info;
    [SerializeField]
    private KMGameInfo _game;
    [SerializeField]
    private TextMesh _text;
    [SerializeField]
    private KMSelectable _button, _reset;
    [SerializeField]
    private GameObject _template;
    [SerializeField]
    private TextAsset _namesToTextFile;

    private Dictionary<string, string> _namesToText = new Dictionary<string, string>();

    private static int _idc, _requestCounter;
    private static bool _mainMod, _alarmStop;
    private static object prevClock = null;
    private int _state, _id = ++_idc, _thirdPress, _fourthPress, _digitIx, _releasedOn;
    private int[] _toReleaseOn;
    private string _deviceHashcode;

    private KMAudio.KMAudioRef _audioRef = null;
    private Direction _firstPress;
    private Color _secondPress;
    private Key _fifthPress;
    private Action _destroyThis, _pressHandler;
    private bool _alarmOn;
    private KMSelectable _chosenHighlight;
    private GameObject _chosenHighlightObj;
    private Action _chosenHighlightOnHighlight, _chosenHighlightOnHighlightEnded;

    private const string URL = "https://ktane.timwi.de/double-defuser";

#if UNITY_EDITOR
    private bool _held = false;
#endif

    private void Start()
    {
        _pressHandler = () =>
        {
            if(!_alarmOn)
            {
                StartCoroutine(AlarmOn());
                return;
            }
            _alarmOn = false;
            StartCoroutine(RealAlarmPress());
        };

        _namesToText = _namesToTextFile.text.Split('\n').Select(s => s.Split(':')).ToDictionary(a => a[0], a => a[1]);

#if UNITY_EDITOR
        FindObjectOfType<DummyScript>().onPress += () => { StartCoroutine(RealAlarmPress()); };
#else
        try
        {
            Regex rx = new Regex("alarm", RegexOptions.IgnoreCase);
            IEnumerable<Component[]> components = FindObjectsOfType<GameObject>().Where(x => rx.IsMatch(x.gameObject.name)).Select(x => x.GetComponents<Component>());

            List<Component> Alarms = new List<Component>();
            foreach(Component[] y in components)
            {
                foreach(Component w in y)
                {
                    if(w.GetType().Name == "Selectable") Alarms.Add(w);
                }
            }

            System.Object alarm = Alarms[0];

            System.Object[] alarmButtons = (System.Object[])alarm.GetType().GetField("Children").GetValue(alarm);
            object AlarmButton = alarmButtons[0];

            Assembly assem = AlarmButton.GetType().Assembly;
            Type tSel = assem.GetType(AlarmButton.GetType().Name);
            FieldInfo fldOnInt = tSel.GetField("OnInteract");
            Type tOIH = fldOnInt.FieldType;
            MethodInfo miHandler = typeof(DoubleDefuserScript).GetMethod("HandleAlarmPress", BindingFlags.NonPublic | BindingFlags.Static);
            Delegate d = Delegate.CreateDelegate(tOIH, this, miHandler);
            Regex rxAdd = new Regex("combineimpl", RegexOptions.IgnoreCase);
            MethodInfo addHandler = tOIH.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static).Where(x => rxAdd.IsMatch(x.Name)).First();
            object[] addHandlerArgs = new System.Object[] { d };
            if(_mainMod)
            {
                System.Object clock = fldOnInt.GetValue(AlarmButton);
                fldOnInt.SetValue(AlarmButton, prevClock);
                prevClock = clock;
            }
            if(fldOnInt.GetValue(AlarmButton) == null)
                fldOnInt.SetValue(AlarmButton, d);
            else
            {
                System.Object obj = addHandler.Invoke(fldOnInt.GetValue(AlarmButton), addHandlerArgs);
                fldOnInt.SetValue(AlarmButton, obj);
            }
            _info.OnBombExploded += delegate ()
            {
                try
                {
                    Regex rxDel = new Regex("removeimpl", RegexOptions.IgnoreCase);
                    MethodInfo delHandler = tOIH.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static).Where(x => rxDel.IsMatch(x.Name)).First();
                    System.Object obj = delHandler.Invoke(fldOnInt.GetValue(AlarmButton), addHandlerArgs);
                    fldOnInt.SetValue(AlarmButton, obj);
                }
                catch
                {
                    Debug.LogFormat("[Double Defuser #{0}] Very, very bad error. This module will likely stop working until you restart your game. Please report this ASAP.", _id);
                }
            };
            _info.OnBombSolved += delegate ()
            {
                try
                {
                    Regex rxDel2 = new Regex("removeimpl", RegexOptions.IgnoreCase);
                    MethodInfo del2Handler = tOIH.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static).Where(x => rxDel2.IsMatch(x.Name)).First();
                    System.Object obj = del2Handler.Invoke(fldOnInt.GetValue(AlarmButton), addHandlerArgs);
                    fldOnInt.SetValue(AlarmButton, obj);
                }
                catch
                {
                    Debug.LogFormat("[Double Defuser #{0}] Very, very bad error. This module will likely stop working until you restart your game. Please report this ASAP.", _id);
                }
            };
            _game.OnStateChange += delegate (KMGameInfo.State state)
            {
                try
                {
                    Regex rxDel2 = new Regex("removeimpl", RegexOptions.IgnoreCase);
                    MethodInfo del2Handler = tOIH.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static).Where(x => rxDel2.IsMatch(x.Name)).First();
                    System.Object obj = del2Handler.Invoke(fldOnInt.GetValue(AlarmButton), addHandlerArgs);
                    fldOnInt.SetValue(AlarmButton, obj);
                }
                catch
                {
                    Debug.LogFormat("[Double Defuser #{0}] Very, very bad error. This module will likely stop working until you restart your game. Please report this ASAP.", _id);
                }
            };
            _destroyThis += delegate ()
            {
                try
                {
                    Regex rxDel2 = new Regex("removeimpl", RegexOptions.IgnoreCase);
                    MethodInfo del2Handler = tOIH.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static).Where(x => rxDel2.IsMatch(x.Name)).First();
                    System.Object obj = del2Handler.Invoke(fldOnInt.GetValue(AlarmButton), addHandlerArgs);
                    fldOnInt.SetValue(AlarmButton, obj);
                }
                catch
                {
                    Debug.LogFormat("[Double Defuser #{0}] Very, very bad error. This module will likely stop working until you restart your game. Please report this ASAP.", _id);
                }
            };
        }
        catch (Exception e)
        {
            Debug.LogFormat("[Double Defuser #{0}] Failed hooking alarm. Press the button to solve.", _id);
            Debug.LogFormat("<Double Defuser #{0}> Exception: {1}", _id, e);

            GetComponent<KMSelectable>().Children[0].OnInteract += () => { _module.HandlePass(); return false; };
            return;
        }

        _game.OnAlarmClockChange += s =>
        {
            if(s)
                HandleAlarmChange();
        };
#endif

#if UNITY_EDITOR
        Debug.LogFormat("[Double Defuser #{0}] Editor mode active! Tap the button to toggle holding it.", _id);
        _button.OnInteract += () =>
        {
            if(_held)
                StartCoroutine(HandleRelease());
            else
            {
                StopAllCoroutines();
                StartCoroutine(HandlePress());
            }
            _held ^= true;
            return false;
        };
#else
        Debug.LogFormat("[Double Defuser #{0}] Push the button to begin.", _id);
        _button.OnInteract += () => { StopAllCoroutines(); StartCoroutine(HandlePress()); return false; };
        _button.OnInteractEnded += () => { StartCoroutine(HandleRelease()); };
#endif

        _reset.OnInteract += () => { StartCoroutine(ResetModule()); return false; };
    }

    private IEnumerator ResetModule()
    {
        StopAllCoroutines();

        if(_audioRef != null)
            _audioRef.StopSound();
        if(_chosenHighlightOnHighlight != null)
            _chosenHighlight.OnHighlight -= _chosenHighlightOnHighlight;
        if(_chosenHighlightOnHighlightEnded != null)
            _chosenHighlight.OnHighlightEnded -= _chosenHighlightOnHighlightEnded;
        _chosenHighlight = null;

        _state = 0;

        StartCoroutine(HideButtons());
        yield break;
    }

    private IEnumerator HandleRelease()
    {
        Debug.LogFormat("[Double Defuser #{0}] You have released the button.", _id);
        Debug.LogFormat("<Double Defuser #{0}> State: {1}", _id, _state);
        if(_state == 10)
        {
            if(!_alarmStop)
            {
                _alarmStop = true;
                StopAllCoroutines();
                StartCoroutine(HandleRelease());
                yield break;
            }
            _alarmStop = false;
            if(!_toReleaseOn.Contains((int)_info.GetTime() % 10))
            {
                Debug.LogFormat("[Double Defuser #{0}] You released at an incorrect time. Strike!", _id);
                _module.HandleStrike();
                StartCoroutine(ResetModule());
                yield break;
            }
            _releasedOn = (int)_info.GetTime() % 10;
            yield return new WaitForSeconds(1.5f);
            Debug.LogFormat("[Double Defuser #{0}] You waited to long to rehold the button. Strike!", _id);
            _module.HandleStrike();
            StartCoroutine(ResetModule());
            yield break;
        }
        if(_state == 11)
        {
            if(!_alarmStop)
            {
                _alarmStop = true;
                StopAllCoroutines();
                StartCoroutine(HandleRelease());
                yield break;
            }
            _alarmStop = false;
            bool success = true;
            foreach(object e in GetRequest(r =>
            {
                InstanceJSON[] json = GetJson(r);
                InstanceJSON ijson = json.First(j => j.DeviceHashcode == _deviceHashcode);
                if(ijson.LeverDown == null)
                {
                    Debug.LogFormat("[Double Defuser #{0}] You released while your expert wasn't holding. Strike!", _id);
                    _module.HandleStrike();
                    StartCoroutine(ResetModule());
                    success = false;
                    return;
                }
                if(ijson.KeyPress.AsKey() != _fifthPress)
                {
                    Debug.LogFormat("[Double Defuser #{0}] Your expert pressed the wrong button. Strike!", _id);
                    _module.HandleStrike();
                    StartCoroutine(ResetModule());
                    success = false;
                    return;
                }
            }))
                yield return e;
            if(!success)
                yield break;
            _state++;
            foreach(object e in HandleRemoteLeverDown())
                yield return e;

            string challengeText = null;

            while(challengeText == null)
            {
                foreach(object e in GetRequest(s =>
                {
                    InstanceJSON[] json = GetJson(s);
                    challengeText = json.First(j => j.DeviceHashcode == _deviceHashcode).ChallengeText;
                }, 5f))
                    yield return e;
            }

            foreach(object e in HandleRemoteLeverUp(challengeText.ToUpperInvariant() == _text.text))
                yield return e;
        }
    }

    private void HandleAlarmChange()
    {
        if(!_alarmOn)
        {
            StartCoroutine(AlarmOn());
            return;
        }
        _alarmOn = false;
        StartCoroutine(RealAlarmPress());
    }

    private static bool HandleAlarmPress(DoubleDefuserScript module)
    {
        module._pressHandler();
        return false;
    }

    private IEnumerator RealAlarmPress()
    {
        Debug.LogFormat("[Double Defuser #{0}] You have turned the alarm clock on.", _id);
        Debug.LogFormat("<Double Defuser #{0}> State: {1}", _id, _state);
        if(_state == 3)
        {
            if(!_alarmStop)
            {
                _alarmStop = true;
                StopAllCoroutines();
                StartCoroutine(RealAlarmPress());
                yield break;
            }
            _alarmStop = false;
            _state += 2;
            yield return PlaySound("i6");
            _secondPress = (Color)UnityEngine.Random.Range(0, 8);
            Debug.LogFormat("<Double Defuser #{0}> _secondPress: {1}", _id, _secondPress);

            _chosenHighlightObj = Instantiate(_template, _chosenHighlight.transform);
            _chosenHighlightObj.transform.localPosition = Vector3.zero;
            _chosenHighlightObj.GetComponent<Renderer>().material.color = _secondPress.AsRGBColor();
            _chosenHighlightOnHighlight = () => { if(_chosenHighlightObj != null) _chosenHighlightObj.SetActive(true); };
            _chosenHighlight.OnHighlight += _chosenHighlightOnHighlight;
            _chosenHighlightOnHighlightEnded = () => { if(_chosenHighlightObj != null) _chosenHighlightObj.SetActive(false); };
            _chosenHighlight.OnHighlightEnded += _chosenHighlightOnHighlightEnded;

            if((int)(_secondPress) < 4)
                yield return PlaySound("i7b");
            else
                yield return PlaySound("i7a");
            yield return PlaySound("i8");
            yield break;
        }
        if(_state == 5)
        {
            if(!_alarmStop)
            {
                _alarmStop = true;
                StopAllCoroutines();
                StartCoroutine(RealAlarmPress());
                yield break;
            }
            _alarmStop = false;
            Destroy(_chosenHighlightObj);
            _chosenHighlightObj = null;
            _state += 2;
            yield return PlaySound("i9");
            _thirdPress = UnityEngine.Random.Range(0, 3);
            _fourthPress = UnityEngine.Random.Range(0, 3);
            switch(_thirdPress)
            {
                case 0:
                    yield return PlaySound("i10a");
                    break;
                case 1:
                    yield return PlaySound("i10b");
                    break;
                case 2:
                    yield return PlaySound("i10c");
                    break;
            }
            yield return PlaySound("i11");
            switch(_fourthPress)
            {
                case 0:
                    yield return PlaySound("i12a");
                    break;
                case 1:
                    yield return PlaySound("i12b");
                    break;
                case 2:
                    yield return PlaySound("i12c");
                    break;
            }
            yield return PlaySound("i13");
            yield break;
        }
        if(_state == 7)
        {
            if(!_alarmStop)
            {
                _alarmStop = true;
                StopAllCoroutines();
                StartCoroutine(RealAlarmPress());
                yield break;
            }
            _alarmStop = false;
            _state += 2;
            _digitIx = UnityEngine.Random.Range(0, 4);
            foreach(object e in GetRequest(r =>
            {
                InstanceJSON[] json = GetJson(r);
                int[] nums = json.Where(j =>
                {
                    return _firstPress == j.PressOne.AsDirection() &&
                    (_secondPress == j.PressTwoPos.AsColor(true) || _secondPress == j.PressTwoLabel.AsColor(false)) &&
                    QueryEdgework(j.EnteredText);
                }).Select(j =>
                {
                    int i;
                    if(int.TryParse(j.Numbers[_digitIx].ToString(), out i))
                        return i;
                    return -1;
                }).Where(i => i != -1).ToArray();
                if(nums.Length != 0)
                {
                    _toReleaseOn = nums;
                    return;
                }
                _toReleaseOn = new int[] { -1 };
                Debug.LogFormat("<Double Defuser #{0}> Valid release times are: {1}", _id, _toReleaseOn.Join(", "));
            }))
                yield return e;
            yield return PlaySound("i14");
            yield break;
        }
    }

    private bool QueryEdgework(string q)
    {
        string compareTo = "";
        switch(_thirdPress)
        {
            case 0:
                compareTo += _info.GetSerialNumber();
                break;
            case 1:
                compareTo += _info.GetBatteryCount();
                break;
            case 2:
                compareTo += _info.GetPortCount();
                break;
        }
        switch(_fourthPress)
        {
            case 0:
                compareTo += _info.GetModuleNames().Count;
                break;
            case 1:
                compareTo += _info.GetIndicators().Count();
                break;
            case 2:
                compareTo += _info.GetBatteryHolderCount();
                break;
        }
        return q.ToUpperInvariant() == compareTo.ToUpperInvariant();
    }

    private IEnumerator AlarmOn()
    {
        _alarmOn = true;
        yield return null;
        _alarmOn = false;
    }

    private void OnDestroy()
    {
        if(_destroyThis != null)
            _destroyThis();
    }

    private IEnumerator HandlePress()
    {
        Debug.LogFormat("[Double Defuser #{0}] You have pressed the button.", _id);
        Debug.LogFormat("<Double Defuser #{0}> State: {1}", _id, _state);

        _button.AddInteractionPunch(0.1f);
        _audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, _button.transform);

        if(_state == 0)
        {
            _state++;
            StartCoroutine(RevealButtons());
            yield return PlaySound("i1");
            IEnumerable<KMBombModule> hls = transform.root.GetComponentsInChildren<KMBombModule>().Where(m => m != _module);
            if(hls.Count() != 0)
                _chosenHighlight = hls.ToList().Shuffle().First().GetComponent<KMSelectable>();
            if(_chosenHighlight == null)
            {
                yield return PlaySound("i2e");
                _chosenHighlight = GetComponent<KMSelectable>();
            }
            else
            {
                bool isSelected = false;
                _chosenHighlight.OnHighlight += () => { isSelected = true; };
                _chosenHighlight.OnSelect += () => { isSelected = true; };
                _chosenHighlight.OnHighlightEnded += () => { isSelected = false; };
                _chosenHighlight.OnDeselect += () => { isSelected = false; };
                yield return PlaySound("i2f");
                while(!isSelected)
                {
                    yield return PlaySound(UnityEngine.Random.Range(0, 2) == 1 ? "i2f1" : "i2f2");
                }
                yield return PlaySound("i2f3");
            }
            _state += 2;
            yield return PlaySound("i3");
            _firstPress = (Direction)UnityEngine.Random.Range(0, 4);
            switch(_firstPress)
            {
                case Direction.TR:
                    yield return PlaySound("i4a");
                    break;
                case Direction.BR:
                    yield return PlaySound("i4b");
                    break;
                case Direction.TL:
                    yield return PlaySound("i4c");
                    break;
                case Direction.BL:
                    yield return PlaySound("i4d");
                    break;
            }
            yield return PlaySound("i5");
            yield break;
        }
        if(_state == 9)
        {
            _state++;
            switch(_digitIx)
            {
                case 0:
                    yield return PlaySound("i15a");
                    break;
                case 1:
                    yield return PlaySound("i15b");
                    break;
                case 2:
                    yield return PlaySound("i15c");
                    break;
                case 3:
                    yield return PlaySound("i15d");
                    break;
            }
            yield return PlaySound("i16");
            yield break;
        }
        if(_state == 10)
        {
            _state++;

            foreach(object e in GetRequest(s =>
            {
                InstanceJSON[] json = GetJson(s);
                _deviceHashcode = json.First(j => j.PressOne.AsDirection() == _firstPress &&
                (j.PressTwoLabel.AsColor(false) == _secondPress || j.PressTwoPos.AsColor(true) == _secondPress) &&
                QueryEdgework(j.EnteredText) &&
                j.Numbers[_digitIx] == _releasedOn.ToString()[0]).DeviceHashcode;
                Debug.LogFormat("<Double Defuser #{0}> Linked with hash code \"{1}\".", _id, _deviceHashcode);
            }))
                yield return e;
            yield return PlaySound("i17");
            _fifthPress = (Key)UnityEngine.Random.Range(0, 4);
            switch(_fifthPress)
            {
                case Key.F:
                    yield return PlaySound("i18b");
                    break;
                case Key.J:
                    yield return PlaySound("i18a");
                    break;
                case Key.L:
                    yield return PlaySound("i18d");
                    break;
                case Key.S:
                    yield return PlaySound("i18c");
                    break;
            }
            yield return PlaySound("i19");
            yield break;
        }
        if(_state >= 14)
            yield break;

        Debug.LogFormat("[Double Defuser #{0}] You weren't supposed to press the button now. Strike!", _id);
        _module.HandleStrike();
        StartCoroutine(ResetModule());
    }

    private IEnumerable GetRequest(Action<string> callback, float minTimeout = 0f)
    {
        retry:
        float time = Time.time;
        Debug.LogFormat("<Double Defuser #{0}> Sending GET request {1}...", _id, ++_requestCounter);
        int j = _requestCounter;

        UnityWebRequest req = UnityWebRequest.Get(URL);

        req.SendWebRequest();

        yield return new WaitUntil(() => req.downloadHandler.isDone);

        if(req.responseCode != 200)
        {
            Debug.LogFormat("<Double Defuser #{0}> GET request {1} failed with response {2}. Retrying.", _id, j, req.responseCode);
            yield return new WaitUntil(() => Time.time - time >= minTimeout);
            goto retry;
        }

        callback(req.downloadHandler.text);

        Debug.LogFormat("<Double Defuser #{0}> GET request {1} done!", _id, j);

        yield return new WaitUntil(() => Time.time - time >= minTimeout);
    }

    private IEnumerable HandleRemoteLeverDown()
    {
        if(_state == 12)
        {
            _state++;
            string text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray().ToList().Shuffle().Take(4).Join("");
            _text.text = text;
            yield return PlaySound("i20");
            yield break;
        }
    }

    private IEnumerable HandleRemoteLeverUp(bool passedChallenge)
    {
        if(_state == 13)
        {
            _state++;
            if(passedChallenge)
            {
                Debug.LogFormat("[Double Defuser #{0}] Your expert successfully released their lever.", _id);
                Debug.LogFormat("[Double Defuser #{0}] Module solved.", _id);
                _module.HandlePass();
                StartCoroutine(HideButtons());
            }
            else
            {
                Debug.LogFormat("[Double Defuser #{0}] Your expert unsuccessfully released their lever. Strike!", _id);
                _module.HandleStrike();
                StartCoroutine(ResetModule());
            }
            yield return PlaySound("i21");

            yield break;
        }
    }

    private IEnumerator HideButtons()
    {
        float time = Time.time;
        while(Time.time - time < .5f)
        {
            _buttons.transform.localPosition = new Vector3(0f, Mathf.Lerp(0.008f, 0f, (Time.time - time) / .5f), 0f);
            yield return null;
        }
        _buttons.transform.localPosition = new Vector3(0f, 0f, 0f);
    }

    private Coroutine PlaySound(string clipId)
    {
        return StartCoroutine(PlaySoundCoroutine(clipId));
    }

    private IEnumerator PlaySoundCoroutine(string clipId)
    {
        Debug.LogFormat("<Double Defuser #{0}> Playing sound \"{1}\".", _id, clipId);
        Debug.LogFormat("[Double Defuser #{0}] Playing sound \"{1}\".", _id, _namesToText[clipId]);
        if(_audioRef != null && _audioRef.StopSound != null)
            _audioRef.StopSound();
        _audioRef = _audio.PlaySoundAtTransformWithRef(clipId, transform);
        AudioClip clip = _clips.Where(c => c.name == clipId).FirstOrDefault();
        if(clip == null)
            yield return new WaitForSeconds(2.5f);
        else
            yield return new WaitForSeconds(clip.length);
        _audioRef.StopSound();
        yield return null;
        yield break;
    }

    private IEnumerator RevealButtons()
    {
        float time = Time.time;
        while(Time.time - time < .5f)
        {
            _buttons.transform.localPosition = new Vector3(0f, Mathf.Lerp(0f, 0.008f, (Time.time - time) / .5f), 0f);
            yield return null;
        }
        _buttons.transform.localPosition = new Vector3(0f, 0.008f, 0f);
    }

    private InstanceJSON[] GetJson(string s)
    {
        Debug.LogFormat("<Double Defuser #{0}> Deserilizing JSON: {1}", _id, s);
        InstanceJSON[] json = Newtonsoft.Json.JsonConvert.DeserializeObject<InstanceJSON[]>(s);
        return json;
    }

    internal enum Direction
    {
        TL,
        TR,
        BL,
        BR,
        None
    }

    internal enum Color
    {
        LB,
        LR,
        LG,
        LY,
        CR,
        CG,
        CB,
        CY,
        None
    }

    internal enum Key
    {
        J,
        F,
        S,
        L,
        None
    }
}