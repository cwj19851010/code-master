using CodeMaster.Agent.Contracts;
using CodeMaster.Agent.Services;

namespace CodeMaster.Agent.Tests.Services;

public class CodeMasterProjectChangeSetRulesTests
{
    [Fact]
    public void Validate_AllowsNewModuleEntitiesAndRelationInOneChangeSet()
    {
        var blueprint = CreateBlueprint();
        var proposal = new ProjectChangeSetProposal
        {
            Summary = "Create order aggregate",
            Modules =
            {
                new CreateModuleProposal
                {
                    ModuleName = "OrderManagement",
                    ModuleDescription = "Order management"
                }
            },
            Entities =
            {
                new CreateEntityProposal
                {
                    ModuleName = "OrderManagement",
                    Name = "Order",
                    Description = "Order",
                    Fields =
                    {
                        Field("OrderNo", "Order number")
                    },
                    Relations =
                    {
                        new CreateEntityRelationProposal
                        {
                            TargetEntityName = "OrderItem",
                            RelationName = "Items",
                            SourceField = "Id",
                            TargetField = "OrderId",
                            Cardinality = "OneToMany",
                            Ownership = "Owned",
                            DeleteBehavior = "Delete"
                        }
                    }
                },
                new CreateEntityProposal
                {
                    ModuleName = "OrderManagement",
                    Name = "OrderItem",
                    Description = "Order item",
                    IsChildTable = true,
                    Fields =
                    {
                        new CreateEntityFieldProposal
                        {
                            Name = "OrderId",
                            Description = "Order id",
                            DataType = "long",
                            FormControlType = "input"
                        },
                        Field("ProductName", "Product name")
                    }
                }
            }
        };

        var result = CodeMasterProjectChangeSetRules.Validate(blueprint, proposal);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.Equal(1, result.ModuleCount);
        Assert.Equal(2, result.EntityCount);
        Assert.Equal(3, result.FieldCount);
        Assert.Equal(1, result.RelationCount);
    }

    [Fact]
    public void Validate_RejectsChineseTechnicalModuleName()
    {
        var proposal = new ProjectChangeSetProposal
        {
            Summary = "Create order module",
            Modules =
            {
                new CreateModuleProposal
                {
                    ModuleName = "订单管理",
                    ModuleDescription = "订单管理"
                }
            }
        };

        var result = CodeMasterProjectChangeSetRules.Validate(CreateBlueprint(), proposal);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("ASCII PascalCase", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsChineseModuleRename()
    {
        var proposal = new ProjectChangeSetProposal
        {
            Summary = "Rename module",
            ModuleUpdates =
            {
                new UpdateModuleProposal
                {
                    ModuleId = 10,
                    ModuleName = "系统管理"
                }
            }
        };

        var result = CodeMasterProjectChangeSetRules.Validate(CreateBlueprint(), proposal);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("ASCII PascalCase", StringComparison.Ordinal));
    }

    [Fact]
    public void MapFields_AddsStandardPrimaryKeyMetadata()
    {
        var fields = CodeMasterProjectChangeSetService.MapFields(new CreateEntityProposal
        {
            Name = "Order",
            Description = "Order",
            HasPrimaryKey = true,
            Fields = { Field("OrderNo", "Order number") }
        });

        var primaryKey = Assert.Single(fields, field => field.IsPrimaryKey);
        Assert.Equal("Id", primaryKey.Name);
        Assert.Equal("long", primaryKey.DataType);
        Assert.True(primaryKey.IsSystemField);
    }

    [Fact]
    public void MapFields_NormalizesAgentDataTypeAliases()
    {
        var fields = CodeMasterProjectChangeSetService.MapFields(new CreateEntityProposal
        {
            Name = "Order",
            Description = "Order",
            HasPrimaryKey = false,
            Fields =
            {
                new CreateEntityFieldProposal
                {
                    Name = "OrderDate",
                    Description = "Order date",
                    DataType = "datetime",
                    FormControlType = "datetime"
                }
            }
        });

        var field = Assert.Single(fields);
        Assert.Equal("DateTime", field.DataType);
        Assert.Equal("datetime", field.FormControlType);
    }

    [Fact]
    public void MapOneToManyRelation_UsesParentKeyAndChildForeignKey()
    {
        var relation = new CreateEntityRelationProposal
        {
            TargetEntityName = "OrderItem",
            RelationName = "Items",
            SourceField = "Id",
            TargetField = "OrderId",
            Cardinality = "OneToMany",
            Ownership = "Owned",
            DeleteBehavior = "Delete",
            OrderNum = 10
        };

        Assert.True(CodeMasterProjectChangeSetService.IsLegacyOneToMany(relation));
        var mapped = CodeMasterProjectChangeSetService.MapOneToManyRelation(
            relation,
            new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase) { ["OrderItem"] = 200 });

        Assert.Equal("Id", mapped.MasterField);
        Assert.Equal(200, mapped.ChildEntityId);
        Assert.Equal("OrderItem", mapped.ChildEntityName);
        Assert.Equal("OrderId", mapped.ChildForeignKey);
    }

    [Fact]
    public void Validate_AllowsReferenceManyToOneForeignKeyToPrimaryKey()
    {
        var proposal = new ProjectChangeSetProposal
        {
            Summary = "Create invoice customer reference",
            Entities =
            {
                new CreateEntityProposal
                {
                    ModuleId = 10,
                    Name = "Invoice",
                    Description = "Invoice",
                    Fields =
                    {
                        new CreateEntityFieldProposal
                        {
                            Name = "CustomerId",
                            Description = "Customer id",
                            DataType = "long",
                            FormControlType = "input"
                        }
                    },
                    Relations =
                    {
                        new CreateEntityRelationProposal
                        {
                            TargetEntityId = 20,
                            RelationName = "Customer",
                            SourceField = "CustomerId",
                            TargetField = "Id",
                            Cardinality = "ManyToOne",
                            Ownership = "Reference",
                            DeleteBehavior = "Restrict"
                        }
                    }
                }
            }
        };

        var result = CodeMasterProjectChangeSetRules.Validate(CreateBlueprint(), proposal);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
    }

    [Fact]
    public void Validate_RejectsMismatchedRelationFieldTypes()
    {
        var proposal = new ProjectChangeSetProposal
        {
            Summary = "Create invalid customer reference",
            Entities =
            {
                new CreateEntityProposal
                {
                    ModuleId = 10,
                    Name = "Invoice",
                    Description = "Invoice",
                    Fields =
                    {
                        new CreateEntityFieldProposal
                        {
                            Name = "CustomerId",
                            Description = "Customer id",
                            DataType = "string",
                            FormControlType = "input"
                        }
                    },
                    Relations =
                    {
                        new CreateEntityRelationProposal
                        {
                            TargetEntityId = 20,
                            RelationName = "Customer",
                            SourceField = "CustomerId",
                            TargetField = "Id",
                            Cardinality = "ManyToOne",
                            Ownership = "Reference",
                            DeleteBehavior = "Restrict"
                        }
                    }
                }
            }
        };

        var result = CodeMasterProjectChangeSetRules.Validate(CreateBlueprint(), proposal);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("field types do not match", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_RejectsUnknownControlAndMissingRelationField()
    {
        var blueprint = CreateBlueprint();
        var proposal = new ProjectChangeSetProposal
        {
            Summary = "Invalid design",
            Entities =
            {
                new CreateEntityProposal
                {
                    ModuleId = 10,
                    Name = "Invoice",
                    Description = "Invoice",
                    Fields =
                    {
                        new CreateEntityFieldProposal
                        {
                            Name = "InvoiceNo",
                            Description = "Invoice number",
                            DataType = "string",
                            FormControlType = "not-a-control"
                        }
                    },
                    Relations =
                    {
                        new CreateEntityRelationProposal
                        {
                            TargetEntityId = 20,
                            RelationName = "Customer",
                            SourceField = "MissingCustomerId",
                            TargetField = "MissingId"
                        }
                    }
                }
            }
        };

        var result = CodeMasterProjectChangeSetRules.Validate(blueprint, proposal);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.Contains("unknown control", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, x => x.Contains("missing source field", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, x => x.Contains("missing target field", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_RejectsDuplicateExistingNames()
    {
        var blueprint = CreateBlueprint();
        var proposal = new ProjectChangeSetProposal
        {
            Summary = "Duplicate names",
            Modules =
            {
                new CreateModuleProposal { ModuleName = "System", ModuleDescription = "Duplicate" }
            },
            Entities =
            {
                new CreateEntityProposal
                {
                    ModuleId = 10,
                    Name = "Customer",
                    Description = "Duplicate",
                    Fields = { Field("Name", "Name") }
                }
            }
        };

        var result = CodeMasterProjectChangeSetRules.Validate(blueprint, proposal);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.Contains("Module 'System' already exists", StringComparison.Ordinal));
        Assert.Contains(result.Errors, x => x.Contains("Entity 'Customer' already exists", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_AllowsFieldUpdateReorderAndIncrementalGeneration()
    {
        var proposal = new ProjectChangeSetProposal
        {
            Summary = "Move customer name and make it searchable",
            GenerationMode = "Incremental",
            EntityUpdates =
            {
                new UpdateEntityProposal
                {
                    EntityId = 20,
                    UpdatedFields =
                    {
                        new UpdateEntityFieldProposal
                        {
                            FieldId = 21,
                            OrderNum = 50000,
                            ShowInSearch = true,
                            FormControlType = "input"
                        }
                    }
                }
            }
        };

        var result = CodeMasterProjectChangeSetRules.Validate(CreateBlueprint(), proposal);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.Equal(1, result.UpdatedEntityCount);
    }

    [Fact]
    public void Validate_RejectsDeletingModuleThatStillContainsEntities()
    {
        var proposal = new ProjectChangeSetProposal
        {
            Summary = "Delete system module",
            DeleteModuleIds = { 10 }
        };

        var result = CodeMasterProjectChangeSetRules.Validate(CreateBlueprint(), proposal);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.Contains("still contains entities", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_RequiresIncomingRelationToBeRemovedBeforeTargetDeletion()
    {
        var blueprint = CreateBlueprint();
        blueprint.Modules[0].Entities.Add(new AiEntityBlueprintDto
        {
            Id = 30,
            ModuleId = 10,
            Name = "Order",
            Description = "Order",
            HasPrimaryKey = true,
            Fields =
            {
                new AiFieldBlueprintDto
                {
                    Id = 31,
                    Name = "CustomerId",
                    Description = "Customer id",
                    DataType = "long",
                    FormControlType = "input"
                }
            },
            Relations =
            {
                new AiRelationBlueprintDto
                {
                    Id = 32,
                    SourceEntityId = 30,
                    TargetEntityId = 20,
                    TargetEntityName = "Customer",
                    RelationName = "Customer",
                    SourceField = "CustomerId",
                    TargetField = "Id",
                    Cardinality = "OneToOne",
                    Ownership = "Reference",
                    DeleteBehavior = "Restrict"
                }
            }
        });
        var proposal = new ProjectChangeSetProposal
        {
            Summary = "Delete customer",
            DeleteEntityIds = { 20 }
        };

        var invalid = CodeMasterProjectChangeSetRules.Validate(blueprint, proposal);
        proposal.EntityUpdates.Add(new UpdateEntityProposal
        {
            EntityId = 30,
            DeletedRelationIds = { 32 }
        });
        var valid = CodeMasterProjectChangeSetRules.Validate(blueprint, proposal);

        Assert.False(invalid.IsValid);
        Assert.Contains(invalid.Errors, x => x.Contains("still references it", StringComparison.OrdinalIgnoreCase));
        Assert.True(valid.IsValid, string.Join(Environment.NewLine, valid.Errors));
    }

    [Fact]
    public void Validate_DuplicateEntityUpdatesReturnValidationErrorInsteadOfThrowing()
    {
        var proposal = new ProjectChangeSetProposal
        {
            Summary = "Duplicate updates",
            EntityUpdates =
            {
                new UpdateEntityProposal { EntityId = 20, OrderNum = 10 },
                new UpdateEntityProposal { EntityId = 20, OrderNum = 20 }
            }
        };

        var result = CodeMasterProjectChangeSetRules.Validate(CreateBlueprint(), proposal);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.Contains("Duplicate entity update id", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ComputedFieldRequiresNumericTypeAndFormulaReference()
    {
        var proposal = new ProjectChangeSetProposal
        {
            Summary = "Create calculated invoice",
            Entities =
            {
                new CreateEntityProposal
                {
                    ModuleId = 10,
                    Name = "Invoice",
                    Description = "Invoice",
                    Fields =
                    {
                        new CreateEntityFieldProposal
                        {
                            Name = "Total",
                            Description = "Total",
                            DataType = "string",
                            FormControlType = "input",
                            FieldCategory = "Computed",
                            Formula = "100"
                        }
                    }
                }
            }
        };

        var result = CodeMasterProjectChangeSetRules.Validate(CreateBlueprint(), proposal);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("numeric", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, error => error.Contains("[FieldName]", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_RejectsDeletingFieldStillUsedByIncomingRelation()
    {
        var blueprint = CreateBlueprint();
        blueprint.Modules[0].Entities.Add(new AiEntityBlueprintDto
        {
            Id = 30,
            ModuleId = 10,
            Name = "Order",
            Description = "Order",
            Fields =
            {
                new AiFieldBlueprintDto
                {
                    Id = 31,
                    Name = "CustomerId",
                    Description = "Customer id",
                    DataType = "long",
                    FormControlType = "input"
                }
            },
            Relations =
            {
                new AiRelationBlueprintDto
                {
                    Id = 32,
                    SourceEntityId = 30,
                    TargetEntityId = 20,
                    TargetEntityName = "Customer",
                    RelationName = "CustomerName",
                    SourceField = "CustomerId",
                    TargetField = "Name",
                    Cardinality = "OneToOne",
                    Ownership = "Reference",
                    DeleteBehavior = "Restrict"
                }
            }
        });
        var proposal = new ProjectChangeSetProposal
        {
            Summary = "Remove customer name",
            EntityUpdates =
            {
                new UpdateEntityProposal
                {
                    EntityId = 20,
                    DeletedFieldIds = { 21 }
                }
            }
        };

        var result = CodeMasterProjectChangeSetRules.Validate(blueprint, proposal);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.Contains("removed or renamed target field", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ToolPayloadCodec_CompressesAndRestoresLargeChangeSetJson()
    {
        var json = "{\"entities\":[" + string.Join(',', Enumerable.Repeat("{\"name\":\"OrderItem\",\"formControlType\":\"input\",\"showInList\":true}", 400)) + "]}";

        var stored = AiToolPayloadCodec.Encode(json);
        var restored = AiToolPayloadCodec.Decode(stored);

        Assert.StartsWith("gzip:", stored, StringComparison.Ordinal);
        Assert.True(stored.Length < 7800);
        Assert.Equal(json, restored);
    }

    private static AiProjectBlueprintDto CreateBlueprint()
    {
        return new AiProjectBlueprintDto
        {
            Project = new AiProjectSummaryDto { Id = 1, ProjectName = "TestProject" },
            Controls =
            {
                new AiControlCatalogDto { ControlType = "input", PageSections = { "add", "edit", "search", "list", "detail" } }
            },
            Modules =
            {
                new AiModuleBlueprintDto
                {
                    Id = 10,
                    ModuleName = "System",
                    ModuleDescription = "System",
                    Entities =
                    {
                        new AiEntityBlueprintDto
                        {
                            Id = 20,
                            ModuleId = 10,
                            Name = "Customer",
                            Description = "Customer",
                            HasPrimaryKey = true,
                            Fields =
                            {
                                new AiFieldBlueprintDto { Id = 21, Name = "Name", Description = "Name", DataType = "string", FormControlType = "input" }
                            }
                        }
                    }
                }
            }
        };
    }

    private static CreateEntityFieldProposal Field(string name, string description)
    {
        return new CreateEntityFieldProposal
        {
            Name = name,
            Description = description,
            DataType = "string",
            FormControlType = "input"
        };
    }
}
