# Planto

Simple DB seeding for entities which have foreign keys.<br>
Currently only supporting Sql Server(MsSql)

## NuGet

https://www.nuget.org/packages/Planto or<br>
`dotnet add package Planto --version 0.4.0`

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

// only creates entities with default values, see Attention part for more details
var plantoDefaultValues = new Planto(_msSqlContainer.GetConnectionString(), DbmsType.MsSql);

// if pk is not int, change e.g. to string or decimal
var id = await plantoRandomValues.CreateEntity<int>(TableName);
var id = await plantoDefaultValues.CreateEntity<int>(TableName);
```

## Restrictions

- supports only tables with a single primary key

## Usage

### MsSql (Sql Server)

#### General
- supports tables where id is managed by IDENTITY
  - if a PK is not IDENTITY it is important to change SetValueGeneration to Random

```csharp
new Planto(ConnectionString, DbmsType.MsSql, 
    options => options.SetValueGeneration(ValueGeneration.Random));
 ```

- add `MultipleActiveResultSets=true;` to your ConnectionString or set `options.SetMaxDegreeOfParallelism(1)`

#### Use custom data

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

Then you can just add a object to the function

```csharp
await planto.CreateEntity<int>(TableName, new TestTable { name = myName, age = age});
```

### Postgres (NpgSql)

- Coming soon

## TODOs
- add test and readme for table schema
- handle computed columns
- handle simple check constraints
- change custom data to list
- cache columnInfo for tables
- Support special PKs for MsSql, like multiple PKs
- improve multiple unique constraints for a table (mssql)
- Logs
- comment functions
- DeSerialize
- NpgSql
- set values for indirect table entities
