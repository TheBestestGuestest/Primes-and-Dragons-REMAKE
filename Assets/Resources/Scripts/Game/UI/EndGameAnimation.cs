﻿using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EndGameAnimation : MonoBehaviour {
    [SerializeField] private Image backgImg = default;
    [SerializeField] private GameObject gameOverUI = default, winScreenUI = default;
    [SerializeField] private Animator endGameAnimator = default;

    public IEnumerator endGameAnimation(bool win) {
        yield return win ? winAnimation() : gameOverAnimation();
        Scenes.Load("Results Screen");
    }
    private IEnumerator gameOverAnimation() {
        gameOverUI.SetActive(true);
        backgImg.gameObject.SetActive(true);
        endGameAnimator.SetBool("lose", true);
        yield return new WaitUntil(() => endGameAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.5f);
        yield return new WaitUntil(() => endGameAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f);
    }
    private IEnumerator winAnimation() {
        winScreenUI.SetActive(true);
        backgImg.gameObject.SetActive(true);
        endGameAnimator.SetBool("win", true);
        yield return new WaitUntil(() => endGameAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.5f);
        yield return new WaitUntil(() => endGameAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f);
    }
}
