using GoodMorningGFN.Logging;
using Microsoft.Extensions.Logging;

namespace GoodMorningGFN.Tests;

public class SimpleFileLoggerProviderTests
{
    [Fact]
    public void Erzeugt_Logverzeichnis_und_schreibt_Log_Datei()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GoodMorningGFN-LoggerTest-" + Guid.NewGuid());

        try
        {
            using (var provider = new SimpleFileLoggerProvider(tempDir))
            {
                var logger = provider.CreateLogger("TestKategorie");
                logger.LogInformation("Testnachricht {Wert}", 42);
            }

            Assert.True(Directory.Exists(tempDir));
            string[] logFiles = Directory.GetFiles(tempDir, "*.log");
            Assert.Single(logFiles);

            string inhalt = File.ReadAllText(logFiles[0]);
            Assert.Contains("Testnachricht 42", inhalt);
            Assert.Contains("TestKategorie", inhalt);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
