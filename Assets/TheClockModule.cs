using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using TheClock;
using UnityEngine;

using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of The Clock
/// Created by TheAuthorOfOZ, implemented by Timwi
/// </summary>
public class TheClockModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;

    public Mesh[] RomanNumerals;
    public Mesh[] ArabicNumerals;
    public MeshFilter Numerals;

    public Mesh[] HourLine;
    public Mesh[] HourArrow;
    public Mesh[] HourSpade;
    public Mesh[] MinuteLine;
    public Mesh[] MinuteArrow;
    public Mesh[] MinuteSpade;
    public MeshFilter HourHand;
    public MeshFilter MinuteHand;
    public GameObject AmPm;
    public MeshRenderer[] AmPmBackground;
    public MeshRenderer[] AmPmWriting;

    public MeshRenderer HourHandObj;
    public MeshRenderer MinuteHandObj;
    public MeshRenderer SecondHandObj;
    public MeshRenderer NumeralsObj;
    public MeshRenderer ClockfaceObj;
    public MeshRenderer ClockfaceBackgroundObj;
    public MeshRenderer ClockFrameObj;
    public MeshRenderer[] Knobs;

    public GameObject SecondHand;
    public Material AmPmWhiteMaterial;
    public Material AmPmBlackMaterial;
    public KMSelectable MinutesDown, MinutesUp, HoursDown, HoursUp, Submit;

    private static Color[] _colors = newArray(
        new Color(0xA5 / 255f, 0x13 / 255f, 0x13 / 255f),   // red
        new Color(0x0E / 255f, 0x8E / 255f, 0x1F / 255f),   // green
        new Color(0x2A / 255f, 0x67 / 255f, 0xCC / 255f),   // blue
        new Color(0xBF / 255f, 0x94 / 255f, 0x17 / 255f),   // gold
        new Color(0x20 / 255f, 0x20 / 255f, 0x20 / 255f)    // black
    );
    private static Color _blackColor = new Color(0x20 / 255f, 0x20 / 255f, 0x20 / 255f);

    private static Color _clockfaceGold = new Color(0xFF / 255f, 0xC8 / 255f, 0x50 / 255f);
    private static Color _clockfaceSilver = new Color(0xBC / 255f, 0xBC / 255f, 0xBC / 255f);

    const int totalMinutes = 24 * 60;

    private NumeralStyle _numeralStyle;
    private HandStyle _handStyle;
    private int _shownTime, _initialTime, _addTime;
    private bool _caseIsGold;
    private bool _handsColorMatchesNumerals;
    private int _numeralsColor;
    private bool _amPmWhiteOnBlack;
    private bool _secondsHandPresent;
    private float _originalBombTime;
    private bool _isSolved;

    // Haha, “handheld”, get it? Hahaha. Seriously, it’s the coroutine that runs while the user holds the selectable that moves a hand.
    private Coroutine _handHeld;
    // Coroutine that runs while the user holds the submit button (which does reset on long-press).
    private Coroutine _submitHeld;
    private bool _submitHeldReset;
    private bool _pauseSecondHand = false;

    private int _moduleId;
    private static int _moduleIdCounter = 1;

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        _isSolved = false;

        // Decide upon all the variables
        setHandStyle();           // minutes, category 1
        setNumeralsColor();   // minutes, category 2
        setAmPmColor();        // minutes, category 3
        setSecondsHand();      // minutes, category 4

        setNumeralStyle();     // hours, category 1
        setCaseColor();           // hours, category 2
        setHandsColor();        // hours, category 3

        _addTime =
            (((int) _numeralStyle * 4 + (_caseIsGold ? 1 : 0) * 2 + (_handsColorMatchesNumerals ? 0 : 1) + 2) % 12 + 1) * 60 +
            ((int) _handStyle * 20 + _numeralsColor * 4 + (_amPmWhiteOnBlack ? 2 : 0) + (_secondsHandPresent ? 0 : 1) + 11) % 60;

        // Start time
        _initialTime = _shownTime = Rnd.Range(0, totalMinutes);
        Debug.LogFormat("[The Clock #{0}] Initial time: {1}", _moduleId, formatTime(_initialTime));
        LogAnswer();

        MinuteHand.transform.localRotation = minuteHandRotation;
        HourHand.transform.localRotation = hourHandRotation;
        AmPm.transform.localRotation = amPmRotation;

        MinutesDown.OnInteract = btnDown(MinutesDown, -1, minutes: true);
        MinutesUp.OnInteract = btnDown(MinutesUp, 1, minutes: true);
        HoursDown.OnInteract = btnDown(HoursDown, -1, minutes: false);
        HoursUp.OnInteract = btnDown(HoursUp, 1, minutes: false);

        MinutesDown.OnInteractEnded = MinutesUp.OnInteractEnded = HoursDown.OnInteractEnded = HoursUp.OnInteractEnded = btnUp;
        Submit.OnInteract = delegate
        {
            if (_submitHeld != null)
                StopCoroutine(_submitHeld);
            _submitHeldReset = false;
            _submitHeld = StartCoroutine(holdSubmit());
            return false;
        };
        Submit.OnInteractEnded = delegate
        {
            if (_submitHeld != null)
                StopCoroutine(_submitHeld);
            _submitHeld = null;
            if (!_submitHeldReset)
                submit();
        };

        Module.OnActivate = delegate
        {
            _originalBombTime = Bomb.GetTime();
            Debug.LogFormat("[The Clock #{0}] Bomb timer: {1}", _moduleId, _originalBombTime);
        };
    }

    private IEnumerator holdSubmit()
    {
        yield return new WaitForSeconds(.5f);

        // Button was pressed for 0.5 seconds: do Reset instead of Submit.
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Submit.transform);
        _submitHeldReset = true;
        _shownTime = _initialTime;
        if (_handHeld != null)
            StopCoroutine(_handHeld);
        _handHeld = StartCoroutine(moveHands(0, false));
    }

    private void LogAnswer()
    {
        Debug.LogFormat("[The Clock #{0}] Expecting you to add/subtract {1} hours and {2} minutes, giving {3} (add) or {4} (subtract).", _moduleId, _addTime / 60, _addTime % 60, formatTime((_initialTime + _addTime) % totalMinutes), formatTime((_initialTime + totalMinutes - _addTime) % totalMinutes));
    }

    private void setHandStyle()
    {
        switch (_handStyle = (HandStyle) Rnd.Range(0, 3))
        {
            case HandStyle.Lines:
                {
                    var ix = Rnd.Range(0, HourLine.Length);
                    HourHand.mesh = HourLine[ix];
                    MinuteHand.mesh = MinuteLine[ix];
                    break;
                }
            case HandStyle.Arrows:
                {
                    var ix = Rnd.Range(0, HourArrow.Length);
                    HourHand.mesh = HourArrow[ix];
                    MinuteHand.mesh = MinuteArrow[ix];
                    break;
                }
            case HandStyle.Spades:
                {
                    var ix = Rnd.Range(0, HourSpade.Length);
                    HourHand.mesh = HourSpade[ix];
                    MinuteHand.mesh = MinuteSpade[ix];
                    break;
                }
        }
        Debug.LogFormat("[The Clock #{0}] Hand style: {1}", _moduleId, _handStyle);
    }

    private void setNumeralsColor()
    {
        _numeralsColor = Rnd.Range(0, 5);
        NumeralsObj.material.color = ClockfaceObj.material.color = _colors[_numeralsColor];
        Debug.LogFormat("[The Clock #{0}] Numerals/tickmarks color: {1}", _moduleId, new[] { "red", "green", "blue", "gold", "black" }[_numeralsColor]);
    }

    private void setAmPmColor()
    {
        _amPmWhiteOnBlack = Rnd.Range(0, 2) == 0;
        foreach (var obj in AmPmWriting)
            obj.material = _amPmWhiteOnBlack ? AmPmWhiteMaterial : AmPmBlackMaterial;
        foreach (var obj in AmPmBackground)
            obj.material = _amPmWhiteOnBlack ? AmPmBlackMaterial : AmPmWhiteMaterial;
        Debug.LogFormat("[The Clock #{0}] AM/PM color: {1}", _moduleId, _amPmWhiteOnBlack ? "white on black" : "black on white");
    }

    private void setSecondsHand()
    {
        _secondsHandPresent = Rnd.Range(0, 2) == 0;
        if (!_secondsHandPresent)
            SecondHand.SetActive(false);
        StartCoroutine(moveSecondHand());
        Debug.LogFormat("[The Clock #{0}] Seconds hand: {1}", _moduleId, _secondsHandPresent ? "present" : "absent");
    }

    private void setNumeralStyle()
    {
        switch (_numeralStyle = (NumeralStyle) Rnd.Range(0, 3))
        {
            case NumeralStyle.None:
                Destroy(Numerals.gameObject);
                break;
            case NumeralStyle.Roman:
                Numerals.mesh = RomanNumerals[Rnd.Range(0, RomanNumerals.Length)];
                break;
            case NumeralStyle.Arabic:
                Numerals.mesh = ArabicNumerals[Rnd.Range(0, ArabicNumerals.Length)];
                break;
        }
        Debug.LogFormat("[The Clock #{0}] Numeral style: {1}", _moduleId, _numeralStyle);
    }

    private void setCaseColor()
    {
        _caseIsGold = Rnd.Range(0, 2) == 0;
        ClockFrameObj.material.color = _caseIsGold ? _clockfaceGold : _clockfaceSilver;
        foreach (var knob in Knobs)
            knob.material.color = _caseIsGold ? _clockfaceGold : _clockfaceSilver;
        Debug.LogFormat("[The Clock #{0}] Casing: {1}", _moduleId, _caseIsGold ? "gold" : "silver");
    }

    private void setHandsColor()
    {
        _handsColorMatchesNumerals = Rnd.Range(0, 2) == 0;
        if (_handsColorMatchesNumerals)
            // Hour and minute hands are the same color 
            MinuteHandObj.material.color = HourHandObj.material.color = _colors[_numeralsColor];
        else
            // Use a non-black color and black to avoid clashing colors
            MinuteHandObj.material.color = HourHandObj.material.color = _numeralsColor == 4 ? _colors[Rnd.Range(0, 4)] : _blackColor;
        Debug.LogFormat("[The Clock #{0}] Hands color: {1}", _moduleId, _handsColorMatchesNumerals ? "matched" : "unmatched");
    }

    private static T[] newArray<T>(params T[] array) { return array; }

    private IEnumerator moveSecondHand()
    {
        var prevSeconds = -1;
        while (true)
        {
            var time = DateTime.Now;
            var seconds = _isSolved
                ? time.Second
                : (-(int) Mathf.Ceil(Bomb.GetTime()) % 60 + 60) % 60;

            if (seconds != prevSeconds && !_pauseSecondHand)
            {
                SecondHand.transform.localEulerAngles = new Vector3(0, (seconds - .7f) * 6, 0);
                yield return null;
                SecondHand.transform.localEulerAngles = new Vector3(0, (seconds - .3f) * 6, 0);
                yield return null;

                SecondHand.transform.localEulerAngles = new Vector3(0, (seconds + .1f) * 6, 0);
                yield return null;
                SecondHand.transform.localEulerAngles = new Vector3(0, seconds * 6, 0);
            }

            if (_isSolved)
            {
                MinuteHand.transform.localRotation = Quaternion.Slerp(MinuteHand.transform.localRotation, Quaternion.Euler(0, (time.Minute * 60 + time.Second) * .1f, 0), .3f);
                HourHand.transform.localRotation = Quaternion.Slerp(HourHand.transform.localRotation, Quaternion.Euler(0, (time.Hour * 60 + time.Minute) * .5f, 0), .3f);
                AmPm.transform.localRotation = Quaternion.Slerp(AmPm.transform.localRotation, Quaternion.Euler(0, time.Hour < 12 ? -60 : 0, 0), .3f);
            }
            prevSeconds = seconds;
            yield return null;
        }
    }

    private Quaternion minuteHandRotation { get { return Quaternion.Euler(0, (_shownTime % 60) * 6, 0); } }
    private Quaternion hourHandRotation { get { return Quaternion.Euler(0, ((_shownTime / 60) % 12) * 30 + (_shownTime % 60) * .5f, 0); } }
    private Quaternion amPmRotation { get { return Quaternion.Euler(0, -60 + (_shownTime / 60 / 12) * 60, 0); } }

    private void btnUp()
    {
        if (!_isSolved && _handHeld != null)
        {
            StopCoroutine(_handHeld);
            _handHeld = StartCoroutine(moveHands(0, false));
        }
    }

    private KMSelectable.OnInteractHandler btnDown(KMSelectable selectable, int multi, bool minutes)
    {
        return delegate
        {
            if (_isSolved)
                return false;

            selectable.AddInteractionPunch(.25f);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, selectable.transform);

            if (_handHeld != null)
                StopCoroutine(_handHeld);
            _handHeld = StartCoroutine(moveHands(multi, minutes));
            return false;
        };
    }

    private IEnumerator moveHands(int multi, bool minutes, bool secondsOnly = false)
    {
        var start = Time.fixedTime;
        var speed = 8f;

        var time = DateTime.Now;
        _shownTime = _isSolved
            ? time.Hour * 60 + time.Minute
            : ((_shownTime + multi * (minutes ? 1 : 60)) % totalMinutes + totalMinutes) % totalMinutes;

        while (true)
        {
            yield return null;

            if (secondsOnly)
                SecondHand.transform.localRotation = Quaternion.Slerp(SecondHand.transform.localRotation, Quaternion.Euler(0, DateTime.Now.Second * 6, 0), .3f);
            else
            {
                MinuteHand.transform.localRotation = Quaternion.Slerp(MinuteHand.transform.localRotation, minuteHandRotation, .3f);
                HourHand.transform.localRotation = Quaternion.Slerp(HourHand.transform.localRotation, hourHandRotation, .3f);
                AmPm.transform.localRotation = Quaternion.Slerp(AmPm.transform.localRotation, amPmRotation, .3f);
            }

            if ((Time.fixedTime - start) * 30 > speed)
            {
                if (multi == 0)
                {
                    if (!secondsOnly)
                    {
                        MinuteHand.transform.localRotation = minuteHandRotation;
                        HourHand.transform.localRotation = hourHandRotation;
                        AmPm.transform.localRotation = amPmRotation;
                    }
                    yield break;
                }

                _shownTime = ((_shownTime + multi * (minutes ? 1 : 60)) % totalMinutes + totalMinutes) % totalMinutes;

                start = Time.fixedTime;
                if (speed > (minutes ? .75f : 4))
                {
                    speed *= .75f;
                    Debug.Log("Setting speed to " + speed);
                }
            }
        }
    }

    private void submit()
    {
        if (_isSolved)
            return;

        Submit.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Submit.transform);

        var bombTime = Bomb.GetTime();
        var isFirstHalf = bombTime > _originalBombTime / 2;
        Debug.LogFormat("[The Clock #{0}] Pressed Submit when time remaining was {3}, which is {1} than half of the bomb’s original time. Expecting {2}.", _moduleId, isFirstHalf ? "more" : "less", isFirstHalf ? "add" : "subtract", bombTime);

        if (_shownTime == (_initialTime + (isFirstHalf ? _addTime : totalMinutes - _addTime)) % totalMinutes)
        {
            // Correct! :)
            Debug.LogFormat("[The Clock #{0}] Module solved.", _moduleId);
            Module.HandlePass();
            _isSolved = true;
            StartCoroutine(transitionSecondHand());
        }
        else
        {
            // Wrong, dude
            _initialTime = Rnd.Range(0, totalMinutes);
            Debug.LogFormat("[The Clock #{0}] {1} submitted. Strike. New initial time: {2}", _moduleId, formatTime(_shownTime), formatTime(_initialTime));
            LogAnswer();
            _shownTime = _initialTime;
            Module.HandleStrike();
        }

        if (_handHeld != null)
            StopCoroutine(_handHeld);
        _handHeld = StartCoroutine(moveHands(0, false, secondsOnly: _isSolved));
    }

    private IEnumerator transitionSecondHand()
    {
        _pauseSecondHand = true;
        yield return new WaitForSeconds(.5f);
        _pauseSecondHand = false;
    }

    private string formatTime(int time)
    {
        return string.Format("{0}:{1:00} {2}", (time / 60 + 11) % 12 + 1, time % 60, time / 720 == 0 ? "am" : "pm");
    }

#pragma warning disable 414
    private string TwitchHelpMessage = @"Use “!{0} hours forward 5” or “!{0} minutes backward 5” to change the time incrementally and then “!{0} submit” to submit; or use “!{0} set 12:34 pm” to set and submit it directly.";
#pragma warning restore 414

    public IEnumerator ProcessTwitchCommand(string command)
    {
        string[] split = command.ToLowerInvariant().Split(new[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
        DateTime result;

        if (split.Length == 1 && split[0] == "reset")
        {
            yield return null;

            // Hold the Submit button for 1sec to do a reset
            yield return Submit;
            yield return new WaitForSeconds(1f);
            yield return Submit;
        }
        else if (split.Length == 1 && (split[0] == "set" || split[0] == "submit"))
        {
            yield return null;
            submit();
        }
        else if (
            (split.Length == 2 && (split[0] == "set" || split[0] == "submit") && DateTime.TryParseExact(split[1], "hh:mmtt", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out result)) ||
            (split.Length == 3 && (split[0] == "set" || split[0] == "submit") && DateTime.TryParseExact(split[1] + " " + split[2], "hh:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out result)))
        {
            Debug.LogFormat(@"<The Clock #{0}> Twitch Plays ‘set’ command: {1}", _moduleId, command);

            // Convert formatted time to minutes.
            var newTime = result.Hour * 60 + result.Minute;
            Debug.LogFormat(@"<The Clock #{0}> newTime = {1}; coroutine active = {2}", _moduleId, newTime, _handHeld != null);

            if (_handHeld != null)
                StopCoroutine(_handHeld);

            yield return null;

            if (newTime > _shownTime) // We need to go backwards.
            {
                if (newTime - _shownTime >= 60)
                {
                    HoursUp.OnInteract();
                    Debug.LogFormat(@"<The Clock #{0}> Going backwards in hours...", _moduleId);
                    yield return new WaitUntil(() => newTime - _shownTime < 60);
                    Debug.LogFormat(@"<The Clock #{0}> ... done", _moduleId);
                    HoursUp.OnInteractEnded();
                    yield return new WaitForSeconds(.25f);
                }

                if (newTime - _shownTime >= 1)
                {
                    MinutesUp.OnInteract();
                    Debug.LogFormat(@"<The Clock #{0}> Going backwards in minutes...", _moduleId);
                    yield return new WaitUntil(() => newTime - _shownTime < 1);
                    Debug.LogFormat(@"<The Clock #{0}> ... done", _moduleId);
                    MinutesUp.OnInteractEnded();
                }
            }
            else if (newTime < _shownTime) // We need to go forwards.
            {
                if (_shownTime - newTime >= 60)
                {
                    HoursDown.OnInteract();
                    Debug.LogFormat(@"<The Clock #{0}> Going forwards in hours...", _moduleId);
                    yield return new WaitUntil(() => _shownTime - newTime < 60);
                    Debug.LogFormat(@"<The Clock #{0}> ... done", _moduleId);
                    HoursDown.OnInteractEnded();
                    yield return new WaitForSeconds(.25f);
                }

                if (_shownTime - newTime >= 1)
                {
                    MinutesDown.OnInteract();
                    Debug.LogFormat(@"<The Clock #{0}> Going forwards in minutes...", _moduleId);
                    yield return new WaitUntil(() => _shownTime - newTime < 1);
                    Debug.LogFormat(@"<The Clock #{0}> ... done", _moduleId);
                    MinutesDown.OnInteractEnded();
                }
            }

            yield return new WaitForSeconds(.5f);
            submit();

            Debug.LogFormat(@"<The Clock #{0}> Twitch Plays handler done", _moduleId);
        }
        else if (split.Length >= 2 && split.Length <= 3 && "hm".Contains(split[0][0]) && "fb".Contains(split[1][0]))
        {
            Debug.LogFormat(@"<The Clock #{0}> Twitch Plays relative command: {1}", _moduleId, command);
            yield return null;

            var hours = split[0][0] == 'h';
            var forward = split[1][0] == 'f';
            var btn = forward ? (hours ? HoursUp : MinutesUp) : (hours ? HoursDown : MinutesDown);

            int amount;
            if (split.Length < 3 || !int.TryParse(split[2], out amount))
                amount = 1;
            if (hours)
                amount *= 60;
            var amountHours = (amount / 60) % 24;
            var amountMinutes = amount % 60;
            var origTime = _shownTime;
            var targetTime = (origTime + totalMinutes + (amount % totalMinutes) * (forward ? 1 : -1)) % totalMinutes;

            Debug.LogFormat(@"<The Clock #{0}> Coroutine active: {1}", _moduleId, _handHeld != null);

            if (_handHeld != null)
                StopCoroutine(_handHeld);

            while (amountHours > 0)
            {
                (forward ? HoursUp : HoursDown).OnInteract();
                yield return new WaitForSeconds(.05f);
                (forward ? HoursUp : HoursDown).OnInteractEnded();
                yield return new WaitForSeconds(.1f);
                amountHours--;
            }

            if (amountMinutes > 0)
            {
                yield return new WaitForSeconds(.5f);
                btn.OnInteract();
                Debug.LogFormat(@"<The Clock #{0}> Going in minutes...", _moduleId);
                yield return new WaitUntil(() => _shownTime == targetTime);
                Debug.LogFormat(@"<The Clock #{0}> ... done", _moduleId);
                btn.OnInteractEnded();
                yield return new WaitForSeconds(.5f);
            }

            Debug.LogFormat(@"<The Clock #{0}> Twitch Plays handler done.", _moduleId);
        }
    }
}
