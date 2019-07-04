using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class QTableRecord 
{
    private string filePath;
    private const string fileName = "QTable";
    private const string EXTENSION = ".csv";
    private CSVTable _table;
    int stateSize;
    int actionSize;
    //使用构造函数完成加载csv文件的功能
    public QTableRecord(int sS,int aS) {
        filePath = Application.dataPath + "/RDW Toolkit/Resources/QTable/";
        stateSize = sS;
        actionSize = aS;
    }
    /// <summary>
    /// 如果训练过程中需要记录数据的话，传递的字符串应为"QTable"，
    /// 否则传递的字符串名应为"QTableyyyyMMdd"
    /// </summary>
    public void Load(string QTableName)
    {
        if (!Directory.Exists(filePath))
        {
            Debug.LogError("The file not be found in this path. path:" + filePath);
            return;
        }
        string fullFileName = filePath + QTableName + EXTENSION;
        StreamReader sr;
        sr = File.OpenText(fullFileName);
        string content = sr.ReadToEnd();
        sr.Close();
        sr.Dispose();
        _table = CSVTable.CreateTable(fileName, content);
    }

    /// <summary>
    /// Save方法生成QTableyyyyMMdd形式csv文件，调用该方法后不应再使用类对象
    /// </summary>
    public void Save()
    {
        if (_table == null)
        {
            Debug.LogError("The table is null.");
            return;
        }
        string tableContent = _table.GetContent();

            if (!Directory.Exists(filePath))
            {
                Debug.Log("未找到路径, 已自动创建");
                Directory.CreateDirectory(filePath);
            }
            //保存csv名称为QTableyyyyMMdd.csv
            string fullFileName = filePath + fileName + System.DateTime.Now.ToString("yyyyMMdd") + EXTENSION;

            StreamWriter sw;
            sw = File.CreateText(fullFileName);
            sw.Write(tableContent);
            sw.Close();
            sw.Dispose();

            _table = null;
     }

    public void AddData(int state, int action)
    {
        CSVDataObject data = new CSVDataObject(state.ToString(),
            new Dictionary<string, string>()
            {
                { "action",action.ToString() },
            },
            new string[] { "state", "action"});
            _table[data.ID] = data;
    }

    public float[] DecodeQTable()
    {
        float[] qtable = new float[stateSize];
        for (int i = 0; i < stateSize; ++i) qtable[i] = Convert.ToSingle(_table[i.ToString()]["action"]);        
        return qtable;
    }

}


public class CSVDataObject : IEnumerable
{
    /// <summary>
    /// 此值作为数据对象的唯一标识，只能通过此属性获取到唯一标识
    /// 无法通过 '数据对象[主键名]' 的方式来获取
    /// </summary>
    public string ID { get { return _major; } }
    private readonly string _major;

    /// <summary>
    /// 一条数据应包含的所有的键名
    /// </summary>
    public string[] AllKeys { get { return _allKeys; } }
    private readonly string[] _allKeys;

    private Dictionary<string, string> _atrributesDic;

    /// <summary>
    /// 初始化，获取唯一标识与除主键之外所有属性的键与值
    /// </summary>
    /// <param name="major"> 唯一标识，主键 </param>
    /// <param name="atrributeDic"> 除主键值外的所有属性键值字典 </param>
    public CSVDataObject(string major, Dictionary<string, string> atrributeDic, string[] allKeys)
    {
        _major = major;
        _atrributesDic = atrributeDic;
        _allKeys = allKeys;
    }

    /// <summary>
    /// 获取数据对象的签名，用于比较是否与数据表的签名一致
    /// </summary>
    /// <returns> 数据对象的签名 </returns>
    public string GetFormat()
    {
        string format = string.Empty;
        foreach (string key in _allKeys)
        {
            format += (key + "-");
        }
        return format;
    }

    public string this[string key]
    {
        get { return GetValue(key); }
        set { SetKey(key, value); }
    }

    private void SetKey(string key, string value)
    {
        if (_atrributesDic.ContainsKey(key))
            _atrributesDic[key] = value;
        else
            Debug.LogError("The data not include the key.");
    }

    private string GetValue(string key)
    {
        string value = string.Empty;

        if (_atrributesDic.ContainsKey(key))
            value = _atrributesDic[key];
        else
            Debug.LogError("The data not include value of this key.");

        return value;
    }

    public override string ToString()
    {
        string content = string.Empty;

        if (_atrributesDic != null)
        {
            foreach (var item in _atrributesDic)
            {
                content += (item.Key + ": " + item.Value + ".  ");
            }
        }
        return content;
    }

    public IEnumerator GetEnumerator()
    {
        foreach (var item in _atrributesDic)
        {
            yield return item;
        }
    }
}

public class CSVTable : IEnumerable
{
    /// <summary>
    /// 获取表名
    /// </summary>
    public string Name { get { return _name; } }
    private string _name;

    /// <summary>
    /// 获取表中的所有属性键
    /// </summary>
    public List<string> AtrributeKeys { get { return _atrributeKeys; } }
    private List<string> _atrributeKeys;

    /// <summary>
    /// 存储表中所有数据对象
    /// </summary>
    private Dictionary<string, CSVDataObject> _dataObjDic;

    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="tableName"> 表名 </param>
    public CSVTable(string tableName, string[] attributeKeys)
    {
        _name = tableName;

        // init 
        _atrributeKeys = new List<string>(attributeKeys);
        _dataObjDic = new Dictionary<string, CSVDataObject>();
    }

    /// <summary>
    /// 获取数据表对象的签名，用于比较是否与数据对象的签名一致
    /// </summary>
    /// <returns> 数据表对象的签名 </returns>
    public string GetFormat()
    {
        string format = string.Empty;
        foreach (string key in _atrributeKeys)
        {
            format += (key + "-");
        }
        return format;
    }

    /// <summary>
    /// 提供类似于键值对的访问方式便捷获取和设置数据对象
    /// </summary>
    /// <param name="key"> 数据对象主键 </param>
    /// <returns> 数据对象 </returns>
    public CSVDataObject this[string dataMajorKey]
    {
        get { return GetDataObject(dataMajorKey); }
        set { AddDataObject(dataMajorKey, value); }
    }

    /// <summary>
    /// 添加数据对象, 并将数据对象主键添加到主键集合中
    /// </summary>
    /// <param name="dataMajorKey"> 数据对象主键 </param>
    /// <param name="value"> 数据对象 </param>
    private void AddDataObject(string dataMajorKey, CSVDataObject value)
    {
        if (dataMajorKey != value.ID)
        {
            Debug.LogError("所设对象的主键值与给定主键值不同！设置失败！");
            return;
        }

        if (value.GetFormat() != GetFormat())
        {
            Debug.LogError("所设对象的的签名与表的签名不同！设置失败！");
            return;
        }

        if (_dataObjDic.ContainsKey(dataMajorKey))
        {
            Debug.LogError("表中已经存在主键为 '" + dataMajorKey + "' 的对象！设置失败！");
            return;
        }

        _dataObjDic.Add(dataMajorKey, value);
    }

    /// <summary>
    /// 通过数据对象主键获取数据对象
    /// </summary>
    /// <param name="dataMajorKey"> 数据对象主键 </param>
    /// <returns> 数据对象 </returns>
    private CSVDataObject GetDataObject(string dataMajorKey)
    {
        CSVDataObject data = null;

        if (_dataObjDic.ContainsKey(dataMajorKey))
            data = _dataObjDic[dataMajorKey];
        else
            Debug.LogError("The table not include data of this key.");

        return data;
    }

    /// <summary>
    /// 根据数据对象主键删除对应数据对象
    /// </summary>
    /// <param name="dataMajorKey"> 数据对象主键 </param>
    public void DeleteDataObject(string dataMajorKey)
    {
        if (_dataObjDic.ContainsKey(dataMajorKey))
            _dataObjDic.Remove(dataMajorKey);
        else
            Debug.LogError("The table not include the key.");
    }

    /// <summary>
    /// 删除所有所有数据对象
    /// </summary>
    public void DeleteAllDataObject()
    {
        _dataObjDic.Clear();
    }

    /// <summary>
    /// 获取数据表对象的文本内容
    /// </summary>
    /// <returns> 数据表文本内容 </returns>
    public string GetContent()
    {
        string content = string.Empty;

        foreach (string key in _atrributeKeys)
        {
            content += (key + ",").Trim();
        }
        content = content.Remove(content.Length - 1);

        if (_dataObjDic.Count == 0)
        {
            Debug.LogWarning("The table is empty, fuction named 'GetContent()' will just retrun key's list.");
            return content;
        }

        foreach (CSVDataObject data in _dataObjDic.Values)
        {
            content += "\n" + data.ID + ",";
            foreach (KeyValuePair<string, string> item in data)
            {
                content += (item.Value + ",").Trim();
            }
            content = content.Remove(content.Length - 1);
        }

        return content;
    }

    /// <summary>
    /// 迭代表中所有数据对象
    /// </summary>
    /// <returns> 数据对象 </returns>
    public IEnumerator GetEnumerator()
    {
        if (_dataObjDic == null)
        {
            Debug.LogWarning("The table is empty.");
            yield break;
        }

        foreach (var data in _dataObjDic.Values)
        {
            yield return data;
        }
    }

    /// <summary>
    /// 获得数据表内容
    /// </summary>
    /// <returns> 数据表内容 </returns>
    public override string ToString()
    {
        string content = string.Empty;

        foreach (var data in _dataObjDic.Values)
        {
            content += data.ToString() + "\n";
        }

        return content;
    }

    /// <summary>
    /// 通过数据表名字和数据表文本内容构造一个数据表对象
    /// </summary>
    /// <param name="tableName"> 数据表名字 </param>
    /// <param name="tableContent"> 数据表文本内容 </param>
    /// <returns> 数据表对象 </returns>
    public static CSVTable CreateTable(string tableName, string tableContent)
    {
        string content = tableContent.Replace("\r", "");
        string[] lines = content.Split('\n');
        if (lines.Length < 2)
        {
            Debug.LogError("The csv file is not csv table format.");
            return null;
        }

        string keyLine = lines[0];
        string[] keys = keyLine.Split(',');
        CSVTable table = new CSVTable(tableName, keys);

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');
            string major = values[0].Trim();
            Dictionary<string, string> tempAttributeDic = new Dictionary<string, string>();
            for (int j = 1; j < values.Length; j++)
            {
                string key = keys[j].Trim();
                string value = values[j].Trim();
                tempAttributeDic.Add(key, value);
            }
            CSVDataObject dataObj = new CSVDataObject(major, tempAttributeDic, keys);
            table[dataObj.ID] = dataObj;
        }

        return table;
    }
}



