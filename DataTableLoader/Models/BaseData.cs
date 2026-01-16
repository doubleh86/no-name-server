namespace DataTableLoader.Models;

public abstract class BaseData
{
    public static long GetCombineKey(long firstKey, long secondKey) => (firstKey << 32) | (secondKey & 0xFFFFFFFFL);

    protected abstract long GetKey();

    public virtual long GetKeyLong()
    {
        return GetKey();
    }
}