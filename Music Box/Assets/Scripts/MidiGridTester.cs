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
    public int maxRows = 25; 
    public int pitchRows = 16; 

    private readonly int[] allowedNotes = {
        60, 62, 64, 65, 67, 69, 71,
        72, 74, 76, 77, 79, 81, 83, 84, 86
    };

    private List<List<List<Note>>> grid;

    void Start()
    {
        if (string.IsNullOrEmpty(midiPath) || !File.Exists(midiPath))
        {
            Debug.LogError("MIDI file path is invalid: " + midiPath);
            return;
        }

        BuildGrid(midiPath);

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
        grid = new List<List<List<Note>>>();
        for (int r = 0; r < maxRows; r++)
        {
            var row = new List<List<Note>>();
            for (int p = 0; p < pitchRows; p++) row.Add(new List<Note>());
            grid.Add(row);
        }

        MidiFile midi = MidiFile.Read(path);
        var tempoMap = midi.GetTempoMap();
        var notesSorted = midi.GetNotes().OrderBy(n => n.Time).ToList();
        if (notesSorted.Count == 0) return;

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
        if (minGap == long.MaxValue) minGap = 1;

        long tolTicks = Math.Max(1, minGap / 2); 

        int currentRow = 0;
        long rowStartTick = notesSorted[0].Time;

        foreach (var note in notesSorted)
        {
            if (note.Time - rowStartTick > tolTicks)
            {
                currentRow++;
                if (currentRow >= maxRows) break;
                rowStartTick = note.Time;
            }

            int pitchIndex = Array.IndexOf(allowedNotes, note.NoteNumber);
            if (pitchIndex < 0) continue;

            grid[currentRow][pitchIndex].Add(note);
        }
    }

}
