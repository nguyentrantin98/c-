namespace c_.Ext
{
    public static class Ext
    {
        public static object GetPropValue(this object obj, string propName)
        {
            return obj?.GetType().GetProperty(propName)?.GetValue(obj);
        }
    }
}
