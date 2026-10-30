-- 5. Write a script that adds a new customer, and an order for them
--    with three products.
--
--    The variables at the top hold everything that changes from customer
--    to customer. The rest of the script must not type any other values
--    or ids: look up each product by its name, and use its list price.
--
--    The order:
--      - CustomerId: the new customer's
--      - EmployeeId: 1
--      - OrderDate: today
--      - ShipName, ShipAddress, ShipCity, ShipRegion, ShipPostalCode
--        and ShipCountry: the customer's company name and address
--    Each order line: the order, the product, the product's UnitPrice,
--    a quantity of 2, and a discount of 0.
--
--    Do it all in a transaction, so the customer, the order and its
--    lines are saved together, or not at all.
--
-- Expected: (1 row affected) for the customer and for the order,
--           then (3 rows affected) for the order lines.

DECLARE @CustomerId nchar(5) = 'TRAIL';
DECLARE @CompanyName nvarchar(40) = 'Trailhead Outfitters';
DECLARE @ContactName nvarchar(30) = 'Dana Brooks';
DECLARE @Address nvarchar(60) = '12 Summit Road';
DECLARE @City nvarchar(15) = 'Boulder';
DECLARE @Region nvarchar(15) = 'CO';
DECLARE @PostalCode nvarchar(10) = '80302';
DECLARE @Country nvarchar(15) = 'USA';
DECLARE @Product1 nvarchar(40) = 'Chai';
DECLARE @Product2 nvarchar(40) = 'Chang';
DECLARE @Product3 nvarchar(40) = 'Aniseed Syrup';

