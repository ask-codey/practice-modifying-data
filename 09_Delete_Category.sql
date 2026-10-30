-- 9. Delete the Sporting Goods category.
--
--    First try just this, and read the error:
--      DELETE FROM Categories WHERE CategoryName = 'Sporting Goods';
--    Why does it fail? What has to happen first?
--
--    Then write a script that does every step needed, in a transaction.
--    Its only input is the category's name, in the variable below.
--
-- Expected: (4 rows affected) for the products, then (1 row affected)

DECLARE @CategoryName nvarchar(15) = 'Sporting Goods';

