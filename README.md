# A set of base classes to cope with Firebird using simple objects (database-first).
Requires .NET 4.0, Microsoft BCL and - of course - Firebird provider.

Contains set off attributes to decorate model's objects to create mappings, i.e. *TableName*, *GeneratorName*, and simple base classes to represent single record (*FirebirdRow*) and whole table (*FirebirdTable*) of rows. The model classes should inherit from these. Table succesor should implement selecting required data from table as required for model.
Row base class provides automatic update and insert sql generation, updating only modified fields, and automatic refresh for fields updated in database SP code. All you need in this class is to define table fields and map them to columns through ColumnAttribute.
Because of database-first approach, there is posible to execute directly any SQL statement. Also selects are required to be written manually (IQueryalble isn't implemented).
