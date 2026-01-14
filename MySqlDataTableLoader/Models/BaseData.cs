namespace MySqlDataTableLoader.Models;

public abstract class BaseData
{
    protected abstract int GetKey();

    public virtual string GetKeyString()
    {
        return GetKey().ToString();
    }
}