namespace HardLife.Core.Effects
{
    //public class Test : Script
    //{
    //    private Player player1 = new Player(0, 0, "Player1");
    //    private Player player2 = new Player(1, 0, "Player2");
    //    [ServerEvent(Event.ResourceStart)]
    //    public void OnResourcestart()
    //    {
    //        EffectManager e = new EffectManager();
    //        e.Initialize();
    //        e.OnPlayerSpawn(player1);
    //        e.OnSetEffect(player1, 0);
    //        //e.OnSetEffect(player1, 0);
    //        //e.OnSetEffect(player1, 0);
    //        //e.OnPlayerDissconect(player1);
    //        //
    //        //
    //        ////e.OnSetPlayerEffect(new Player(1, 0, "Player2"), 0);
    //        Task.Factory.StartNew(async () =>
    //        {
    //            await Task.Delay(2000);
    //            e.OnPlayerDissconect(player1);
    //        });
    //        Task.Factory.StartNew(async () =>
    //        {
    //            await Task.Delay(3000);
    //            e.OnPlayerSpawn(player1);
    //        });
    //    }
    //
    //}
    public class PlayerEffectData
    {
        public int Id { get; set; }//айди эффекта
        public Effect Effect { get; set; }//Сам эффект
        public int LeftTime { get; set; }//сколько времени осталось действовать
        public bool IsForciblyCandel { get; set; } = false;//Флаг, о принудительной остановке
        public CancellationTokenSource CancelToken { get; set; }//Токен для отмены таска
    }
}
