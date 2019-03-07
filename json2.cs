using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Reflection.Emit;
using JsonSharp;

namespace JsonSharp2 {

  public class Template {

  }

  public class Json2 {

    internal static ModuleBuilder CreateModule(string name) {
      AssemblyName aName = new AssemblyName();
      aName.Name = name;

      AssemblyBuilder asm = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);

      return asm.DefineDynamicModule("JsonObjects");
    }

    internal static string RegM(string str, string patn) {
      Regex reg = new Regex(patn);

      return reg.Match(str).Groups[0].Value.Replace("'", "");
    }

    internal static string RegM(string str, string patn, int grp) {
      Regex reg = new Regex(patn);

      return reg.Match(str).Groups[grp].Value.Replace("'", "");
    }

    public static object ConvertJson(string js, out Type typ) {
      if (! String.IsNullOrEmpty(RegM(js, "^[0-9]+$"))) {
        typ = typeof(int);
        return int.Parse(js);
      }

      typ = typeof(string);
      return js;
    }

    internal static TypeBuilder StartType(string ClassName) {
      TypeBuilder builder = CreateModule("JsonAsm").DefineType(ClassName, TypeAttributes.Public);

      //object obj = Activator.CreateInstance(t, null, null);

      //FieldInfo info = t.GetField("str", 
      //      BindingFlags.Public | BindingFlags.Instance);

      //info.SetValue(obj, "Hello!");

      return builder;
    }

    internal static Type ParseObj(TypeBuilder objtb, List<Node> nl) {
        foreach (Node n in nl) {
          
          if (n.IsObj) {
            TypeBuilder nestedtb = StartType("OBJTYPE_" + n.Name);

            Type t = ParseObj(nestedtb, Json.GetList(n.Value));

            objtb.DefineField(n.Name, t, FieldAttributes.Public);
            continue;
          }

          if (n.IsAry) {
            objtb.DefineField(n.Name, typeof(string[]), FieldAttributes.Public);
            continue;
          }
          
          objtb.DefineField(n.Name, typeof(string), FieldAttributes.Public);
        }

        Type objt = objtb.CreateType();

        return objt;
    }

    internal static object CreateObj(Type objt, List<Node> nl) {
        object objinst = Activator.CreateInstance(objt, null, null);

        foreach (Node n2 in nl) {

          FieldInfo ofi = objt.GetField(n2.Name, BindingFlags.Public | BindingFlags.Instance);

          if (n2.IsObj) {
            object obj = Activator.CreateInstance(ofi.FieldType, null, null);

            obj = CreateObj(ofi.FieldType, n2.Value.ToJsonList());

            ofi.SetValue(objinst, obj);
            continue;
          }

          ofi.SetValue(objinst, n2.Value);
        }


        return objinst;
    }

    public static object Parse(string js) {
      TypeBuilder tb = StartType("JsonRoot"); // Make the root Type

      // Define Types
      foreach (Node n in js.ToJsonList()) {

        if (n.IsObj) {
          TypeBuilder objtb = StartType("OBJTYPE_" + n.Name);

          Type objt = ParseObj(objtb, n.Value.ToJsonList());
         
          tb.DefineField(n.Name, objt, FieldAttributes.Public);
          continue;
        }

        if (n.IsAry) {
          tb.DefineField(n.Name, typeof(string[]), FieldAttributes.Public);
          continue;
        }

        tb.DefineField(n.Name, typeof(string), FieldAttributes.Public);
      }

      // Create the type
      Type t = tb.CreateType();

      // Instantiate
      object obj = Activator.CreateInstance(t, null, null);

      foreach (Node n in js.ToJsonList()) {

        if (n.IsObj) {

          // Get the object field
          FieldInfo of = t.GetField(n.Name, BindingFlags.Public | BindingFlags.Instance);

          object o = CreateObj(of.FieldType, Json.GetList(n.Value));

          // Set it and continue
          t.GetField(n.Name, BindingFlags.Public | BindingFlags.Instance).SetValue(obj, o);
          continue;
        }

        // Add an array
        if (n.IsAry) {
          List<string> ary = new List<string>();

          foreach (string val in n.AryVal) {
            ary.Add(val);
          }

          t.GetField(n.Name, BindingFlags.Public | BindingFlags.Instance).SetValue(obj, ary.ToArray());
          continue;
        }

        t.GetField(n.Name, BindingFlags.Public | BindingFlags.Instance).SetValue(obj, n.Value);
      }

      return obj;
    }

    #region Serialization

    internal static string PrimitiveConvert(object o) {
      if (o is string) {
        return '"' + (string) o + '"';
      }

      if (o is int) {
        return o.ToString();
      }

      return Stringify(o);
    }

    internal static string ConvertField(object obj, FieldInfo f) {
      if (f.FieldType == typeof(string)) {
        return '"' + (string) f.GetValue(obj) + '"';
      }

      if (f.FieldType == typeof(int)) {
        return f.GetValue(obj).ToString();
      }

      if (f.FieldType.IsArray) {
        string str = "[";
        int ind = 0;
        Array ary = f.GetValue(obj) as Array;

        foreach (object o in ary) {
          ind++;
          str += PrimitiveConvert(o);
          
          if (ind != ary.Length) {
            str += ", ";
            continue;
          }
        }

        str += "]";
        return str;
      }
      
      return Stringify(f.GetValue(obj));
    }

    public static string Stringify(object obj) {
      FieldInfo[] fields = obj.GetType().GetFields();

      string json = "{";
      int ind = 0;

      foreach (FieldInfo f in fields) {
        ind++;
        json += $" \"{f.Name}\": ";
        json += ConvertField(obj, f);

        if (ind != fields.Length) {
          json += ", ";
          continue;
        }
      }

      json += " }";
      return json;
    }

    #endregion Serialization

    // C# 3.9 and lower
    public static object GetField(object root, string name) {
      FieldInfo fi = root.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);

      if (fi == null) {
        throw new Exception("Object does not have property '" + name + "'");
      }

      return fi.GetValue(root);
    }

    // C# 3.9 and lower
    public static dynamic GetField(object root, params string[] names) {
      FieldInfo fi = root.GetType().GetField(names[0], BindingFlags.Public | BindingFlags.Instance);

      if (fi == null) {
        throw new Exception("Object does not have property '" + names[0] + "'");
      }

      object last = root;

      foreach (string s in names) {
        if (s == null) continue;
        
        FieldInfo ofi = last.GetType().GetField(s, BindingFlags.Public | BindingFlags.Instance);
        
        last = ofi.GetValue(last);

        
      }

      return last;
    }

  }

}