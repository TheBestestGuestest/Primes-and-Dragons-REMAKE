﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class Leaderboards : MonoBehaviour {
    public LeaderboardInput input;
    public WittyComment comment;
    
    private LeaderboardData data;
    private List<LeaderboardItem> topTenItems;

    private bool fastForward = false;

    void Start() {
        StartCoroutine(LoadingScreen.Instance.HideDelay());
        if (!loadLeaderboardData()) data = new LeaderboardData();
        StartCoroutine(leaderboardAnim(data.addEntry(recieveDataFromGameController())));
    }

    private IEnumerator leaderboardAnim(int newEntry) {
        // get player name if new record
        if (newEntry != -1) {
            yield return StartCoroutine(input.getInput());
            data.topTenEntries[newEntry].name = input.getName();
            updateLeaderboardData();
        }
        yield return StartCoroutine(input.exitInput());

        // display the leaderboard
        topTenItems = new List<LeaderboardItem>();
        StartCoroutine(detectPress());
        for (int i = 9; i >= 0 ; i--) {
            topTenItems.Add(LeaderboardItem.Create(i + 1, i < data.topTenEntries.Count ? data.topTenEntries[i] : null, i == newEntry));
            for(float elapsedTime = 0f; elapsedTime <= (fastForward ? 0.075f : i >= data.topTenEntries.Count ? 0.15f : 0.30f); elapsedTime += Time.deltaTime) yield return null;
        }
        StopCoroutine(detectPress());

        // display the witty comment
        yield return StartCoroutine(comment.displayComment(comment.getWittyComment()));
    }
    private IEnumerator detectPress() {
        for (fastForward = false; ; fastForward = fastForward || Input.GetMouseButton(0) || Input.touchCount > 0) yield return null;
    }

    private LeaderboardEntry recieveDataFromGameController() {
        if (!PlayerPrefs.HasKey("Floor") || !PlayerPrefs.HasKey("Time")) return null;
        int floor = PlayerPrefs.GetInt("Floor");
        PlayerPrefs.DeleteKey("Floor");
        double time = double.Parse(PlayerPrefs.GetString("Time"));
        PlayerPrefs.DeleteKey("Time");
        return new LeaderboardEntry("PLACEHLD", floor, time);
    }
    private bool loadLeaderboardData() {
        if (!Directory.Exists("Leaderboard Data")) return false;
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream dataFile = File.Open("Leaderboard Data/data.binary", FileMode.OpenOrCreate);
        if (dataFile.Length == 0) {
            dataFile.Close();
            return false;
        }

        data = formatter.Deserialize(dataFile) as LeaderboardData;
        dataFile.Close();
        return true;
    }
    private void updateLeaderboardData() {
        if (!Directory.Exists("Leaderboard Data")) Directory.CreateDirectory("Leaderboard Data");
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream dataFile = File.Create("Leaderboard Data/data.binary");
        formatter.Serialize(dataFile, data);
        dataFile.Close();
    }
}

[System.Serializable]
public class LeaderboardData {
    public List<LeaderboardEntry> topTenEntries;

    public int addEntry(LeaderboardEntry entry) {
        if (topTenEntries == null) topTenEntries = new List<LeaderboardEntry>();
        if (entry != null) topTenEntries.Add(entry);
        topTenEntries.Sort((e1, e2) => -(e1.floor).CompareTo(e2.floor) != 0 ? -(e1.floor).CompareTo(e2.floor) 
                                        : ((e1.time).CompareTo(e2.time) != 0 ? (e1.time).CompareTo(e2.time)
                                        : (e1 == entry ? -1 : 1)));
        while(topTenEntries.Count > 10) topTenEntries.RemoveAt(topTenEntries.Count - 1);
        return topTenEntries.IndexOf(entry);
    }
}

[System.Serializable]
public class LeaderboardEntry {
    public string name;
    public int floor;
    public double time;

    public LeaderboardEntry(string n, int f, double t) {
        name = n;
        floor = f;
        time = t;
    }
}