using BepInEx;
using RoR2;
using UnityEngine;

namespace Judgement
{
  [BepInPlugin("com.Nuxlar.Judgement", "Judgement", "1.2.0")]

  public class Judgement : BaseUnityPlugin
  {

    public void Awake()
    {
      new GameMode();
      new SimHooks();
    }

  }
}