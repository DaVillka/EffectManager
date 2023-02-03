namespace HardLife.Core.Effects
{
    //Клас атрибутов с помощью которого идет
    //конфигурация самого ефекта
    public class EffectAttribute : Attribute, IEffectConfig
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public int Duration { get; set; } = -1;
        public uint UpdateRate { get; set; } = 1000;
        public bool IsDublicable { get; set; } = false;
        public bool IsCancelable { get; set; } = false;
        public bool IsUpdatable { get; set; } = true;
    }
}
