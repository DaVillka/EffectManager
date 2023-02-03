using GTANetworkAPI;
using HardLife.Instances;
using System.Reflection;
using Task = System.Threading.Tasks.Task;

namespace HardLife.Core.Effects
{
    //Основной класс еффекта, отвечает за
    //манипуляции с еффектом
    public abstract class Effect : IEffectConfig
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public int Duration { get; set; } = -1;
        public uint UpdateRate { get; set; } = 1000;
        public bool IsDublicable { get; set; } = false;
        public bool IsCancelable { get; set; } = false;
        public bool IsUpdatable { get; set; } = true;

        private const int _clientValidation = 2;
        public Effect()
        {
            var attr = (EffectAttribute)GetType().GetCustomAttribute(typeof(EffectAttribute), false);
            if (attr == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR EFFECT] '{GetType().Name}' does not inherit attribute.");
                Console.ResetColor();
                return;
            }
            Id = attr.Id;
            Name = attr.Name;
            Duration = attr.Duration;
            UpdateRate = attr.UpdateRate;
            IsDublicable = attr.IsDublicable;
            IsCancelable = attr.IsCancelable;
            IsUpdatable = attr.IsUpdatable;

            EffectManager.Instance.AddEffect(this);
        }
        //Перегрезка lastLeftTime отвечает за установку конкретного времени до конца выполнения, -1 значит не используется
        public async Task ValidateSet(Player player, PlayerEffects playerEffects, int lastLeftTime = -1)
        {
            //Первым делом мы проверяем наложен ли данный эффект на игрока
            PlayerEffectData effectData = playerEffects.GetEffectDataFromId(Id);
            //эффект есть
            if (effectData != null && !IsDublicable)
            {
                if (IsUpdatable)
                    effectData.CancelToken.CancelAfter(TimeSpan.FromMilliseconds(Duration));
            }
            //эффекта нет или он может дублироваться, накладываем новый
            else
            {
                //Проверяем, проходит ли по условиям сам эффект
                //на пример, нет смысла накладывать эффект на восстановление хп если хп полное
                if (!Set(player)) return;
                PlayerEffectData playerEffectData = new PlayerEffectData();
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                playerEffectData.Id = Id;
                playerEffectData.Effect = this;
                playerEffectData.LeftTime = lastLeftTime == -1 ? Duration : lastLeftTime;
                playerEffectData.CancelToken = cancellationTokenSource;
                playerEffects.Add(playerEffectData);
                //Если Duration больше 0 то устанавливаем таймер на отмену
                if (Duration > 0) cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(playerEffectData.LeftTime));
                //Если Duration меньше 0, то еффект добавляется, но таск не накладывается
                //Можно использовать в каких то пассивных эффектах
                //которые что то дают при активации
                if (Duration < 0) return;
                //Если Duration равно 0 то таск работает бессконечно, пока не отменишь
                await Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        var dfrom = DateTime.Now;
                        while (!cancellationTokenSource.IsCancellationRequested)
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
                            if (!Update(player)) cancellationTokenSource.Cancel();
                            playerEffectData.LeftTime -= (int)UpdateRate;
                            await Task.Delay(TimeSpan.FromMilliseconds(UpdateRate));
                        }
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
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }, cancellationTokenSource.Token);
            }
            //при инициализации эффекта передаем его айди, общее время выполнения, и оставшееся время
            //player.TriggerEvent("effectSet", Id, Duration, playerEffectData.LeftTime);
        }
        public void ValidateEnd(Player player, PlayerEffects playerEffects)
        {
            //Проверяем, можно ли удаленно отменить эффект
            if (!IsCancelable) return;
            PlayerEffectData effectData = playerEffects.GetEffectDataFromId(Id);
            //Если данный эффект наложен 
            if (effectData != null)
            {
                effectData.CancelToken.Cancel();
                End(player);
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
        public abstract bool Set(Player player);
        public abstract bool Update(Player player);
        public abstract void End(Player player);
    }
}
