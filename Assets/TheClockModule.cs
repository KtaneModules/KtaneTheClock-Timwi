using System;
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

    private int _curNumerals;

    void Start()
    {
        Debug.Log("[The Clock] Started");
        Module.OnActivate = ActivateModule;
    }

    void ActivateModule()
    {
        //Debug.Log("[The Clock] Activated");

        tryAgain:
        _curNumerals++;
        if (_curNumerals < RomanNumerals.Length)
            Numerals.mesh = RomanNumerals[_curNumerals];
        else
        {
            var ix = _curNumerals - RomanNumerals.Length;
            if (ix < ArabicNumerals.Length)
                Numerals.mesh = ArabicNumerals[ix];
            else
            {
                _curNumerals = 0;
                goto tryAgain;
            }
        }
    }
}
