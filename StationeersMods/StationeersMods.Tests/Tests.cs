using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Assets.Scripts.Serialization;
using NUnit.Framework;
using StationeersMods.Plugin;

namespace StationeersMods.Tests
{
  public class CustomModAboutTest
  {
    [Test]
    public void Test1()
    {
      var customModAbout = new CustomModAbout();
      customModAbout._inGameDescription = "<size=\"30\">";
      

      var tempPath = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".xml";
      var fileStream = File.Create(tempPath);
      fileStream.Close();
      Serialize(customModAbout, tempPath);
      //XmlSerializer;
      foreach (var line in  File.ReadAllLines(tempPath))
      {
        //Console.Out.Write(line); 
        
      }

      CustomModAbout test = Deserialize<CustomModAbout>(tempPath, "ModMetadata");
      test.WorkshopHandle = 123;
      Serialize(test, tempPath);
      foreach (var line in  File.ReadAllLines(tempPath))
      {
        //Console.Out.Write(line); 
        
      }
      Console.Out.Write( test.InGameDescription.Value); 
      Console.Out.Write( test.InGameDescription.Data); 

     


    }
    public static T Deserialize<T>(string path, string root = "")
    {
      try
      {
        if (!File.Exists(path))
          throw new FileNotFoundException("File not found at path: " + path);
        using (StreamReader streamReader = new StreamReader(path))
          return (T) (string.IsNullOrEmpty(root) ? new XmlSerializer(typeof (T)) : new XmlSerializer(typeof (T), new XmlRootAttribute(root))).Deserialize((TextReader) streamReader);
      }
      catch (Exception ex)
      {
        Console.Out.Write(ex);
        return default (T);
      }
    }
    public static bool Serialize<T>(T obj, string path)
    {
      try
      {
        using (StreamWriter streamWriter = new StreamWriter(path))
        {
          new XmlSerializer(typeof (T)).Serialize((TextWriter) streamWriter, (object) obj);
          return true;
        }
      }
      catch (Exception ex)
      {
        Console.Out.Write(ex);
        return false;
      }
    }
  }
}