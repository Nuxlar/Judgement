using BepInEx;

namespace Judgement
{
  [BepInPlugin("com.Nuxlar.Judgement", "Judgement", "1.4.1")]

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