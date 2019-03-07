using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace JsonSharp {

      public static class extensions {

        public static void Test(this string s) {
          Console.WriteLine(s);
        }

        public static Tree ToJsonTree(this string s) {
          return Json.GetTree(s);
        }

        public static List<Node> ToJsonList(this string s) {
          return Json.GetList(s);
        }
      }

      public class JObject {

      public string Name;

      public string VType;

      private object _Value;

      public bool IsObj;

      public object Value { get {
        return _Value;
      }
      set {
        _Value = value;
      }
     }

     public JObject(string N, object V, string type = "string", bool o = false) {
       Name = N;
       Value = V;
       VType = type;
       IsObj = o;
     }
  }

  public sealed class JArray {

    private List<string> values = new List<string>();

    public string this[int ind] {
      get {
        return values[ind];
      }
    }

    public IEnumerator<string> GetEnumerator() {
      return values.GetEnumerator();
    }

    public void Add(string val) {
      values.Add(val);
    }

    public static JArray FromArray (IEnumerable<string> ary) {
      JArray a = new JArray();

      foreach (string v in ary) {
        a.Add(v);
      }

      return a;
    }

  }

  public class Tree {

    List<Node> nodes = new List<Node>();

    public void Insert(Node n) {
      nodes.Add(n);
    }

    public Node FirstNode { get { return nodes[0]; } }

    public Node Get(string name) {
      foreach (Node n in nodes) {
    
      // Check if the child node has what we are looking for
      foreach (Node n2 in n.nodes) {
        if (n.Name == name) {
          return n2;
        }
      }

        if (n.Name == name) {
          return n;
        }
      }

      return null;
    }

  }

  public class Node { // ToDo: Add a Get function to get child nodes such as nested json

    public List<Node> nodes = new List<Node>();

    public JArray AryVal;

    public string Name;
    public string Value;
    public bool IsObj;
    public bool IsAry;
    public bool IsBool;

    public Node(Tree tr, string name, string value, bool o = false) {
      Name = name;
      Value = value;
      IsObj = o;
      tr.Insert(this);
    }

    public Node(Tree tr, string name, JArray value, bool o = false) {
      Name = name;
      AryVal = value;
      IsObj = o;
      IsAry = true;
        tr.Insert(this);
    }

    public Node(List<Node> tr, string name, JArray value, bool o = false) {
      Name = name;
      AryVal = value;
      IsAry = true;
      IsObj = o;
      tr.Add(this);
    }

    public Node(string name, JArray value, bool o = false) {
      Name = name;
      AryVal = value;
      IsAry = true;
      IsObj = o;
    }

    public Node(List<Node> tr, string name, string value, bool o = false) {
      Name = name;
      Value = value;
      IsObj = o;
      tr.Add(this);
    }

    public Node(string name, string value, bool o = false) {
      Name = name;
      Value = value;
      IsObj = o;
    }
  }

  public class Json {

#region Regex Stuff I Copied From My Other Projects Because Im Lazy
    private static string RegM(string str, string patn) {
      Regex reg = new Regex(patn);

      return reg.Match(str).Groups[0].Value.Replace("'", "");
    }

    private static string RegM(string str, string patn, int grp) {
      Regex reg = new Regex(patn);

      return reg.Match(str).Groups[grp].Value.Replace("'", "");
    }
#endregion

      // 
      private static List<JObject> Scan(string js) {

        if (! js.StartsWith("{")) {
          throw new System.Exception("Malformed Input");
        }

        List<JObject> result = new List<JObject>();

        string valcache = "";

        bool entry = false;

        bool qoute = false;

        bool sep = false;

        string leftsep = "";

        string objholder = "";

        string objstring = "";

        bool ignorenextqoute = false;

        int nestcount = 0;

        string id = "";

        bool array = false;

        bool closewithary = false;

        string inary = "";

        bool skip = false;

        int objnest = 0;

        foreach (char c in js) {
          if (nestcount < 0) nestcount = 0;

          // Skip spaces, newlines, and anything escaped
          if (c == ' ' || c == '\n' || skip || c == '\t' || c == '\r') {
            if (qoute)
              valcache += c;
            if (ignorenextqoute)
              objstring += c;
            skip = false;
            continue;
          }

          // Escape the next character
          if (c == '\\') {
            skip = ! skip;
            continue;
          }

          if (c == '[' && nestcount == 0 && ! ignorenextqoute) {
            if (! entry) {
              entry = true;
              closewithary = true;
            }

            array = true;
            inary = "";
            nestcount++;
            continue;
          }

          if (c == ']') {
            nestcount--;
            array = false;

            JArray ary = new JArray();
            
            string gear = "";

            int nc = 0;

            foreach (char ch in inary) {
              if (ch == '{') {
                nc++;
              }

              if (ch == '}') {
                nc--;
              }

              if (ch == ',' && nc == 0) {
                ary.Add(gear.Trim('"').Trim('\''));
                gear = "";
                continue;
              }

               gear += ch;
            }

            // Leftovers
            ary.Add(gear.Trim('"').Trim('\''));

            result.Add(new JObject(leftsep, ary, "array"));

            if (closewithary && nestcount == 0) {
              entry = false;
            }

          }

          if (array) {
            inary += c;
            continue;
          }


          // There is a start bracket
          if (c == '{') {
            
            
            if (entry) {

              if (nestcount >= 1)
                  objnest++;

              ignorenextqoute = true;
              objstring += '{';
              objholder = leftsep;
              nestcount++;
              continue;
            }
            
            entry = true;
            continue;
          }

          if (c == '}') {

            if (nestcount > 0)
              nestcount--;

            if (ignorenextqoute && objnest == 0) {
              ignorenextqoute = false;
              objstring += '}';
              result.Add(new JObject(objholder, objstring, "string", true));
              continue;
            }

            if (ignorenextqoute && objnest > 0) {
              objnest--;
              objstring += '}';
              continue;
            }

            if (id == "null") {
              result.Add(new JObject(leftsep, id));
            }

            if (id == "true") {
              result.Add(new JObject(leftsep, id));
            }
            else if (id == "false") {
              result.Add(new JObject(leftsep, id));
            }

            string m = RegM(id, "[0-9]+");

            if (! String.IsNullOrEmpty(m)) {
              result.Add(new JObject(leftsep, id));
            }
            
            if (nestcount != 0) continue;

            entry = false;

            continue;
          }

          if (ignorenextqoute) {
            objstring += c;
            continue;
          }

          if (c == ',' && ! qoute) {

            if (id == "null") {
              result.Add(new JObject(leftsep, id));
            }

            if (id == "true") {
              result.Add(new JObject(leftsep, id));
            }
            else if (id == "false") {
              result.Add(new JObject(leftsep, id));
            }

            string m = RegM(id, "[0-9]+");

            if (! String.IsNullOrEmpty(m)) {
              result.Add(new JObject(leftsep, id));
            }

            sep = false;
            qoute = false;
            leftsep = "";
            valcache = "";
            objholder = "";
            objstring = "";
            id = "";
            continue;
          }

          if (c == ':' && ! qoute) {
            sep = true;
            continue;
          }

          if (c == '"' || c == '\'') {
            qoute = ! qoute;

            if (! qoute) {
              if (! sep) {
                leftsep = valcache;
                valcache = "";
                continue;
              }

              result.Add(new JObject(leftsep, valcache));

              valcache = "";
              sep = false;
            }

            continue;
          }

          if (qoute)
            valcache += c;
          if (! qoute )
            id += c;

        }

        if (nestcount - 1 > 0) {
          Console.WriteLine(nestcount);
          throw new Exception("Unclosed {");
        }

        if (qoute) {
          throw new Exception("Unclosed \"");
        }
        
        return result;

      }

      // Make a dictionary of json
      public static List<Node> GetList(string js) {
        List<Node> tr = new List<Node>();

        List<JObject> objs = Scan(js);

        foreach (JObject obj in objs) {

          // Send it off as an object
          if (obj.IsObj) {
            new Node(tr, obj.Name, obj.Value.ToString(), true);
            continue;
          }

          if (obj.VType == "array") {
            new Node(tr, obj.Name, (JArray) obj.Value, false);
            continue;
          }

          new Node(tr, obj.Name, obj.Value.ToString());
        }

        return tr;
      }

      // Make a tree of json
      public static Tree GetTree(string js) {
        Tree tr = new Tree();

        List<JObject> objs = Scan(js);

        foreach (JObject obj in objs) {

          // Send it off as an object
          if (obj.IsObj) {
            Console.Write(obj.Name + "|");
            new Node(tr, obj.Name, obj.Value.ToString(), true);
            continue;
          }

          if (obj.VType == "array") {
            new Node(tr, obj.Name, (JArray) obj.Value);
            continue;
          }

          new Node(tr, obj.Name, obj.Value.ToString());
        }

        return tr;
      }

      internal static bool IsPrim(string val) {
        if (val == "true" || val == "false" || val == "null" || RegM(val, "[0-9]+") != "") {
          return true;
        }

        return false;
      }

      public static string ToString (List<Node> ary) {

        string json = "{ ";

        int elements = 0;

        foreach (Node n in ary) {
          
          if (elements > 0) {
            json += ", ";
          }

          if (n.IsObj) {
            json += $"\"{n.Name}\": {n.Value} ";
            continue;
          }

          if (n.IsAry) {
            json += $"\"{n.Name}\": [ ";
            int aryc = 0;
            foreach (string s in n.AryVal) {
              if (aryc > 0) {
                json += ", " + s;
                aryc++;
                continue;
              }

              json += s;
              aryc++;
            }

            json += " ]";
            continue;
          }

          if (IsPrim(n.Value)) {
            json += ('"' + n.Name + '"') + (": " + n.Value);
            elements++;
            continue;
          }

          elements++;
          json += ('"' + n.Name + '"') + (": " + '"' + n.Value + '"');
        }

        return json + " }";
      }

    }

}