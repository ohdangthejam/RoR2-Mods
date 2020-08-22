using BepInEx;
using RoR2;

namespace OhDangTheJam
{
    [BepInDependency("com.bepis.r2api")]
    //Change these
    [BepInPlugin("com.OhDangTheJam.ItemDropPlus", "ItemDropPlus", "1.0.0")]
    public class ItemDropPlus : BaseUnityPlugin
    {
        public void Awake()
        {
            Chat.AddMessage("ITEM DROP PLUS ACTIVATED");
            On.EntityStates.Huntress.ArrowRain.OnEnter += (orig, self) =>
            {
                // [The code we want to run]
                Chat.AddMessage("You used Huntress's Arrow Rain!");

                // Call the original function (orig)
                // on the object it's normally called on (self)
                orig(self);
            };
        }
    }
}