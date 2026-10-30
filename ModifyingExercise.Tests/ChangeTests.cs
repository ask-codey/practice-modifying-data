using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace ModifyingExercise.Tests;

// Runs your .sql files against your own Northwind database, in order, and checks the data they leave behind.
// Nothing is changed: everything runs inside one transaction that is rolled back. It starts from Northwind
// as it was (the rows you added in SSMS are set aside inside the transaction, and come back afterwards),
// so each test runs problem 1 up to its own problem, as the problems build on each other.
// You don't need to change this file.
[TestClass]
public sealed class ChangeTests
{
    // The NORTHWIND_CONNECTION environment variable, if set, points the tests at another database.
    private static readonly string connectionString =
        Environment.GetEnvironmentVariable("NORTHWIND_CONNECTION")
        ?? "Server=localhost;Database=Northwind;Integrated Security=True;TrustServerCertificate=True";

    private static readonly string[] files =
    [
        "01_Insert_Category.sql", "02_Find_Category_Id.sql", "03_Insert_Products.sql", "04_Add_Yourself.sql",
        "05_New_Customer_With_Order.sql", "06_Update_Hire_Date.sql", "07_Raise_Category_Prices.sql",
        "08_Delete_Product.sql", "09_Delete_Category.sql",
    ];

    // Northwind as Northwind.sql makes it: 77 products, 8 categories, 9 employees, 91 customers, orders up to
    // 11077. Anything added since is set aside first.
    private const string FreshNorthwind = """
        DELETE FROM OrderDetails WHERE OrderId > 11077;
        DELETE FROM Orders WHERE OrderId > 11077;
        DELETE FROM OrderDetails WHERE ProductId > 77;
        DELETE FROM Products WHERE ProductId > 77 OR CategoryId > 8;
        DELETE FROM Categories WHERE CategoryId > 8;
        DELETE FROM EmployeeTerritories WHERE EmployeeId > 9;
        UPDATE Employees SET ReportsTo = NULL WHERE EmployeeId > 9;
        DELETE FROM Employees WHERE EmployeeId > 9;
        DELETE FROM CustomerCustomerDemo WHERE CustomerId IN
            (SELECT c.CustomerId FROM Customers AS c WHERE c.CustomerId NOT IN ('FISSA', 'PARIS') AND NOT EXISTS (SELECT 1 FROM Orders AS o WHERE o.CustomerId = c.CustomerId));
        DELETE FROM Customers WHERE CustomerId NOT IN ('FISSA', 'PARIS') AND NOT EXISTS (SELECT 1 FROM Orders AS o WHERE o.CustomerId = Customers.CustomerId);
        """;

    // Runs problems 1 to the one given, in order, then the check query, and returns the results of the last
    // problem's file followed by the check's.
    private static (List<DataTable> Problem, DataTable Check) Run(int upTo, string check)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        var results = new List<DataTable>();
        try
        {
            Execute(connection, transaction, FreshNorthwind, results);
            foreach (string file in files.Take(upTo))
            {
                results = [];
                string sql = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Queries", file));
                foreach (string batch in Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(batch)) continue;
                    try
                    {
                        Execute(connection, transaction, batch, results);
                    }
                    catch (SqlException e)
                    {
                        Assert.Fail($"{file} stopped with an error: {e.Message}");
                    }
                }
            }
            var checks = new List<DataTable>();
            Execute(connection, transaction, check, checks);
            return (results, checks[^1]);
        }
        finally
        {
            // A script's own error can roll the transaction back already (SET XACT_ABORT ON does).
            if (transaction.Connection is not null) transaction.Rollback();
        }
    }

    private static void Execute(SqlConnection connection, SqlTransaction transaction, string sql, List<DataTable> results)
    {
        using var command = new SqlCommand(sql, connection, transaction);
        using var reader = command.ExecuteReader();
        while (!reader.IsClosed && reader.FieldCount > 0)
        {
            var result = new DataTable();
            result.Load(reader);   // reads one result, then moves on to the next
            results.Add(result);
        }
    }

    private static DataRow Only(DataTable result, string message)
    {
        Assert.HasCount(1, result.Rows, message);
        return result.Rows[0];
    }

    private static int Int(DataRow row, string column) => Convert.ToInt32(row[column]);

    [TestMethod]
    public void Problem01_InsertCategory_AddsSportingGoods()
    {
        DataRow row = Only(Run(1, "SELECT Description FROM Categories WHERE CategoryName = 'Sporting Goods';").Check,
            "1: add one category named Sporting Goods.");

        StringAssert.Contains(row["Description"].ToString(), "camping", "1: give it the description from the problem.");
    }

    [TestMethod]
    public void Problem02_FindCategoryId_ShowsSportingGoodsAndItsId()
    {
        var (problem, check) = Run(2, "SELECT CategoryId FROM Categories WHERE CategoryName = 'Sporting Goods';");
        int id = Int(Only(check, "2: problem 1 adds the Sporting Goods category this one finds."), "CategoryId");

        Assert.IsNotEmpty(problem, "2: write a SELECT that finds Sporting Goods by its name.");
        DataRow row = Only(problem[^1], "2: one row: Sporting Goods.");
        Assert.IsTrue(row.ItemArray.Any(cell => cell is int value && value == id), $"2: show its CategoryId ({id} this time).");
    }

    [TestMethod]
    public void Problem03_InsertProducts_AddsFiveToSportingGoods()
    {
        DataTable products = Run(3, """
            SELECT p.ProductName, p.UnitPrice, p.UnitsInStock, p.UnitsOnOrder, p.ReorderLevel, p.SupplierId
            FROM Products AS p INNER JOIN Categories AS c ON p.CategoryId = c.CategoryId
            WHERE c.CategoryName = 'Sporting Goods';
            """).Check;

        Assert.HasCount(5, products.Rows, "3: five products in the Sporting Goods category.");
        foreach (DataRow p in products.Rows)
        {
            Assert.IsFalse(p.IsNull("UnitPrice"), $"3: {p["ProductName"]} needs a price.");
            Assert.IsTrue(Int(p, "UnitsInStock") == 20 && Int(p, "UnitsOnOrder") == 0 && Int(p, "ReorderLevel") == 10,
                $"3: {p["ProductName"]}: 20 in stock, 0 on order, a reorder level of 10.");
            Assert.IsTrue(p.IsNull("SupplierId"), $"3: leave {p["ProductName"]}'s supplier empty.");
        }
    }

    private const string Newest = """
        SELECT TOP (1) FirstName, LastName, Title, TitleOfCourtesy, BirthDate, HireDate, Address, City, Region, PostalCode, Country,
            ReportsTo, HomePhone, Extension, Notes, CAST(HireDate AS date) AS Hired, CAST(GETDATE() AS date) AS Today
        FROM Employees ORDER BY EmployeeId DESC;
        """;

    [TestMethod]
    public void Problem04_AddYourself_AddsASalesAssociateHiredToday()
    {
        DataRow you = Only(Run(4, Newest).Check, "4: you are the newest employee.");

        Assert.AreEqual("Sales Associate", you["Title"].ToString(), "4: your title is Sales Associate.");
        foreach (string column in new[] { "FirstName", "LastName", "TitleOfCourtesy", "BirthDate", "Address", "City", "Region", "PostalCode", "Country" })
        {
            Assert.IsFalse(you.IsNull(column) || string.IsNullOrWhiteSpace(you[column].ToString()), $"4: fill in your {column}.");
        }
        Assert.AreEqual(you["Today"], you["Hired"], "4: your hire date is today: GETDATE().");
        Assert.IsTrue(you.IsNull("ReportsTo") && you.IsNull("HomePhone") && you.IsNull("Extension") && you.IsNull("Notes"),
            "4: leave every other column empty.");
    }

    [TestMethod]
    public void Problem05_NewCustomerWithOrder_AddsTheCustomerAnOrderAndThreeLines()
    {
        DataTable lines = Run(5, """
            SELECT c.CompanyName, c.Address, o.EmployeeId, o.ShipName, o.ShipAddress, o.ShipCity, o.ShipCountry,
                CAST(o.OrderDate AS date) AS Ordered, CAST(GETDATE() AS date) AS Today,
                od.Quantity, od.Discount, od.UnitPrice, p.UnitPrice AS ListPrice
            FROM Customers AS c
            INNER JOIN Orders AS o ON o.CustomerId = c.CustomerId
            INNER JOIN OrderDetails AS od ON od.OrderId = o.OrderId
            INNER JOIN Products AS p ON p.ProductId = od.ProductId
            WHERE o.OrderId > 11077;
            """).Check;

        Assert.HasCount(3, lines.Rows, "5: one new order, for the new customer, with three lines.");
        foreach (DataRow line in lines.Rows)
        {
            Assert.AreEqual(1, Int(line, "EmployeeId"), "5: the order's EmployeeId is 1.");
            Assert.AreEqual(line["Today"], line["Ordered"], "5: the order is dated today.");
            Assert.IsTrue(line["ShipName"].Equals(line["CompanyName"]) && line["ShipAddress"].Equals(line["Address"]),
                "5: ship the order to the customer: their company name and address.");
            Assert.IsTrue(Int(line, "Quantity") == 2 && Convert.ToDouble(line["Discount"]) == 0, "5: each line: a quantity of 2, no discount.");
            Assert.AreEqual(line["ListPrice"], line["UnitPrice"], "5: each line has its product's list price.");
        }
    }

    [TestMethod]
    public void Problem06_UpdateHireDate_MovesItToTwoWeeksAgo()
    {
        DataRow you = Only(Run(6, Newest).Check, "6: you are the newest employee.");

        Assert.AreEqual(((DateTime)you["Today"]).AddDays(-14), (DateTime)you["Hired"], "6: your hire date is 14 days before today.");
    }

    private const string SportingPrices = """
        SELECT p.ProductName, p.UnitPrice FROM Products AS p INNER JOIN Categories AS c ON p.CategoryId = c.CategoryId
        WHERE c.CategoryName = 'Sporting Goods' ORDER BY p.ProductName;
        """;

    [TestMethod]
    public void Problem07_RaiseCategoryPrices_RaisesSportingGoodsByTenPercent()
    {
        DataTable before = Run(6, SportingPrices).Check, after = Run(7, SportingPrices).Check;

        Assert.HasCount(5, before.Rows, "7: problems 1 to 6 give Sporting Goods the five products whose prices this one raises.");
        Assert.HasCount(5, after.Rows, "7: change the prices, not the products.");
        for (int i = 0; i < before.Rows.Count; i++)
        {
            decimal was = Convert.ToDecimal(before.Rows[i]["UnitPrice"]), now = Convert.ToDecimal(after.Rows[i]["UnitPrice"]);
            Assert.IsTrue(Math.Abs(now - was * 1.10m) < 0.01m, $"7: {before.Rows[i]["ProductName"]} goes from {was:0.00} to {was * 1.10m:0.00}, 10% more.");
        }
    }

    [TestMethod]
    public void Problem08_DeleteProduct_LeavesFourSportingGoods() =>
        Assert.HasCount(4, Run(8, SportingPrices).Check.Rows, "8: delete one of the five Sporting Goods products.");

    [TestMethod]
    public void Problem09_DeleteCategory_RemovesItAndItsProducts()
    {
        Assert.HasCount(4, Run(8, SportingPrices).Check.Rows, "9: problems 1 to 8 leave Sporting Goods with four products for this one to delete.");
        DataRow counts = Only(Run(9, """
            SELECT (SELECT COUNT(*) FROM Categories WHERE CategoryName = 'Sporting Goods') AS Categories,
                (SELECT COUNT(*) FROM Products) AS Products;
            """).Check, "9");

        Assert.AreEqual(0, Int(counts, "Categories"), "9: the Sporting Goods category is gone.");
        Assert.AreEqual(77, Int(counts, "Products"), "9: its products went first, so Northwind has its 77 again.");
    }
}
