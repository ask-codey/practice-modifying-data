# Exercise: Modifying Data

Exercise SQL for the lesson **Exercise: Modifying Data** in the C# Full-Stack Bootcamp on [ask-codey.com](https://www.ask-codey.com).

Nine problems that add, change and remove data in the **Northwind** database. Each file holds a problem as a comment: write your SQL under it. The problems build on each other, so do them in order: the products you add in problem 3 are the ones you change and delete later.

## Getting started

1. Start fresh: run `Northwind.sql` from the course's sample databases, so the examples you tried in the lessons (including their Sporting Goods category) are gone.
2. Open SQL Server Management Studio, and connect to `localhost`.
3. Choose **File > Open > File...** and open `01_Insert_Category.sql`.
4. Choose **Northwind** in the database drop-down.
5. Write your SQL under the problem, select the lines you want to run, and press **F5**. In problems 5, 7 and 9, include the `DECLARE` lines at the top in your selection: a variable exists only in the run that declares it.

Each problem says what to expect, such as `(1 row affected)`. After each change, run a `SELECT` to see that the data is what you meant.

## Check your answers

`ModifyingExercise.Tests` runs your files against your Northwind database, in order, and checks the data they leave behind. Nothing is changed: everything runs inside one transaction that is rolled back. The tests start from Northwind as `Northwind.sql` makes it (the rows you added in SSMS are set aside inside the transaction, and come back afterwards), so you can run them whenever you like. It needs the .NET SDK, which you installed in the first week. Open a terminal in this folder and run:

```bash
dotnet test ModifyingExercise.Tests
```

At first every test fails: that is expected. Each problem you solve turns its test green, and a failing test's message says what it looked for. The problems build on each other, so the test for problem 7 runs problems 1 to 7. The tests use the Northwind database on `localhost`; to use another one, set the `NORTHWIND_CONNECTION` environment variable to its connection string.

## Starting over

If something goes wrong, run `Northwind.sql` from the course's sample databases again. It creates Northwind fresh, with its original data. Then start again from problem 1.

## Hand it in

When it works, push your solution to a public repo of your own on GitHub, then paste that repo's address into **Submit your exercise** on the lesson page at [ask-codey.com](https://www.ask-codey.com). That's what completes the lesson. The lesson page shows the Git commands, step by step.
