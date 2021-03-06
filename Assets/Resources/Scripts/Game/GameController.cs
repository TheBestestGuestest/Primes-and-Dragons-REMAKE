﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;

[Serializable]
public class GameState {
    public int floor = 47, turnCount = -1;
    public double elapsedTime = 0, timeOnFloor = 0, timeOnTurn = 0;
    public List<EnemyState> es;
}

public class GameController : MonoBehaviour {
    public static GameController Instance;
    private const double MAX_TIME = 3600 * 99 + 60 * 99 + 99;
    private GameState currState = new GameState();
    [SerializeField] private GameStatsUI gsUI = default;
    [HideInInspector] public bool isPaused = false;
    [HideInInspector] public bool waitingForInput = false;

    private EnemySpawner es = new EnemySpawner();
    private List<Enemy> currEnemies;
    [SerializeField] private SpriteRenderer currEnemyBG = default;
    private Sprite[] enemyBGs;
    private AudioClip[] musBGs;

    [SerializeField] private DamageBar damageBar = default;
    [SerializeField] private GameObject endAnim = default;

    void Awake() {
        Instance = this;
        enemyBGs = Resources.LoadAll<Sprite>("Sprites/Main Screen/Board/Enemy Board");
        musBGs = Resources.LoadAll<AudioClip>("Audio/Music");
    }
    void Start() {
        StartCoroutine(LoadingScreen.Instance.HideDelay());
        isPaused = false;
        currEnemies = new List<Enemy>();
        if (isLoadingData()) SaveStateController.Instance.loadDataIntoGame();
        StartCoroutine(TurnRoutine());
    }
    void Update() {
        if (waitingForInput && !isPaused) {
            currState.elapsedTime = Math.Min(currState.elapsedTime + Time.deltaTime, MAX_TIME);
            currState.timeOnFloor = Math.Min(currState.timeOnFloor + Time.deltaTime, MAX_TIME);  //sus
            currState.timeOnTurn = Math.Min(currState.timeOnTurn + Time.deltaTime, MAX_TIME);
            gsUI.updateText(currState);
        }
    }
    private bool isLoadingData() => PlayerPrefs.HasKey("LoadFromSaveFile") && PlayerPrefs.GetInt("LoadFromSaveFile") == 1;
    public GameState getState() {
        currState.es = new List<EnemyState>();
        foreach (Enemy e in currEnemies) currState.es.Add(e.getState());
        return currState;
    }
    public int getCurrTurn() => currState.turnCount;
    public int getFloor() => currState.floor;
    public double getTimeOnFloor() => currState.timeOnFloor;
    public double getTimeOnTurn() => currState.timeOnTurn;
    public bool isTurnMod(int mod, int remainder = 0) => currState.turnCount % mod == remainder;
    public void setState(GameState gs) => currState = gs;
    public List<Enemy> getCurrEnemies() => currEnemies;
    public void loadEnemy(Enemy e) => currEnemies.Add(e);
    public void saveGame() => SaveStateController.Instance.saveCurrData();

    private IEnumerator TurnRoutine() {
        do {
            yield return StartCoroutine(initRound());
            do {
                yield return StartCoroutine(PlayerTurn());
                if (!Player.Instance.isAlive()) break;
                currState.timeOnTurn = 0;
                currState.turnCount++;
                yield return StartCoroutine(EnemyTurn());
            } while (Player.Instance.isAlive() && currEnemies.Count > 0);
            if (Player.Instance.isAlive()) {
                currState.timeOnFloor = 0;
                currState.floor++;
                currState.turnCount = -1;
            }
        } while (currState.floor <= 50 && Player.Instance.isAlive());
        yield return StartCoroutine(gameEnd(Player.Instance.isAlive() && currState.floor == 51));
    }

    private IEnumerator initRound() {
        gsUI.updateText(currState);
        adjustBackground();
        adjustMusic();
        if (!isLoadingData()) currEnemies = es.getEnemies(currState.floor);
        else {
            foreach(EnemyState e in currState.es) {
                Enemy temp = Enemy.Create(e.prefab, e.number, e.maxHealth, e.damage, e.spriteName);
                temp.setState(e);
                currEnemies.Add(temp);
            }
            PlayerPrefs.SetInt("LoadFromSaveFile", 0);
        }
        displayEnemies();
        adjustOrbRates();
        yield return StartCoroutine(adjustPlayerStats());
        saveGame();
    }
    private void adjustBackground() {
        int currEnemyBGIndex = 0;
        if (currState.floor > 0) currEnemyBGIndex++;
        if (currState.floor > 15) currEnemyBGIndex++;
        if (currState.floor > 30) currEnemyBGIndex++;
        if (currState.floor > 45) currEnemyBGIndex++;
        if (currState.floor == 50) currEnemyBGIndex++;
        currEnemyBG.sprite = enemyBGs[currEnemyBGIndex];
    }
    private void adjustMusic() {
        int bgMus = 0;
        if (currState.floor > 0) bgMus++;
        if (currState.floor > 15) bgMus++;
        if (currState.floor > 30) bgMus++;
        if (currState.floor > 45) bgMus++;
        if (currState.floor == 50) bgMus++;
        AudioClip clip = AudioController.Instance.musicSource.clip;
        if (clip == null || clip.name != musBGs[bgMus].name) {
            AudioController.Instance.musicSource.clip = musBGs[bgMus];
            AudioController.Instance.musicSource.Play();
        }
    }
    private IEnumerator adjustPlayerStats() {
        int maxHealth = 400;
        if (currState.floor > 0) maxHealth += 100;
        if (currState.floor > 15) maxHealth += 250;
        if (currState.floor > 30) maxHealth += 250;
        if (currState.floor > 45) maxHealth += 500;
        if (currState.floor == 50) maxHealth += 500;
        yield return StartCoroutine(Player.Instance.setMaxHealth(maxHealth));
    }
    public void adjustOrbRates() {
        int[][] totalsArr = new int[Enum.GetValues(typeof(ORB_VALUE)).Length][];
        for (int i = 0; i < totalsArr.Length; i++) totalsArr[i] = new int[Enum.GetValues(typeof(OrbSpawnRate)).Length];
        foreach (Enemy e in currEnemies) {
            OrbSpawnRate[] osrArr = e.getState().orbSpawnRates;
            for (int i = 0; i < totalsArr.Length; i++) totalsArr[i][(int)osrArr[i]]++;
        }

        Board.Instance.resetOrbSpawnRates();
        OrbSpawnRate[] finalSpawnRates = new OrbSpawnRate[Enum.GetValues(typeof(ORB_VALUE)).Length];
        for (int i = 0; i <= (int)ORB_VALUE.NINE; i++) {
            if (totalsArr[i][0] > totalsArr[i][3]) finalSpawnRates[i] = OrbSpawnRate.NONE;
            else if (totalsArr[i][0] < totalsArr[i][3]) finalSpawnRates[i] = OrbSpawnRate.MAX;
            else {
                if (totalsArr[i][1] > totalsArr[i][4]) finalSpawnRates[i] = OrbSpawnRate.DECREASED;
                else if (totalsArr[i][1] < totalsArr[i][4]) finalSpawnRates[i] = OrbSpawnRate.INCREASED;
                else finalSpawnRates[i] = OrbSpawnRate.NORMAL;
            }
        }
        for(int i = (int)ORB_VALUE.NINE + 1; i < totalsArr.Length; i++) {
            if (totalsArr[i][3] > 0) finalSpawnRates[i] = OrbSpawnRate.MAX;
            else if (totalsArr[i][1] > 0 || totalsArr[i][2] > 0 || totalsArr[i][4] > 0) {
                if (totalsArr[i][1] > totalsArr[i][4]) finalSpawnRates[i] = OrbSpawnRate.DECREASED;
                else if (totalsArr[i][1] < totalsArr[i][4]) finalSpawnRates[i] = OrbSpawnRate.INCREASED;
                else finalSpawnRates[i] = OrbSpawnRate.NORMAL;
            }
            else finalSpawnRates[i] = OrbSpawnRate.NONE;
        }
        if (finalSpawnRates.Contains(OrbSpawnRate.MAX)) for(int i = 0; i < finalSpawnRates.Length; i++) if(finalSpawnRates[i] !=  OrbSpawnRate.MAX) finalSpawnRates[i] = OrbSpawnRate.NONE;
        Board.Instance.setOrbSpawnRates(finalSpawnRates);
    }
    private void setWaitingForInput(bool getInput) {
        waitingForInput = getInput;
        gsUI.toggleAll(waitingForInput);

        bool pauseIsOn = true;
        foreach (Enemy e in currEnemies) {
            pauseIsOn = !e.toggleAllTimerSkills(waitingForInput) && pauseIsOn;
            e.enableSkillToggle(waitingForInput);
        }
        if (waitingForInput) gsUI.togglePauseButton(pauseIsOn);
    }

    private IEnumerator PlayerTurn() {
        //getting input
        yield return StartCoroutine(Board.Instance.toggleForeground(false));
        setWaitingForInput(true);
        yield return StartCoroutine(Board.Instance.getInput());
        setWaitingForInput(false);
        string inputNum = Board.Instance.getInputNum(false);
        bool isNulified = Board.Instance.numberIsNullified();
        BigInteger actualNum = Board.Instance.getInputNum(true).Equals("") ? new BigInteger(1) : BigInteger.Parse(Board.Instance.getInputNum(true));

        //checking if the input is divisible by any enemy
        bool anyDMGdealt = false;
        if (!isNulified) {
            foreach (Enemy e in currEnemies) {
                bool dealDMG = actualNum % e.getState().number == 0;
                if (dealDMG) StartCoroutine(e.targetedAnimation(false));  //flashing red animation start
                anyDMGdealt = anyDMGdealt || dealDMG;
            }
        }
        Board.Instance.setNumBarColor(anyDMGdealt ? NUMBAR_STATE.SUCCESS : NUMBAR_STATE.FAILURE);

        //clear board while calculating damage/heals/poisons sequentially
        foreach (char c in inputNum) {
            if (!isNulified) {
                switch (c) {
                    case 'P':
                        StartCoroutine(Player.Instance.addToHealth(-50, ColorPalette.getColor(14, 2)));
                        break;
                    case 'E': case 'S': case 'N':
                        /* Do nothing */
                        break;
                    case '0':
                        if (anyDMGdealt) damageBar.addNextDigit(0);
                        StartCoroutine(Player.Instance.addToHealth(50));
                        break;
                    default:
                        if (anyDMGdealt) damageBar.addNextDigit((int)char.GetNumericValue(c));
                        break;
                }
            }
            yield return StartCoroutine(Board.Instance.rmvNextOrb(!isNulified ? Board.DISAPPEAR_DELTA : 0));
        }
        if (!Player.Instance.isAlive()) Player.Instance.setCauseOfDeath("poison");

        //fill the board
        yield return new WaitForSeconds(Board.DISAPPEAR_DURATION);
        yield return StartCoroutine(Board.Instance.fillBoard());
        yield return StartCoroutine(Board.Instance.toggleForeground(true));

        //deal damage to enemies
        damageBar.displayText(false);
        if (anyDMGdealt) {
            int damageDealt = damageBar.getCurrDamage();
            for (int i = 0; i < currEnemies.Count; i++) {
                Enemy e = currEnemies[i];
                if (actualNum % e.getState().number == 0) {
                    yield return StartCoroutine(e.takeDMG(-damageDealt));
                    if (!e.isAlive()) {
                        currEnemies.Remove(e);
                        e.endAllSkills();
                        // TO-DO: delay here? or enemy death animation
                        Destroy(e.gameObject);
                        i--;
                        displayEnemies();
                    }
                }
            }
        }
        damageBar.resetValues();
        yield return StartCoroutine(Player.Instance.resetDeltaHealth());
    }
    private IEnumerator EnemyTurn() {
        foreach (Enemy e in currEnemies) {
            yield return StartCoroutine(e.clearAllMarkedTimerOrbs());
            yield return StartCoroutine(e.updateAndRmvAllSkills(true));
        }
        foreach (Enemy e in currEnemies) {
            yield return StartCoroutine(e.Attack());
            if (!Player.Instance.isAlive()) {
                Player.Instance.setCauseOfDeath(e.getState().number.ToString());
                break;
            }
        }
        yield return StartCoroutine(Player.Instance.resetDeltaHealth());

        if (currState.floor == 47 && currState.turnCount > 0 && currEnemies.Count == 2) {  //spaghett
            Enemy e = currEnemies[0];
            currEnemies.Remove(e);
            currEnemies.Add(e);
            displayEnemies();
        }
    }
    private void displayEnemies() {
        switch (currEnemies.Count) {
            case 1:
                currEnemies[0].setPosition(EnemyPosition.CENTER_1);
                break;
            case 2:
                currEnemies[0].setPosition(EnemyPosition.LEFT_2);
                currEnemies[1].setPosition(EnemyPosition.RIGHT_2);
                break;
            case 3:
                currEnemies[0].setPosition(EnemyPosition.LEFT_3);
                currEnemies[1].setPosition(EnemyPosition.CENTER_3);
                currEnemies[2].setPosition(EnemyPosition.RIGHT_3);
                break;
            default:
                break;
        }
    }
    private IEnumerator gameEnd(bool win) {
        // Sending data to the leaderboard.
        PlayerPrefs.SetInt("Floor", currState.floor);
        PlayerPrefs.SetString("Time", currState.elapsedTime.ToString("R"));
        PlayerPrefs.SetString("Death", Player.Instance.getCauseOfDeath());

        // Ending animation.
        yield return endAnim.GetComponent<EndGameAnimation>().endGameAnimation(win);
    }
}