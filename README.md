# Planto

Simple DB seeding for entities which have foreign keys.<br>
Currently only supporting Sql Server(MsSql)

## NuGet

https://www.nuget.org/packages/Planto or<br>
`dotnet add package Planto --version 0.6.1`

## What

Seed entities in a database and automatically created related entities.

## Quick Start

Create a Planto object and use the CreateEntity function.
See below for custom data.

```csharp
string TableName = "ExampleTable";

// recommended!
var plantoRandomValues = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql, 
            options => options.SetValueGeneration(ValueGeneration.Random));

// only creates entities with default values, see Restriction part for more details
var plantoDefaultValues = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

// if pk is not int, change e.g. to string or decimal
var id = await plantoRandomValues.CreateEntity<int>(TableName);
var id = await plantoDefaultValues.CreateEntity<int>(TableName);
```

## Restrictions

- supports only tables with a single primary key

## Usage

### Schema

Set Schema if needed, else tables from all schemas are used
```csharp
new Planto(ConnectionString, DbmsType,
    options => options.SetDefaultSchema("myDbSchema"));
 ```

### Column Check Constraints

Right now the program can solve simple expressions
involving value comparisons using `=`, `<=`, `>=` and `like`<br>
To deactivate Check-Constraint value generation(currently experimental) do:

```csharp
new Planto(ConnectionString, DbmsType,
    options => options.SetColumnCheckValueGenerator(false));
 ```

**Recommended**<br>
To identify which columns require special values based on a Check, you can use planto.AnalyzeColumnChecks(tableName).
This function returns the check constraints for the specified table and its columns.
From there, you can determine which data should likely be provided manually.

### Custom data

Create a class and use the `TableName` and `ColumnName` attribute to match the database naming with your class.
If no attribute is provided the _class name_ and _property name_ is used.
Properties not explicitly provided will be auto-generated using the configuration defined in options.

```csharp
[TableName("TestTable")]
private class TestTableDb
{
    [ColumnName("name")] 
    public string? MyName { get; set; }
    
    [ColumnName("age")] 
    public int TestAge { get; set; }
}
```

You can add one or more objects to the function, and the library will automatically use the corresponding object for
the related table. This is useful if, for example, you want to create a random Customer that is linked to another
table where the generated entity requires a specific value.

```csharp
await planto.CreateEntity<int>(TableName, new TestTable { name = myName, age = age});
```

### MsSql (Sql Server)

#### General

- supports tables where id is managed by IDENTITY
  - if a PK is not IDENTITY it is important to change SetValueGeneration to Random

```csharp
new Planto(ConnectionString, DbmsType.MsSql, 
    options => options.SetValueGeneration(ValueGeneration.Random));
 ```

- add `MultipleActiveResultSets=true;` to your ConnectionString or set `options.SetMaxDegreeOfParallelism(1)`
### Postgres (NpgSql)

- Coming soon

## TODOs
- cache columnInfo for tables
- improve multiple unique constraints for a table (mssql)
- Support special PKs for MsSql, like multiple PKs
- Logs
- comment functions
- refactor
- DeSerialize
- NpgSql