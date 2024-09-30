# Planto

Simple DB seeding for entities which have foreign keys.<br>
Currently only supporting Sql Server(MsSql)

## NuGet

https://www.nuget.org/packages/Planto or<br>
`dotnet add package Planto --version 0.2.1`

## What

Seed entities in a database and automatically created related entities.

## How to use

Create a Planto object and use the CreateEntity function

```c#
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

- add `MultipleActiveResultSets=true;` to connection string or set `options.SetMaxDegreeOfParallelism(1)`
- supports tables where id is managed by IDENTITY
  - if a PK is not IDENTITY it is important to change SetValueGeneration to Random
  ```c#
  new Planto(ConnectionString, DbmsType.MsSql, 
            options => options.SetValueGeneration(ValueGeneration.Random));
  ```
- A table with a foreign key referencing multiple tables throws an error.

### Postgres (NpgSql)

- Coming soon

## TODOs
- allow user to set values for main entity
- cache columnInfo for tables
- Support special PKs for MsSql, like multiple PKs
- Logs
- comment functions
- DeSerialize
- NpgSql
