using System.Text.Json;
using DataTableLoader.Models;

namespace DataTableLoader.Utils;


public class DataDictionary<TData> where TData : BaseData
{
    private Dictionary<long, TData> _dictionary = new();
    public Dictionary<long, TData> Dictionary => _dictionary;

    public TData GetDataValue(long key)
    {
        return _dictionary.GetValueOrDefault(key, null);
    }

    public bool LoadDataFromDB(DataTableDbService service)
    {
        var dataList = service.LoadData<TData>();
        if (dataList == null)
            return false;
        
        // 데이터 자체가 없을 수 있다.
        if (dataList.Count == 0)
            return true;

        if (dataList.FirstOrDefault() is IPrepareLoad)
        {
            foreach (var prepareLoad in dataList.Select(data => data as IPrepareLoad))
            {
                prepareLoad?.PrepareLoad();
            }
        }
        
        _dictionary = dataList.ToDictionary(x => x.GetKeyLong());
        return true;
    }

    public bool LoadGameDataFromJsonFile(string tableName)
    {
        var filePath = $"./JsonGameData/{typeof(TData).Name}.json";
        using var r = new StreamReader(filePath);
        var jsonString = r.ReadToEnd();
        if (string.IsNullOrEmpty(jsonString) == true)
        {
            return false;
        }
            
        _dictionary = JsonSerializer.Deserialize<Dictionary<long, TData>>(jsonString);
        return true;
    }

    public List<TData> ToValueList()
    {
        return _dictionary.Values.ToList();
    }
}