using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;


namespace SingleResponsibilityPrinciple.Tests
{
    [TestClass()]
    public class TradeProcessorTests
    {
        private TradeProcessor tradeProcessor;

        [TestInitialize]
        public void Setup()
        {
            tradeProcessor = new TradeProcessor();
        }

        private int CountDbRecords()
        {
            string azureConnectString = @"Server=tcp:cis3285-sql-server.database.windows.net,1433; Initial Catalog = Unit8_TradesDatabase; Persist Security Info=False; User ID=cis3285;Password=Saints4SQL; MultipleActiveResultSets = False; Encrypt = True; TrustServerCertificate = False; Connection Timeout = 60;";
            string bamlakConnectionString = @"Data Source = (LocalDB)\MSSQLLocalDB; AttachDbFilename = ""C:\Users\Bamlak Amedie\Documents\tradedatabase.mdf""; Integrated Security = True; Connect Timeout = 30";
            // Change the connection string used to match the one you want
            using (var connection = new SqlConnection(bamlakConnectionString))
            {
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }
                string myScalarQuery = "SELECT COUNT(*) FROM trade";
                SqlCommand myCommand = new SqlCommand(myScalarQuery, connection);
                //myCommand.Connection.Open();
                int count = (int)myCommand.ExecuteScalar();
                connection.Close();
                return count;
            }
        }

        [TestMethod()]
        public void TestNormalFile()
        {
            //Arrange
            var tradeStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Unit8_SRP_F24Tests.goodtrades.txt");
            var tradeProcessor = new TradeProcessor();

            //Act
            int countBefore = CountDbRecords();
            tradeProcessor.ProcessTrades(tradeStream);
            //Assert
            int countAfter = CountDbRecords();
            Assert.AreEqual(countBefore + 4, countAfter);
        }

    [TestMethod()]
    public void ProcessTrades_SingleValidTrade_AddsOneTradeToDatabase()
    {
        using (var stream = new MemoryStream())
        using (var writer = new StreamWriter(stream))
        {
            writer.WriteLine("GBPUSD,1000,1.51");
            writer.Flush();
            stream.Position = 0;

            int countBefore = CountDbRecords();
            tradeProcessor.ProcessTrades(stream);
            int countAfter = CountDbRecords();

            Assert.AreEqual(countBefore + 1, countAfter);
        }
    }

    [TestMethod]
    public void ProcessTrades_MultipleValidTrades_AddsMultipleTradesToDatabase()
    {
        using (var stream = new MemoryStream())
        using (var writer = new StreamWriter(stream))
        {
            for (int i = 0; i < 10; i++)
            {
                writer.WriteLine("GBPUSD,1000,1.51");
            }
            writer.Flush();
            stream.Position = 0;

            int countBefore = CountDbRecords();
            tradeProcessor.ProcessTrades(stream);
            int countAfter = CountDbRecords();

            Assert.AreEqual(countBefore + 10, countAfter);
        }
    }

    [TestMethod]
    public void ProcessTrades_EmptyFile_AddsNoTradesToDatabase()
    {
        using (var stream = new MemoryStream())
        {
            int countBefore = CountDbRecords();
            tradeProcessor.ProcessTrades(stream);
            int countAfter = CountDbRecords();

            Assert.AreEqual(countBefore, countAfter);
        }
    }

    [TestMethod]
    [ExpectedException(typeof(FileNotFoundException))]
    public void ProcessTrades_NonexistentFile_ThrowsException()
    {
        using (var stream = new FileStream("nonexistentfile.txt", FileMode.Open))
        {
            tradeProcessor.ProcessTrades(stream);
        }
    }

    [TestMethod]
    public void ReadTradeData_WellFormedData_ReturnsAllLines()
    {
        using (var stream = new MemoryStream())
        using (var writer = new StreamWriter(stream))
        {
            writer.WriteLine("GBPUSD,1000,1.51");
            writer.WriteLine("EURUSD,2000,1.25");
            writer.Flush();
            stream.Position = 0;

            var lines = tradeProcessor.ReadTradeData(stream);
            Assert.AreEqual(2, lines.Count());
        }
    }

    [TestMethod]
    public void ReadTradeData_MalformedData_SkipsInvalidLines()
    {
        using (var stream = new MemoryStream())
        using (var writer = new StreamWriter(stream))
        {
            writer.WriteLine("GBPUSD,1000,1.51");  // Valid
            writer.WriteLine("EURUSD,2000");      // Invalid
            writer.WriteLine("USDJPY,1500,0.95"); // Valid
            writer.Flush();
            stream.Position = 0;

            var trades = tradeProcessor.ParseTrades(tradeProcessor.ReadTradeData(stream));
            Assert.AreEqual(2, trades.Count());
        }
    }
}
}
