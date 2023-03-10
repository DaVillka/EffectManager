using GTANetworkAPI;
using System.Reflection;
using Newtonsoft.Json;
using HardLife.Core.Effects;

namespace HardLife.Instances
{
    //Менеджер ефектов, отвечает за инициализацию 
    //ефектов и обработку внешних команд
    public class EffectManager : Script
    {
        #region Singleton
        private static EffectManager instance = null;
        public static EffectManager Instance { get { return instance; } }
        public EffectManager() { instance = this; }
        #endregion

        //Список всех ефектов(id эффекта, экземпляр эффекта)
        private Dictionary<int, Effect> effects = new Dictionary<int, Effect>();
        //
        private Dictionary<int, PlayerEffects> playerEffects = new Dictionary<int, PlayerEffects>();
        //Инициализация происходи с помощью рефлексии
        //находит все классы которые наследуют клас Effect
        //и инициализирует их
        private string testSaveData;
        public void Initialize()
        {
            //Полчучаем все классы которые унаследованы от Effect
            var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(Effect)));
            foreach (var t in types)
            {
                //Ищем в них отаттрибученые параметры
                var effectAttribute = (EffectAttribute)t.GetCustomAttribute(typeof(EffectAttribute), false);
                //если атрибута нету то выкидываешь ошибку
                if (effectAttribute == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[ERROR EFFECT] '{GetType().Name}' does not inherit attribute.");
                    Console.ResetColor();
                    return;
                }
                try
                {
                    //создаем экземпляр эффекта
                    var instance = (Effect)Activator.CreateInstance(t);

                    //Копируем в него сеттеры из атрибута
                    var sourceProps = effectAttribute.GetType().GetProperties()
                    .Where(x => x.CanRead && x.CanWrite)
                    .ToList();
                    var destProps = instance.GetType().GetProperties()
                            .Where(x => x.CanRead && x.CanWrite)
                            .ToList();
                    foreach (var sourceProp in sourceProps)
                    {
                        if (destProps.Any(x => x.Name == sourceProp.Name))
                        {
                            var p = destProps.First(x => x.Name == sourceProp.Name);
                            p.SetValue(instance, sourceProp.GetValue(effectAttribute, null), null);
                        }
                    }
                    if(instance.Initialize())
                        EffectManager.Instance.AddEffect(instance);
                }
                catch(Exception e)
                {
                   
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e);
                    Console.ResetColor();
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[EFFECT] Realized {effects.Count} effects.");
            Console.ResetColor();

            Player player = new Player(new NetHandle());
            player.Name = "Test";
            player.Health = 50;
            OnPlayerSpawn(player);
            OnSetEffect(player, 0);
            //OnSetEffect(player, 0);

            Task.Run(async () =>
            {
                await Task.Delay(3000);
                OnCancelEffect(player, 0);
            });
        }
        private void AddEffect(Effect effect)
        {
            if (effects.TryGetValue(effect.Id, out var thisEffect) && thisEffect != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR EFFECT] '{effect.GetType().Name}' An effect with the same ID already exists.");
                Console.ResetColor();
                return;
            }
            effects.Add(effect.Id, effect);
        }
        /********************* Серверны ивенты ***********************/
        [ServerEvent(Event.ResourceStart)]
        public void OnResourceStart()
        {
            Initialize();
        }
        //При спавне выделяем персонажу клас управления ефектами персонажа
        [ServerEvent(Event.PlayerSpawn)]
        public void OnPlayerSpawn(Player player)
        {
            PlayerEffects playerEffect = new PlayerEffects();
            playerEffects.Add(player.Id, playerEffect);
            //как то получаем сохраненные еффекты
            if (testSaveData != null)
            {
                List<object[]> list = JsonConvert.DeserializeObject<List<object[]>>(testSaveData);
                foreach (object[] item in list)
                {
                    long id = (long)item[0];
                    long duration = (long)item[1];
                    long leftTime = (long)item[2];
                    if (effects.TryGetValue((int)id, out var effect))
                        effect?.ValidateSet(player, playerEffect, (int)leftTime);
                }
            }            
        }
        [ServerEvent(Event.PlayerDisconnected)]
        public void OnPlayerDisconnect(Player player, DisconnectionType type, string reason)
        {
            if (playerEffects.ContainsKey(player.Id))
            {
                //сохраняем как-то информацию об ивентах персонажа
                testSaveData = playerEffects[player.Id].GetSerealizeEffects();
                //Останавливаем все таски
                playerEffects[player.Id].CancelAllEffect();
                //удаляем объект с эффектами нашего персонажа
                playerEffects.Remove(player.Id);
            }
        }
        /*********************** Клиентские ивенты ***********************/
        //Внешний ивент, добавление ефекта игроку
        public void OnSetEffect(Player player, int id)
        {
            if(playerEffects.TryGetValue(player.Id, out var playerEffect))
                if (effects.TryGetValue(id, out var effect)) 
                    effect?.ValidateSet(player, playerEffect);
        }
        //Внешний ивент, удаление еффекта у игрока
        //index - позиция эффекта в масиве
        public void OnCancelEffect(Player player, int index)
        {
            //Ищем класс, отвечающий за хранение эфектов данного игрока
            if (playerEffects.TryGetValue(player.Id, out var playerEffect) &&
                //если находим ищем дату этого эффекта по индексу в масиве
                playerEffect.GetEffectDataFromIndex(index, out var playerEffectData) &&
                //если находим то ищем реализацию эффекта по айди из полученой даты
                effects.TryGetValue(playerEffectData.Id, out var effect))
                //если находим то передаем в нее нашего перса, и его класс отвечающий за эффекты
                effect?.ValidateEnd(player, playerEffect);
        }
    }

}
