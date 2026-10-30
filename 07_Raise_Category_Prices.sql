-- 7. Write a script that raises the price of every product in a category
--    by a percentage. The category's name and the percentage are in the
--    variables below; the rest of the script must work for any category.
--
-- Expected: (5 rows affected) for Sporting Goods

DECLARE @CategoryName nvarchar(15) = 'Sporting Goods';
DECLARE @PercentIncrease decimal(5, 2) = 10;

