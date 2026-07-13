using CodeMaster.Domain.Entities.CodeGen;
using System.Collections.Generic;

namespace CodeMaster.CodeGenerator.Tests.Fixtures;

public static class MockEntityData
{
    public static ModuleEntity GetFullFeatureEntity()
    {
        return new ModuleEntity
        {
            Id = 1,
            ProjectId = 1,
            ModuleId = 1,
            Name = "Employee",
            Description = "员工表",
            HasPrimaryKey = true,
            TableName = "business_employee",
            IsTree = false,
            IsReadOnly = false,
            HasTenant = true,
            HasDataPermission = true,
            GenerateFrontend = true,
            FrontendRoute = "employee",
            MenuIcon = "user",
            OrderNum = 1
        };
    }

    public static List<EntityField> GetFullFeatureEntityFields()
    {
        return new List<EntityField>
        {
            new EntityField
            {
                Id = 1,
                ModuleEntityId = 1,
                Name = "Name",
                Description = "姓名",
                DataType = "string",
                MaxLength = 50,
                IsRequired = true,
                ShowInList = true,
                ShowInEditForm = true,
                ShowInSearch = true,
                FormControlType = "input",
                OrderNum = 1
            },
            new EntityField
            {
                Id = 2,
                ModuleEntityId = 1,
                Name = "DepartmentId",
                Description = "部门",
                DataType = "long",
                IsRequired = true,
                ShowInList = true,
                ShowInEditForm = true,
                ShowInSearch = true,
                FormControlType = "cascader",
                IsMultiple = false,
                RelatedEntityName = "Department",
                RelatedEntityIdField = "Id",
                RelatedEntityDisplayFields = "[\"Name\"]",
                OrderNum = 2
            }
        };
    }

    public static List<OneToManyRelation> GetEmptyRelations()
    {
        return new List<OneToManyRelation>();
    }
}
