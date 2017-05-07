using System;
using System.Collections;
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

    public MeshRenderer HourHandObj;
    public MeshRenderer MinuteHandObj;
    public MeshRenderer SecondHandObj;
    public MeshRenderer NumeralsObj;
    public MeshRenderer ClockfaceObj;
    public MeshRenderer ClockfaceBackgroundObj;
    public MeshRenderer ClockFrameObj;

    public GameObject SecondHand;

    private static Color[] _colors = newArray(
        new Color(0x2A / 255f, 0x67 / 255f, 0xCC / 255f),   // blue
        new Color(0xA5 / 255f, 0x13 / 255f, 0x13 / 255f),   // red
        new Color(0x0E / 255f, 0x8E / 255f, 0x1F / 255f),   // green
        new Color(0xBF / 255f, 0x94 / 255f, 0x17 / 255f),   // brown
        new Color(0xC1 / 255f, 0x1E / 255f, 0xDB / 255f),   // purple
        new Color(0x20 / 255f, 0x20 / 255f, 0x20 / 255f)    // black
    );
    private static Color _blackColor = new Color(0x20 / 255f, 0x20 / 255f, 0x20 / 255f);

    private static Color _clockfaceGold = new Color(0xFF / 255f, 0xC8 / 255f, 0x50 / 255f);
    private static Color _clockfaceSilver = new Color(0xBC / 255f, 0xBC / 255f, 0xBC / 255f);

    private NumeralStyle _numeralStyle;
    private HandStyle _handStyle;
    private int _startTime;
    private bool _caseIsGold;
    private bool _handsColorMatchesNumerals;
    private int _numeralsColor;
    private bool _amPmWhiteOnBlack;
    private bool _secondsHandPresent;

    void Start()
    {
        // Decide upon all the variables
        setHandStyle();           // minutes, category 1
        setNumeralsColor();   // minutes, category 2
        setAmPmColor();        // minutes, category 3
        setSecondsHand();      // minutes, category 4

        setNumeralStyle();     // hours, category 1
        setCaseColor();           // hours, category 2
        setHandsColor();        // hours, category 3

        // Start time
        _startTime = Rnd.Range(0, 60 * 24);
        var hour = _startTime / 60;
        var minute = _startTime % 60;
        MinuteHand.transform.localEulerAngles = new Vector3(0, minute * 6, 0);
        HourHand.transform.localEulerAngles = new Vector3(0, hour * 30 + minute * .5f, 0);
    }

    private void setHandStyle()
    {
        switch (_handStyle = (HandStyle) Rnd.Range(0, 3))
        {
            case HandStyle.Line:
                {
                    var ix = Rnd.Range(0, HourLine.Length);
                    HourHand.mesh = HourLine[ix];
                    MinuteHand.mesh = MinuteLine[ix];
                    break;
                }
            case HandStyle.Arrow:
                {
                    var ix = Rnd.Range(0, HourArrow.Length);
                    HourHand.mesh = HourArrow[ix];
                    MinuteHand.mesh = MinuteArrow[ix];
                    break;
                }
            case HandStyle.Spade:
                {
                    var ix = Rnd.Range(0, HourSpade.Length);
                    HourHand.mesh = HourSpade[ix];
                    MinuteHand.mesh = MinuteSpade[ix];
                    break;
                }
        }
    }

    private void setNumeralsColor()
    {
        _numeralsColor = Rnd.Range(0, 6);
        NumeralsObj.material.color = ClockfaceObj.material.color = _colors[_numeralsColor];
    }

    private void setAmPmColor()
    {
        _amPmWhiteOnBlack = Rnd.Range(0, 2) == 0;
        // TODO
    }

    private void setSecondsHand()
    {
        _secondsHandPresent = Rnd.Range(0, 2) == 0;
        if (_secondsHandPresent)
            StartCoroutine(moveSecondHand());
        else
            Destroy(SecondHand);
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
    }

    private void setCaseColor()
    {
        _caseIsGold = Rnd.Range(0, 2) == 0;
        ClockFrameObj.material.color = _caseIsGold ? _clockfaceGold : _clockfaceSilver;
    }

    private void setHandsColor()
    {
        _handsColorMatchesNumerals = Rnd.Range(0, 2) == 0;
        if (_handsColorMatchesNumerals)
            // Hour and minute hands are the same color 
            MinuteHandObj.material.color = HourHandObj.material.color = _colors[_numeralsColor];
        else
            // Use a non-black color and black to avoid clashing colors
            MinuteHandObj.material.color = HourHandObj.material.color = _numeralsColor == 5 ? _colors[Rnd.Range(0, 5)] : _blackColor;
    }

    private static T[] newArray<T>(params T[] array) { return array; }

    private IEnumerator moveSecondHand()
    {
        var secondsValue = Rnd.Range(0, 60);
        while (true)
        {
            SecondHand.transform.localEulerAngles = new Vector3(0, (secondsValue + .3f) * 6, 0);
            yield return null;
            SecondHand.transform.localEulerAngles = new Vector3(0, (secondsValue + .7f) * 6, 0);
            yield return null;
            secondsValue = (secondsValue + 1) % 60;
            SecondHand.transform.localEulerAngles = new Vector3(0, (secondsValue + .1f) * 6, 0);
            yield return null;
            SecondHand.transform.localEulerAngles = new Vector3(0, secondsValue * 6, 0);
            yield return new WaitForSeconds(57f / 60f);    // assumes 60 fps
        }
    }
}
