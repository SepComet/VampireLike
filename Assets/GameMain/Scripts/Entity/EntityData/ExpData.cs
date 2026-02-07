namespace Entity.EntityData
{
    public class ExpData : EntityDataBase
    {
        private int _value;

        public ExpData(int value, int entityId, int typeId) : base(entityId, typeId)
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