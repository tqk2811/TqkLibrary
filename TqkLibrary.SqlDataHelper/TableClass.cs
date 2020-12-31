using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;

namespace TqkLibrary.SqlDataHelper
{
  public class TableClass
  {
    private List<KeyValuePair<string, Type>> _fieldInfo = new List<KeyValuePair<string, Type>>();
    private string _className = string.Empty;

    private Dictionary<Type, string> dataMapper
    {
      get
      {
        // Add the rest of your CLR Types to SQL Types mapping here
        Dictionary<Type, string> dataMapper = new Dictionary<Type, string>();
        dataMapper.Add(typeof(int), "BIGINT");
        dataMapper.Add(typeof(string), "NVARCHAR(500)");
        dataMapper.Add(typeof(bool), "BIT");
        dataMapper.Add(typeof(DateTime), "DATETIME");
        dataMapper.Add(typeof(float), "FLOAT");
        dataMapper.Add(typeof(decimal), "DECIMAL(18,0)");
        dataMapper.Add(typeof(Guid), "UNIQUEIDENTIFIER");

        return dataMapper;
      }
    }

    public List<KeyValuePair<string, Type>> Fields
    {
      get { return this._fieldInfo; }
      set { this._fieldInfo = value; }
    }

    public string ClassName
    {
      get { return this._className; }
      set { this._className = value; }
    }

    public TableClass(Type t)
    {
      this._className = t.Name;

      foreach (PropertyInfo p in t.GetProperties())
      {
        KeyValuePair<string, Type> field = new KeyValuePair<string, Type>(p.Name, p.PropertyType);

        this.Fields.Add(field);
      }
    }

    public string CreateTableScript()
    {
      StringBuilder script = new StringBuilder();

      script.AppendLine("CREATE TABLE " + this.ClassName);
      script.AppendLine("(");
      //script.AppendLine("\t ID BIGINT,");
      for (int i = 0; i < this.Fields.Count; i++)
      {
        KeyValuePair<string, Type> field = this.Fields[i];

        if (dataMapper.ContainsKey(field.Value))
        {
          script.Append("\t " + field.Key + " " + dataMapper[field.Value]);
        }
        else
        {
          // Complex Type?
          script.Append("\t " + field.Key + " BIGINT");
        }

        if (i != this.Fields.Count - 1)
        {
          script.Append(",");
        }

        script.Append(Environment.NewLine);
      }

      script.AppendLine(")");

      return script.ToString();
    }
  }

  public class TableClassCollection : Collection<TableClass>
  {
  }
}