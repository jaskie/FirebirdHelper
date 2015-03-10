# A set of base classes to cope with Firebird using POCO objects.
Requires .NET 4.0, Microsoft BCL and - of course - Firebird provider.

Contains set off attributes to decorate model's POCO objects to create mappings, i.e. *TableName*, *GeneratorName*, and base classes to represent single record (*FirebirdRow*) and whole table (*FirebirdTable*). The model classes should inherit from these.

Provides automatic update and insert sql generation for rows, updating only modified fields, and automatic refresh for fields updated in database SP code.
Selects are currently required to be written manually.