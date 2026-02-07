namespace Definition.DataStruct
{
    public class StatProperty
    {
        public float Value;
        public float Percent;

        public StatProperty()
        {
            Value = 0f;
            Percent = 1f;
        }

        public StatProperty(float value, float percent)
        {
            Value = value;
            Percent = percent;
        }
    }
}