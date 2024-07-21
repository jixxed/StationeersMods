using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace StationeersMods.Plugin.Tests
{
    [TestClass]
    public class CustomModAboutTests
    {
        
        [TestMethod]
        public void TestCustomModAboutSerialization()
        {
            try
            {
                // Arrange
                var aboutData = WorkshopMenuPatch.Deserialize( "..\\..\\Resources\\text1.xml", "ModMetadata");

                // Act
                string fileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".xml";
                WorkshopMenuPatch.SaveXml(aboutData, fileName);
                // Assert
                StringReader reader1 = new StringReader(File.ReadAllText("..\\..\\Resources\\text1.xml"));
                StringReader reader2 = new StringReader(File.ReadAllText(fileName));
                StringAssert.Contains(reader2.ToString(), reader1.ToString());
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }
        

    }
}
