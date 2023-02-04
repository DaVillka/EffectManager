using GTANetworkAPI;
using HardLife.Instances;
using System.Reflection;
using Task = System.Threading.Tasks.Task;

namespace HardLife.Core.Effects
{
    //Основной класс еффекта, отвечает за
    //манипуляции с еффектом
    public abstract class Effect
    {
        public int Id { get => effectAttribute.Id; }
        public string Name { get => effectAttribute.Name; }
        public int Duration { get => effectAttribute.Duration; }
        private EffectAttribute effectAttribute = null;
        private const int _clientValidation = 2;
        public Effect()
        {
            effectAttribute = (EffectAttribute)GetType().GetCustomAttribute(typeof(EffectAttribute), false);
            if (effectAttribute == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR EFFECT] '{GetType().Name}' does not inherit attribute.");
                Console.ResetColor();
                return;
            }
            if(effectAttribute.IsDublicable && effectAttribute.IsUpdatable)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR EFFECT] '{GetType().Name}' IsDublicable and IsUpdatable shouldn't be `true` at the same time.");
                Console.ResetColor();
                return;
            }

            EffectManager.Instance.AddEffect(this);
        }
        //Перегрезка lastLeftTime отвечает за установку конкретного времени до конца выполнения, -1 значит не используется
        public async Task ValidateSet(Player player, PlayerEffects playerEffects, int lastLeftTime = -1)
        {
            PlayerEffectData effectData = playerEffects.GetEffectDataFromId(effectAttribute.Id);

            //IsUpdatable и IsDublicable не могут быть true одновременно, иначе этот код не будет выполнен вообще
            if (effectData != null) {
                if (effectAttribute.IsUpdatable)
                    effectData.CancelToken.CancelAfter(TimeSpan.FromMilliseconds(effectAttribute.Duration));
                if (effectAttribute.IsDublicable == false)
                    return;
            }
            //Проверяем, проходит ли по условиям сам эффект
            //на пример, нет смысла накладывать эффект на восстановление хп если хп полное
            if (!Set(player)) return;
            //Накладываем на игрока эффект
            PlayerEffectData playerEffectData = CreateEffect(playerEffects, (lastLeftTime == -1 ? effectAttribute.Duration : lastLeftTime));
            //при инициализации эффекта передаем его айди, общее время выполнения, и оставшееся время
            //player.TriggerEvent("effectSet", Id, Duration, playerEffectData.LeftTime);

            //Duration == -1 то таск не создается
            if (effectAttribute.Duration == -1) return;

            //Если Duration больше 0 то устанавливаем таймер на отмену
            if (effectAttribute.Duration > 0) playerEffectData.CancelToken.CancelAfter(TimeSpan.FromMilliseconds(playerEffectData.LeftTime));
            //иначе создаем таск
            await CreateUpdateTask(player, playerEffects, playerEffectData);
            //Если ивент завершился принудительно ничего не далем, инаце ↓
            if (!playerEffectData.IsForciblyCandel)
            {
                End(player);
                //сообщаем на клиент чт оивента завершился
                //playerTriggerEvent("effectEnd", Id, playerEffects.GetEffectIndexFromData(effectData));
            }
            //удаляем эффект из списка эффектов игрока
            playerEffects.Destroy(playerEffectData);
            
        }
        public void ValidateEnd(Player player, PlayerEffects playerEffects)
        {
            //Проверяем, можно ли удаленно отменить эффект
            if (!effectAttribute.IsCancelable) return;
            PlayerEffectData effectData = playerEffects.GetEffectDataFromId(effectAttribute.Id);
            //Если данный эффект наложен 
            if (effectData != null)
            {
                effectData.CancelToken.Cancel();
                //отправляем  инфу на клиент что такой то эффект завершился
                //player.TriggerEvent("effectCancel", playerEffects.GetEffectIndexFromData(effectData));
            }
        }
        public void ValidateUpdate(Player player, PlayerEffects playerEffects)
        {
            Console.WriteLine("Validation Effect");
            //PlayerEffectData effectData = playerEffects.GetEffectDataFromId(Id);
            //player.TriggerEvent("effectUpdate", playerEffects.GetEffectIndexFromData(effectData), effectData.LeftTime);
        }

        private PlayerEffectData CreateEffect(PlayerEffects playerEffects, int duration)
        {
            PlayerEffectData playerEffectData = new PlayerEffectData();
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            playerEffectData.Id = effectAttribute.Id;
            playerEffectData.Effect = this;
            playerEffectData.LeftTime = duration;
            playerEffectData.CancelToken = cancellationTokenSource;
            playerEffects.Add(playerEffectData);
            return playerEffectData;
        }
        private async Task CreateUpdateTask(Player player, PlayerEffects playerEffects, PlayerEffectData playerEffectData)
        {
            await Task.Run(() =>
            {
                try
                {
                    var dfrom = DateTime.Now;
                    while (!playerEffectData.CancelToken.IsCancellationRequested)
                    {
                        //каждые `_clientValidation` отправляем игроку данные о состоянии эффекта
                        //при условии что работает таск
                        //ВАЖНО: Работает синхронно с таском, время указывается в секундах
                        //нужно для того что бы валидация с клиентом не флудила ивентами
                        //если UpdateRate допустим 100 миллисек.
                        //но если UpdateRate больше _clientValidation то валидация будет происходить по UpdateRate
                        if ((DateTime.Now - dfrom).Seconds > _clientValidation)
                        {
                            ValidateUpdate(player, playerEffects);
                            dfrom = DateTime.Now;
                        }
                        if (!Update(player)) playerEffectData.CancelToken.Cancel();
                        playerEffectData.LeftTime -= (int)effectAttribute.UpdateRate;
                        //Task.Delay(TimeSpan.FromMilliseconds(effectAttribute.UpdateRate));
                        Thread.Sleep(TimeSpan.FromMilliseconds(effectAttribute.UpdateRate));
                    }
                }
                catch (Exception e) { Console.WriteLine(e); }
                
            }, playerEffectData.CancelToken.Token);
        }
        protected abstract bool Set(Player player);
        protected abstract bool Update(Player player);
        protected abstract void End(Player player);
    }
}
