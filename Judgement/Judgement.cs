using BepInEx;
using RoR2;
using UnityEngine;

namespace Judgement
{
  [BepInPlugin("com.Nuxlar.Judgement", "Judgement", "1.2.1")]

  public class Judgement : BaseUnityPlugin
  {

    public void Awake()
    {
      new GameMode();
      new Hooks();
      new SimHooks();
      new BazaarHooks();
    }

  }
}