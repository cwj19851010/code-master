using CodeMaster.Agent.Contracts;
using Microsoft.Extensions.AI;

namespace CodeMaster.Agent.Tools;

public static class CodeMasterAgentToolCatalog
{
    public const string GetProjectStructure = "get_project_structure";
    public const string GetEntityBlueprint = "get_entity_blueprint";
    public const string GetUiPageBlueprint = "get_ui_page_blueprint";
    public const string ProposeProjectChangeSet = "propose_project_change_set";
    public const string ProposeUiPageEnhancement = "propose_ui_page_enhancement";

    public static IList<AITool> Create(
        bool includeReadTools,
        Func<Task<string>> getProjectStructure,
        Func<EntityBlueprintQuery, Task<string>> getEntityBlueprint,
        Func<UiPageBlueprintQuery, Task<string>> getUiPageBlueprint,
        bool includeChangeTools,
        Func<ProjectChangeSetProposal, Task<string>> proposeProjectChangeSet,
        Func<UiPageEnhancementProposal, Task<string>> proposeUiPageEnhancement)
    {
        var tools = new List<AITool>();
        if (includeReadTools)
        {
            tools.Add(AIFunctionFactory.Create(
                getProjectStructure,
                name: GetProjectStructure,
                description: "Read the complete CodeMaster blueprint for the project bound to this conversation, including modules, entities, every field option, relations, generation state, and the available control/template catalog."));

            tools.Add(AIFunctionFactory.Create(
                getEntityBlueprint,
                name: GetEntityBlueprint,
                description: "Read one entity in full detail by entityId or entityName from the project bound to this conversation."));

            tools.Add(AIFunctionFactory.Create(
                getUiPageBlueprint,
                name: GetUiPageBlueprint,
                description: "Read the stable generated node identifiers for one entity page. Use this before proposing page layout, grouping, grid, visual property, movement, or compatible node-type changes."));
        }

        if (includeChangeTools)
        {
            tools.Add(AIFunctionFactory.Create(
                proposeProjectChangeSet,
                name: ProposeProjectChangeSet,
                description: "Validate and create one approval card for a complete CodeMaster project change set. It can create, edit, reorder, and delete modules, entities, fields, and relations, then request full or incremental generation through the existing Web/Tauri execution bridge."));

            tools.Add(AIFunctionFactory.Create(
                proposeUiPageEnhancement,
                name: ProposeUiPageEnhancement,
                description: "Create an approval card for a controlled visual redesign. Scaffold targets redesign Login or Dashboard. EntityPage targets index/add/edit/detail and uses stable genId-anchored SetTag, SetProp, RemoveProp, SetGrid, Move, and Group operations that are replayed after future code generation."));
        }

        return tools;
    }
}
