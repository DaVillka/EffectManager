using GTANetworkAPI;

namespace HardLife.Core.Effects
{
    [Effect(Id = 0, Name = "Heal", Duration = 10000, UpdateRate = 1000, IsDublicable = true)]
    public class HealEffect : Effect
    {
        public override bool Set(Player player)
        {
            Console.WriteLine($"Sets effect: {Name}");
            return true;
        }
        public override void End(Player player)
        {
            Console.WriteLine($"End effect: {Name}");
        }
        public override bool Update(Player player)
        {
            bool status = true;
            if (player.Health + 3 > 100) { player.Health = 100; status = false; }
            else player.Health += 3;
            Console.WriteLine($"Name {player.Name}, Heal: {player.Health}");
            return status;
        }
    }
}
