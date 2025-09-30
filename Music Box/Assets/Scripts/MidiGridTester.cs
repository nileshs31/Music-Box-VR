using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class MidiGridTester : MonoBehaviour
{
    [Header("MIDI Settings")]
    [Tooltip("Full path to the MIDI file (absolute or StreamingAssets path).")]
    public string midiPath;

    [Header("Grid Settings")]
    public int maxRows = 25;   // time slices
    public int pitchRows = 16; // fixed vertical notes C4–D6

    // Allowed pitches: C4, D4, …, D6
    private readonly int[] allowedNotes = {
        60, 62, 64, 65, 67, 69, 71,
        72, 74, 76, 77, 79, 81, 83, 84, 86
    };

    // The grid: rows × pitches, each cell holds a list of notes
    private List<List<List<Note>>> grid;

    void Start()
    {
        if (string.IsNullOrEmpty(midiPath) || !File.Exists(midiPath))
        {
            Debug.LogError("MIDI file path is invalid: " + midiPath);
            return;
        }

        BuildGrid(midiPath);

        // Print results
        for (int r = 0; r < maxRows; r++)
        {
            for (int p = 0; p < pitchRows; p++)
            {
                if (grid[r][p].Count > 0)
                    Debug.Log($"Row {r}, PitchIndex {p} (MIDI {allowedNotes[p]}): {grid[r][p].Count} notes");
            }
        }
    }

    void BuildGrid(string path)
    {
        // Init grid (rows x 16)
        grid = new List<List<List<Note>>>();
        for (int r = 0; r < maxRows; r++)
        {
            var row = new List<List<Note>>();
            for (int p = 0; p < pitchRows; p++) row.Add(new List<Note>());
            grid.Add(row);
        }

        // Read MIDI + notes sorted by start tick
        MidiFile midi = MidiFile.Read(path);
        var tempoMap = midi.GetTempoMap();
        var notesSorted = midi.GetNotes().OrderBy(n => n.Time).ToList();
        if (notesSorted.Count == 0) return;

        // ---- compute onset tolerance in ticks ----
        // build list of unique start ticks
        var uniqueTicks = new List<long>();
        long lastTick = -1;
        foreach (var n in notesSorted)
        {
            if (n.Time != lastTick)
            {
                uniqueTicks.Add(n.Time);
                lastTick = n.Time;
            }
        }

        long minGap = long.MaxValue;
        for (int i = 1; i < uniqueTicks.Count; i++)
        {
            long gap = uniqueTicks[i] - uniqueTicks[i - 1];
            if (gap > 0 && gap < minGap) minGap = gap;
        }
        // fallback if everything is at the same tick
        if (minGap == long.MaxValue) minGap = 1;

        long tolTicks = Math.Max(1, minGap / 2); // tolerance = half of the smallest positive gap

        // ---- group notes into rows by onset with tolerance ----
        int currentRow = 0;
        long rowStartTick = notesSorted[0].Time;

        foreach (var note in notesSorted)
        {
            // new row if beyond tolerance
            if (note.Time - rowStartTick > tolTicks)
            {
                currentRow++;
                if (currentRow >= maxRows) break; // ignore the rest of the song
                rowStartTick = note.Time;
            }

            // map pitch to 0..15 (C4..D6)
            int pitchIndex = Array.IndexOf(allowedNotes, note.NoteNumber);
            if (pitchIndex < 0) continue;

            grid[currentRow][pitchIndex].Add(note);
        }
    }

}
