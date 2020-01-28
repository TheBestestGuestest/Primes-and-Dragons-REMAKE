﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavigationButton : MonoBehaviour {
    public void goToScene(string sceneName) {
        LoadingScreen.Instance.Show(Scenes.LoadAsync(sceneName));
    }
}