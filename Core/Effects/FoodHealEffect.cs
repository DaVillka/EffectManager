using GTANetworkAPI;

namespace HardLife.Core.Effects
{
    //Duration 10000 означает что эффект будет длится 10 секунд
    //IsDublicable false значит что его нельзя наложить несколько раз
    //UpdateRate 1000 значит что функция Update будет вызыватся каждую секунду на протяжении Duration(10 сек)
    [Effect(Id = 0, Name = "Heal", Duration = 10000, UpdateRate = 1000, IsDublicable = false)]
    public class FoodHealEffect : Effect
    {
        //Вызывается при наложении эффекта
        //возвращает bool, наложение эффекта можно отменить вернув false
        //так как на пример нету смысла накладывать эффект восстановления хп
        //если хп полное
        public override bool Set(Player player)
        {
            Console.WriteLine($"Sets effect: {Name}");
            if(player.Health >= 100) return false;
            return true;
        }
        //Вызывается при завершении работы эффекта
        public override void End(Player player)
        {
            Console.WriteLine($"End effect: {Name}");
        }
        //Если вернуть false, то еффект прекратит действовать
        //в данной функции false возвращается когда hp игрока полное
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
