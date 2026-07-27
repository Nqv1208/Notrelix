using System.Reflection;
using FluentAssertions;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Domain.Tests.Automation;

public class ExperimentalRuntimeIsolationTests
{
    private static readonly Type TriggerEntityType = typeof(Notrelix.Domain.Automation.Triggers.AutomationTrigger);
    private static readonly Type TriggerConfigType = typeof(Notrelix.Domain.Automation.Triggers.TriggerConfig);
    private static readonly Type TriggerEnumType = typeof(Notrelix.Domain.Automation.Triggers.AutomationTriggerType);

    private static readonly Type ActionType = typeof(Notrelix.Domain.Automation.Actions.AutomationAction);
    private static readonly Type ActionConfigType = typeof(Notrelix.Domain.Automation.Actions.ActionConfig);
    private static readonly Type ActionEnumType = typeof(Notrelix.Domain.Automation.Actions.AutomationActionType);

    private static readonly Type ConditionType = typeof(Notrelix.Domain.Automation.Conditions.AutomationCondition);
    private static readonly Type ConditionConfigType = typeof(Notrelix.Domain.Automation.Conditions.ConditionConfig);
    private static readonly Type ConditionEnumType = typeof(Notrelix.Domain.Automation.Conditions.AutomationConditionType);

    private static readonly Type[] ExperimentalTypes =
    [
        TriggerEntityType, TriggerConfigType, TriggerEnumType,
        ActionType, ActionConfigType, ActionEnumType,
        ConditionType, ConditionConfigType, ConditionEnumType
    ];

    [Fact]
    public void ExperimentalTypes_ShouldNotReferenceFrozenDefinitionTypes()
    {
        var frozenDefinitionTypes = new[]
        {
            typeof(AutomationTriggerDefinition),
            typeof(AutomationActionDefinition),
            typeof(AutomationConditionDefinition),
            typeof(AutomationConfiguration)
        };

        foreach (var experimentalType in ExperimentalTypes)
        {
            if (experimentalType.IsEnum) continue;

            var referencedTypes = GetReferencedTypes(experimentalType);
            foreach (var frozenType in frozenDefinitionTypes)
            {
                referencedTypes.Should().NotContain(frozenType,
                    $"{experimentalType.Name} should not reference frozen type {frozenType.Name}");
            }
        }
    }

    [Fact]
    public void ExperimentalEntityTypes_ShouldNotReferenceFrozenValidatorTypes()
    {
        var validatorTypes = new[]
        {
            typeof(AutomationRuleValidator),
            typeof(AutomationTriggerValidator),
            typeof(AutomationActionValidator)
        };

        foreach (var experimentalType in ExperimentalTypes)
        {
            if (experimentalType.IsEnum) continue;

            var referencedTypes = GetReferencedTypes(experimentalType);
            foreach (var validatorType in validatorTypes)
            {
                referencedTypes.Should().NotContain(validatorType,
                    $"{experimentalType.Name} should not reference frozen validator {validatorType.Name}");
            }
        }
    }

    [Fact]
    public void ExperimentalTypes_ShouldAllBeInAutomationNamespace()
    {
        foreach (var type in ExperimentalTypes)
        {
            type.Namespace.Should().StartWith("Notrelix.Domain.Automation.",
                $"{type.Name} should remain in the Automation namespace");
        }
    }

    private static HashSet<Type> GetReferencedTypes(Type type)
    {
        var types = new HashSet<Type>();

        if (type.BaseType != null && type.BaseType != typeof(object) && type.BaseType != typeof(ValueObject) && type.BaseType != typeof(Entity))
            types.Add(type.BaseType);

        foreach (var iface in type.GetInterfaces())
            types.Add(iface);

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            types.Add(prop.PropertyType);

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            types.Add(field.FieldType);

        foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            foreach (var param in ctor.GetParameters())
                types.Add(param.ParameterType);
        }

        return types;
    }
}
