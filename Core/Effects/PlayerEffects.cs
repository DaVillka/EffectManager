using Newtonsoft.Json;

namespace HardLife.Core.Effects
{
    public class PlayerEffects
    {
        private List<PlayerEffectData> playerEffectDatas = new List<PlayerEffectData>();
        public void Add(PlayerEffectData playerEffectData) { playerEffectDatas.Add(playerEffectData); }
        public void Destroy(PlayerEffectData playerEffectData)
        {
            if (playerEffectDatas.Contains(playerEffectData))
            {
                playerEffectData.CancelToken?.Cancel();
                playerEffectDatas.Remove(playerEffectData);
            }
        }
        public PlayerEffectData GetEffectDataFromId(int id)
        {
            return playerEffectDatas.Find(x => x.Id == id);
        }
        public PlayerEffectData GetEffectDataFromIndex(int index)
        {
            if (index >= 0 && index < playerEffectDatas.Count)
                return playerEffectDatas[index];
            return null;
        }
        public bool GetEffectDataFromIndex(int index, out PlayerEffectData playerEffectData)
        {
            bool status = false;
            if (index >= 0 && index < playerEffectDatas.Count)
            {
                playerEffectData = playerEffectDatas[index];
                status = true;
            }
            else playerEffectData = null;
            return status;
        }
        public void CancelAllEffect()
        {
            foreach (var item in playerEffectDatas) { item.IsForciblyCandel = true; item.CancelToken?.Cancel(); }
        }
        public string GetSerealizeEffects()
        {
            List<object> list = new List<object>();
            foreach (var item in playerEffectDatas)
                list.Add(new object[] { item.Id, item.Effect.Duration, item.LeftTime });
            return JsonConvert.SerializeObject(list);
        }

        public int GetEffectIndexFromData(PlayerEffectData effectData)
        {
            return playerEffectDatas.IndexOf(effectData);
        }
    }
}
