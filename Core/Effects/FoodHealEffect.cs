using GTANetworkAPI;

namespace HardLife.Core.Effects
{
    //Duration 10000 означает что эффект будет длится 10 секунд
    //IsDublicable true значит что его можно наложить несколько раз
    //UpdateRate 1000 значит что функция Update будет вызыватся каждую секунду на протяжении Duration(10 сек)
    [Effect(Id = 0, Name = "Heal", Duration = 10000, UpdateRate = 1000, IsDublicable = true, IsCancelable = true)]
    public class FoodHealEffect : Effect
    {
        protected override bool Set(Player player)
        {
            Console.WriteLine($"Sets effect: Heal");
            return true;
        }
        protected override void End(Player player)
        {
            Console.WriteLine($"End effect: Heal2");
        }
        //Если вернуть false, то еффект прекратит действовать
        //в данной функции false возвращается когда hp игрока полное
        protected override bool Update(Player player)
        {
            bool status = true;
            if (player.Health + 3 > 100) { player.Health = 100; status = false; }
            else player.Health += 3;
            Console.WriteLine($"Name {player.Name}, Heal: {player.Health}");
            return status;
        }
    }
}
