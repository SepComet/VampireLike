namespace Entity.EntityData
{
    public class CoinData : EntityDataBase
    {
        private int _value;

        public CoinData(int value, int entityId, int typeId) : base(entityId, typeId)
        {
            _value = value;
        }

        public int Value
        {
            get => _value;
            set => _value = value;
        }
    }
}