using BepInEx;

namespace Judgement
{
  [BepInPlugin("com.Nuxlar.Judgement", "Judgement", "1.3.4")]

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