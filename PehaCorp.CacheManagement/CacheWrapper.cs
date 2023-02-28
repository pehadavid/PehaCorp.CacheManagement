namespace PehaCorp.CacheManagement
{
    public class CacheWrapper<T>
    {
        public CacheWrapper()
        {
            
        }

        public CacheWrapper(T value)
        {
            this.Value = value;
        }
        public T Value { get; set; }
        
    }
}