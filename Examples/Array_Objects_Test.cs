using System;
using JsonSharp2;

class lego {
  public string OUCH = "OWW A LEGO!";
}

class MainClass {

  public string teststr = "Wow";
  public int number = 5;
  public int[] ary = new int[] { 1, 2, 3, 4, 5 };
  public lego oof = new lego();

  public static void Main (string[] args) {
    Console.WriteLine("Serialized output: ");
    Console.WriteLine(Json2.Stringify(new MainClass()));

    Console.WriteLine("Deserialized output: ");

    dynamic json = Json2.Parse("{ \"test\": [1,2,3,4,5], \"obj\": { \"inobj\": \"Im inside an object\" }  }");

    Console.WriteLine("Array Contents: ");
    foreach (string val in json.test) {
      Console.WriteLine(val);
    }

    Console.WriteLine();

    Console.WriteLine("Object property value: ");
    Console.WriteLine(json.obj.inobj);
  }
}
