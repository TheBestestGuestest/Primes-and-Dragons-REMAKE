﻿using System;
using System.Linq;
using UnityEngine;

public class TutorialEnemy : Enemy {
    public static Enemy Create() {
        return Create("Tutorial Enemy", 2, 500, 40, "tv1");
    }
    protected override void addAllSkills() {
        skillList.Add(EnemyAttack.Create(() => GameController.Instance.getState().turnCount % 2 == 0, true, () => gameObject, () => 50, skillTrans));
        skillList.Add(EnemyHPBuff.Create(() => GameController.Instance.getState().turnCount % 3 == 0, EnemyBuffs.DMG_REFLECT, 2, skillTrans));
        skillList.Add(EnemyTimer.Create(() => GameController.Instance.getState().turnCount % 3 == 1, () => -GameController.Instance.getState().turnCount, skillTrans));

        OrbSpawnRate[] osr = Board.getDefaultOrbSpawnRates();
        for (int i = 0; i < 10; i++) if (i % 2 == 0) osr[i] = OrbSpawnRate.NONE;
        skillList.Add(EnemyOrbSkill.Create(() => GameController.Instance.getState().turnCount % 3 == 2, osr, 2, skillTrans));

        Func<Orb, bool> isEven = (Orb o) => o.isEven();
        EnemyBoardSkill test = EnemyBoardSkill.MarkIfSkill(() => GameController.Instance.getState().turnCount % 2 == 0, getUniqueID(), isEven, 0.1f, skillTrans);
        test.addRmvSkill(0.03f);
        skillList.Add(test);

        Func<Orb, bool> isOdd = (Orb o) => o.isOdd();
        EnemyBoardSkill test2 = EnemyBoardSkill.MarkIfSkill(() => GameController.Instance.getState().turnCount % 3 == 1, getUniqueID(), isOdd, 0.1f, skillTrans, 1);
        test2.addIncSkill(0.03f, (Orb o) => -1);
        skillList.Add(test2);

        Func<Orb, bool> rowCon = (Orb o) => o.getGridPos().x % 2 == 1;
        EnemyBoardSkill test3 = EnemyBoardSkill.MarkIfSkill(() => GameController.Instance.getState().turnCount % 3 == 2, getUniqueID(), rowCon, 0.1f, skillTrans);
        test3.addSetSkill(0.03f, (Orb o) => ORB_VALUE.EMPTY);
        skillList.Add(test3);

        skillList.Add(EnemyBoardSkill.ShuffleSkill(() => RNG.NextDouble() < 0.5, 10, 0.075f, skillTrans));
        base.addAllSkills();
    }
}
