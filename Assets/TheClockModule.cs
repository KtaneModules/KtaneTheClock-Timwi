using System;
using System.Collections;
using System.Collections.Generic;
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

    private int _curNumerals;
    private NumeralStyle _numeralStyle;
    private HandStyle _handStyle;
    private int _curTime;

    private static Color[] _darkColors = newArray(
        new Color(0x2A / 255f, 0x67 / 255f, 0xCC / 255f),   // blue
        new Color(0xA5 / 255f, 0x13 / 255f, 0x13 / 255f),   // red
        new Color(0x0E / 255f, 0x8E / 255f, 0x1F / 255f),   // green
        new Color(0xE2 / 255f, 0xD7 / 255f, 0x00 / 255f),   // yellow
        new Color(0xC1 / 255f, 0x1E / 255f, 0xDB / 255f),   // purple
        new Color(0x20 / 255f, 0x20 / 255f, 0x20 / 255f)    // black
    );

    private static Color[] _lightColors = newArray(
        new Color(0x54 / 255f, 0x7B / 255f, 0xFF / 255f),   // blue
        new Color(0xD7 / 255f, 0x2C / 255f, 0x2C / 255f),   // red
        new Color(0x39 / 255f, 0xEE / 255f, 0x01 / 255f),   // green
        new Color(0xFF / 255f, 0xF5 / 255f, 0x3E / 255f),   // yellow
        new Color(0xB3 / 255f, 0x25 / 255f, 0xFF / 255f),   // purple
        new Color(0xf8 / 255f, 0xf8 / 255f, 0xf8 / 255f)    // white
    );

    private static Color _darkClockfaceBackgroundColor = new Color(0x20 / 255f, 0x20 / 255f, 0x20 / 255f);
    private static Color _lightClockfaceBackgroundColor = new Color(0xf8 / 255f, 0xf8 / 255f, 0xf8 / 255f);

    private static Color _clockfaceGold = new Color(0xFF / 255f, 0xC8 / 255f, 0x50 / 255f);
    private static Color _clockfaceSilver = new Color(0xBC / 255f, 0xBC / 255f, 0xBC / 255f);

    private static T[] newArray<T>(params T[] array) { return array; }

    void Start()
    {
        Debug.Log("[The Clock] Started");

        switch (_numeralStyle = (NumeralStyle) Rnd.Range(0, 3))
        {
            case NumeralStyle.None:
                Numerals.gameObject.SetActive(false);
                break;
            case NumeralStyle.Roman:
                Numerals.mesh = RomanNumerals[Rnd.Range(0, RomanNumerals.Length)];
                break;
            case NumeralStyle.Arabic:
                Numerals.mesh = ArabicNumerals[Rnd.Range(0, ArabicNumerals.Length)];
                break;
        }

        int ix;
        switch (_handStyle = (HandStyle) Rnd.Range(0, 3))
        {
            case HandStyle.Line:
                ix = Rnd.Range(0, HourLine.Length);
                HourHand.mesh = HourLine[ix];
                MinuteHand.mesh = MinuteLine[ix];
                break;
            case HandStyle.Arrow:
                ix = Rnd.Range(0, HourArrow.Length);
                HourHand.mesh = HourArrow[ix];
                MinuteHand.mesh = MinuteArrow[ix];
                break;
            case HandStyle.Spade:
                ix = Rnd.Range(0, HourSpade.Length);
                HourHand.mesh = HourSpade[ix];
                MinuteHand.mesh = MinuteSpade[ix];
                break;
        }

        // Dark or light background?
        var _light = Rnd.Range(0, 2) == 0;
        ClockfaceBackgroundObj.material.color = _light ? _lightClockfaceBackgroundColor : _darkClockfaceBackgroundColor;

        // Gold or silver frame?
        ClockFrameObj.material.color = Rnd.Range(0, 2) == 0 ? _clockfaceGold : _clockfaceSilver;

        // Pick a color for the numerals/tickmarks
        var numeralColors = _light ? _darkColors : _lightColors;
        var nColorIx = Rnd.Range(0, numeralColors.Length);
        NumeralsObj.material.color = ClockfaceObj.material.color = numeralColors[nColorIx];

        // Hour and minute hands are either the same color as the numerals or black/white
        MinuteHandObj.material.color = HourHandObj.material.color = Rnd.Range(0, 2) == 0 ? numeralColors[nColorIx] : numeralColors.Last();

        // Second hand
        switch (Rnd.Range(0, 3))
        {
            case 0: // Absent
                SecondHand.SetActive(false);
                break;
            case 1: // Matched
                SecondHandObj.material.color = MinuteHandObj.material.color;
                StartCoroutine(moveSecondHand());
                break;
            case 2: // Unmatched
                var secHandColorIxs = Enumerable.Range(0, numeralColors.Length).Where(i => i != nColorIx && i != numeralColors.Length - 1).ToArray();
                var secHandColorIx = secHandColorIxs[Rnd.Range(0, secHandColorIxs.Length)];
                SecondHandObj.material.color = numeralColors[secHandColorIx];
                StartCoroutine(moveSecondHand());
                break;
        }

        _curTime = Rnd.Range(0, 60 * 24);
        var hour = _curTime / 60;
        var minute = _curTime % 60;
        MinuteHand.transform.localEulerAngles = new Vector3(0, minute * 6, 0);
        HourHand.transform.localEulerAngles = new Vector3(0, hour * 30 + minute * .5f, 0);

        Module.OnActivate = ActivateModule;
    }

    private IEnumerator moveSecondHand()
    {
        var secondsValue = Rnd.Range(0, 60);
        while (true)
        {
            SecondHand.transform.localEulerAngles = new Vector3(0, secondsValue * 6, 0);
            yield return new WaitForSeconds(1f);
            secondsValue = (secondsValue + 1) % 60;
        }
    }

    void ActivateModule()
    {
        Debug.Log("[The Clock] Activated");
    }
}
