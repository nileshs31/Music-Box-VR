using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Standards;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class CylinderPinSpawner : MonoBehaviour
{
    [Header("References")]
    public Transform cylinder;         
    public GameObject pinPrefab;      

    [Header("Layout")]
    public int noOfRows = 5;         
    public int distanceRefernce = 25;         

    public int noOfPitch = 16;          
    public float radius = 0.5f;        
    public float height = 1.0f;

    public string midiPath = "";

    private Transform pinsParent;

    private GameObject[,] pins;

    private readonly int[] allowedNotes = {
        60, 62, 64, 65, 67, 69, 71,
        72, 74, 76, 77, 79, 81, 83, 84, 86
    };

    // The grid: rows × pitches, each cell holds a list of notes
    private List<List<List<Note>>> grid;


    void Start()
    {
        BuildGrid(midiPath);
        SpawnGrid();
    }



    public void SpawnGrid()
    {
        if (pinsParent == null)
        {
            GameObject go = new GameObject("Pins");
            go.transform.SetParent(cylinder, false);
            pinsParent = go.transform;
        }

        // clear previous pins (optional)
        for (int i = pinsParent.childCount - 1; i >= 0; i--)
            DestroyImmediate(pinsParent.GetChild(i).gameObject);

        pins = new GameObject[noOfRows, noOfPitch];

        for (int step = 0; step < noOfRows; step++)
        {
            float frac = step / (float)distanceRefernce;
            float angleRad = frac * Mathf.PI * 2f;

            for (int pitch = 0; pitch < noOfPitch; pitch++)
            {
                Vector3 localPos = AnglePitchToLocalPosition(angleRad, pitch);
                GameObject pin = Instantiate(pinPrefab, pinsParent);
                pin.transform.localPosition = localPos;

                // face outward
                Vector3 outward = new Vector3(localPos.x, 0f, localPos.z).normalized;
                if (outward.sqrMagnitude < 1e-6f) outward = Vector3.forward;
                pin.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);

                pin.SetActive(false);            // <- start OFF
                pins[step, pitch] = pin;
            }
        }

        ApplyGridToPins(); // <- turn on only those present in grid
    }

    void ApplyGridToPins()
    {
        if (grid == null) return;

        for (int r = 0; r < noOfRows; r++)
        {
            for (int p = 0; p < noOfPitch; p++)
            {
                var pin = pins[r, p];
                if (pin == null) continue;

                bool hasNote =
                    r < grid.Count &&
                    p < grid[r].Count &&
                    grid[r][p].Count > 0;

                pin.SetActive(hasNote);
            }
        }
    }

    Vector3 AnglePitchToLocalPosition(float angleRad, int pitchIndex)
    {
        float x = Mathf.Cos(angleRad) * radius;
        float z = Mathf.Sin(angleRad) * radius;

        float rowHeight = height / Mathf.Max(1, noOfPitch);
        float y = -height * 0.5f + (pitchIndex + 0.5f) * rowHeight;

        return new Vector3(x, y, z);
    }


    void BuildGrid(string path)
    {
        // Init grid (rows x pitches)
        grid = new List<List<List<Note>>>();
        for (int r = 0; r < noOfRows; r++)
        {
            var row = new List<List<Note>>();
            for (int p = 0; p < noOfPitch; p++) row.Add(new List<Note>());
            grid.Add(row);
        }

        MidiFile midi = MidiFile.Read(path);
        var notesSorted = midi.GetNotes().OrderBy(n => n.Time).ToList();
        if (notesSorted.Count == 0) return;

        // ----- PPQ & jitter tolerance (very small) -----
        int ppq = 480; // sensible default
        if (midi.TimeDivision is TicksPerQuarterNoteTimeDivision tpq)
            ppq = tpq.TicksPerQuarterNote;

        // Only collapse micro jitter (≈ 2 ms @ 120 BPM). Increase if your files are noisy.
        long jitterTicks = Math.Max(1, ppq / 240); // ~0.00417 quarter notes

        // ----- (Optional) empty-row insertion threshold -----
        // Estimate a "typical" gap in ticks from unique onsets, then insert empties when gap is much larger.
        // You can tune or disable with gapMultiplierForEmpty <= 0.
        const float gapMultiplierForEmpty = 1.75f;

        // Build unique onset list using the tiny jitter
        List<long> uniqueOnsets = new List<long>();
        long groupStart = notesSorted[0].Time;
        uniqueOnsets.Add(groupStart);
        foreach (var n in notesSorted)
        {
            if (n.Time > groupStart + jitterTicks)
            {
                groupStart = n.Time;
                uniqueOnsets.Add(groupStart);
            }
        }

        // Typical gap (median of gaps). Fallback to ppq/4 if not enough data.
        long medianGapTicks = ppq / 4;
        if (uniqueOnsets.Count >= 3)
        {
            List<long> gaps = new List<long>(uniqueOnsets.Count - 1);
            for (int i = 1; i < uniqueOnsets.Count; i++)
            {
                long g = uniqueOnsets[i] - uniqueOnsets[i - 1];
                if (g > 0) gaps.Add(g);
            }
            if (gaps.Count > 0)
            {
                gaps.Sort();
                int mid = gaps.Count / 2;
                medianGapTicks = gaps.Count % 2 == 1 ? gaps[mid] : (gaps[mid - 1] + gaps[mid]) / 2;
                medianGapTicks = Math.Max(1, medianGapTicks);
            }
        }

        // Now fill rows: distinct onsets with tiny jitter; insert empties for big gaps
        int currentRow = 0;
        long currentRowStart = notesSorted[0].Time;

        // Place notes in row 0
        foreach (var n in notesSorted)
        {
            if (n.Time <= currentRowStart + jitterTicks)
            {
                int pIdx0 = Array.IndexOf(allowedNotes, n.NoteNumber);
                if (pIdx0 >= 0) grid[currentRow][pIdx0].Add(n);
            }
        }

        // Walk all notes again to advance rows on new onsets
        for (int i = 1; i < notesSorted.Count && currentRow < noOfRows; i++)
        {
            var n = notesSorted[i];

            // new onset?
            if (n.Time > currentRowStart + jitterTicks)
            {
                long gap = n.Time - currentRowStart;

                // Insert empty rows for big gaps (optional)
                if (gapMultiplierForEmpty > 0 && gap > (long)(medianGapTicks * gapMultiplierForEmpty))
                {
                    int empties = (int)Math.Floor(gap / (double)medianGapTicks) - 1;
                    if (empties > 0)
                    {
                        currentRow += Math.Min(empties, Math.Max(0, noOfRows - 1 - currentRow));
                    }
                }

                // advance to next row for this onset
                if (currentRow + 1 >= noOfRows) break;
                currentRow++;
                currentRowStart = n.Time;

                // put *all* notes of this onset (within jitter) into this new row
                // (scan forward while within jitter window)
                for (int k = i; k < notesSorted.Count; k++)
                {
                    var nk = notesSorted[k];
                    if (nk.Time > currentRowStart + jitterTicks) break;

                    int pk = Array.IndexOf(allowedNotes, nk.NoteNumber);
                    if (pk >= 0) grid[currentRow][pk].Add(nk);
                }
            }
        }
    }

}
