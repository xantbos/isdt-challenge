namespace ISDTApp.Application.UnitTests
{
    [TestClass]
    public class IDSTApp_Test
    {
        private readonly ISDTApp.Application.Parser _IDSTApp;

        public IDSTApp_Test()
        {
            _IDSTApp = new ISDTApp.Application.Parser();
        }

        private string Get_Local_Asset_Directory_FilePath(string filename)
        {
            // instead of needing to include this in every test using known quantity asset files, split it into a function
            var projectFolder = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            return Path.Combine(projectFolder, $@"assets\{filename}");
        }

        [TestMethod]
        public void BadPath_Send_No_File_String()
        {
            var result = _IDSTApp.DoWork(@"C:\Fake\Folder\Path\To\File.csv") == 0;

            Assert.IsFalse(result, "File should not exist.");
        }

        [TestMethod]
        public void BadPath_Send_Malformed_String()
        {
            var result = _IDSTApp.DoWork(@"ABC:Fake\Folder\Path\To\~<{%@#$#@$.csv") == 0;

            Assert.IsFalse(result, "Filepath cannot exist.");
        }
        [TestMethod]
        public void ValidPath_Send_Known_Malformed_File()
        {
            var result = _IDSTApp.DoWork(Get_Local_Asset_Directory_FilePath("MachineStateLog_badalarm.csv")) == 0;

            Assert.IsFalse(result, "File should exist and alarm code should be invalid.");
        }
        [TestMethod]
        public void ValidPath_Send_Known_Bad_AlarmCode()
        {
            var result = _IDSTApp.DoWork(Get_Local_Asset_Directory_FilePath("MachineStateLog_malformed.csv")) == 0;

            Assert.IsFalse(result, "File should exist and data should be malformed.");
        }
        [TestMethod]
        public void ValidPath_Send_Known_Quantity()
        {
            var result = _IDSTApp.DoWork(Get_Local_Asset_Directory_FilePath("MachineStateLog.csv")) == 0;

            Assert.IsTrue(result, "File should exist and data should be valid.");
        }
    }
}