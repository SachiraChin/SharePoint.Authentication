namespace SharePoint.Authentication.Caching
{
    public class BaseCacheExtension
    {
        public string MemoryGroup { get; }
        public BaseCacheExtension(string memoryGroup)
        {
            MemoryGroup = memoryGroup;
        }
        
        protected string GetKey(string key) => $"{MemoryGroup}-{key}";
    }
}
