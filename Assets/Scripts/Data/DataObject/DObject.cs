namespace SA
{
    public abstract class DObject
    {
        private static long _currentInstnaceId = 0;
        private long _instanceId = 0;
        public long InstanceId
        {
            get
            {
                return _instanceId;
            }
        }

        public DObject()
        {
            _instanceId = ++_currentInstnaceId;
        }
    }
}